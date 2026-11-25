using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Physics;
using OneGame.UI;

namespace PhysicsFluidExample;

/// <summary>One world unit is a metre and is drawn 16 pixels wide, y up in the sim and down on screen.</summary>
public static class Level
{
    public const float Scale = 16f;
}

/// <summary>
/// A cup of water held up by a pin, a ball sunk in it, and a drain on the floor. Pulling the pin lets the water fall into the drain.
/// The fluid, the pin and the drain live in the physics space (part of the world's saved and hashed state); this entity keeps the
/// numbers a game cares about in synced properties and the view draws every particle straight from the space.
/// </summary>
[Entity]
public partial class Tank : Entity, IView2D
{
    [Synced] public partial BodyRef Pin { get; private set; }

    [Synced] public partial BodyRef Ball { get; private set; }

    [Synced] public partial FluidRef Water { get; private set; }

    [Synced] public partial bool Pulled { get; private set; }

    /// <summary>Particles put into the cup at the start.</summary>
    [Synced] public partial int Filled { get; private set; }

    /// <summary>Particles that have reached the drain. A drain raises no event: sim code sees the fluid's particle count fall, which is also true after a save is loaded.</summary>
    [Synced] public partial int Drained { get; private set; }

    /// <summary>Particles still alive.</summary>
    [Synced] public partial int Remaining { get; private set; }

    /// <summary>Times the ball became wet.</summary>
    [Synced] public partial int BallWet { get; private set; }

    /// <summary>Times the ball became dry.</summary>
    [Synced] public partial int BallDry { get; private set; }

    /// <summary>Whether liquid touches the cup's rim region: true while any particle is within a unit of the middle of the cup.</summary>
    [Synced] public partial bool CupHasWater { get; private set; }

    private readonly FluidParticle[] _particles = new FluidParticle[2048];

    private readonly PhysicsDebugShapes _shapes = new();

    internal void Build(PhysicsSpace2D space)
    {
        var solid = new BodyOptions { Kind = BodyKind.Static };
        space.AddBox(new FVec2(20, 1), new FVec2(0, -5), solid);
        space.AddBox(new FVec2(1, 12), new FVec2(-10, 0), solid);
        space.AddBox(new FVec2(1, 12), new FVec2(10, 0), solid);

        // The cup: two walls and the pin they stand on.
        space.AddBox(new FVec2(Fixed.FromRatio(3, 10), Fixed.FromRatio(32, 10)), new FVec2(Fixed.FromRatio(-123, 20), Fixed.FromRatio(29, 20)), solid);
        space.AddBox(new FVec2(Fixed.FromRatio(3, 10), Fixed.FromRatio(32, 10)), new FVec2(Fixed.FromRatio(-17, 20), Fixed.FromRatio(29, 20)), solid);
        Pin = space.AddBox(new FVec2(5, Fixed.FromRatio(3, 10)), new FVec2(Fixed.FromRatio(-7, 2), Fixed.Zero), solid);

        // Water in the cup, 0.15 apart: a block on the pin.
        space.SetFluidSettings(new FluidWorldSettings { MaxParticles = 4000 });
        Water = space.AddFluid(new FluidOptions { Spacing = Fixed.FromRatio(15, 100) });
        space.FillFluidBox(Water, new FVec2(Fixed.FromRatio(-59, 10), Fixed.FromRatio(2, 10)), new FVec2(Fixed.FromRatio(-11, 10), Fixed.FromInt(2)));

        // The drain is a strip on the floor under the cup.
        space.AddDrain(Water, new FluidDrainOptions { Position = new FVec2(Fixed.FromRatio(-7, 2), Fixed.FromRatio(-42, 10)), Size = new FVec2(Fixed.FromRatio(27, 10), Fixed.FromRatio(3, 10)) });

        // A heavy ball that sinks into the water.
        Ball = space.AddCircle(Fixed.FromRatio(1, 2), new FVec2(Fixed.FromRatio(-7, 2), 3), new BodyOptions { Density = Fixed.FromRatio(3, 2) });
        Remaining = space.ParticleCount(Water);
        Filled = Remaining;
    }

