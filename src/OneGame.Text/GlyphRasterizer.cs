using OneFont;

namespace OneGame.Text;

/// <summary>
/// Turns a glyph outline into an 8-bit coverage bitmap. Coverage is the exact area of each pixel inside the outline: every edge adds its
/// signed area and cover to the cells it crosses, and a running sum along each row gives the winding-weighted area of every pixel.
/// Where parts of an outline overlap with the same direction the sum is clamped to full coverage.
/// </summary>
internal sealed class GlyphRasterizer : IOutlineSink
{
    private const float Tolerance = 0.1f;
    private float[] _acc = [];
    private int _w;
    private int _h;
    private int _stride;
    private float _ox;
    private float _oy;
    private float _scale;
    private float _px;
    private float _py;
    private float _sx;
    private float _sy;
    private bool _open;

    /// <summary>The pixel bounds of a glyph at a scale and horizontal subpixel shift: left and top of the bitmap relative to the pen, and its size.</summary>
    public static bool Bounds(GlyphOutline outline, float scale, float shiftX, out int left, out int top, out int width, out int height)
    {
        left = top = width = height = 0;
        if (outline.IsEmpty)
        {
            return true;
        }

        var (minX, minY, maxX, maxY) = outline.TightBox();
        var x0 = MathF.Floor((minX * scale) + shiftX);
        var x1 = MathF.Ceiling((maxX * scale) + shiftX);
        var y0 = MathF.Floor(-maxY * scale);
        var y1 = MathF.Ceiling(-minY * scale);
        if (!float.IsFinite(x0) || !float.IsFinite(x1) || !float.IsFinite(y0) || !float.IsFinite(y1) || x1 - x0 > 1 << 14 || y1 - y0 > 1 << 14)
        {
            width = height = int.MaxValue;
            return false;
        }

        left = (int)x0;
        top = (int)y0;
        width = Math.Max(1, (int)(x1 - x0));
        height = Math.Max(1, (int)(y1 - y0));
        return true;
    }

    /// <summary>Draws the outline into <paramref name="coverage"/> (<paramref name="width"/> × <paramref name="height"/>, one byte per pixel).</summary>
    public void Rasterize(GlyphOutline outline, float scale, float shiftX, int left, int top, int width, int height, Span<byte> coverage)
    {
        _w = width;
        _h = height;
        _stride = width + 2;
        var need = _stride * height;
        if (_acc.Length < need)
        {
            _acc = new float[need];
        }
        else
        {
            Array.Clear(_acc, 0, need);
        }

        _scale = scale;
        _ox = shiftX - left;
        _oy = -top;
        _open = false;
        outline.Write(this);
        CloseFigure();

        for (var y = 0; y < height; y++)
        {
            var sum = 0f;
            var row = y * _stride;
            for (var x = 0; x < width; x++)
            {
                sum += _acc[row + x];
                coverage[(y * width) + x] = (byte)(MathF.Min(MathF.Abs(sum), 1f) * 255f + 0.5f);
            }
        }
    }

    void IOutlineSink.MoveTo(float x, float y)
    {
        CloseFigure();
        (_sx, _sy) = Map(x, y);
        (_px, _py) = (_sx, _sy);
        _open = true;
    }

    void IOutlineSink.LineTo(float x, float y)
    {
        var (nx, ny) = Map(x, y);
        Line(_px, _py, nx, ny);
        (_px, _py) = (nx, ny);
    }

    void IOutlineSink.QuadTo(float cx, float cy, float x, float y)
    {
        var (c1x, c1y) = Map(cx, cy);
        var (ex, ey) = Map(x, y);
        var ddx = _px - (2 * c1x) + ex;
        var ddy = _py - (2 * c1y) + ey;
        var n = Math.Clamp((int)MathF.Ceiling(MathF.Sqrt(MathF.Sqrt((ddx * ddx) + (ddy * ddy)) / (4 * Tolerance))), 1, 64);
        var p0x = _px;
        var p0y = _py;
        for (var i = 1; i <= n; i++)
        {
            var t = (float)i / n;
            var u = 1 - t;
            var qx = (u * u * p0x) + (2 * u * t * c1x) + (t * t * ex);
            var qy = (u * u * p0y) + (2 * u * t * c1y) + (t * t * ey);
            Line(_px, _py, qx, qy);
            (_px, _py) = (qx, qy);
        }
    }

