using OneGame.Gpu;
using OneGame.Gpu.Graph;
using OneGame.Render3D;

namespace OneGame.Host;

/// <summary>What a render stage that draws a 3D scene shows, for tools that ask about it (the <c>camera</c> and <c>pick</c> operations).</summary>
public interface IScene3DStage
{
    /// <summary>The view that builds the scene from the world's <see cref="IView3D"/> entities; null for a stage whose game fills the scene by hand.</summary>
    SceneView3D? View { get; }
}

/// <summary>
/// A render stage that draws a 3D scene first and the 2D scene and UI over it. A game returns it from
/// <see cref="GameDefinition.CreateRenderStage"/>. Two ways to fill the scene:
/// <list type="bullet">
/// <item>Pass a <see cref="SceneView3D"/>: the stage asks every <see cref="IView3D"/> entity of the world being drawn what it looks like, so a replica
/// shows what it holds. <see cref="UpdateScene"/> then runs on the built scene, for the environment, post settings and extra lights.</item>
/// <item>Pass none: upload models in <see cref="RendererCreated(Renderer3D)"/> and move the camera, models and lights in <see cref="UpdateScene"/>.</item>
/// </list>
/// </summary>
public class Render3DStage : Render2DStage, IScene3DStage
{
    private Renderer3D? _renderer;

    /// <summary>Creates the stage.</summary>
    /// <param name="resources">What the host provides: fonts, shaders, art.</param>
    /// <param name="view">Builds the scene from entities, or null to fill the scene by hand.</param>
    public Render3DStage(HostResources resources, SceneView3D? view = null)
        : base(resources)
    {
        View = view;
    }

    /// <inheritdoc />
    public override string Name => "render3d";

    /// <summary>The scene drawn every frame. With a <see cref="View"/> its camera, models and lights are rebuilt from the world each frame.</summary>
    public Scene3D Scene { get; } = new();

    /// <inheritdoc />
    public SceneView3D? View { get; }

    /// <summary>The 3D renderer, once the first frame has created it.</summary>
    public Renderer3D? Renderer => _renderer;

    /// <summary>Called once, on the first frame, with the new renderer: upload models and environments here.</summary>
    /// <param name="renderer">The renderer.</param>
    protected virtual void RendererCreated(Renderer3D renderer)
    {
    }

    /// <summary>Called every frame before the scene is drawn: set the environment and post settings, or (with no view) the camera, models and lights.</summary>
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

        if (View is { } view)
        {
            view.Prepare(frame.World, frame.Tick, frame.Alpha);
            view.Build(Scene, _renderer);
        }

        UpdateScene(Scene, frame);
        _renderer.Render(graph, Scene, target, (int)targetTexture.Width, (int)targetTexture.Height, targetTexture.Format);
        return true;
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        // Models and environments the game uploaded must be disposed by the game first; the renderer then releases the rest.
        View?.Dispose();
        _renderer?.Dispose();
        _renderer = null;
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
