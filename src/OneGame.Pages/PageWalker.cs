using System.Numerics;
using OneGame.Gpu;
using OneRender.Painting;

namespace OneGame.Pages;

/// <summary>
/// Turns one display list (format 1.3) into a <see cref="PagePlan"/>: instanced primitives for the leaf items, and layers with composite steps for
/// everything that has to be drawn apart and combined (groups, masks, filters, backdrop filters, projections, path clips, host scopes).
/// The walk keeps one stack with an entry per open push and never recurses.
/// </summary>
internal sealed partial class PageWalker
{
    private enum ScopeKind : byte
    {
        Skip,
        Suppress,
        Clip,
        ClipPath,
        Transform,
        Transform3D,
        Group,
        Filter,
        Mask,
        MaskLayer,
        HostLayer,
        HostAdd,
        HostMaterial,
    }

    private struct ClipState
    {
        public int Id;
        public int X0;
        public int Y0;
        public int X1;
        public int Y1;

        public readonly bool IsEmpty => X1 <= X0 || Y1 <= Y0;
    }

    private struct Scope
    {
        public ScopeKind Kind;
        public int Item;
        public int ParentCur;
        public int Child;
        public Matrix3x2 Ctm;
        public ClipState Clip;
        public int Layer;
        public int Aux;
        public float Value;
        public GpuTexture? Coverage;
        public int CovX;
        public int CovY;
        public int CovW;
        public int CovH;
        public float Alpha;
    }

    private readonly PageRenderer _renderer;
    private PagePlan _plan = null!;
    private DisplayList _list = null!;
    private PageImages? _images;
    private PageScopes _scopes = null!;
    private Scope[] _stack = new Scope[32];
    private int _sp;
    private Matrix3x2 _ctm;
    private ClipState _clip;
    private ClipState _full;
    private int _cur;
    private int _openDraw = -1;
    private int _suppress;
    private int _layerNow;
    private int _layerFilter;
    private float _scale;
    private float _alpha = 1f;
    private float _viewScale;

    public PageWalker(PageRenderer renderer) => _renderer = renderer;

    private bool Emitting => _suppress == 0 && _layerNow == _layerFilter;

    /// <summary>Adds one screen's items to the plan. All screens of a plan share its root layer.</summary>
    public void Walk(PagePlan plan, DisplayList list, PageImages? images, PageScopes scopes, float scale, int layer)
    {
        if (list.RequiredMinor > DisplayList.FormatMinor)
        {
            throw new NotSupportedException($"The display list needs format 1.{list.RequiredMinor}; this renderer draws up to 1.{DisplayList.FormatMinor}.");
        }

        _plan = plan;
        _list = list;
        _images = images;
        _scopes = scopes;
        _viewScale = scale;
        _layerFilter = layer;
        _layerNow = 0;
        _suppress = 0;
        _sp = 0;
        _alpha = 1f;
        _ctm = Matrix3x2.CreateScale(scale);
        _scale = scale;
        _full = new ClipState { Id = 0, X0 = 0, Y0 = 0, X1 = plan.Width, Y1 = plan.Height };
        _clip = _full;
        _cur = plan.Root;
        _openDraw = -1;

        var count = list.Count;
        for (var i = 0; i < count; i++)
        {
            var it = list[i];
            switch (it.Kind)
            {
                case ItemKind.FillRect:
                case ItemKind.FillRoundedRect:
                    if (Emitting)
                    {
                        FillBox(it);
                    }

                    break;
                case ItemKind.FillPath:
                    if (Emitting)
                    {
                        FillPath(it);
                    }

                    break;
                case ItemKind.Border:
                    if (Emitting)
                    {
                        DrawBorder(it);
                    }

                    break;
                case ItemKind.BoxShadow:
                    if (Emitting)
                    {
                        DrawShadow(it);
                    }

                    break;
                case ItemKind.Image:
                    if (Emitting)
                    {
                        DrawImage(it);
                    }

                    break;
                case ItemKind.GlyphRun:
                    if (Emitting)
                    {
                        DrawGlyphs(it);
                    }

                    break;
                case ItemKind.SvgImage:
                    if (Emitting)
                    {
                        DrawSvg(it);
                    }

                    break;
                case ItemKind.BackdropFilter:
                    if (Emitting)
                    {
                        DrawBackdrop(it);
                    }

                    break;
                case ItemKind.PushClipRect:
                case ItemKind.PushClipRoundedRect:
                    PushClip(it);
                    break;
                case ItemKind.PushClipPath:
                    PushClipPath(it, i);
                    break;
                case ItemKind.PopClip:
                    PopClip();
                    break;
                case ItemKind.PushTransform:
                    Push(ScopeKind.Transform, i);
                    _ctm = it.Matrix * _ctm;
                    _scale = ScaleOf(_ctm);
                    break;
                case ItemKind.PushTransform3D:
                    PushTransform3D(it, i);
                    break;
                case ItemKind.PopTransform:
                    PopTransform();
                    break;
                case ItemKind.PushGroup:
                    PushGroup(it, i);
                    break;
                case ItemKind.PopGroup:
                    PopGroup();
                    break;
                case ItemKind.PushFilter:
                    PushLayerScope(ScopeKind.Filter, i);
                    break;
                case ItemKind.PopFilter:
                    PopFilter();
                    break;
                case ItemKind.PushMask:
                    PushMask(i);
                    break;
                case ItemKind.PopMask:
                    PopMask();
                    break;
                case ItemKind.PushMaskLayer:
                    PushMaskLayer(it, i);
                    break;
                case ItemKind.PopMaskLayer:
                    PopMaskLayer();
                    break;
                case ItemKind.PushHostScope:
                    PushHostScope(it, i);
                    break;
                case ItemKind.PopHostScope:
                    PopHostScope();
                    break;
                default:
                    // Tags carry structure for other backends.
                    break;
            }
        }

        if (_sp != 0)
        {
            throw new InvalidOperationException("The display list ended with scopes still open.");
        }
    }

