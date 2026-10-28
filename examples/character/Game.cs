using System;
using System.IO;
using OneGame.Core;
using OneGame.Gltf;
using OneGame.Host;
using OneGame.Render3D;
using OneGame.UI;

namespace CharacterExample;

/// <summary>
/// The figure's model and everything loaded from it: the rig, the root motion of each clip as fixed-point steps per tick, and the animator
/// a view plays. Loaded once, before any world exists; sim code reads only the baked tracks.
/// </summary>
public static class Figure
{
    /// <summary>The imported model: skeleton, skin and the clips idle, walk, run and jump.</summary>
    public static GltfModel Model { get; } = GltfLoader.Load(Read("models/figure.glb"));

    /// <summary>The skeleton and clip lookup.</summary>
    public static Rig Rig { get; } = new(Model);

    /// <summary>The ground, a plain tiled floor.</summary>
    public static GltfModel Ground { get; } = GltfLoader.Load(Read("models/ground.glb"));

    /// <summary>Root movement of one walk cycle, per tick. Baked from the clip, so the sim moves the figure by exactly what its feet do.</summary>
    public static RootMotionTrack Walk { get; } = RootMotion.Bake(Rig.Clip("walk"), Rig.Node("Root"), GameInstance.TickRate);

    /// <summary>Root movement of one run cycle, per tick.</summary>
    public static RootMotionTrack Run { get; } = RootMotion.Bake(Rig.Clip("run"), Rig.Node("Root"), GameInstance.TickRate);

    /// <summary>Root movement of the jump, per tick.</summary>
    public static RootMotionTrack Jump { get; } = RootMotion.Bake(Rig.Clip("jump"), Rig.Node("Root"), GameInstance.TickRate);

    /// <summary>The animator for one figure: locomotion blends idle, walk and run by "speed", and a trigger plays the jump.</summary>
    public static Animator CreateAnimator()
    {
        var animator = new Animator(Rig, Rig.Node("Root"));
        animator.AddFloat("speed").AddTrigger("jump");
        animator.AddState("move", new BlendSpace1DMotion(new BlendSpace1D(0, 1, 2), Rig.Clip("idle"), Rig.Clip("walk"), Rig.Clip("run")) { XParameter = "speed" });
        animator.AddState("jump", new ClipMotion(Rig.Clip("jump")), loop: false);
        animator.Transition("move", "jump").WhenTrigger("jump").Fade(0.08f);
        animator.Transition("jump", "move").AtExit(0.9f).Fade(0.1f);
        return animator;
    }

    internal static byte[] Read(string name)
    {
        using var stream = typeof(Figure).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"The program has no embedded file '{name}'.");
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }
}

/// <summary>
/// The figure. The sim moves it with the baked root motion of whichever clip it is playing (walk, run or jump); the view blends the clips by
/// the gait and plays the jump when the jump counter changes. Animation never writes state: the animator lives in a [View] field.
/// </summary>
[Entity]
public partial class Hero : Entity, IView3D
{
    [Synced] public partial Transform3 Transform { get; private set; }

    /// <summary>0 stands, 1 walks, 2 runs.</summary>
    [Synced] public partial int Gait { get; private set; }

    /// <summary>Ticks spent in the current gait: where in the cycle the root motion is.</summary>
    [Synced] public partial int GaitTick { get; private set; }

    /// <summary>True while a jump is under way.</summary>
    [Synced] public partial bool Airborne { get; private set; }

    /// <summary>Ticks since the jump began.</summary>
    [Synced] public partial int JumpTick { get; private set; }

    /// <summary>How many jumps there have been; a view starts the jump clip when it changes.</summary>
    [Synced] public partial int Jumps { get; private set; }

    [View] private Animator? _animator;
    [View] private int _seenJumps;

    /// <summary>Picks stand, walk or run.</summary>
    [Command]
    public void SetGait(int gait)
    {
        Gait = Math.Clamp(gait, 0, 2);
        GaitTick = 0;
    }

    /// <summary>Jumps, unless already in the air.</summary>
    [Command]
    public void Jump()
    {
        if (!Airborne)
        {
            Airborne = true;
            JumpTick = 0;
            Jumps++;
        }
    }

    /// <summary>Turns the figure to the left by some degrees (negative turns right).</summary>
    [Command]
    public void Turn(int degrees)
        => Transform = Transform.WithRotation(FQuat.FromAxisAngle(FVec3.UnitY, Fixed.Pi * Fixed.FromRatio(degrees, 180)) * Transform.Rotation);

