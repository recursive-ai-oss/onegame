using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using OneGame.Atlas;
using OneGame.Gpu;
using OneGame.Gpu.Graph;
using OneGame.Text;
using OneRender.Painting;

namespace OneGame.Pages;

/// <summary>How a frame of pages is drawn.</summary>
/// <param name="Width">Target width in device pixels.</param>
/// <param name="Height">Target height in device pixels.</param>
/// <param name="Scale">Device pixels per CSS pixel.</param>
/// <param name="FrameNumber">A number that grows with every frame, shared with the UI renderer's atlas bookkeeping (<c>Renderer2D.FrameNumber</c>).</param>
/// <param name="Layer">The host-scope layer to draw: pages draw their own content as layer 0 and any <see cref="PageScopes.Layer"/> subtree in its layer.</param>
/// <param name="Clear">Clear the target to this linear colour first, or <see langword="null"/> to draw over what is there.</param>
public readonly record struct PageFrame(int Width, int Height, float Scale, int FrameNumber, int Layer = 0, Vector4? Clear = null);

/// <summary>What the last <see cref="PageRenderer.AddPasses(RenderGraph, GraphTexture, IReadOnlyList{PageScreen}, PageScopes, in PageFrame)"/> did.</summary>
public readonly record struct PageStats(int Passes, int Instances, int Layers, int Dropped, int MissingImages, int RefusedGlyphs, int PathBitmaps, int ArtPages, int GlyphPages);

/// <summary>One display list to draw and the images it names.</summary>
internal readonly record struct PageDraw(DisplayList List, PageImages? Images);

/// <summary>
/// The GPU side of pages: turns display lists into passes of a render graph. One per device and target format. It keeps the textures, pipelines
/// and buffers it needs between frames, so a frame that changes nothing allocates nothing.
/// </summary>
public sealed class PageRenderer : IDisposable
{
    internal const int MaxInstances = 1 << 20;
    internal const int MaxClips = 4096;
    private const int MaxSvgKeys = 512;

    private readonly GpuDevice _device;
    private readonly PageWalker _walker;
    private readonly List<PagePlan> _plans = [];
    private readonly List<PagePass> _passes = [];
    private readonly Dictionary<(ulong Key, uint Color), (string Id, int W, int H)> _svgs = [];
    private readonly List<PageDraw> _draws = [];
    private int _planUsed;
    private int _passUsed;
    private int _frame = -1;
    private int _layers;
    private int _instances;
    private int _passCount;

    private PageRenderer(GpuDevice device, GpuFormat targetFormat, GlyphAtlas glyphs, ArtAtlas art)
    {
        _device = device;
        Gpu = new PageGpu(device, targetFormat);
        Textures = new PageTextures(Gpu, glyphs, art);
        Ramps = new GradientRamps(Gpu);
        Coverage = new PathCoverageCache(Gpu);
        BlankGroup = Gpu.Sampled(Gpu.Blank);
        _walker = new PageWalker(this);
    }

