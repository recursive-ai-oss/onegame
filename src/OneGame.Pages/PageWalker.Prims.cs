using System.Numerics;
using OneGame.Atlas;
using OneGame.Gpu;
using OneGame.Text;
using OneRender.Painting;

namespace OneGame.Pages;

internal sealed partial class PageWalker
{
    private const float DarkFactor = 2f / 3f;

    private static uint Pack(in PaintColor c, float alpha = 1f)
    {
        var r = PaintColor.ToByte(c.R);
        var g = PaintColor.ToByte(c.G);
        var b = PaintColor.ToByte(c.B);
        var a = PaintColor.ToByte(c.A * alpha);
        return r | ((uint)g << 8) | ((uint)b << 16) | ((uint)a << 24);
    }

    private static void SetRadii(ref PageInstance i, in CornerRadii r)
    {
        i.Radii = new Vector4(r.TopLeftX, r.TopRightX, r.BottomRightX, r.BottomLeftX);
        i.RadiiY = new Vector4(r.TopLeftY, r.TopRightY, r.BottomRightY, r.BottomLeftY);
    }

    private float Pad => 1.5f / _scale;

    // ---- fills ----

    private void FillBox(in DisplayItem it)
    {
        var rect = it.Rect;
        if (rect.IsEmpty)
        {
            return;
        }

        var paint = it.Paint;
        if (paint.Kind == PaintKind.Solid && paint.Color.IsTransparent)
        {
            return;
        }

        var idx = BeginPrim(0, rect, Pad, out var x0, out var y0, out var x1, out var y1);
        if (idx < 0)
        {
            return;
        }

        ref var inst = ref _plan.Instances[idx];
        if (it.Kind == ItemKind.FillRoundedRect)
        {
            SetRadii(ref inst, it.Radii);
        }

        GpuBindGroup group = _renderer.BlankGroup;
        if (paint.Kind == PaintKind.Solid)
        {
            inst.C0 = Pack(paint.Color, _alpha);
        }
        else if (!SetGradient(ref inst, it.Gradient, out group))
        {
            // Degenerate or over the table's size: the last stop's colour everywhere.
            var g = it.Gradient;
            inst.Sub = 0;
            inst.C0 = g.StopCount > 0 ? Pack(g.GetStop(g.StopCount - 1).Color, _alpha) : 0;
            group = _renderer.BlankGroup;
        }

        Commit(idx, group, x0, y0, x1, y1);
    }

    /// <summary>Fills in the gradient fields of an instance. False when the gradient is degenerate or the ramp table is full.</summary>
    private bool SetGradient(ref PageInstance inst, in GradientView g, out GpuBindGroup group)
    {
        group = _renderer.Ramps.Group;
        if (g.StopCount == 0)
        {
            return false;
        }

        switch (g.Kind)
        {
            case PaintKind.Linear:
                var dx = g.X1 - g.X0;
                var dy = g.Y1 - g.Y0;
                if (dx * dx + dy * dy < 1e-12f)
                {
                    return false;
                }

                inst.Sub = 1;
                inst.Grad = new Vector4(g.X0, g.Y0, g.X1, g.Y1);
                break;
            case PaintKind.Radial:
                if (!(g.X1 > 0f) || !(g.Y1 > 0f))
                {
                    return false;
                }

                inst.Sub = 2;
                inst.Grad = new Vector4(g.X0, g.Y0, g.X1, g.Y1);
                break;
            default:
                inst.Sub = 3;
                inst.Grad = new Vector4(g.X0, g.Y0, g.FromAngle, 0);
                break;
        }

        if (!_renderer.Ramps.TryRow(g, out var v, out var first, out var last) || (g.Repeating && !(last > first)))
        {
            _plan.Dropped += _renderer.Ramps.RowsInUse >= GradientRamps.Rows ? 1 : 0;
            return false;
        }

        inst.Uv = new Vector4(v, first, last, g.Repeating ? 1f : 0f);
        return true;
    }

