using System.Numerics;

namespace OneGame.Pages;

/// <summary>
/// Fills a vector path into an 8-bit coverage bitmap on the CPU: curves are flattened, each pixel row is sampled at eight sub-scanlines with
/// exact horizontal coverage, and the fill rule (non-zero or even-odd) decides which spans count. Used for path fills and path clips.
/// </summary>
internal sealed class PathRaster
{
    private const int Sub = 8;
    private const float Tolerance = 0.1f;
    private const int MaxCrossings = 1 << 22;

    private static readonly Comparer<(int Key, float X, int Dir)> Order = Comparer<(int Key, float X, int Dir)>.Create(static (a, b) => a.Key != b.Key ? a.Key.CompareTo(b.Key) : a.X.CompareTo(b.X));

    private float[] _segs = new float[256 * 4];
    private int _segCount;
    private (int Key, float X, int Dir)[] _cross = new (int, float, int)[1024];
    private int _crossCount;
    private float[] _acc = new float[256];
    private float[] _diff = new float[258];
    private float _cx;
    private float _cy;
    private float _sx;
    private float _sy;
    private bool _open;

    /// <summary>Rasterizes the path, mapped by <paramref name="matrix"/>, into <paramref name="output"/> (width × height bytes) for the pixel rectangle at (x0, y0).</summary>
    /// <returns>False when the path is too complex to rasterize (its crossings exceed the limit).</returns>
    public bool Fill(ReadOnlySpan<byte> verbs, ReadOnlySpan<float> coords, in Matrix3x2 matrix, bool evenOdd, int x0, int y0, int width, int height, Span<byte> output)
    {
        output[..(width * height)].Clear();
        Flatten(verbs, coords, matrix, x0, y0);
        if (_segCount == 0)
        {
            return true;
        }

        if (_acc.Length < width)
        {
            _acc = new float[width];
            _diff = new float[width + 2];
        }

        // Crossings of every segment with every sub-scanline it spans, keyed by sub-scanline.
        _crossCount = 0;
        var total = height * Sub;
        for (var s = 0; s < _segCount; s++)
        {
            var ax = _segs[s * 4];
            var ay = _segs[(s * 4) + 1];
            var bx = _segs[(s * 4) + 2];
            var by = _segs[(s * 4) + 3];
            if (ay == by)
            {
                continue;
            }

            var dir = 1;
            if (ay > by)
            {
                (ax, bx) = (bx, ax);
                (ay, by) = (by, ay);
                dir = -1;
            }

            // Sub-scanline n samples at y = (n + 0.5) / Sub.
            var n0 = Math.Max(0, (int)MathF.Ceiling((ay * Sub) - 0.5f));
            var n1 = Math.Min(total - 1, (int)MathF.Ceiling((by * Sub) - 0.5f) - 1);
            var slope = (bx - ax) / (by - ay);
            for (var n = n0; n <= n1; n++)
            {
                var y = (n + 0.5f) / Sub;
                if (_crossCount == _cross.Length)
                {
                    if (_crossCount >= MaxCrossings)
                    {
                        return false;
                    }

                    Array.Resize(ref _cross, _cross.Length * 2);
                }

                _cross[_crossCount++] = (n, ax + ((y - ay) * slope), dir);
            }
        }

        Array.Sort(_cross, 0, _crossCount, Order);
        var i = 0;
        for (var row = 0; row < height; row++)
        {
            Array.Clear(_acc, 0, width);
            Array.Clear(_diff, 0, width + 2);
            for (var sub = 0; sub < Sub; sub++)
            {
                var key = (row * Sub) + sub;
                while (i < _crossCount && _cross[i].Key < key)
                {
                    i++;
                }

                var winding = 0;
                var inside = false;
                var start = 0f;
                while (i < _crossCount && _cross[i].Key == key)
                {
                    var c = _cross[i++];
                    var was = evenOdd ? inside : winding != 0;
                    winding += c.Dir;
                    inside = !inside;
                    var now = evenOdd ? inside : winding != 0;
                    if (!was && now)
                    {
                        start = c.X;
                    }
                    else if (was && !now)
                    {
                        AddSpan(start, c.X, width);
                    }
                }
            }

            var run = 0f;
            var o = row * width;
            for (var x = 0; x < width; x++)
            {
                run += _diff[x];
                var v = (_acc[x] + run) * (1f / Sub);
                output[o + x] = (byte)Math.Clamp((int)((v * 255f) + 0.5f), 0, 255);
            }
        }

        return true;
    }