    /// <summary>Creates a renderer for targets of one format.</summary>
    /// <param name="device">The device.</param>
    /// <param name="targetFormat">The format of the targets pages are drawn onto; an sRGB format.</param>
    /// <param name="glyphs">The game's glyph atlas; the renderer registers OneRender's font faces in it.</param>
    /// <param name="art">The game's art atlas, which holds page images and SVG.</param>
    public static PageRenderer Create(GpuDevice device, GpuFormat targetFormat, GlyphAtlas glyphs, ArtAtlas art)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(glyphs);
        ArgumentNullException.ThrowIfNull(art);
        return new PageRenderer(device, targetFormat, glyphs, art);
    }

    internal PageGpu Gpu { get; }

    internal PageTextures Textures { get; }

    internal GradientRamps Ramps { get; }

    internal PathCoverageCache Coverage { get; }

    internal GpuBindGroup BlankGroup { get; }

    internal int MissingImages { get; set; }

    internal int RefusedGlyphs { get; set; }

    /// <summary>What the most recent call drew.</summary>
    public PageStats Stats { get; private set; }

    /// <summary>
    /// Adds the passes that draw one layer of the screens onto <paramref name="target"/>, in the order given. Without <see cref="PageFrame.Clear"/> the
    /// target must already have been written by an earlier pass.
    /// </summary>
    /// <param name="graph">The frame's graph, already begun.</param>
    /// <param name="target">The target as a graph resource.</param>
    /// <param name="screens">The shown screens, back to front.</param>
    /// <param name="scopes">What the host scopes mean.</param>
    /// <param name="frame">The frame.</param>
    public void AddPasses(RenderGraph graph, GraphTexture target, IReadOnlyList<PageScreen> screens, PageScopes scopes, in PageFrame frame)
    {
        ArgumentNullException.ThrowIfNull(screens);
        _draws.Clear();
        for (var i = 0; i < screens.Count; i++)
        {
            if (screens[i].Visible && screens[i].Draw is { } d)
            {
                _draws.Add(d);
            }
        }

        AddPasses(graph, target, _draws, scopes, frame);
    }

    internal void AddPasses(RenderGraph graph, GraphTexture target, IReadOnlyList<PageDraw> draws, PageScopes scopes, in PageFrame frame)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(scopes);
        if (frame.FrameNumber != _frame)
        {
            _frame = frame.FrameNumber;
            _planUsed = 0;
            _passUsed = 0;
            _layers = 0;
            _instances = 0;
            _passCount = 0;
            MissingImages = 0;
            RefusedGlyphs = 0;
            Textures.Glyphs.BeginFrame(frame.FrameNumber);
            Textures.Art.BeginFrame(frame.FrameNumber);
            Ramps.BeginFrame(frame.FrameNumber);
            Coverage.BeginFrame(frame.FrameNumber);
        }

        var plan = RentPlan();
        plan.Reset(frame.Width, frame.Height);
        plan.Root = plan.NewSlot();
        for (var i = 0; i < draws.Count; i++)
        {
            _walker.Walk(plan, draws[i].List, draws[i].Images, scopes, frame.Scale, frame.Layer);
            plan.Root = _walker.Cur;
        }

        Textures.Sync();
        if (plan.StepCount == 0 || !plan.SlotWritten[plan.Root])
        {
            if (frame.Clear is { } c)
            {
                AddClearOnly(graph, target, c);
            }

            Stats = new PageStats(_passCount, _instances, _layers, plan.Dropped, MissingImages, RefusedGlyphs, Coverage.Count, Textures.ArtPageCount, Textures.GlyphPageCount);
            return;
        }

        // The page is presented by one more quad over the whole target.
        var presentIndex = plan.InstanceCount;
        ref var pi = ref plan.AddInstance();
        pi.Rect = new Vector4(0, 0, frame.Width, frame.Height);
        pi.M = new Vector4(1, 0, 0, 1);
        Upload(plan);

        var handles = plan.Handles;
        if (handles.Length < plan.SlotCount)
        {
            handles = plan.Handles = new GraphTexture[Math.Max(plan.SlotCount, handles.Length * 2)];
        }

        for (var s = 0; s < plan.SlotCount; s++)
        {
            handles[s] = graph.CreateTexture(SlotName(s), new TextureDesc((uint)frame.Width, (uint)frame.Height, PageGpu.LayerFormat, TextureUsage.RenderTarget | TextureUsage.Sampled));
        }

        for (var s = 0; s < plan.StepCount; s++)
        {
            var pass = RentPass();
            pass.Set(this, plan, s, default, null);
            graph.AddPass(pass);
        }

        var present = RentPass();
        present.Set(this, plan, -1, target, frame.Clear);
        present.PresentInstance = presentIndex;
        graph.AddPass(present);

        _layers += plan.SlotCount;
        _instances += plan.InstanceCount;
        _passCount += plan.StepCount + 1;
        Stats = new PageStats(_passCount, _instances, _layers, plan.Dropped, MissingImages, RefusedGlyphs, Coverage.Count, Textures.ArtPageCount, Textures.GlyphPageCount);
    }

    /// <summary>Registers an SVG item's markup in the art atlas, with <c>currentColor</c> resolved, once per key and colour.</summary>
    internal bool TryRegisterSvg(ulong key, string markup, PaintColor color, out string id, out int width, out int height)
    {
        var packed = ((uint)ToByte(color.R) << 24) | ((uint)ToByte(color.G) << 16) | ((uint)ToByte(color.B) << 8) | ToByte(color.A);
        if (_svgs.TryGetValue((key, packed), out var known))
        {
            (id, width, height) = known;
            return true;
        }

        id = "";
        width = height = 0;
        if (_svgs.Count >= MaxSvgKeys)
        {
            return false;
        }

        var hex = string.Create(7, packed, static (span, p) =>
        {
            span[0] = '#';
            const string digits = "0123456789abcdef";
            for (var i = 0; i < 3; i++)
            {
                var b = (int)((p >> (24 - (i * 8))) & 0xFF);
                span[1 + (i * 2)] = digits[b >> 4];
                span[2 + (i * 2)] = digits[b & 15];
            }
        });
        var text = markup.Replace("currentColor", hex, StringComparison.OrdinalIgnoreCase);
        var name = $"page-svg:{key:x16}:{packed:x8}";
        try
        {
            Textures.Art.AddSvg(name, Encoding.UTF8.GetBytes(text));
            if (!Textures.Art.TryGetNaturalSize(name, out width, out height))
            {
                return false;
            }
        }
        catch (AtlasAssetException)
        {
            return false;
        }

        id = name;
        _svgs[(key, packed)] = (name, width, height);
        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _device.WaitIdle();
        foreach (var p in _plans)
        {
            p.InstanceBuffer?.Dispose();
            if (p.ClipBuffer is { } c)
            {
                Gpu.ForgetFrame(c);
                c.Dispose();
            }
        }

        _plans.Clear();
        Coverage.Dispose();
        Ramps.Dispose();
        Textures.Dispose();
        Gpu.Dispose();
    }

    private static byte ToByte(float v) => (byte)Math.Clamp((int)MathF.Round(v * 255f), 0, 255);

    private static readonly string[] SlotNames = Enumerable.Range(0, 128).Select(i => "page.layer " + i).ToArray();

    private static string SlotName(int slot) => slot < SlotNames.Length ? SlotNames[slot] : "page.layer";

    private PagePlan RentPlan()
    {
        if (_planUsed == _plans.Count)
        {
            _plans.Add(new PagePlan());
        }

        return _plans[_planUsed++];
    }

    private PagePass RentPass()
    {
        if (_passUsed == _passes.Count)
        {
            _passes.Add(new PagePass());
        }

        return _passes[_passUsed++];
    }

    private void AddClearOnly(RenderGraph graph, GraphTexture target, Vector4 clear)
    {
        var pass = RentPass();
        pass.Set(this, null, -2, target, clear);
        graph.AddPass(pass);
    }

    private void Upload(PagePlan plan)
    {
        var bytes = (ulong)plan.InstanceCount * PageInstance.Size;
        if (plan.InstanceBuffer is null || plan.InstanceCapacity < bytes)
        {
            if (plan.InstanceBuffer is not null)
            {
                _device.WaitIdle();
                plan.InstanceBuffer.Dispose();
            }

            plan.InstanceCapacity = (ulong)Math.Max(1024, BitOperations.RoundUpToPowerOf2((uint)Math.Min(bytes, int.MaxValue / 2)));
            plan.InstanceBuffer = _device.CreateBuffer(new BufferDesc(plan.InstanceCapacity, BufferUsage.Vertex | BufferUsage.CopyDestination) { Label = "page instances" });
        }

        _device.WriteBuffer(plan.InstanceBuffer, 0, MemoryMarshal.AsBytes(plan.Instances.AsSpan(0, plan.InstanceCount)));

        var clipBytes = (ulong)Math.Max(plan.ClipCount, 1) * 80;
        if (plan.ClipBuffer is null || plan.ClipCapacity < clipBytes)
        {
            if (plan.ClipBuffer is not null)
            {
                _device.WaitIdle();
                Gpu.ForgetFrame(plan.ClipBuffer);
                plan.ClipBuffer.Dispose();
            }

            plan.ClipCapacity = (ulong)Math.Max(80 * 16, BitOperations.RoundUpToPowerOf2((uint)clipBytes));
            plan.ClipBuffer = _device.CreateBuffer(new BufferDesc(plan.ClipCapacity, BufferUsage.Storage | BufferUsage.CopyDestination) { Label = "page clips" });
        }

        if (plan.ClipCount > 0)
        {
            _device.WriteBuffer(plan.ClipBuffer, 0, MemoryMarshal.AsBytes(plan.Clips.AsSpan(0, plan.ClipCount * 5)));
        }
    }

    /// <summary>One pass of a plan. Reused every frame.</summary>
    private sealed class PagePass : IGraphPass
    {
        private PageRenderer _owner = null!;
        private PagePlan? _plan;
        private int _step;
        private GraphTexture _target;
        private Vector4? _clear;

        public int PresentInstance { get; set; }

        public string Name { get; private set; } = "page";

        public void Set(PageRenderer owner, PagePlan? plan, int step, GraphTexture target, Vector4? clear)
        {
            _owner = owner;
            _plan = plan;
            _step = step;
            _target = target;
            _clear = clear;
            Name = step >= 0 && plan is not null ? plan.Steps[step].Name : "page.present";
        }

        public void Setup(PassBuilder builder)
        {
            if (_step == -2)
            {
                var c = _clear!.Value;
                builder.ColorTarget(_target, LoadOp.Clear, c.X, c.Y, c.Z, c.W);
                return;
            }

            var plan = _plan!;
            if (_step < 0)
            {
                builder.Read(plan.Handles[plan.Root]);
                if (_clear is { } c)
                {
                    builder.ColorTarget(_target, LoadOp.Clear, c.X, c.Y, c.Z, c.W);
                }
                else
                {
                    builder.ColorTarget(_target, LoadOp.Load);
                }

                return;
            }

            ref var s = ref plan.Steps[_step];
            if (s.Kind != StepKind.Draw)
            {
                if (s.Src >= 0)
                {
                    builder.Read(plan.Handles[s.Src]);
                }

                if (s.Src2 >= 0)
                {
                    builder.Read(plan.Handles[s.Src2]);
                }
            }

            builder.ColorTarget(plan.Handles[s.Dst], s.Clear ? LoadOp.Clear : LoadOp.Load, 0, 0, 0, 0);
        }

        public void Execute(PassContext context)
        {
            var gpu = _owner.Gpu;
            var pass = context.Render;
            if (_step == -2)
            {
                return;
            }

            var plan = _plan!;
            pass.SetVertexBuffer(0, plan.InstanceBuffer!);
            pass.SetBindGroup(0, gpu.Frame((uint)plan.Width, (uint)plan.Height, plan.ClipBuffer!));
            if (_step < 0)
            {
                pass.SetPipeline(gpu.Present);
                pass.SetBindGroup(1, gpu.Sampled(context.Texture(plan.Handles[plan.Root])));
                pass.SetScissor(0, 0, (uint)plan.Width, (uint)plan.Height);
                pass.Draw(6, 1, 0, (uint)PresentInstance);
                return;
            }

            ref var s = ref plan.Steps[_step];
            if (s.Kind == StepKind.Draw)
            {
                pass.SetPipeline(gpu.Prim);
                for (var b = 0; b < s.BatchCount; b++)
                {
                    ref var batch = ref plan.Batches[s.BatchFirst + b];
                    if (batch.Sw <= 0 || batch.Sh <= 0)
                    {
                        continue;
                    }

                    pass.SetBindGroup(1, batch.Group);
                    pass.SetScissor(batch.Sx, batch.Sy, (uint)batch.Sw, (uint)batch.Sh);
                    pass.Draw(6, (uint)batch.Count, 0, (uint)batch.First);
                }

                return;
            }

            pass.SetPipeline(s.Kind switch
            {
                StepKind.Layer => gpu.LayerOver,
                StepKind.LayerAdd => gpu.LayerAdd,
                StepKind.Blend => gpu.Blend,
                StepKind.MaskCombine => gpu.MaskCombine,
                StepKind.MaskApply => gpu.MaskApply,
                StepKind.Color => gpu.Color,
                StepKind.Blur => gpu.Blur,
                StepKind.DropTint => gpu.DropTint,
                StepKind.Over => gpu.Over,
                StepKind.Backdrop => gpu.Backdrop,
                StepKind.Project => gpu.Project,
                _ => gpu.Encode,
            });
            var src = s.Src >= 0 ? context.Texture(plan.Handles[s.Src]) : gpu.Blank;
            var src2 = s.Src2 >= 0 ? context.Texture(plan.Handles[s.Src2]) : s.External;
            pass.SetBindGroup(1, gpu.Sampled(src, src2));
            pass.SetScissor(s.Sx, s.Sy, (uint)Math.Max(s.Sw, 0), (uint)Math.Max(s.Sh, 0));
            pass.Draw(6, 1, 0, (uint)s.Instance);
        }
    }
}