    private void FillPath(in DisplayItem it)
    {
        var paint = it.Paint;
        if (paint.Kind == PaintKind.Solid && paint.Color.IsTransparent)
        {
            return;
        }

        var box = DeviceBox(it.LocalBounds, _ctm);
        var cx0 = Math.Max(box.X0, Math.Max(_clip.X0, 0));
        var cy0 = Math.Max(box.Y0, Math.Max(_clip.Y0, 0));
        var cx1 = Math.Min(box.X1, Math.Min(_clip.X1, _plan.Width));
        var cy1 = Math.Min(box.Y1, Math.Min(_clip.Y1, _plan.Height));
        if (!_renderer.Coverage.TryGet(it, _ctm, cx0, cy0, cx1, cy1, out var cov))
        {
            return;
        }

        if (paint.Kind == PaintKind.Solid)
        {
            var rect = new RectF(cov.X, cov.Y, cov.W, cov.H);
            var idx = BeginPrim(4, rect, 0f, Matrix3x2.Identity, out var x0, out var y0, out var x1, out var y1);
            if (idx < 0)
            {
                return;
            }

            ref var inst = ref _plan.Instances[idx];
            inst.Uv = new Vector4(0, 0, 1, 1);
            inst.C0 = Pack(paint.Color, _alpha);
            Commit(idx, _renderer.Gpu.Sampled(cov.Texture), x0, y0, x1, y1);
            return;
        }

        // A gradient through a path: the gradient over the path's box, multiplied by the path's coverage.
        var saveAlpha = _alpha;
        _alpha = 1f;
        var parent = _cur;
        var parentClip = _clip;
        var child = OpenLayer();
        var local = it.LocalBounds;
        var gi = BeginPrim(0, local, Pad, out var gx0, out var gy0, out var gx1, out var gy1);
        if (gi >= 0)
        {
            ref var inst = ref _plan.Instances[gi];
            GpuBindGroup group;
            if (!SetGradient(ref inst, it.Gradient, out group))
            {
                var g = it.Gradient;
                inst.Sub = 0;
                inst.C0 = g.StopCount > 0 ? Pack(g.GetStop(g.StopCount - 1).Color) : 0;
                group = _renderer.BlankGroup;
            }

            Commit(gi, group, gx0, gy0, gx1, gy1);
        }

        _alpha = saveAlpha;
        _cur = parent;
        _clip = parentClip;
        _openDraw = -1;
        CompositeCoverage(child, cov);
    }

    // ---- borders ----

    private void DrawBorder(in DisplayItem it)
    {
        var rect = it.Rect;
        if (rect.IsEmpty)
        {
            return;
        }

        var radii = it.Radii;
        var t = it.BorderTop;
        var r = it.BorderRight;
        var b = it.BorderBottom;
        var l = it.BorderLeft;
        var widths = new Vector4(t.Width, r.Width, b.Width, l.Width);

        // Four solid sides of one colour are one ring, which also avoids seams along the corner diagonals.
        if (t.Style == BorderStyle.Solid && r.Style == BorderStyle.Solid && b.Style == BorderStyle.Solid && l.Style == BorderStyle.Solid
            && t.Color == r.Color && t.Color == b.Color && t.Color == l.Color)
        {
            if (!t.Color.IsTransparent)
            {
                Ring(rect, radii, widths, 4, 0f, 1f, t.Color, 0, 0);
            }

            return;
        }

        for (var side = 0; side < 4; side++)
        {
            var s = side switch { 0 => t, 1 => r, 2 => b, _ => l };
            if (!(s.Width > 0f) || s.Style is BorderStyle.None or BorderStyle.Hidden || s.Color.IsTransparent)
            {
                continue;
            }

            var style = s.Style;
            if (style == BorderStyle.DoubleLine && s.Width < 3f)
            {
                style = BorderStyle.Solid;
            }

            var topLeft = side is 0 or 3;
            switch (style)
            {
                case BorderStyle.Solid:
                    Ring(rect, radii, widths, side, 0f, 1f, s.Color, 0, 0);
                    break;
                case BorderStyle.DoubleLine:
                    Ring(rect, radii, widths, side, 0f, 1f / 3f, s.Color, 0, 0);
                    Ring(rect, radii, widths, side, 2f / 3f, 1f, s.Color, 0, 0);
                    break;
                case BorderStyle.Groove:
                case BorderStyle.Ridge:
                    var outerDark = (style == BorderStyle.Groove) == topLeft;
                    Ring(rect, radii, widths, side, 0f, 0.5f, outerDark ? Dark(s.Color) : s.Color, 0, 0);
                    Ring(rect, radii, widths, side, 0.5f, 1f, outerDark ? s.Color : Dark(s.Color), 0, 0);
                    break;
                case BorderStyle.Inset:
                case BorderStyle.Outset:
                    var dark = (style == BorderStyle.Inset) == topLeft;
                    Ring(rect, radii, widths, side, 0f, 1f, dark ? Dark(s.Color) : s.Color, 0, 0);
                    break;
                case BorderStyle.Dashed:
                    Ring(rect, radii, widths, side, 0f, 1f, s.Color, 1, s.Width);
                    break;
                case BorderStyle.Dotted:
                    Ring(rect, radii, widths, side, 0f, 1f, s.Color, 2, s.Width);
                    break;
                default:
                    break;
            }
        }
    }