    protected override void OnTick()
    {
        if (Airborne)
        {
            Transform = Figure.Jump.Step(Transform, JumpTick, loop: false);
            JumpTick++;
            if (JumpTick >= Figure.Jump.Length)
            {
                Airborne = false;
                JumpTick = 0;
            }
        }
        else if (Gait > 0)
        {
            Transform = (Gait == 2 ? Figure.Run : Figure.Walk).Step(Transform, GaitTick);
            GaitTick++;
        }
    }

    [View]
    public void Describe3D(SceneWriter scene)
    {
        _animator ??= Figure.CreateAnimator();
        _animator.MoveFloat("speed", Gait, 4f);
        if (Jumps != _seenJumps)
        {
            _seenJumps = Jumps;
            _animator.Trigger("jump");
        }

        // Time comes from the tick, so a capture after N ticks always shows the same pose.
        _animator.UpdateTo((scene.Tick + scene.Alpha.ToSingle()) / GameInstance.TickRate);

        // The sim knows exactly how far into the jump the figure is. A view that missed ticks (a capture after a long step, a restore) cuts to it.
        if (Airborne)
        {
            var progress = (JumpTick + scene.Alpha.ToSingle()) / Figure.Jump.Length;
            if (_animator.CurrentState != "jump" || MathF.Abs(_animator.StateProgress - progress) > 0.1f)
            {
                _animator.Play("jump", progress);
            }
        }

        scene.Model("figure", Transform, _animator.Pose);

        // A side-on camera that follows the figure on the ground plane.
        var at = new FVec3(Transform.Position.X, Fixed.Zero, Transform.Position.Z);
        scene.Camera(Transform3.LookAt(at + new FVec3(5, Fixed.FromRatio(3, 2), 0), at + new FVec3(0, 1, 0)), 38f);
    }
}

/// <summary>The floor and the light.</summary>
[Entity]
public partial class Stage : Entity, IView3D
{
    [View]
    public void Describe3D(SceneWriter scene)
    {
        scene.Model("ground", Transform3.Identity, pickable: false);
        scene.DirectionalLight(Transform3.LookAt(FVec3.Zero, new FVec3(-1, -2, -1)), new System.Numerics.Vector3(1f, 0.96f, 0.9f), 2.2f);
    }
}

public sealed class CharacterGame : GameDefinition
{
    /// <summary>Builds the scene from the world. One game definition drives one game.</summary>
    public SceneView3D View { get; } = CreateView();

    public override string Name => "Character";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.CharacterExample.EntityTypes.Register(registry);

    public override void Setup(World world)
    {
        world.Add(new Stage());
        world.Add(new Hero());
    }

    public override void DefineFonts(OneGame.Text.FontLibrary fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        fonts.Add(Figure.Read("fonts/NotoSans-Regular.ttf"));
    }

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", game => Ui.Column(
        Ui.Label(() => game.World.Find(new EntityId(2)) is Hero h ? $"{(h.Airborne ? "jump" : h.Gait switch { 0 => "idle", 1 => "walk", _ => "run" })}  z {h.Transform.Position.Z.ToSingle():0.0} m" : string.Empty).Named("hud"))
        .Padding(Length.Px(8)).TextColor(Color.White));

    public override IRenderStage CreateRenderStage(HostResources resources)
    {
        ArgumentNullException.ThrowIfNull(resources);
        return resources.Fonts is not null && resources.Shaders is not null ? new SceneStage(resources, View) : new StubStage();
    }

    private static SceneView3D CreateView()
    {
        var view = new SceneView3D();
        view.Models.Add("figure", Figure.Model);
        view.Models.Add("ground", Figure.Ground);
        return view;
    }

    private sealed class SceneStage(HostResources resources, SceneView3D view) : Render3DStage(resources, view)
    {
        protected override void UpdateScene(Scene3D scene, in FrameInfo frame)
        {
            // A fixed backdrop and one sample per pixel keep captures identical from run to run.
            scene.Environment.Background = BackgroundMode.Color;
            scene.Environment.BackgroundColor = new System.Numerics.Vector3(0.10f, 0.14f, 0.20f);
            scene.Environment.Intensity = 0.45f;
            scene.Samples = 1;
        }
    }
}
