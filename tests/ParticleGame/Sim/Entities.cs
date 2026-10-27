using System;
using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Particles;
using OneGame.Render2D;
using OneGame.UI;

namespace ParticleGame;

/// <summary>Switches for tests. Particles are view-only, so turning them off must change no simulation hash.</summary>
public static class Fx
{
    /// <summary>False makes the views draw nothing from particles (and not even create emitters).</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>Multiplies the capacities and rates of the steady emitters, for the allocation tests.</summary>
    public static int Scale { get; set; } = 1;

    public static ParticleSettings Sparks(ParticleSpace space) => new()
    {
        Space = space,
        Capacity = 256 * Scale,
        Rate = 90 * Scale,
        Prewarm = 1.5f,
        Lifetime = new FloatRange(0.6f, 1.0f),
        Speed = space == ParticleSpace.Plane ? new FloatRange(120, 200) : new FloatRange(3.5f, 5f),
        Direction = space == ParticleSpace.Plane ? -Vector3.UnitY : Vector3.UnitY,
        Spread = 0.35f,
        Gravity = space == ParticleSpace.Plane ? new Vector3(0, 320, 0) : new Vector3(0, -9, 0),
        Size = space == ParticleSpace.Plane ? new FloatRange(8, 14) : new FloatRange(0.10f, 0.16f),
        SizeOverLife = ParticleCurve.Linear(1f, 0.3f),
        Color = ParticleGradient.Of((0f, ParticleGradient.Rgb(0xfff2b0)), (0.4f, ParticleGradient.Rgb(0xff9a30)), (1f, ParticleGradient.Rgb(0xd02010, 0f))),
        Emission = 3f,
        Blend = ParticleBlend.Additive,
    };

    public static ParticleSettings Smoke(ParticleSpace space) => new()
    {
        Space = space,
        Capacity = 128 * Scale,
        Rate = 14 * Scale,
        Prewarm = 3f,
        Lifetime = new FloatRange(2f, 3f),
        Speed = space == ParticleSpace.Plane ? new FloatRange(15, 30) : new FloatRange(0.3f, 0.6f),
        Direction = space == ParticleSpace.Plane ? -Vector3.UnitY : Vector3.UnitY,
        Spread = 0.3f,
        Drag = 0.5f,
        Size = space == ParticleSpace.Plane ? new FloatRange(18, 26) : new FloatRange(0.4f, 0.6f),
        SizeOverLife = ParticleCurve.Linear(0.5f, 1.6f),
        Rotation = new FloatRange(0, 6.28f),
        AngularVelocity = new FloatRange(-0.5f, 0.5f),
        Color = ParticleGradient.Of((0f, ParticleGradient.Rgb(0x9aa4b0, 0f)), (0.15f, ParticleGradient.Rgb(0x9aa4b0, 0.55f)), (1f, ParticleGradient.Rgb(0x56606c, 0f))),
        Blend = ParticleBlend.Alpha,
    };

    public static ParticleSettings Pop(ParticleSpace space) => new()
    {
        Space = space,
        Capacity = 64,
        Rate = 0,
        Lifetime = new FloatRange(0.7f, 1.1f),
        Speed = space == ParticleSpace.Plane ? new FloatRange(60, 160) : new FloatRange(1.5f, 3.5f),
        Spread = MathF.PI,
        Gravity = space == ParticleSpace.Plane ? new Vector3(0, 200, 0) : new Vector3(0, -5, 0),
        Drag = 1f,
        Size = space == ParticleSpace.Plane ? new FloatRange(14, 20) : new FloatRange(0.15f, 0.25f),
        SizeOverLife = ParticleCurve.Of((0f, 0.5f), (0.2f, 1f), (1f, 0.2f)),
        Color = ParticleGradient.FadeOut(Vector4.One),
        SheetColumns = space == ParticleSpace.Plane ? 2 : 1,
        SheetRows = space == ParticleSpace.Plane ? 2 : 1,
        Emission = 1.5f,
        Blend = ParticleBlend.Alpha,
    };

    /// <summary>Four coloured spots in a 2 x 2 sheet: red, green, blue and yellow.</summary>
    public static byte[] SheetPixels()
    {
        const int cell = 16;
        var px = new byte[2 * cell * 2 * cell * 4];
        uint[] colours = [0xff4040, 0x40ff40, 0x4080ff, 0xffe040];
        for (var f = 0; f < 4; f++)
        {
            for (var y = 0; y < cell; y++)
            {
                for (var x = 0; x < cell; x++)
                {
                    var dx = (x + 0.5f - (cell / 2f)) / (cell / 2f);
                    var dy = (y + 0.5f - (cell / 2f)) / (cell / 2f);
                    var a = Math.Clamp((1f - MathF.Sqrt((dx * dx) + (dy * dy))) * 3f, 0f, 1f);
                    var i = ((((f / 2 * cell) + y) * 2 * cell) + (f % 2 * cell) + x) * 4;
                    px[i] = (byte)(colours[f] >> 16);
                    px[i + 1] = (byte)(colours[f] >> 8);
                    px[i + 2] = (byte)colours[f];
                    px[i + 3] = (byte)(a * 255f);
                }
            }
        }

        return px;
    }
}