    private static PaintColor Dark(in PaintColor c) => new(c.R * DarkFactor, c.G * DarkFactor, c.B * DarkFactor, c.A);

    private void Ring(in RectF rect, in CornerRadii radii, Vector4 widths, int side, float a, float b, in PaintColor color, int pattern, float patternWidth)
    {
        var idx = BeginPrim(1, rect, Pad, out var x0, out var y0, out var x1, out var y1);
        if (idx < 0)
        {
            return;
        }

        ref var inst = ref _plan.Instances[idx];
        SetRadii(ref inst, radii);
        inst.Widths = widths;
        inst.Sub = (uint)side;
        inst.Grad = new Vector4(a, b, 0, 0);
        inst.Uv = new Vector4(pattern, patternWidth, 0, 0);
        inst.C0 = Pack(color, _alpha);
        Commit(idx, _renderer.BlankGroup, x0, y0, x1, y1);
    }

    // ---- shadows ----

    private void DrawShadow(in DisplayItem it)
    {
        var color = it.ShadowColor;
        if (color.IsTransparent)
        {
            return;
        }

        var box = it.Rect;
        var radii = it.Radii;
        var dx = it.ShadowOffsetX;
        var dy = it.ShadowOffsetY;
        var spread = it.ShadowSpread;
        var sigma = MathF.Max(it.ShadowBlur * 0.5f, 0.5f / _scale);
        RectF shape;
        CornerRadii shapeRadii;
        if (it.ShadowInset)
        {
            shape = new RectF(box.X + dx + spread, box.Y + dy + spread, box.Width - (2 * spread), box.Height - (2 * spread));
            shapeRadii = GrowRadii(radii, -spread);
        }
        else
        {
            shape = new RectF(box.X + dx - spread, box.Y + dy - spread, box.Width + (2 * spread), box.Height + (2 * spread));
            if (shape.IsEmpty)
            {
                return;
            }

            shapeRadii = GrowRadii(radii, spread);
        }

        // The quad covers the box (inset) or the shadow shape plus the reach of the blur (outer).
        var draw = it.ShadowInset ? box : new RectF(shape.X - (3 * sigma), shape.Y - (3 * sigma), shape.Width + (6 * sigma), shape.Height + (6 * sigma));
        var idx = BeginPrim(2, draw, Pad, out var x0, out var y0, out var x1, out var y1);
        if (idx < 0)
        {
            return;
        }

        ref var inst = ref _plan.Instances[idx];
        // An empty hole of an inset shadow leaves the whole padding box shadowed: park the hole far away.
        inst.Rect = shape.IsEmpty ? new Vector4(-1e6f, -1e6f, 1f, 1f) : new Vector4(shape.X, shape.Y, shape.Width, shape.Height);
        SetRadii(ref inst, shapeRadii);
        inst.Widths = new Vector4(box.X, box.Y, box.Width, box.Height);
        inst.Uv = new Vector4(radii.TopLeftX, radii.TopRightX, radii.BottomRightX, radii.BottomLeftX);
        inst.Grad = new Vector4(sigma, it.ShadowInset ? 1f : 0f, 0, 0);
        inst.C0 = Pack(color, _alpha);
        Commit(idx, _renderer.BlankGroup, x0, y0, x1, y1);
    }

    // CSS Backgrounds 3 §7.1: a non-zero radius grows with the spread and is floored at zero; a square corner stays square.
    private static CornerRadii GrowRadii(in CornerRadii r, float spread)
    {
        static float G(float v, float s) => v > 0f ? MathF.Max(0f, v + s) : 0f;
        return new CornerRadii(G(r.TopLeftX, spread), G(r.TopLeftY, spread), G(r.TopRightX, spread), G(r.TopRightY, spread),
            G(r.BottomRightX, spread), G(r.BottomRightY, spread), G(r.BottomLeftX, spread), G(r.BottomLeftY, spread));
    }