    void IOutlineSink.CubicTo(float c1x, float c1y, float c2x, float c2y, float x, float y)
    {
        var (a, b) = Map(c1x, c1y);
        var (c, d) = Map(c2x, c2y);
        var (ex, ey) = Map(x, y);
        var d1x = _px - (2 * a) + c;
        var d1y = _py - (2 * b) + d;
        var d2x = a - (2 * c) + ex;
        var d2y = b - (2 * d) + ey;
        var dd = MathF.Max(MathF.Sqrt((d1x * d1x) + (d1y * d1y)), MathF.Sqrt((d2x * d2x) + (d2y * d2y)));
        var n = Math.Clamp((int)MathF.Ceiling(MathF.Sqrt(0.75f * dd / Tolerance)), 1, 64);
        var p0x = _px;
        var p0y = _py;
        for (var i = 1; i <= n; i++)
        {
            var t = (float)i / n;
            var u = 1 - t;
            var qx = (u * u * u * p0x) + (3 * u * u * t * a) + (3 * u * t * t * c) + (t * t * t * ex);
            var qy = (u * u * u * p0y) + (3 * u * u * t * b) + (3 * u * t * t * d) + (t * t * t * ey);
            Line(_px, _py, qx, qy);
            (_px, _py) = (qx, qy);
        }
    }

    void IOutlineSink.Close() => CloseFigure();

    private (float X, float Y) Map(float x, float y) => ((x * _scale) + _ox, (-y * _scale) + _oy);

    private void CloseFigure()
    {
        if (_open)
        {
            Line(_px, _py, _sx, _sy);
            (_px, _py) = (_sx, _sy);
            _open = false;
        }
    }

    // Adds one edge. For each pixel row it crosses, the edge's cover (dy, signed) is spread over the cells it passes through in
    // proportion to how much of each cell lies to its right, so the row's running sum is the area to the left of the edge.
    private void Line(float x0, float y0, float x1, float y1)
    {
        if (y0 == y1)
        {
            return;
        }

        float dir = 1;
        if (y0 > y1)
        {
            (x0, y0, x1, y1) = (x1, y1, x0, y0);
            dir = -1;
        }

        var dxdy = (x1 - x0) / (y1 - y0);
        var yStart = MathF.Max(y0, 0);
        var yEnd = MathF.Min(y1, _h);
        if (yStart >= yEnd)
        {
            return;
        }

        var x = x0 + ((yStart - y0) * dxdy);
        for (var y = (int)yStart; y < (int)MathF.Ceiling(yEnd); y++)
        {
            var ya = MathF.Max(y, yStart);
            var yb = MathF.Min(y + 1, yEnd);
            var dy = yb - ya;
            var xNext = x + (dxdy * dy);
            var d = dy * dir;
            var xa = MathF.Min(x, xNext);
            var xb = MathF.Max(x, xNext);
            x = xNext;
            xa = Math.Clamp(xa, 0, _w);
            xb = Math.Clamp(xb, 0, _w);
            var row = y * _stride;
            var ix0 = (int)xa;
            var ix1 = (int)MathF.Ceiling(xb);
            if (ix1 <= ix0 + 1)
            {
                var mid = (0.5f * (xa + xb)) - ix0;
                _acc[row + ix0] += d - (d * mid);
                _acc[row + ix0 + 1] += d * mid;
            }
            else
            {
                var s = 1 / (xb - xa);
                var f0 = xa - ix0;
                var a0 = 0.5f * s * (1 - f0) * (1 - f0);
                var f1 = xb - ix1 + 1;
                var am = 0.5f * s * f1 * f1;
                _acc[row + ix0] += d * a0;
                if (ix1 == ix0 + 2)
                {
                    _acc[row + ix0 + 1] += d * (1 - a0 - am);
                }
                else
                {
                    var a1 = s * (1.5f - f0);
                    _acc[row + ix0 + 1] += d * (a1 - a0);
                    for (var xi = ix0 + 2; xi < ix1 - 1; xi++)
                    {
                        _acc[row + xi] += d * s;
                    }

                    var a2 = a1 + ((ix1 - ix0 - 3) * s);
                    _acc[row + ix1 - 1] += d * (1 - a2 - am);
                }

                _acc[row + ix1] += d * am;
            }
        }
    }
}