/// <summary>A place that sprays sparks and smoke, and pops on command, drawn in 2D. Position and pop count are synced; the particles are not.</summary>
[Entity]
public partial class Spout : Entity, IView2D
{
    [Synced] public partial Fixed X { get; private set; }

    [Synced] public partial int Pops { get; private set; }

    /// <summary>Counts a pop; the view notices the change and bursts.</summary>
    [Command]
    public void Pop() => Pops++;

    /// <summary>Moves the spout sideways.</summary>
    [Command]
    public void MoveTo(Fixed x) => X = x;

    private ParticleEmitter? _sparks;
    private ParticleEmitter? _smoke;
    private ParticleEmitter? _pop;
    private Texture2D? _sheet;
    private int _seen;

    [View]
    public void Draw(Draw2D d)
    {
        if (!Fx.Enabled)
        {
            return;
        }

        _sparks ??= new ParticleEmitter(Fx.Sparks(ParticleSpace.Plane), seed: 11, name: "sparks");
        _smoke ??= new ParticleEmitter(Fx.Smoke(ParticleSpace.Plane), seed: 12, name: "smoke");
        _pop ??= new ParticleEmitter(Fx.Pop(ParticleSpace.Plane), seed: 13, name: "pop");
        _sheet ??= d.Renderer!.CreateTexture(32, 32, Fx.SheetPixels());
        var x = X.ToSingle();
        if (Pops != _seen)
        {
            _seen = Pops;
            _pop.Burst(24, new Vector3(x, -50, 0));
        }

        d.Particles(_smoke, new Vector2(x + 90, 60), z: 1);
        d.Particles(_sparks, new Vector2(x, 60), z: 2);
        d.Particles(_pop, new Vector2(x, -50), _sheet, z: 3);
    }
}

/// <summary>The same spout in the 3D scene.</summary>
[Entity]
public partial class Spout3D : Entity, IView3D
{
    [Synced] public partial Fixed X { get; private set; }

    [Synced] public partial int Pops { get; private set; }

    /// <summary>Counts a pop; the view notices the change and bursts.</summary>
    [Command]
    public void Pop() => Pops++;

    /// <summary>Moves the spout sideways.</summary>
    [Command]
    public void MoveTo(Fixed x) => X = x;

    private ParticleEmitter? _sparks;
    private ParticleEmitter? _smoke;
    private ParticleEmitter? _pop;
    private int _seen;

    [View]
    public void Describe3D(SceneWriter scene)
    {
        if (!Fx.Enabled)
        {
            return;
        }

        _sparks ??= new ParticleEmitter(Fx.Sparks(ParticleSpace.Volume), seed: 11, name: "sparks");
        _smoke ??= new ParticleEmitter(Fx.Smoke(ParticleSpace.Volume), seed: 12, name: "smoke");
        _pop ??= new ParticleEmitter(Fx.Pop(ParticleSpace.Volume), seed: 13, name: "pop");
        var x = X.ToSingle();
        if (Pops != _seen)
        {
            _seen = Pops;
            _pop.Burst(24, new Vector3(x, 1.5f, 0));
        }

        scene.Particles(_smoke, new Vector3(x + 1.5f, 0.1f, 0));
        scene.Particles(_sparks, new Vector3(x, 0.1f, 0));
        scene.Particles(_pop, new Vector3(x, 1.5f, 0));
    }
}

/// <summary>A dark floor for the 2D variant.</summary>
[Entity]
public partial class Floor : Entity, IView2D
{
    [View]
    public void Draw(Draw2D d) => d.FillRect(new Rect(-400, 60, 800, 200), Color.Rgb(0x26303c));
}

/// <summary>A box placed in the 3D scene.</summary>
[Entity]
public partial class Block : Entity, IView3D
{
    [Synced] public partial Transform3 Transform { get; private set; }

    /// <summary>Sets the block up before the first tick.</summary>
    public void Init(Transform3 transform) => Transform = transform;

    [View]
    public void Describe3D(SceneWriter scene) => scene.Model("box", Transform, pickable: false);
}

/// <summary>The 3D camera.</summary>
[Entity]
public partial class Rig : Entity, IView3D
{
    [View]
    public void Describe3D(SceneWriter scene) => scene.Camera(Transform3.LookAt(new FVec3(0, Fixed.FromRatio(3, 2), 6), new FVec3(0, 1, 0)), 40f);
}

/// <summary>A sun.</summary>
[Entity]
public partial class Sun : Entity, IView3D
{
    [View]
    public void Describe3D(SceneWriter scene) =>
        scene.DirectionalLight(Transform3.LookAt(FVec3.Zero, new FVec3(-1, -3, -2)), new Vector3(1f, 0.96f, 0.9f), 1.2f);
}