    // ---- images ----

    private void DrawImage(in DisplayItem it)
    {
        var entry = _images?.Resolve(it.Image);
        if (entry is null)
        {
            _renderer.MissingImages++;
            return;
        }

        var dst = it.ImageDestination;
        if (dst.IsEmpty)
        {
            return;
        }

        var src = it.ImageSource;
        if (src.IsEmpty)
        {
            src = new RectF(0, 0, entry.Width, entry.Height);
        }

        var art = _renderer.Textures.Art;
        AtlasStatus status;
        ArtRegion region;
        if (entry.IsSvg)
        {
            // A vector image is rasterized at the size it is drawn.
            var wDev = Math.Clamp((int)MathF.Ceiling(dst.Width * _scale), 1, 8192);
            var hDev = Math.Clamp((int)MathF.Ceiling(dst.Height * _scale), 1, 8192);
            status = art.TryGet(entry.ArtId, wDev, hDev, out region);
            src = new RectF(0, 0, entry.Width, entry.Height);
        }
        else
        {
            status = art.TryGetScaled(entry.ArtId, 1f, out region);
        }

        if (status != AtlasStatus.Ok)
        {
            _renderer.MissingImages++;
            return;
        }

        var nearest = it.Sampling == ImageSampling.Pixelated;
        var group = _renderer.Textures.ArtGroup(region, nearest, out var pageW, out var pageH);
        DrawRegion(region, pageW, pageH, group, src, entry.Width, entry.Height, dst, it.IsTiledImage ? it.ImageTileArea : default, it.ImageTileGapX, it.ImageTileGapY, nearest);
    }

    private void DrawSvg(in DisplayItem it)
    {
        var rect = it.Rect;
        if (rect.IsEmpty || it.SvgMarkup is not { } markup)
        {
            return;
        }

        var color = it.SvgColor;
        if (!_renderer.TryRegisterSvg(it.SvgKey, markup, color, out var id, out var w, out var h))
        {
            return;
        }

        var wDev = Math.Clamp((int)MathF.Ceiling(rect.Width * _scale), 1, 8192);
        var hDev = Math.Clamp((int)MathF.Ceiling(rect.Height * _scale), 1, 8192);
        if (_renderer.Textures.Art.TryGet(id, wDev, hDev, out var region) != AtlasStatus.Ok)
        {
            _renderer.MissingImages++;
            return;
        }

        var group = _renderer.Textures.ArtGroup(region, false, out var pageW, out var pageH);
        DrawRegion(region, pageW, pageH, group, new RectF(0, 0, w, h), w, h, rect, default, 0, 0, false);
    }

    private void DrawRegion(in ArtRegion region, int pageW, int pageH, GpuBindGroup group, in RectF src, int imageW, int imageH, in RectF dst, in RectF tileArea, float gapX, float gapY, bool nearest)
    {
        // The source rectangle is in image pixels; the region holds the whole image from (U0, V0) to (U1, V1).
        var du = (region.U1 - region.U0) / Math.Max(imageW, 1);
        var dv = (region.V1 - region.V0) / Math.Max(imageH, 1);
        var u0 = region.U0 + (src.X * du);
        var v0 = region.V0 + (src.Y * dv);
        var uw = src.Width * du;
        var vh = src.Height * dv;
        // Smooth sampling stays half a texel inside the region so it never reads a neighbour on a shared page.
        var hx = nearest ? 0f : 0.5f / pageW;
        var hy = nearest ? 0f : 0.5f / pageH;
        var clampMin = new Vector2(MathF.Max(region.U0 + hx, MathF.Min(u0, u0 + uw) + hx), MathF.Max(region.V0 + hy, MathF.Min(v0, v0 + vh) + hy));
        var clampMax = new Vector2(MathF.Min(region.U1 - hx, MathF.Max(u0, u0 + uw) - hx), MathF.Min(region.V1 - hy, MathF.Max(v0, v0 + vh) - hy));
        var tiled = !tileArea.IsEmpty;
        var rect = tiled ? tileArea : dst;
        var idx = BeginPrim(tiled ? 5u : 3u, rect, tiled ? 0f : Pad, out var x0, out var y0, out var x1, out var y1);
        if (idx < 0)
        {
            return;
        }

        ref var inst = ref _plan.Instances[idx];
        inst.Uv = new Vector4(u0, v0, uw, vh);
        inst.RadiiY = new Vector4(clampMin.X, clampMin.Y, clampMax.X, clampMax.Y);
        if (tiled)
        {
            inst.Widths = new Vector4(dst.X, dst.Y, dst.Width, dst.Height);
            inst.Grad = new Vector4(0, 0, gapX, gapY);
        }

        // Group alpha cannot be folded into an image; a group around one is drawn as a layer (see PushGroup).
        Commit(idx, group, x0, y0, x1, y1);
    }

