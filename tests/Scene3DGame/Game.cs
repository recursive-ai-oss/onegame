using System;
using System.IO;
using OneGame.Core;
using OneGame.Gltf;
using OneGame.Host;
using OneGame.Render3D;
using OneGame.UI;

namespace Scene3DGame;

/// <summary>A small 3D scene made of entities: a floor, three boxes (one standing, two circling), a camera, a lamp, and a label that follows a box.</summary>
public sealed class Scene3DGameDefinition : GameDefinition
{
    /// <summary>Builds the scene from the world. One game definition drives one game.</summary>
    public SceneView3D View { get; } = CreateView();

    public override string Name => "Scene3D";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.Scene3DGame.EntityTypes.Register(registry);

    public override void Setup(World world)
    {
        world.Add(new Slab()).Init(new Transform3(new FVec3(0, Fixed.FromRatio(-1, 20), 0), FQuat.Identity, new FVec3(8, Fixed.FromRatio(1, 10), 8)));
        world.Add(new Crate()).Init(new FVec3(-2, Fixed.Half, 0), Fixed.Zero, Fixed.Zero, Fixed.One);
        world.Add(new Crate()).Init(new FVec3(1, Fixed.Half, 0), Fixed.FromInt(2), Fixed.FromRatio(1, 40), Fixed.FromRatio(3, 4));
        world.Add(new Crate()).Init(new FVec3(0, Fixed.FromRatio(3, 2), -2), Fixed.One, Fixed.FromRatio(-1, 30), Fixed.Half);
        world.Add(new Rig()).Init(new FVec3(0, 3, 8), new FVec3(0, Fixed.Half, 0), Fixed.FromInt(40));
        world.Add(new Lamp()).Init(new FVec3(-2, 4, 3));
    }

    public override void DefineFonts(OneGame.Text.FontLibrary fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        fonts.Add(Read("fonts/NotoSans-Regular.ttf"));
    }

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", game => Ui.Column(
        Ui.Label(() => "tick " + game.World.Tick).Named("hud"),
        Ui.Label("orbiter").Named("tag").Padding(Length.Px(4)).Background(Color.Rgb(0x203040)).TextColor(Color.White)
            .PinToWorld(View, game, () => game.World.Find(new EntityId(3)) is Crate c ? c.Transform.Position + new FVec3(0, 1, 0) : null))
        .Padding(Length.Px(8)).TextColor(Color.White));

    public override IRenderStage CreateRenderStage(HostResources resources)
    {
        ArgumentNullException.ThrowIfNull(resources);
        return resources.Fonts is not null && resources.Shaders is not null ? new SceneStage(resources, View) : new StubStage();
    }

    private static SceneView3D CreateView()
    {
        var view = new SceneView3D();
        view.Models.Add("box", GltfLoader.Load(Read("models/Box.glb")));
        return view;
    }

    private static byte[] Read(string name)
    {
        using var stream = typeof(Scene3DGameDefinition).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"The program has no embedded file '{name}'.");
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    private sealed class SceneStage(HostResources resources, SceneView3D view) : Render3DStage(resources, view)
    {
        protected override void UpdateScene(Scene3D scene, in FrameInfo frame)
        {
            // A fixed backdrop and one sample per pixel keep captures identical from run to run.
            scene.Environment.Background = BackgroundMode.Color;
            scene.Environment.BackgroundColor = new System.Numerics.Vector3(0.02f, 0.03f, 0.05f);
            scene.Environment.Intensity = 0.25f;
            scene.Samples = 1;
        }
    }
}
