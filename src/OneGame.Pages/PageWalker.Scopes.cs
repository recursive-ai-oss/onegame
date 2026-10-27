using System.Numerics;
using OneGame.Gpu;
using OneRender.Painting;

namespace OneGame.Pages;

internal sealed partial class PageWalker
{
    private const float MaxSigma = 256f;

    // ---- clips ----

    private int AddClip(in RectF rect, in CornerRadii r, in Matrix3x2 ctm, int parent)
    {
        if (_plan.ClipCount >= PageRenderer.MaxClips)
        {
            _plan.Dropped++;
            return parent;
        }

        var b = _plan.ClipCount * 5;
        if (b + 5 > _plan.Clips.Length)
        {
            Array.Resize(ref _plan.Clips, _plan.Clips.Length * 2);
        }

        if (!Matrix3x2.Invert(ctm, out var inv))
        {
            inv = Matrix3x2.Identity;
        }

        _plan.Clips[b] = new Vector4(rect.X, rect.Y, rect.Width, rect.Height);
        _plan.Clips[b + 1] = new Vector4(r.TopLeftX, r.TopRightX, r.BottomRightX, r.BottomLeftX);
        _plan.Clips[b + 2] = new Vector4(r.TopLeftY, r.TopRightY, r.BottomRightY, r.BottomLeftY);
        _plan.Clips[b + 3] = new Vector4(inv.M11, inv.M12, inv.M21, inv.M22);
        _plan.Clips[b + 4] = new Vector4(inv.M31, inv.M32, parent, 0);
        return ++_plan.ClipCount;
    }

    private (int X0, int Y0, int X1, int Y1) DeviceBox(in RectF rect, in Matrix3x2 m)
    {
        var p0 = Vector2.Transform(new Vector2(rect.X, rect.Y), m);
        var p1 = Vector2.Transform(new Vector2(rect.X + rect.Width, rect.Y), m);
        var p2 = Vector2.Transform(new Vector2(rect.X, rect.Y + rect.Height), m);
        var p3 = Vector2.Transform(new Vector2(rect.X + rect.Width, rect.Y + rect.Height), m);
        var minX = MathF.Min(MathF.Min(p0.X, p1.X), MathF.Min(p2.X, p3.X));
        var minY = MathF.Min(MathF.Min(p0.Y, p1.Y), MathF.Min(p2.Y, p3.Y));
        var maxX = MathF.Max(MathF.Max(p0.X, p1.X), MathF.Max(p2.X, p3.X));
        var maxY = MathF.Max(MathF.Max(p0.Y, p1.Y), MathF.Max(p2.Y, p3.Y));
        if (!float.IsFinite(minX + minY + maxX + maxY))
        {
            return (0, 0, 0, 0);
        }

        return ((int)MathF.Floor(Math.Max(minX, -1e6f)), (int)MathF.Floor(Math.Max(minY, -1e6f)), (int)MathF.Ceiling(Math.Min(maxX, 1e6f)), (int)MathF.Ceiling(Math.Min(maxY, 1e6f)));
    }

    private void PushClip(in DisplayItem it)
    {
        Push(ScopeKind.Clip, 0);
        var rect = it.Rect;
        var radii = it.Kind == ItemKind.PushClipRoundedRect ? it.Radii : default;
        if (rect.IsEmpty)
        {
            _clip.X1 = _clip.X0;
            return;
        }

        var (x0, y0, x1, y1) = DeviceBox(rect, _ctm);
        var axis = MathF.Abs(_ctm.M12) < 1e-4f && MathF.Abs(_ctm.M21) < 1e-4f;
        var aligned = axis && Near(x0, rect.X * _ctm.M11 + _ctm.M31) && Near(x1, (rect.X + rect.Width) * _ctm.M11 + _ctm.M31)
            && Near(y0, rect.Y * _ctm.M22 + _ctm.M32) && Near(y1, (rect.Y + rect.Height) * _ctm.M22 + _ctm.M32);
        var next = _clip;
        next.X0 = Math.Max(next.X0, x0);
        next.Y0 = Math.Max(next.Y0, y0);
        next.X1 = Math.Min(next.X1, x1);
        next.Y1 = Math.Min(next.Y1, y1);
        if (!aligned || !radii.IsZero)
        {
            next.Id = AddClip(rect, radii.IsZero ? default : radii.Constrained(rect.Width, rect.Height), _ctm, _clip.Id);
        }

        _clip = next;
    }