    // ---- glyphs ----

    private void DrawGlyphs(in DisplayItem it)
    {
        var run = it.GlyphRun;
        var ids = run.GlyphIds;
        if (ids.Length == 0 || run.Color.IsTransparent)
        {
            return;
        }

        var glyphs = _renderer.Textures.Glyphs;
        var faceId = glyphs.RegisterFace(run.FaceKey, run.Face);
        if (faceId < 0)
        {
            _plan.Dropped++;
            return;
        }

        var blur = run.Blur > 0f;
        var saveAlpha = _alpha;
        var parent = _cur;
        var parentClip = _clip;
        var child = -1;
        if (blur)
        {
            // A text shadow: the glyphs drawn apart and blurred, like a box shadow (Backgrounds 3 §7.2).
            _alpha = 1f;
            child = OpenLayer();
        }

        var size = run.FontSize * _scale;
        var xs = run.X;
        var ys = run.Y;
        var color = Pack(run.Color, _alpha);
        var upright = MathF.Abs(_ctm.M12) < 1e-4f && MathF.Abs(_ctm.M21) < 1e-4f && _ctm.M11 > 0f && _ctm.M22 > 0f && MathF.Abs(_ctm.M11 - _ctm.M22) < 1e-4f * _ctm.M11;
        var facing = _ctm.M11 / MathF.Max(_scale, 1e-4f);
        var bitmapSize = size;
        for (var n = 0; n < ids.Length; n++)
        {
            var px = Vector2.Transform(new Vector2(run.OriginX + xs[n], run.OriginY + ys[n]), _ctm);
            int step;
            float ix;
            if (upright)
            {
                ix = MathF.Floor(px.X);
                step = Math.Clamp((int)((px.X - ix) * GlyphAtlas.SubpixelSteps), 0, GlyphAtlas.SubpixelSteps - 1);
            }
            else
            {
                ix = px.X;
                step = 0;
            }

            if (glyphs.TryGet(faceId, ids[n], bitmapSize, step, out var info) != AtlasStatus.Ok)
            {
                _renderer.RefusedGlyphs++;
                continue;
            }

            if (info.Width == 0)
            {
                continue;
            }

            int idx, x0, y0, x1, y1;
            if (upright)
            {
                var bx = ix + info.OffsetX;
                var by = MathF.Round(px.Y) + info.OffsetY;
                idx = BeginPrim(4, new RectF(bx, by, info.Width, info.Height), 0f, Matrix3x2.Identity, out x0, out y0, out x1, out y1);
            }
            else
            {
                var rot = new Matrix3x2(_ctm.M11 / _scale, _ctm.M12 / _scale, _ctm.M21 / _scale, _ctm.M22 / _scale, px.X, px.Y);
                idx = BeginPrim(4, new RectF(info.OffsetX, info.OffsetY, info.Width, info.Height), 1f, rot, out x0, out y0, out x1, out y1);
            }

            if (idx < 0)
            {
                continue;
            }

            ref var inst = ref _plan.Instances[idx];
            inst.Uv = new Vector4(info.U0, info.V0, info.U1 - info.U0, info.V1 - info.V0);
            inst.C0 = color;
            Commit(idx, _renderer.Textures.GlyphGroup(info.Page), x0, y0, x1, y1);
        }

        _ = facing;
        if (blur)
        {
            _alpha = saveAlpha;
            _cur = parent;
            _clip = parentClip;
            _openDraw = -1;
            BlurAndComposite(child, run.Blur * 0.5f * _scale);
        }
    }
}
