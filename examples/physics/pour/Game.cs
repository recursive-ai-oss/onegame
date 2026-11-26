using System;
using System.Collections.Generic;
using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Physics;
using OneGame.UI;

namespace PhysicsPourExample;

/// <summary>One world unit is a metre and is drawn 16 pixels wide, y up in the sim and down on screen.</summary>
public static class Level
{
    public const float Scale = 16f;
}

/// <summary>
/// A jug of liquid with a gate in its side, and a basin below. Opening the gate lets the liquid pour out in a stream and pool on the floor.
/// The liquid is many small particles (a thousand by default) drawn as one smooth surface by <see cref="FluidSurface2D"/>; the particles
/// and their solver live in the physics space, which is part of the world's saved and hashed state.
/// </summary>
[Entity]
public partial class Jug : Entity, IView2D
{
    [Synced] public partial BodyRef Gate { get; private set; }

    [Synced] public partial FluidRef Liquid { get; private set; }

    [Synced] public partial bool Open { get; private set; }

    /// <summary>Particles put into the jug at the start.</summary>
    [Synced] public partial int Filled { get; private set; }

    /// <summary>Particles in the basin, to the right of its divider.</summary>
    [Synced] public partial int InBasin { get; private set; }

    /// <summary>Particles still in the jug.</summary>
    [Synced] public partial int InJug { get; private set; }

    /// <summary>The particle spacing in thousandths of a metre, so the view can size its splats.</summary>
    [Synced] public partial int SpacingMilli { get; private set; }

    /// <summary>True to draw every particle as a dot instead of the smooth surface, to see what the solver is doing.</summary>
    [Synced] public partial bool Dots { get; private set; }

    private Vector2[] _points = new Vector2[256];

    private FluidSurface2D? _surface;

    internal void Build(PhysicsSpace2D space, Fixed spacing, int particles)
    {
        var solid = new BodyOptions { Kind = BodyKind.Static };

        // The basin: a floor and two walls (sizes are full widths and heights).
        space.AddBox(new FVec2(20, 1), new FVec2(0, -5), solid);
        space.AddBox(new FVec2(1, 12), new FVec2(-10, 0), solid);
        space.AddBox(new FVec2(1, 12), new FVec2(10, 0), solid);
        space.AddBox(new FVec2(Fixed.FromRatio(6, 10), Fixed.FromRatio(15, 10)), new FVec2(Fixed.FromRatio(-16, 10), Fixed.FromRatio(-375, 100)), solid);

        // The jug: a floor, a back wall, and a front wall with a gate in its lower part.
        // The floor tilts down toward the gate, so the last of the liquid runs out instead of resting against the lip.
        space.AddBox(new FVec2(Fixed.FromRatio(56, 10), 1), new FVec2(Fixed.FromRatio(-55, 10), 1), new BodyOptions { Kind = BodyKind.Static, Angle = Fixed.FromRatio(-1, 10) });
        space.AddBox(new FVec2(1, 5), new FVec2(Fixed.FromRatio(-84, 10), 3), solid);
        space.AddBox(new FVec2(1, Fixed.FromRatio(35, 10)), new FVec2(Fixed.FromRatio(-26, 10), Fixed.FromRatio(375, 100)), solid);
        Gate = space.AddBox(new FVec2(1, Fixed.FromRatio(8, 10)), new FVec2(Fixed.FromRatio(-26, 10), Fixed.FromRatio(16, 10)), solid);

        // Liquid, 5 by 2 metres in the jug. The spacing follows from how many particles were asked for.
        SpacingMilli = (spacing * 1000).RoundToInt();
        space.SetFluidSettings(new FluidWorldSettings { MaxParticles = particles + particles / 4 + 64 });
        Liquid = space.AddFluid(new FluidOptions { Preset = FluidPreset.Liquid, Spacing = spacing });
        space.FillFluidBox(Liquid, new FVec2(Fixed.FromRatio(-76, 10), Fixed.FromRatio(19, 10)), new FVec2(Fixed.FromRatio(-34, 10), Fixed.FromRatio(39, 10)));
        Filled = space.ParticleCount(Liquid);
    }

    /// <summary>Opens the gate.</summary>
    [Command]
    public bool Pour()
    {
        if (Open)
        {
            return false;
        }

        PhysicsSpace2D.Of(World!)!.Remove(Gate);
        Open = true;
        return true;
    }

    /// <summary>Switches between the smooth surface and one dot per particle.</summary>
    [Command]
    public void ToggleDots() => Dots = !Dots;