    private static bool Near(float device, float exact) => MathF.Abs(device - exact) < 1e-3f;

    private void PushClipPath(in DisplayItem it, int index)
    {
        if (!Emitting)
        {
            Push(ScopeKind.Clip, index);
            return;
        }

        var box = DeviceBox(it.LocalBounds, _ctm);
        var x0 = Math.Max(box.X0, Math.Max(_clip.X0, 0));
        var y0 = Math.Max(box.Y0, Math.Max(_clip.Y0, 0));
        var x1 = Math.Min(box.X1, Math.Min(_clip.X1, _plan.Width));
        var y1 = Math.Min(box.Y1, Math.Min(_clip.Y1, _plan.Height));
        if (x1 <= x0 || y1 <= y0 || !_renderer.Coverage.TryGet(it, _ctm, x0, y0, x1, y1, out var cov))
        {
            Push(ScopeKind.ClipPath, index).Aux = 1;
            _suppress++;
            return;
        }

        ref var s = ref Push(ScopeKind.ClipPath, index);
        s.Coverage = cov.Texture;
        s.CovX = cov.X;
        s.CovY = cov.Y;
        s.CovW = cov.W;
        s.CovH = cov.H;
        OpenLayer();
    }

    private void PopClip()
    {
        ref var s = ref _stack[_sp - 1];
        if (s.Kind == ScopeKind.Clip)
        {
            _clip = s.Clip;
            _sp--;
            return;
        }

        var scope = Pop(ScopeKind.ClipPath);
        if (scope.Aux == 1)
        {
            _suppress--;
            return;
        }

        var child = _cur;
        _cur = scope.ParentCur;
        _clip = scope.Clip;
        _openDraw = -1;
        CompositeCoverage(child, scope.Coverage!, scope.CovX, scope.CovY, scope.CovW, scope.CovH);
    }

    // ---- transforms ----

    private void PushTransform3D(in DisplayItem it, int index)
    {
        if (!Emitting)
        {
            Push(ScopeKind.Transform, index);
            var m4 = it.Matrix4;
            var w = m4.M44 > 1e-6f ? m4.M44 : 1f;
            _ctm = new Matrix3x2(m4.M11 / w, m4.M12 / w, m4.M21 / w, m4.M22 / w, m4.M41 / w, m4.M42 / w) * _ctm;
            _scale = ScaleOf(_ctm);
            return;
        }

        Push(ScopeKind.Transform3D, index);
        OpenLayer();
    }

    private void PopTransform()
    {
        ref var top = ref _stack[_sp - 1];
        if (top.Kind == ScopeKind.Transform)
        {
            _ctm = top.Ctm;
            _scale = ScaleOf(_ctm);
            _sp--;
            return;
        }

        var s = Pop(ScopeKind.Transform3D);
        var child = _cur;
        _cur = s.ParentCur;
        _clip = s.Clip;
        _openDraw = -1;
        _ctm = s.Ctm;
        ProjectLayer(child, _list[s.Item].Matrix4, s.Ctm);
    }

