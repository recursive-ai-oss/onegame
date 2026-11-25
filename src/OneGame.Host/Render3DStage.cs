using RecursiveAI.OneGpu;
using OneGame.Gpu.Graph;
using OneGame.Render3D;

namespace OneGame.Host;

/// <summary>What a render stage that draws a 3D scene shows, for tools that ask about it (the <c>camera</c> and <c>pick</c> operations).</summary>
public interface IScene3DStage
{
    /// <summary>The view that builds the scene from the world's <see cref="IView3D"/> entities; null for a stage whose game fills the scene by hand.</summary>
    SceneView3D? View { get; }

    /// <summary>
    /// The probe for a scene the game fills by hand, so the <c>camera</c>, <c>pick</c> and <c>project</c> operations work without a
    /// <see cref="SceneView3D"/>; null for a stage that has a view or draws no 3D scene.
    /// </summary>
    ManualScene3D? Manual => null;
}

/// <summary>Which environment of the scene an adopted <see cref="EnvironmentLoad"/> replaces.</summary>
public enum EnvironmentSlot
{
    /// <summary>The scene's <c>Environment.Map</c>: the picture changes at once when the load is ready.</summary>
    Map,

    /// <summary>The scene's <c>Environment.NextMap</c>: fade to it by raising <c>Environment.Blend</c>.</summary>
    NextMap,
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
    private readonly List<(EnvironmentLoad Load, EnvironmentSlot Slot)> _adopting = [];
    private readonly List<EnvironmentMap> _adopted = [];

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

    private ManualScene3D? _manual;

    /// <summary>
    /// For a stage built without a <see cref="View"/>: the probe the live <c>camera</c>, <c>pick</c> and <c>project</c> operations use on
    /// <see cref="Scene"/>. Null when there is a view (it answers instead). Give models a <see cref="ModelInstance3D.Label"/> to see them named in <c>pick</c>.
    /// </summary>
    public ManualScene3D? Manual => View is null ? _manual ??= new ManualScene3D(Scene) : null;

    /// <summary>The 3D renderer, once the first frame has created it.</summary>
    public Renderer3D? Renderer => _renderer;

    /// <summary>The newest load given to <see cref="AdoptEnvironment"/>, running, ready (and already swapped in) or failed; null before the first.</summary>
    public EnvironmentLoad? LatestEnvironmentLoad { get; private set; }

    /// <summary>The slot the newest adopted load fills.</summary>
    public EnvironmentSlot LatestEnvironmentSlot { get; private set; }

    /// <summary>
    /// Hands a running environment load to the stage: when it is ready the stage puts its map in <paramref name="slot"/> of <see cref="Scene"/> at the start of a frame,
    /// frees the map it replaces (when the stage made it) and owns the new one. The stage disposes what it owns with itself. A load that fails or is cancelled changes nothing.
    /// </summary>
    /// <param name="load">The load, from <see cref="Renderer3D.CreateEnvironmentAsync"/>.</param>
    /// <param name="slot">Which environment to replace.</param>
    public void AdoptEnvironment(EnvironmentLoad load, EnvironmentSlot slot = EnvironmentSlot.NextMap)
    {
        ArgumentNullException.ThrowIfNull(load);
        if (LatestEnvironmentLoad is { } previous && previous != load && previous.IsRunning)
        {
            previous.Cancel();
        }

        LatestEnvironmentLoad = load;
        LatestEnvironmentSlot = slot;
        _adopting.Add((load, slot));
    }

    private void SwapReadyEnvironments()
    {
        for (var i = 0; i < _adopting.Count; i++)
        {
            var (load, slot) = _adopting[i];
            if (load.IsRunning)
            {
                continue;
            }

            _adopting.RemoveAt(i--);
            if (!load.IsReady)
            {
                continue;
            }

            var map = load.Take();
            var env = Scene.Environment;
            var old = slot == EnvironmentSlot.Map ? env.Map : env.NextMap;
            if (slot == EnvironmentSlot.Map)
            {
                env.Map = map;
            }
            else
            {
                env.NextMap = map;
            }

            _adopted.Add(map);
            if (old is not null && _adopted.Remove(old))
            {
                _renderer!.RetireEnvironment(old);
            }
        }
    }

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

        SwapReadyEnvironments();
        if (View is { } view)
        {
            view.Prepare(frame.World, frame.Tick, frame.Alpha);
            view.Build(Scene, _renderer);
        }

        UpdateScene(Scene, frame);
        Manual?.ApplyOverride();
        _renderer.Render(graph, Scene, target, (int)targetTexture.Width, (int)targetTexture.Height, targetTexture.Format);
        return true;
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        // Models and environments the game uploaded must be disposed by the game first; the renderer then releases the rest.
        View?.Dispose();
        foreach (var load in _adopting)
        {
            load.Load.Dispose();
        }

        _adopting.Clear();
        foreach (var map in _adopted)
        {
            map.Dispose();
        }

        _adopted.Clear();
        _renderer?.Dispose();
        _renderer = null;
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
