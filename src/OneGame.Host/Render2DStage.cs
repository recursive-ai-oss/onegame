using OneGame.Atlas;
using OneGame.Gpu;
using OneGame.Render2D;
using OneGame.Text;
using OneGame.UI;

namespace OneGame.Host;

/// <summary>What the host has ready when it asks the game for a render stage.</summary>
/// <param name="Fonts">The game's fonts, or null when it has none.</param>
/// <param name="Measurer">Measures and shapes text with <paramref name="Fonts"/>; null when there are no fonts.</param>
/// <param name="Art">The atlas of SVG and image art the UI draws.</param>
/// <param name="Shaders">Where compiled renderer shaders come from; null when none are available.</param>
public sealed record HostResources(FontLibrary? Fonts, FontTextMeasurer? Measurer, ArtAtlas Art, IShaderSource? Shaders);

/// <summary>
/// Reads the renderer's compiled shaders from a folder of <c>Program.Entry.spv</c> files (for example <c>Ui.VSMain.spv</c>), each with the
/// entry point <c>main</c>. It is the stand-in for the shader build package: set <c>ONEGAME_SHADER_DIR</c> to use it.
/// </summary>
public sealed class DirectoryShaders : IShaderSource
{
    private readonly string _directory;

    /// <summary>Creates a source reading from <paramref name="directory"/>.</summary>
    public DirectoryShaders(string directory)
    {
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
    }

    /// <inheritdoc />
    public ShaderCode Load(string program, string entryPoint)
    {
        var path = Path.Combine(_directory, program + "." + entryPoint + ".spv");
        var stage = entryPoint.StartsWith("VS", StringComparison.Ordinal) ? ShaderStage.Vertex : ShaderStage.Pixel;
        return new ShaderCode(stage, "main", File.ReadAllBytes(path));
    }
}

/// <summary>
/// The default render stage: the 2D renderer draws the UI display list into the host's target. The renderer is made on first use for
/// the target's format (which must be an sRGB format). With no usable GPU the host falls back to <see cref="DisplayListRaster"/>.
/// </summary>
public sealed class Render2DStage : IRenderStage, IDisposable
{
    private static readonly OneGame.UI.Color ClearColor = new(StubStage.Clear.R, StubStage.Clear.G, StubStage.Clear.B, StubStage.Clear.A);

    private readonly HostResources _resources;
    private readonly GlyphAtlas _glyphs;
    private readonly TextQuadBuilder _text;
    private readonly Dictionary<GpuFormat, Renderer2D> _renderers = [];
    private readonly HashSet<GpuTexture> _seen = [];

    /// <summary>Creates the stage over the game's fonts, art and shaders.</summary>
    /// <exception cref="ArgumentException">The resources have no fonts or no shaders.</exception>
    public Render2DStage(HostResources resources)
    {
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
        if (resources.Fonts is null || resources.Measurer is null || resources.Shaders is null)
        {
            throw new ArgumentException("The 2D renderer needs fonts and shaders.", nameof(resources));
        }

        _glyphs = new GlyphAtlas(resources.Fonts);
        _text = new TextQuadBuilder(resources.Measurer, _glyphs);
    }

    /// <inheritdoc />
    public string Name => "render2d";

    /// <inheritdoc />
    public bool RequiresSrgb => true;

    /// <inheritdoc />
    public void Render(GpuDevice device, GpuTexture target, in FrameInfo frame)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(target);
        if (!_renderers.TryGetValue(target.Format, out var renderer))
        {
            renderer = Renderer2D.Create(device, target.Format, _resources.Shaders!);
            _renderers[target.Format] = renderer;
        }

        _seen.Add(target);
        renderer.BeginFrame(target, new FrameOptions { Clear = ClearColor });
        renderer.Ui.Draw(frame.List, _text, _glyphs, _resources.Art);
        var enc = device.BeginCommands();
        renderer.EndFrame(enc);
        device.Submit(enc);
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
    public void Dispose()
    {
        foreach (var r in _renderers.Values)
        {
            r.Dispose();
        }

        _renderers.Clear();
    }
}