    private void ProjectLayer(int child, in Matrix4x4 m4, in Matrix3x2 ctm)
    {
        var b = _plan.BoundsOf(child);
        if (b.X1 <= b.X0)
        {
            return;
        }

        var c4 = new Matrix4x4(ctm.M11, ctm.M12, 0, 0, ctm.M21, ctm.M22, 0, 0, 0, 0, 1, 0, ctm.M31, ctm.M32, 0, 1);
        if (!Matrix4x4.Invert(c4, out var c4i))
        {
            return;
        }

        var t = c4i * m4 * c4;
        // Row-vector homography on (x, y, 1): the x, y and w columns of the 4x4.
        var h = new Matrix3x3(t.M11, t.M12, t.M14, t.M21, t.M22, t.M24, t.M41, t.M42, t.M44);
        if (!h.TryInvert(out var inv))
        {
            return;
        }

        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        var behind = false;
        ReadOnlySpan<float> xs = [b.X0, b.X1, b.X0, b.X1];
        ReadOnlySpan<float> ys = [b.Y0, b.Y0, b.Y1, b.Y1];
        for (var k = 0; k < 4; k++)
        {
            var w = (xs[k] * h.M13) + (ys[k] * h.M23) + h.M33;
            if (w <= 1e-4f)
            {
                behind = true;
                break;
            }

            var px = ((xs[k] * h.M11) + (ys[k] * h.M21) + h.M31) / w;
            var py = ((xs[k] * h.M12) + (ys[k] * h.M22) + h.M32) / w;
            minX = MathF.Min(minX, px);
            minY = MathF.Min(minY, py);
            maxX = MathF.Max(maxX, px);
            maxY = MathF.Max(maxY, py);
        }

        var x0 = behind ? 0 : (int)MathF.Floor(minX) - 1;
        var y0 = behind ? 0 : (int)MathF.Floor(minY) - 1;
        var x1 = behind ? _plan.Width : (int)MathF.Ceiling(maxX) + 1;
        var y1 = behind ? _plan.Height : (int)MathF.Ceiling(maxY) + 1;
        x0 = Math.Max(x0, Math.Max(_clip.X0, 0));
        y0 = Math.Max(y0, Math.Max(_clip.Y0, 0));
        x1 = Math.Min(x1, Math.Min(_clip.X1, _plan.Width));
        y1 = Math.Min(y1, Math.Min(_clip.Y1, _plan.Height));
        if (x1 <= x0 || y1 <= y0)
        {
            return;
        }

        EnsureWritten(_cur);
        ref var i = ref Effect(StepKind.Project, _cur, child, -1, null, x0, y0, x1, y1, _clip.Id, "page.project");
        i.Radii = new Vector4(inv.M11, inv.M21, inv.M31, 0);
        i.RadiiY = new Vector4(inv.M12, inv.M22, inv.M32, 0);
        i.Widths = new Vector4(inv.M13, inv.M23, inv.M33, 0);
        i.Grad = new Vector4(1, 0, 0, 0);
        _plan.GrowBounds(_cur, x0, y0, x1, y1);
    }

    private readonly record struct Matrix3x3(float M11, float M12, float M13, float M21, float M22, float M23, float M31, float M32, float M33)
    {
        public bool TryInvert(out Matrix3x3 inverse)
        {
            var c11 = (M22 * M33) - (M23 * M32);
            var c12 = (M23 * M31) - (M21 * M33);
            var c13 = (M21 * M32) - (M22 * M31);
            var det = (M11 * c11) + (M12 * c12) + (M13 * c13);
            if (MathF.Abs(det) < 1e-12f)
            {
                inverse = default;
                return false;
            }

            var d = 1f / det;
            inverse = new Matrix3x3(
                c11 * d, ((M13 * M32) - (M12 * M33)) * d, ((M12 * M23) - (M13 * M22)) * d,
                c12 * d, ((M11 * M33) - (M13 * M31)) * d, ((M13 * M21) - (M11 * M23)) * d,
                c13 * d, ((M12 * M31) - (M11 * M32)) * d, ((M11 * M22) - (M12 * M21)) * d);
            return true;
        }
    }

    // ---- steps ----

    /// <summary>Adds a pass that draws one quad over a device rectangle and returns its instance for the caller to fill in.</summary>
    private ref PageInstance Effect(StepKind kind, int dst, int src, int src2, GpuTexture? external, int x0, int y0, int x1, int y1, int clipId, string name)
    {
        var fresh = !_plan.SlotWritten[dst];
        ref var inst = ref _plan.AddInstance();
        var index = _plan.InstanceCount - 1;
        inst.Rect = new Vector4(x0, y0, x1 - x0, y1 - y0);
        inst.M = new Vector4(1, 0, 0, 1);
        inst.ClipId = (uint)clipId;
        ref var s = ref NewStep(kind, dst);
        s.Src = src;
        s.Src2 = src2;
        s.External = external;
        s.Clear = fresh;
        s.Instance = index;
        s.Sx = x0;
        s.Sy = y0;
        s.Sw = x1 - x0;
        s.Sh = y1 - y0;
        s.Name = name;
        _plan.SlotWritten[dst] = true;
        return ref _plan.Instances[index];
    }

