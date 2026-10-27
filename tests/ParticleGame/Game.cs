using System;
using System.IO;
using OneGame.Core;
using OneGame.Gltf;
using OneGame.Host;
using OneGame.Render3D;
using OneGame.UI;

namespace ParticleGame;

/// <summary>
/// A spout of sparks, a plume of smoke and a burst on command, drawn with particles in 2D or 3D. The simulation is a counter and a position;
/// everything that moves is a view-side emitter, which <see cref="Fx.Enabled"/> can switch off to show that the simulation does not notice.
/// </summary>
public sealed class ParticleGameDefinition : GameDefinition
{
    private readonly bool _threeD;

    public ParticleGameDefinition(bool threeD = false)
    {
        _threeD = threeD;
        if (threeD)
        {
            View = new SceneView3D();
            View.Models.Add("box", GltfLoader.Load(Read("models/Box.glb")));
        }
    }

    /// <summary>The 3D view, in the 3D variant.</summary>
    public SceneView3D? View { get; }

    public override string Name => _threeD ? "Particles3D" : "Particles2D";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.ParticleGame.EntityTypes.Register(registry);

    public override void Setup(World world)
    {
        if (_threeD)
        {
            world.Add(new Spout3D());
            world.Add(new Rig());
            world.Add(new Sun());
            world.Add(new Block()).Init(new Transform3(new FVec3(0, Fixed.FromRatio(-1, 20), 0), FQuat.Identity, new FVec3(8, Fixed.FromRatio(1, 10), 8)));
            world.Add(new Block()).Init(new Transform3(new FVec3(Fixed.FromRatio(-3, 10), Fixed.FromRatio(2, 5), Fixed.FromRatio(3, 2)), FQuat.Identity, new FVec3(Fixed.FromRatio(3, 5), Fixed.FromRatio(4, 5), Fixed.FromRatio(1, 5))));
        }
        else
        {
            world.Add(new Spout());
            world.Add(new Floor());
        }
    }

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", game => Ui.Column().Padding(Length.Px(8)));

    public override void DefineFonts(OneGame.Text.FontLibrary fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        fonts.Add(Read("fonts/NotoSans-Regular.ttf"));
    }

    public override IRenderStage CreateRenderStage(HostResources resources)
    {
        ArgumentNullException.ThrowIfNull(resources);
        if (!_threeD)
        {
            return base.CreateRenderStage(resources);
        }

        return resources.Fonts is not null && resources.Shaders is not null ? new SceneStage(resources, View!) : new StubStage();
    }

    private static byte[] Read(string name)
    {
        using var stream = typeof(ParticleGameDefinition).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"The program has no embedded file '{name}'.");
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
