using OneGame.Atlas;
using OneGame.Gpu;
using OneGame.Gpu.Graph;
using OneGame.Render2D;
using OneGame.Text;
using OneGame.UI;

namespace OneGame.Host;

/// <summary>What the host has ready when it asks the game for a render stage.</summary>
/// <param name="Fonts">The game's fonts, or null when it has none.</param>
/// <param name="Measurer">Measures and shapes text with <paramref name="Fonts"/>; null when there are no fonts.</param>
/// <param name="Art">The atlas of SVG and image art the UI draws.</param>
/// <param name="Shaders">Where compiled renderer shaders come from; null when none are available.</param>
public sealed record HostResources(FontLibrary? Fonts, FontTextMeasurer? Measurer, ArtAtlas Art, IShaderSource? Shaders)
{
    /// <summary>Where a stage registers materials whose shader files may be edited while the game runs.</summary>
    public HotShaders HotShaders { get; init; } = new();

    /// <summary>The game's asset service; the 2D stage attaches its renderer so textures can be uploaded.</summary>
    public Assets? Assets { get; init; }
}

/// <summary>
/// The default render stage: the 2D renderer draws the UI display list into the host's target. The renderer is made on first use for
/// the target's format (which must be an sRGB format). With no usable GPU the host falls back to <see cref="DisplayListRaster"/>.
/// </summary>
public class Render2DStage : IRenderStage, IDisposable
{
    private static readonly OneGame.UI.Color ClearColor = new(StubStage.Clear.R, StubStage.Clear.G, StubStage.Clear.B, StubStage.Clear.A);

    private readonly HostResources _resources;
    private readonly GlyphAtlas _glyphs;
    private readonly TextQuadBuilder _text;
    private readonly Dictionary<GpuFormat, Renderer2D> _renderers = [];
    private readonly HashSet<GpuTexture> _seen = [];
    private readonly Render2DPass _pass = new();
    private readonly Draw2D _draw = new();
    private readonly OverlayCanvas _overlayCanvas = new();
    private RenderGraph? _graph;

    /// <summary>Creates the stage over the game's fonts, art and shaders.</summary>
    /// <exception cref="ArgumentException">The resources have no fonts or no shaders.</exception>
    public Render2DStage(HostResources resources)
    {
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
        if (resources.Fonts is null || resources.Measurer is null || resources.Shaders is null)
        {
            throw new ArgumentException("The 2D renderer needs fonts and shaders.", nameof(resources));
        }

        _draw.Assets = resources.Assets;
        _glyphs = new GlyphAtlas(resources.Fonts);
        _text = new TextQuadBuilder(resources.Measurer, _glyphs);
    }

    /// <summary>
    /// Immediate-mode 2D drawing. Every frame, before the UI, the stage asks each <see cref="IView2D"/> entity of the world to draw into it
    /// (and then <see cref="Draw2D.Describer"/>, for entities that cannot reference the host), and shows the result under the UI.
    /// </summary>
    public Draw2D Draw => _draw;

    /// <summary>Drawn over everything else in each frame, captures included; null for none. Development builds set it; a release build never does.</summary>
    public IFrameOverlay? Overlay { get; set; }

    /// <summary>Where this stage registers materials whose shader files may be edited while the game runs.</summary>
    protected HotShaders HotShaders => _resources.HotShaders;

    /// <summary>
    /// Called once for each renderer the stage creates (one per target format). A game that draws its own sprites creates its materials
    /// here and registers them with <see cref="HotShaders"/>.
    /// </summary>
    /// <param name="renderer">The new renderer.</param>
    protected virtual void RendererCreated(Renderer2D renderer)
    {
    }

    /// <summary>
    /// Adds passes that draw into the target before the 2D renderer and the UI, for a stage that draws a 3D scene. Return true when it did;
    /// the 2D pass then keeps the target's contents instead of clearing it.
    /// </summary>
    /// <param name="device">The graphics device.</param>
    /// <param name="graph">The frame's render graph, already begun.</param>
    /// <param name="target">The target as a graph resource.</param>
    /// <param name="targetTexture">The target texture.</param>
    /// <param name="frame">The frame being drawn.</param>
    protected virtual bool AddScenePasses(GpuDevice device, RenderGraph graph, GraphTexture target, GpuTexture targetTexture, in FrameInfo frame) => false;