    private (int X0, int Y0, int X1, int Y1) ClipBoxOf(int slot)
    {
        var b = _plan.BoundsOf(slot);
        return (Math.Max(b.X0, Math.Max(_clip.X0, 0)), Math.Max(b.Y0, Math.Max(_clip.Y0, 0)), Math.Min(b.X1, Math.Min(_clip.X1, _plan.Width)), Math.Min(b.Y1, Math.Min(_clip.Y1, _plan.Height)));
    }

    /// <summary>Lays a finished layer over the current one inside the current clip.</summary>
    private void CompositeLayer(StepKind kind, int child, float opacity)
    {
        var (x0, y0, x1, y1) = ClipBoxOf(child);
        if (x1 <= x0 || y1 <= y0 || !_plan.SlotWritten[child])
        {
            return;
        }

        EnsureWritten(_cur);
        ref var i = ref Effect(kind, _cur, child, -1, null, x0, y0, x1, y1, _clip.Id, kind == StepKind.LayerAdd ? "page.add" : "page.composite");
        i.Grad = new Vector4(opacity, 0, 0, 0);
        _plan.GrowBounds(_cur, x0, y0, x1, y1);
    }

    private void CompositeCoverage(int child, PathCoverage cov) =>
        CompositeCoverage(child, cov.Texture, cov.X, cov.Y, cov.W, cov.H);

    private void CompositeCoverage(int child, GpuTexture coverage, int cx, int cy, int cw, int ch)
    {
        var (x0, y0, x1, y1) = ClipBoxOf(child);
        x0 = Math.Max(x0, cx);
        y0 = Math.Max(y0, cy);
        x1 = Math.Min(x1, cx + cw);
        y1 = Math.Min(y1, cy + ch);
        if (x1 <= x0 || y1 <= y0 || !_plan.SlotWritten[child])
        {
            return;
        }

        EnsureWritten(_cur);
        ref var i = ref Effect(StepKind.MaskApply, _cur, child, -1, coverage, x0, y0, x1, y1, _clip.Id, "page.mask");
        i.Widths = new Vector4(cx, cy, cw, ch);
        i.Grad = new Vector4(0, 1, 0, 0);
        _plan.GrowBounds(_cur, x0, y0, x1, y1);
    }

    // ---- groups ----

    private static bool FoldableLeaf(in DisplayItem leaf) => leaf.Kind switch
    {
        ItemKind.FillRect or ItemKind.FillRoundedRect or ItemKind.FillPath => leaf.Paint.Kind == PaintKind.Solid,
        ItemKind.Border or ItemKind.BoxShadow => true,
        ItemKind.GlyphRun => leaf.GlyphRun.Blur <= 0f,
        _ => false,
    };

    private void PushGroup(in DisplayItem it, int index)
    {
        var opacity = it.Opacity;
        var blend = it.Blend;
        if (!Emitting || (blend == BlendMode.Normal && opacity >= 0.999f))
        {
            Push(ScopeKind.Skip, index);
            return;
        }

        if (!(opacity > 0f))
        {
            Push(ScopeKind.Suppress, index);
            _suppress++;
            return;
        }

        // One leaf item in a normal group can carry the opacity itself (the format allows it): no layer.
        if (blend == BlendMode.Normal && it.MatchIndex == index + 2 && FoldableLeaf(_list[index + 1]))
        {
            Push(ScopeKind.Group, index).Aux = 1;
            _alpha *= opacity;
            return;
        }

        ref var s = ref Push(ScopeKind.Group, index);
        s.Value = opacity;
        s.Aux = 2 + (int)blend;
        OpenLayer();
    }