    private static float ScaleOf(in Matrix3x2 m) => MathF.Max(MathF.Sqrt(MathF.Abs((m.M11 * m.M22) - (m.M12 * m.M21))), 1e-4f);

    private ref Scope Push(ScopeKind kind, int item)
    {
        if (_sp == _stack.Length)
        {
            Array.Resize(ref _stack, _sp * 2);
        }

        ref var s = ref _stack[_sp++];
        s = default;
        s.Kind = kind;
        s.Item = item;
        s.ParentCur = _cur;
        s.Ctm = _ctm;
        s.Clip = _clip;
        s.Layer = _layerNow;
        s.Alpha = _alpha;
        return ref s;
    }

    private ref Scope Pop(ScopeKind kind)
    {
        if (_sp == 0 || _stack[_sp - 1].Kind != kind)
        {
            throw new InvalidOperationException($"Unbalanced display list: expected the end of a {kind} scope.");
        }

        return ref _stack[--_sp];
    }

    // ---- plan helpers ----

    /// <summary>Starts a pass of a plan; it closes the open draw run, whose batches must stay contiguous.</summary>
    private ref Step NewStep(StepKind kind, int dst)
    {
        _openDraw = -1;
        ref var s = ref _plan.AddStep();
        s.Kind = kind;
        s.Dst = dst;
        s.Src = -1;
        s.Src2 = -1;
        return ref s;
    }

    internal void EnsureWritten(int slot)
    {
        if (_plan.SlotWritten[slot])
        {
            return;
        }

        ref var s = ref NewStep(StepKind.Draw, slot);
        s.Clear = true;
        s.Name = "page.layer";
        _plan.SlotWritten[slot] = true;
    }

    private int OpenLayer()
    {
        var slot = _plan.NewSlot();
        _cur = slot;
        _clip = _full;
        _openDraw = -1;
        return slot;
    }

    /// <summary>Adds an instance for a primitive in the current layer and returns its index, or -1 when it is outside the clip.</summary>
    private int BeginPrim(uint kind, in RectF rect, float padLocal, out int x0, out int y0, out int x1, out int y1) =>
        BeginPrim(kind, rect, padLocal, _ctm, out x0, out y0, out x1, out y1);

