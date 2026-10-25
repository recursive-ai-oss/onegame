using System.Numerics;
using OneGame.Control;
using OneGame.Core;
using OneGame.Gltf;
using OneGame.Host;
using OneGame.UI;

namespace OneGame.AotSmoke;

/// <summary>An entity that shows a box and a camera, to take the entity-to-3D path through the native publish.</summary>
[Entity]
public partial class Prop : Entity, IView3D
{
    [Synced] public partial Transform3 Transform { get; private set; }

    [Synced] public partial Timed<Transform3> Slide { get; private set; }

    [Synced] public partial bool IsCamera { get; private set; }

    public void Init(Transform3 transform, bool camera)
    {
        Transform = transform;
        IsCamera = camera;
    }

    [Command]
    public void Move(Fixed x) => Slide = new Timed<Transform3>(Transform, Transform.WithPosition(new FVec3(x, Fixed.Zero, Fixed.Zero)), World!.Tick, World.Tick + 4);

    protected override void OnTick()
    {
        if (Slide.EndTick > 0)
        {
            Transform = Slide.ValueAt(World!.Tick);
        }
    }

    [View]
    public void Describe3D(SceneWriter scene)
    {
        if (IsCamera)
        {
            scene.Camera(Transform, 50f);
            scene.DirectionalLight(Transform, Vector3.One, 2f);
        }
        else
        {
            scene.Model("box", Slide.EndTick > 0 ? Slide.ValueAt(scene.Tick, scene.Alpha) : Transform);
        }
    }
}

internal sealed class Scene3DSmokeGame(byte[] font, byte[] box) : GameDefinition
{
    public SceneView3D View { get; } = Create(box);

    public override string Name => "AotSmoke3D";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.OneGame_AotSmoke.EntityTypes.Register(registry);

    public override void Setup(World world)
    {
        world.Add(new Prop()).Init(Transform3.At(FVec3.Zero), false);
        world.Add(new Prop()).Init(Transform3.LookAt(new FVec3(0, 2, 6), FVec3.Zero), true);
    }

    public override void DefineFonts(OneGame.Text.FontLibrary fonts) => fonts.Add(font);

    public override void DefineScreens(ScreenSet screens) =>
        screens.Add("main", game => Ui.Column(Ui.Label("pinned").Named("pin").PinToWorld(View, game, () => new FVec3(0, 1, 0))));

    public override IRenderStage CreateRenderStage(HostResources resources) => new Render3DStage(resources, View);

    private static SceneView3D Create(byte[] box)
    {
        var view = new SceneView3D();
        view.Models.Add("box", GltfLoader.Load(box));
        return view;
    }
}

/// <summary>Entities, camera, projection, picking, world-anchored UI and the 3D control operations, with no graphics device.</summary>
internal static class Scene3DSmoke
{
    public static string? Run(byte[] font, byte[] box)
    {
        var definition = new Scene3DSmokeGame(font, box);
        using var game = GameInstance.Create(definition, new GameOptions { Headless = true, Width = 320, Height = 200, Gpu = GpuMode.Off });
        var surface = new ControlSurface(game, Path.GetTempPath());
        game.Step(5);
        var camera = surface.Call("camera");
        var pick = surface.Call("pick", """{"x":160,"y":100}""");
        var project = surface.Call("project", """{"point":[0,1,0]}""");
        surface.Call("camera", """{"position":[0,6,0.1],"target":[0,0,0]}""");
        var down = surface.Call("pick", """{"x":160,"y":100}""");
        surface.Call("camera", """{"reset":true}""");
        surface.Call("command_send", """{"entity":"Prop","command":"Move","args":["2"]}""");
        game.Step(6);
        var moved = surface.Call("pick", """{"x":160,"y":100}""");
        var shot = game.Capture(320, 200);
        if (!camera.Contains("\"fovDegrees\":50", StringComparison.Ordinal) || !pick.Contains("\"hit\":true", StringComparison.Ordinal)
            || !project.Contains("\"onScreen\":true", StringComparison.Ordinal) || !down.Contains("\"hit\":true", StringComparison.Ordinal)
            || moved.Contains("\"hit\":true", StringComparison.Ordinal) || shot.Rgba.Length != 320 * 200 * 4)
        {
            return $"camera {camera}; pick {pick}; project {project}; down {down}; moved {moved}";
        }

        return game.Ui.Find("pin") is { IsVisible: true } ? null : "the pinned label is not shown";
    }
}