    private void AddSpan(float xa, float xb, int width)
    {
        xa = Math.Clamp(xa, 0f, width);
        xb = Math.Clamp(xb, 0f, width);
        if (xb <= xa)
        {
            return;
        }

        var ia = (int)xa;
        var ib = (int)xb;
        if (ia >= width)
        {
            return;
        }

        if (ia == ib)
        {
            _acc[ia] += xb - xa;
            return;
        }

        _acc[ia] += ia + 1 - xa;
        if (ib - ia > 1)
        {
            _diff[ia + 1] += 1f;
            _diff[ib] -= 1f;
        }

        if (ib < width)
        {
            _acc[ib] += xb - ib;
        }
    }

    private void Flatten(ReadOnlySpan<byte> verbs, ReadOnlySpan<float> coords, in Matrix3x2 m, int x0, int y0)
    {
        _segCount = 0;
        _open = false;
        var c = 0;
        var offset = new Vector2(-x0, -y0);
        for (var v = 0; v < verbs.Length; v++)
        {
            switch (verbs[v])
            {
                case 0:
                    CloseFigure();
                    var p = Vector2.Transform(new Vector2(coords[c], coords[c + 1]), m) + offset;
                    c += 2;
                    (_sx, _sy) = (p.X, p.Y);
                    (_cx, _cy) = (p.X, p.Y);
                    _open = true;
                    break;
                case 1:
                    var l = Vector2.Transform(new Vector2(coords[c], coords[c + 1]), m) + offset;
                    c += 2;
                    Line(l.X, l.Y);
                    break;
                case 2:
                    var q1 = Vector2.Transform(new Vector2(coords[c], coords[c + 1]), m) + offset;
                    var q2 = Vector2.Transform(new Vector2(coords[c + 2], coords[c + 3]), m) + offset;
                    c += 4;
                    Quad(q1, q2);
                    break;
                case 3:
                    var k1 = Vector2.Transform(new Vector2(coords[c], coords[c + 1]), m) + offset;
                    var k2 = Vector2.Transform(new Vector2(coords[c + 2], coords[c + 3]), m) + offset;
                    var k3 = Vector2.Transform(new Vector2(coords[c + 4], coords[c + 5]), m) + offset;
                    c += 6;
                    Cubic(k1, k2, k3);
                    break;
                default:
                    CloseFigure();
                    break;
            }
        }

        CloseFigure();
    }

    private void CloseFigure()
    {
        if (_open && (_cx != _sx || _cy != _sy))
        {
            Line(_sx, _sy);
        }

        _open = false;
    }

    private void Line(float x, float y)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y))
        {
            return;
        }

        if (_segCount * 4 + 4 > _segs.Length)
        {
            Array.Resize(ref _segs, _segs.Length * 2);
        }

        var o = _segCount * 4;
        _segs[o] = _cx;
        _segs[o + 1] = _cy;
        _segs[o + 2] = x;
        _segs[o + 3] = y;
        _segCount++;
        (_cx, _cy) = (x, y);
    }

    private void Quad(Vector2 c, Vector2 e)
    {
        var p0 = new Vector2(_cx, _cy);
        var dd = (p0 - (2 * c) + e).Length();
        var n = Math.Clamp((int)MathF.Ceiling(MathF.Sqrt(dd / (4 * Tolerance))), 1, 64);
        for (var i = 1; i <= n; i++)
        {
            var t = (float)i / n;
            var u = 1 - t;
            var p = (u * u * p0) + (2 * u * t * c) + (t * t * e);
            Line(p.X, p.Y);
        }
    }

    private void Cubic(Vector2 c1, Vector2 c2, Vector2 e)
    {
        var p0 = new Vector2(_cx, _cy);
        var dd = MathF.Max((p0 - (2 * c1) + c2).Length(), (c1 - (2 * c2) + e).Length());
        var n = Math.Clamp((int)MathF.Ceiling(MathF.Sqrt(0.75f * dd / Tolerance)), 1, 96);
        for (var i = 1; i <= n; i++)
        {
            var t = (float)i / n;
            var u = 1 - t;
            var p = (u * u * u * p0) + (3 * u * u * t * c1) + (3 * u * t * t * c2) + (t * t * t * e);
            Line(p.X, p.Y);
        }
    }
}
