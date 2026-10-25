using OneGame.Gpu;
using OneGame.Gpu.Graph;
using OneGame.Render3D;

namespace OneGame.Host;

/// <summary>
/// A render stage that draws a 3D scene first and the 2D scene and UI over it. A game returns it from
/// <see cref="GameDefinition.CreateRenderStage"/>, uploads its models in <see cref="RendererCreated(Renderer3D)"/> and moves its camera, models
/// and lights in <see cref="UpdateScene"/>.
/// </summary>
public class Render3DStage : Render2DStage
{
    private Renderer3D? _renderer;

    /// <summary>Creates the stage.</summary>
    /// <param name="resources">What the host provides: fonts, shaders, art.</param>
    public Render3DStage(HostResources resources)
        : base(resources)
    {
    }

    /// <summary>The scene drawn every frame. Fill it in <see cref="UpdateScene"/>.</summary>
    public Scene3D Scene { get; } = new();

    /// <summary>The 3D renderer, once the first frame has created it.</summary>
    public Renderer3D? Renderer => _renderer;

    /// <summary>Called once, on the first frame, with the new renderer: upload models and environments here.</summary>
    /// <param name="renderer">The renderer.</param>
    protected virtual void RendererCreated(Renderer3D renderer)
    {
    }

    /// <summary>Called every frame before the scene is drawn: set the camera, move instances, add or remove lights.</summary>
    /// <param name="scene">The scene to update.</param>
    /// <param name="frame">The frame being drawn.</param>
    protected virtual void UpdateScene(Scene3D scene, in FrameInfo frame)
    {
    }

    /// <inheritdoc />
    protected override bool AddScenePasses(GpuDevice device, RenderGraph graph, GraphTexture target, GpuTexture targetTexture, in FrameInfo frame)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(targetTexture);
        if (_renderer is null)
        {
            _renderer = new Renderer3D(device);
            RendererCreated(_renderer);
        }

        UpdateScene(Scene, frame);
        _renderer.Render(graph, Scene, target, (int)targetTexture.Width, (int)targetTexture.Height, targetTexture.Format);
        return true;
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        // Models and environments the game uploaded must be disposed by the game first; the renderer then releases the rest.
        _renderer?.Dispose();
        _renderer = null;
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