    private void PopGroup()
    {
        var s = _stack[--_sp];
        switch (s.Kind)
        {
            case ScopeKind.Skip:
                return;
            case ScopeKind.Suppress:
                _suppress--;
                return;
            case ScopeKind.Group when s.Aux == 1:
                _alpha = s.Alpha;
                return;
        }

        var child = _cur;
        _cur = s.ParentCur;
        _clip = s.Clip;
        _openDraw = -1;
        var blend = (BlendMode)(s.Aux - 2);
        switch (blend)
        {
            case BlendMode.Normal:
                CompositeLayer(StepKind.Layer, child, s.Value);
                break;
            case BlendMode.PlusLighter:
                CompositeLayer(StepKind.LayerAdd, child, s.Value);
                break;
            default:
                BlendLayer(child, blend, s.Value);
                break;
        }
    }

    private void BlendLayer(int child, BlendMode mode, float opacity)
    {
        var (x0, y0, x1, y1) = ClipBoxOf(child);
        if (x1 <= x0 || y1 <= y0 || !_plan.SlotWritten[child])
        {
            return;
        }

        EnsureWritten(_cur);
        var parent = _cur;
        var next = _plan.NewSlot();
        ref var i = ref Effect(StepKind.Blend, next, child, parent, null, 0, 0, _plan.Width, _plan.Height, _clip.Id, "page.blend");
        i.Sub = (uint)mode;
        i.Grad = new Vector4(opacity, 0, 0, 0);
        InheritBounds(next, parent);
        _plan.GrowBounds(next, x0, y0, x1, y1);
        _cur = next;
    }

    private void InheritBounds(int slot, int from)
    {
        var b = _plan.BoundsOf(from);
        if (b.X1 > b.X0)
        {
            _plan.GrowBounds(slot, b.X0, b.Y0, b.X1, b.Y1);
        }
    }

    // ---- filters ----

    private void PushLayerScope(ScopeKind kind, int index)
    {
        if (!Emitting)
        {
            Push(ScopeKind.Skip, index);
            return;
        }

        Push(kind, index);
        OpenLayer();
    }

    private void PopFilter()
    {
        var s = _stack[--_sp];
        if (s.Kind == ScopeKind.Skip)
        {
            return;
        }

        var child = _cur;
        _cur = s.ParentCur;
        _clip = s.Clip;
        _openDraw = -1;
        var result = ApplyChain(child, _list[s.Item], ScaleOf(s.Ctm), s.Ctm);
        CompositeLayer(StepKind.Layer, result, 1f);
    }

    private static bool IsColorOp(FilterOpKind k) => k is not (FilterOpKind.Blur or FilterOpKind.DropShadow);

    /// <summary>Runs a filter chain over a layer and returns the slot holding the result.</summary>
    private int ApplyChain(int source, in DisplayItem item, float scale, in Matrix3x2 ctm)
    {
        if (!_plan.SlotWritten[source])
        {
            return source;
        }

        var x = source;
        var count = item.FilterOpCount;
        for (var n = 0; n < count;)
        {
            var op = item.GetFilterOp(n);
            if (IsColorOp(op.Kind))
            {
                var kinds = new Vector4(-1, -1, -1, -1);
                var amounts = Vector4.Zero;
                var used = 0;
                while (used < 4 && n < count)
                {
                    var o = item.GetFilterOp(n);
                    if (!IsColorOp(o.Kind))
                    {
                        break;
                    }

                    kinds[used] = (float)(int)o.Kind;
                    amounts[used] = o.A;
                    used++;
                    n++;
                }

                var b = _plan.BoundsOf(x);
                var next = _plan.NewSlot();
                ref var i = ref Effect(StepKind.Color, next, x, -1, null, b.X0, b.Y0, b.X1, b.Y1, 0, "page.filter");
                i.Radii = kinds;
                i.Widths = amounts;
                _plan.GrowBounds(next, b.X0, b.Y0, b.X1, b.Y1);
                x = next;
                continue;
            }

            n++;
            if (op.Kind == FilterOpKind.Blur)
            {
                x = BlurPasses(x, op.A * scale);
            }
            else
            {
                x = DropShadow(x, op, scale, ctm);
            }
        }

        return x;
    }