    protected override void OnTick()
    {
        var space = PhysicsSpace2D.Of(World!)!;
        InBasin = space.OverlapFluidBox(new FVec2(Fixed.FromRatio(-13, 10), -5), new FVec2(10, -2), default, Liquid);
        InJug = space.OverlapFluidBox(new FVec2(Fixed.FromRatio(-79, 10), Fixed.FromRatio(15, 10)), new FVec2(Fixed.FromRatio(-26, 10), 6), default, Liquid);
    }

    [View]
    public void Draw(Draw2D d)
    {
        var space = PhysicsSpace2D.Of(World!)!;
        // Fit the 20 by 11.25 metre scene to the screen, whatever its size.
        d.Camera.Zoom = MathF.Min(d.Width / 320f, d.Height / 180f);
        d.FillRect(new Rect(-1000, -1000, 2000, 2000), Color.Rgb(0x1d2733), 0, -3);
        Block(d, 0, -5, 20, 1);
        Block(d, -10, 0, 1, 12);
        Block(d, 10, 0, 1, 12);
        Block(d, -1.6f, -3.75f, 0.6f, 1.5f);
        Tilted(d, -5.5f, 1, 5.6f, 1, -0.1f);
        Block(d, -8.4f, 3, 1, 5);
        Block(d, -2.6f, 3.75f, 1, 3.5f);
        if (!Open)
        {
            Block(d, -2.6f, 1.6f, 1, 0.8f);
        }

        // A buffer made once, grown only when the liquid has more particles than it holds.
        var count = space.FluidParticleCount;
        if (_points.Length < count)
        {
            _points = new Vector2[count + (count / 2)];
        }

        var n = space.GetFluidPositions(_points);
        var spacing = SpacingMilli / 1000f * Level.Scale;
        _surface ??= new FluidSurface2D("liquid");
        _surface.Style.ParticleRadius = spacing * 1.0f;
        _surface.Style.Blur = spacing * 0.9f;
        _surface.Style.Threshold = 0.38f;
        if (Dots)
        {
            for (var i = 0; i < n; i++)
            {
                d.FillCircle(new Vector2(_points[i].X * Level.Scale, -_points[i].Y * Level.Scale), spacing * 0.5f, Color.Rgb(0x3b8fe0), 2);
            }

            _surface.Draw(d, default, Level.Scale);
        }
        else
        {
            _surface.Draw(d, new ReadOnlySpan<Vector2>(_points, 0, n), Level.Scale);
        }
    }

    [View]
    private static void Tilted(Draw2D d, float x, float y, float w, float h, float angle)
    {
        var c = new Vector2(x * Level.Scale, -y * Level.Scale);
        var half = new Vector2(w * Level.Scale / 2, 0);
        var cos = MathF.Cos(angle);
        var sin = MathF.Sin(angle);
        // The sim's y is up and the screen's is down, so a turn to the right-down in the sim is the same turn on screen.
        Vector2 P(float px, float py) => c + new Vector2((px * cos) - (py * sin), -((px * sin) + (py * cos)));
        var a = P(-half.X, 0);
        var b = P(half.X, 0);
        d.Line(a, b, Color.Rgb(0x34424f), h * Level.Scale, -2);
    }

    [View]
    private static void Block(Draw2D d, float x, float y, float w, float h) =>
        d.FillRect(new Rect((x - (w / 2)) * Level.Scale, (-y - (h / 2)) * Level.Scale, w * Level.Scale, h * Level.Scale), Color.Rgb(0x34424f), 0, -2);
}

public sealed class PourGame(int particles = PourGame.DefaultParticles) : GameDefinition
{
    public const int DefaultParticles = 1000;

    // The jug always holds 8.4 square metres of liquid, so the spacing is what makes the count: spacing = sqrt(8.4 / count), to 1/10,000.
    private readonly Fixed _spacing = Fixed.FromRatio((int)Math.Round(Math.Sqrt(8.4 / particles) * 10000), 10000);

    public override string Name => "PhysicsPour";

    public override IReadOnlyList<string> OptionNames => ["particles", "bench", "dots"];

    private Bench? _bench;

    // `--bench` pours the jug and prints how long the steps and frames took, then quits (see Bench).
    public override void OnEachFrame(GameInstance game)
    {
        if (game.Args.Has("bench"))
        {
            (_bench ??= new Bench()).OnFrame(game);
        }
    }

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.PhysicsPourExample.EntityTypes.Register(registry);

    // The space is part of the world, so it is added where the world is built, before a save is loaded: the same on every node.
    public override void RegisterSystems(World world) => PhysicsSpace2D.AddTo(world);

    public override void Setup(World world)
    {
        var jug = world.Add(new Jug());
        jug.Build(PhysicsSpace2D.Of(world)!, _spacing, particles);
    }

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column());
}