    /// <summary>Pulls the pin out from under the water.</summary>
    [Command]
    public bool Pull()
    {
        if (Pulled)
        {
            return false;
        }

        PhysicsSpace2D.Of(World!)!.Remove(Pin);
        Pulled = true;
        return true;
    }

    protected override void OnTick()
    {
        var space = PhysicsSpace2D.Of(World!)!;
        Remaining = space.ParticleCount(Water);
        Drained = Filled - Remaining;

        // Wet and dry events of the last step, in the same order on every node.
        foreach (var e in space.FluidEvents)
        {
            if (e.Body == Ball)
            {
                if (e.Kind == FluidBodyEventKind.Entered)
                {
                    BallWet++;
                }
                else
                {
                    BallDry++;
                }
            }
        }

        // "Is liquid touching this spot": a count query with no buffer.
        CupHasWater = space.OverlapFluidCircle(new FVec2(Fixed.FromRatio(-7, 2), Fixed.FromRatio(11, 10)), Fixed.One, default, Water) > 0;
    }

    [View]
    public void Draw(Draw2D d)
    {
        d.FillRect(new Rect(-160, -90, 320, 180), Color.Rgb(0x1d2733), 0, -3);
        Block(d, 0, -5, 20, 1);
        Block(d, -10, 0, 1, 12);
        Block(d, 10, 0, 1, 12);
        Block(d, -6.15f, 1.45f, 0.3f, 3.2f);
        Block(d, -0.85f, 1.45f, 0.3f, 3.2f);
        if (!Pulled)
        {
            d.FillRect(new Rect(-6f * Level.Scale, -0.15f * Level.Scale, 5f * Level.Scale, 0.3f * Level.Scale), Color.Rgb(0xf2c14e), 0, -1);
        }

        d.FillRect(new Rect(-6.2f * Level.Scale, 3.9f * Level.Scale, 5.4f * Level.Scale, 0.6f * Level.Scale), Color.Rgb(0x6a3d2a), 0, -2);

        if (PhysicsSpace2D.Of(World!) is not { } space)
        {
            return;
        }

        // Every particle, copied into one buffer made once: a view allocates nothing per frame.
        var n = space.GetFluidParticles(_particles);
        var radius = 0.12f * Level.Scale;
        for (var i = 0; i < n; i++)
        {
            var c = new Vector2(_particles[i].Position.X.ToSingle() * Level.Scale, -_particles[i].Position.Y.ToSingle() * Level.Scale);
            d.FillCircle(c, radius, Color.Rgb(0x3b8fe0), 2);
        }

        var ball = space.Position(Ball);
        d.FillCircle(new Vector2(ball.X.ToSingle() * Level.Scale, -ball.Y.ToSingle() * Level.Scale), 0.5f * Level.Scale, Color.Rgb(0xe07a5f), 3);

        if (d.DebugVisible)
        {
            _shapes.Clear();
            space.AddDebugShapes(_shapes);
            foreach (var l in _shapes.Lines)
            {
                d.Line(new Vector2(l.From.X * Level.Scale, -l.From.Y * Level.Scale), new Vector2(l.To.X * Level.Scale, -l.To.Y * Level.Scale), Color.Rgba(l.Rgba), 1f, 9);
            }
        }
    }

    [View]
    private static void Block(Draw2D d, float x, float y, float w, float h) =>
        d.FillRect(new Rect((x - (w / 2)) * Level.Scale, (-y - (h / 2)) * Level.Scale, w * Level.Scale, h * Level.Scale), Color.Rgb(0x34424f), 0, -2);
}

public sealed class PhysicsFluidGame : GameDefinition
{
    public override string Name => "PhysicsFluid";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.PhysicsFluidExample.EntityTypes.Register(registry);

    // The space is part of the world, so it is added where the world is built, before a save is loaded: the same on every node.
    public override void RegisterSystems(World world) => PhysicsSpace2D.AddTo(world);

    public override void Setup(World world)
    {
        var tank = world.Add(new Tank());
        tank.Build(PhysicsSpace2D.Of(world)!);
    }

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column());
}