    private int BeginPrim(uint kind, in RectF rect, float padLocal, in Matrix3x2 m, out int x0, out int y0, out int x1, out int y1)
    {
        x0 = y0 = x1 = y1 = 0;
        if (_clip.IsEmpty)
        {
            return -1;
        }

        var ax = rect.X - padLocal;
        var ay = rect.Y - padLocal;
        var bx = rect.X + rect.Width + padLocal;
        var by = rect.Y + rect.Height + padLocal;
        var p0 = Vector2.Transform(new Vector2(ax, ay), m);
        var p1 = Vector2.Transform(new Vector2(bx, ay), m);
        var p2 = Vector2.Transform(new Vector2(ax, by), m);
        var p3 = Vector2.Transform(new Vector2(bx, by), m);
        var minX = MathF.Min(MathF.Min(p0.X, p1.X), MathF.Min(p2.X, p3.X));
        var minY = MathF.Min(MathF.Min(p0.Y, p1.Y), MathF.Min(p2.Y, p3.Y));
        var maxX = MathF.Max(MathF.Max(p0.X, p1.X), MathF.Max(p2.X, p3.X));
        var maxY = MathF.Max(MathF.Max(p0.Y, p1.Y), MathF.Max(p2.Y, p3.Y));
        if (!float.IsFinite(minX + minY + maxX + maxY) || maxX < _clip.X0 || minX > _clip.X1 || maxY < _clip.Y0 || minY > _clip.Y1)
        {
            return -1;
        }

        x0 = (int)MathF.Floor(minX);
        y0 = (int)MathF.Floor(minY);
        x1 = (int)MathF.Ceiling(maxX);
        y1 = (int)MathF.Ceiling(maxY);
        if (_plan.InstanceCount >= PageRenderer.MaxInstances)
        {
            _plan.Dropped++;
            return -1;
        }

        var index = _plan.InstanceCount;
        ref var i = ref _plan.AddInstance();
        i.Rect = new Vector4(rect.X, rect.Y, rect.Width, rect.Height);
        i.M = new Vector4(m.M11, m.M12, m.M21, m.M22);
        i.T = new Vector2(m.M31, m.M32);
        i.Kind = kind;
        i.ClipId = (uint)_clip.Id;
        i.Pad = padLocal;
        return index;
    }

    /// <summary>Queues a primitive instance for drawing with a bind group, merging it into the open batch when it can.</summary>
    private void Commit(int instance, GpuBindGroup group, int x0, int y0, int x1, int y1)
    {
        var sx = Math.Max(_clip.X0, 0);
        var sy = Math.Max(_clip.Y0, 0);
        var ex = Math.Min(_clip.X1, _plan.Width);
        var ey = Math.Min(_clip.Y1, _plan.Height);
        _plan.GrowBounds(_cur, Math.Max(x0, sx), Math.Max(y0, sy), Math.Min(x1, ex), Math.Min(y1, ey));
        if (_openDraw < 0 || _plan.Steps[_openDraw].Dst != _cur)
        {
            var wasWritten = _plan.SlotWritten[_cur];
            ref var s = ref NewStep(StepKind.Draw, _cur);
            s.Clear = !wasWritten;
            s.BatchFirst = _plan.BatchCount;
            s.Name = "page.layer";
            _plan.SlotWritten[_cur] = true;
            _openDraw = _plan.StepCount - 1;
        }

        ref var step = ref _plan.Steps[_openDraw];
        if (step.BatchCount > 0)
        {
            ref var last = ref _plan.Batches[step.BatchFirst + step.BatchCount - 1];
            if (ReferenceEquals(last.Group, group) && last.Sx == sx && last.Sy == sy && last.Sw == ex - sx && last.Sh == ey - sy
                && last.First + last.Count == instance)
            {
                last.Count++;
                return;
            }
        }

        ref var b = ref _plan.AddBatch();
        b.Group = group;
        b.First = instance;
        b.Count = 1;
        b.Sx = sx;
        b.Sy = sy;
        b.Sw = ex - sx;
        b.Sh = ey - sy;
        step.BatchCount++;
    }
}