    private int BlurPasses(int source, float sigma)
    {
        sigma = MathF.Min(sigma, MaxSigma);
        var b = _plan.BoundsOf(source);
        if (!(sigma > 0.05f) || b.X1 <= b.X0)
        {
            return source;
        }

        var margin = (int)MathF.Ceiling(3f * sigma) + 1;
        var x0 = Math.Max(b.X0 - margin, 0);
        var y0 = Math.Max(b.Y0 - margin, 0);
        var x1 = Math.Min(b.X1 + margin, _plan.Width);
        var y1 = Math.Min(b.Y1 + margin, _plan.Height);
        var radius = Math.Max(1, (int)MathF.Ceiling(3f * sigma));
        var taps = Math.Min(radius, 32);
        var stride = (float)radius / taps;
        var horizontal = _plan.NewSlot();
        ref var h = ref Effect(StepKind.Blur, horizontal, source, -1, null, x0, y0, x1, y1, 0, "page.blur");
        h.Grad = new Vector4(stride / _plan.Width, 0, sigma, taps);
        h.Radii = new Vector4(stride, 0, 0, 0);
        _plan.GrowBounds(horizontal, x0, y0, x1, y1);
        var vertical = _plan.NewSlot();
        ref var v = ref Effect(StepKind.Blur, vertical, horizontal, -1, null, x0, y0, x1, y1, 0, "page.blur");
        v.Grad = new Vector4(0, stride / _plan.Height, sigma, taps);
        v.Radii = new Vector4(stride, 0, 0, 0);
        _plan.GrowBounds(vertical, x0, y0, x1, y1);
        return vertical;
    }

    private int DropShadow(int source, in FilterOp op, float scale, in Matrix3x2 ctm)
    {
        var b = _plan.BoundsOf(source);
        if (b.X1 <= b.X0)
        {
            return source;
        }

        var offset = Vector2.TransformNormal(new Vector2(op.A, op.B), ctm);
        var dx = MathF.Round(offset.X);
        var dy = MathF.Round(offset.Y);
        var sigma = MathF.Min(op.C * scale, MaxSigma);
        var margin = (int)MathF.Ceiling(3f * sigma) + 1;
        var x0 = Math.Max((int)MathF.Min(b.X0, b.X0 + dx) - margin, 0);
        var y0 = Math.Max((int)MathF.Min(b.Y0, b.Y0 + dy) - margin, 0);
        var x1 = Math.Min((int)MathF.Max(b.X1, b.X1 + dx) + margin, _plan.Width);
        var y1 = Math.Min((int)MathF.Max(b.Y1, b.Y1 + dy) + margin, _plan.Height);
        var tint = _plan.NewSlot();
        ref var t = ref Effect(StepKind.DropTint, tint, source, -1, null, x0, y0, x1, y1, 0, "page.shadow");
        t.Grad = new Vector4(dx, dy, 0, 0);
        t.C0 = Pack(op.Color);
        _plan.GrowBounds(tint, x0, y0, x1, y1);
        var blurred = BlurPasses(tint, sigma);
        var result = _plan.NewSlot();
        _ = Effect(StepKind.Over, result, source, blurred, null, x0, y0, x1, y1, 0, "page.shadow");
        _plan.GrowBounds(result, x0, y0, x1, y1);
        return result;
    }

    private void BlurAndComposite(int child, float sigmaDevice)
    {
        var result = BlurPasses(child, sigmaDevice);
        CompositeLayer(StepKind.Layer, result, 1f);
    }

    private void DrawBackdrop(in DisplayItem it)
    {
        var rect = it.Rect;
        if (rect.IsEmpty || it.FilterOpCount == 0 || _clip.IsEmpty)
        {
            return;
        }

        EnsureWritten(_cur);
        var filtered = ApplyChain(_cur, it, _scale, _ctm);
        if (filtered == _cur)
        {
            return;
        }

        var parent = _cur;
        var next = _plan.NewSlot();
        ref var i = ref Effect(StepKind.Backdrop, next, filtered, parent, null, 0, 0, _plan.Width, _plan.Height, _clip.Id, "page.backdrop");
        // The shape is the element's rounded box in its own space, so the quad is stretched to cover the target and the shader samples its distance field.
        var r = it.Radii;
        i.Rect = new Vector4(rect.X, rect.Y, rect.Width, rect.Height);
        i.M = new Vector4(_ctm.M11, _ctm.M12, _ctm.M21, _ctm.M22);
        i.T = new Vector2(_ctm.M31, _ctm.M32);
        i.Pad = (_plan.Width + _plan.Height + 8f) / _scale * 2f;
        SetRadii(ref i, r);
        InheritBounds(next, parent);
        _cur = next;
        _openDraw = -1;
    }