    /// <summary>Called every frame after the UI is drawn, to add the game's own sprites and tiles.</summary>
    /// <param name="renderer">The renderer drawing this frame.</param>
    /// <param name="frame">The frame being drawn.</param>
    protected virtual void DrawOverUi(Renderer2D renderer, in FrameInfo frame)
    {
    }

    /// <inheritdoc />
    public virtual string Name => "render2d";

    /// <inheritdoc />
    public bool RequiresSrgb => true;

    /// <inheritdoc />
    public RenderGraph? Graph => _graph;

    /// <inheritdoc />
    public void Render(GpuDevice device, GpuTexture target, in FrameInfo frame)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(target);
        if (!_renderers.TryGetValue(target.Format, out var renderer))
        {
            renderer = Renderer2D.Create(device, target.Format, _resources.Shaders!);
            _renderers[target.Format] = renderer;
            if (_resources.Assets is { Renderer: null } assets)
            {
                assets.AttachRenderer(renderer);
            }

            RendererCreated(renderer);
        }

        _seen.Add(target);
        _graph ??= new RenderGraph(device);
        _graph.BeginFrame();
        _pass.Target = _graph.ImportTexture("target", target);

        // A scene stage draws first; the 2D pass then adds to what it left instead of clearing the target.
        var hasScene = AddScenePasses(device, _graph, _pass.Target, target, frame);
        renderer.BeginFrame(target, new FrameOptions { Clear = hasScene ? null : ClearColor });
        _draw.Draw(renderer, frame, _text, _resources.Measurer!, _glyphs);
        renderer.Ui.Draw(frame.List, _text, _glyphs, _resources.Art);
        DrawOverUi(renderer, frame);
        if (Overlay is { } overlay)
        {
            _overlayCanvas.Begin(renderer, _text, _glyphs, _resources.Measurer!, frame);
            overlay.Draw(_overlayCanvas);
            _overlayCanvas.End();
        }

        _pass.Renderer = renderer;
        _pass.Preserve = hasScene;
        _graph.AddPass(_pass);
        _graph.Execute();
        _pass.Renderer = null;
    }

    /// <summary>The 2D renderer's frame as one graph pass: it writes the whole target, and records its own render passes.</summary>
    private sealed class Render2DPass : IGraphPass
    {
        public Renderer2D? Renderer { get; set; }

        public GraphTexture Target { get; set; }

        public bool Preserve { get; set; }

        public string Name => "render2d";

        public void Setup(PassBuilder builder)
        {
            if (Preserve)
            {
                builder.ReadWrite(Target);
            }
            else
            {
                builder.Write(Target);
            }
        }

        public void Execute(PassContext context) => Renderer!.EndFrame(context.Encoder);
    }

    /// <inheritdoc />
    public void RenderToPixels(Span<byte> rgba, in FrameInfo frame)
    {
        for (var i = 0; i + 3 < rgba.Length; i += 4)
        {
            rgba[i] = StubStage.Clear.R;
            rgba[i + 1] = StubStage.Clear.G;
            rgba[i + 2] = StubStage.Clear.B;
            rgba[i + 3] = StubStage.Clear.A;
        }

        DisplayListRaster.Paint(rgba, frame.Width, frame.Height, frame.List);
    }

    /// <inheritdoc />
    public void PaintOver(Span<byte> rgba, in FrameInfo frame)
    {
    }

    /// <inheritdoc />
    public void ReleaseTargets(GpuTexture? only)
    {
        foreach (var t in only is null ? [.. _seen] : new[] { only })
        {
            if (_seen.Remove(t))
            {
                foreach (var r in _renderers.Values)
                {
                    r.ForgetTarget(t);
                }
            }
        }
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        foreach (var r in _renderers.Values)
        {
            r.Dispose();
        }

        _renderers.Clear();
        _graph?.Dispose();
        _graph = null;
        GC.SuppressFinalize(this);
    }
}
