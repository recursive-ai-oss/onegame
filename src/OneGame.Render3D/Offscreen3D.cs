using System.Threading.Tasks;
using OneGame.Gpu;
using OneGame.Gpu.Graph;

namespace OneGame.Render3D;

/// <summary>
/// Renders a scene into an offscreen colour image and reads it back: the capture path for tools and tests. It owns a render graph and a
/// target texture of a given size.
/// </summary>
public sealed class Offscreen3D : IDisposable
{
    private readonly Renderer3D _renderer;
    private readonly RenderGraph _graph;

    /// <summary>Creates a target of <paramref name="width"/> x <paramref name="height"/> pixels.</summary>
    /// <param name="renderer">The renderer to draw with.</param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="format"><c>Rgba8UnormSrgb</c> (default), <c>Rgba8Unorm</c> or <c>Rgba16Float</c>.</param>
    public Offscreen3D(Renderer3D renderer, int width, int height, GpuFormat format = GpuFormat.Rgba8UnormSrgb)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        _renderer = renderer;
        Width = width;
        Height = height;
        Format = format;
        _graph = new RenderGraph(renderer.Device);
        Target = renderer.Device.CreateTexture(new TextureDesc((uint)width, (uint)height, format, TextureUsage.RenderTarget | TextureUsage.CopySource) { Label = "3d capture" });
    }

    /// <summary>Width in pixels.</summary>
    public int Width { get; }

    /// <summary>Height in pixels.</summary>
    public int Height { get; }

    /// <summary>Format of <see cref="Target"/>.</summary>
    public GpuFormat Format { get; }

    /// <summary>The image that was drawn.</summary>
    public GpuTexture Target { get; }

    /// <summary>The render graph the frame ran on (for inspection).</summary>
    public RenderGraph Graph => _graph;

    /// <summary>Draws one frame and submits it.</summary>
    /// <param name="scene">What to draw.</param>
    public void Render(Scene3D scene)
    {
        _graph.BeginFrame();
        var target = _graph.ImportTexture("target", Target);
        _renderer.Render(_graph, scene, target, Width, Height, Format);
        _graph.Execute();
    }

    /// <summary>Draws one frame and reads it back as tightly packed RGBA bytes (sRGB-encoded for the default format).</summary>
    /// <param name="scene">What to draw.</param>
    /// <returns><c>Width * Height * 4</c> bytes, top row first.</returns>
    public byte[] Capture(Scene3D scene)
    {
        Render(scene);
        if (Format == GpuFormat.Rgba16Float)
        {
            throw new InvalidOperationException("Capture returns 8-bit pixels; use Render and read the float target yourself.");
        }

        var bytes = new byte[Width * Height * 4];
        _renderer.Device.ReadTexture(Target, 0, 0, bytes);
        return bytes;
    }

    /// <summary>
    /// Like <see cref="Capture"/>, for backends that cannot block on a read (a browser): the frame is drawn now and the read completes later.
    /// </summary>
    /// <param name="scene">What to draw.</param>
    /// <returns><c>Width * Height * 4</c> bytes, top row first.</returns>
    public async ValueTask<byte[]> CaptureAsync(Scene3D scene)
    {
        Render(scene);
        if (Format == GpuFormat.Rgba16Float)
        {
            throw new InvalidOperationException("Capture returns 8-bit pixels; use Render and read the float target yourself.");
        }

        var bytes = new byte[Width * Height * 4];
        await _renderer.Device.ReadTextureAsync(Target, 0, 0, bytes).ConfigureAwait(true);
        return bytes;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _renderer.Device.WaitIdle();
        _graph.Dispose();
        Target.Dispose();
    }
}