    // ---- masks ----

    private void PushMask(int index)
    {
        if (!Emitting)
        {
            Push(ScopeKind.Skip, index);
            return;
        }

        ref var s = ref Push(ScopeKind.Mask, index);
        s.Aux = -1;
        OpenLayer();
    }

    private void PushMaskLayer(in DisplayItem it, int index)
    {
        if (_sp == 0 || _stack[_sp - 1].Kind != ScopeKind.Mask)
        {
            Push(ScopeKind.Skip, index);
            return;
        }

        ref var s = ref Push(ScopeKind.MaskLayer, index);
        s.Aux = ((int)it.MaskComposite << 1) | (it.MaskMode == MaskLayerMode.Luminance ? 1 : 0);
        OpenLayer();
    }

    private void PopMaskLayer()
    {
        var s = _stack[--_sp];
        if (s.Kind == ScopeKind.Skip)
        {
            return;
        }

        var layer = _cur;
        _cur = s.ParentCur;
        _clip = s.Clip;
        _openDraw = -1;
        ref var mask = ref _stack[_sp - 1];
        var accum = mask.Aux;
        var next = _plan.NewSlot();
        var src = _plan.SlotWritten[layer] ? layer : -1;
        ref var i = ref Effect(StepKind.MaskCombine, next, src, accum, null, 0, 0, _plan.Width, _plan.Height, 0, "page.mask");
        i.Sub = (uint)(s.Aux >> 1);
        i.Grad = new Vector4((s.Aux & 1) != 0 ? 1 : 0, 0, 0, 0);
        _plan.GrowBounds(next, 0, 0, _plan.Width, _plan.Height);
        mask.Aux = next;
    }

    private void PopMask()
    {
        var s = _stack[--_sp];
        if (s.Kind == ScopeKind.Skip)
        {
            return;
        }

        var content = _cur;
        _cur = s.ParentCur;
        _clip = s.Clip;
        _openDraw = -1;

        // A mask with no layers hides what it masks.
        if (s.Aux < 0)
        {
            return;
        }

        var (x0, y0, x1, y1) = ClipBoxOf(content);
        if (x1 <= x0 || y1 <= y0 || !_plan.SlotWritten[content])
        {
            return;
        }

        EnsureWritten(_cur);
        _ = Effect(StepKind.MaskApply, _cur, content, s.Aux, null, x0, y0, x1, y1, _clip.Id, "page.mask");
        _plan.GrowBounds(_cur, x0, y0, x1, y1);
    }

    // ---- host scopes ----

    private void PushHostScope(in DisplayItem it, int index)
    {
        var binding = _scopes.Get(it.HostScope);
        switch (binding.Kind)
        {
            case PageScopeKind.Layer:
                Push(ScopeKind.HostLayer, index);
                _layerNow = binding.Layer;
                break;
            case PageScopeKind.Additive when Emitting:
                Push(ScopeKind.HostAdd, index);
                OpenLayer();
                break;
            default:
                Push(ScopeKind.Skip, index);
                break;
        }
    }

    private void PopHostScope()
    {
        var s = _stack[--_sp];
        switch (s.Kind)
        {
            case ScopeKind.HostLayer:
                _layerNow = s.Layer;
                break;
            case ScopeKind.HostAdd:
                var child = _cur;
                _cur = s.ParentCur;
                _clip = s.Clip;
                _openDraw = -1;
                CompositeLayer(StepKind.LayerAdd, child, 1f);
                break;
            default:
                break;
        }
    }
}
