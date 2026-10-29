using System;
using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Physics;
using OneGame.Platform;
using OneGame.UI;

namespace PhysicsExample;

/// <summary>The level: one world unit is a metre and is drawn 16 pixels wide, y up in the sim and down on screen.</summary>
public static class Level
{
    public const float Scale = 16f;

    /// <summary>Ground, a wall at each end and two platforms: centre x, centre y, width, height.</summary>
    public static readonly (int X, int Y, int W, int H)[] Solids =
    [
        (0, -5, 20, 1),     // ground, top at y = -4.5
        (-10, 0, 1, 12),    // left wall
        (10, 0, 1, 12),     // right wall
    ];

    /// <summary>Floating platforms in tenths of a unit, since a level is data and data is fixed-point: centre x, centre y, width, height.</summary>
    public static readonly (int X, int Y, int W, int H)[] Platforms =
    [
        (-30, -25, 40, 4),
        (40, -10, 40, 4),
    ];

    public static Fixed Tenths(int tenths) => Fixed.FromRatio(tenths, 10);
}

/// <summary>
/// The static geometry, a trigger on the high platform, and the debug drawing. The physics space belongs to the world (a hot table that
/// steps after the entities each tick); this entity only fills it at setup and counts what enters the trigger.
/// </summary>
[Entity]
public partial class Stage : Entity, IView2D
{
    /// <summary>The trigger's body: a box on the high platform that notices whatever enters it.</summary>
    [Synced] public partial BodyRef Beacon { get; internal set; }

    /// <summary>How many times something has entered the trigger.</summary>
    [Synced] public partial int Entered { get; private set; }

    private readonly PhysicsDebugShapes _shapes = new();

    internal void Build(PhysicsSpace2D space)
    {
        var solid = new BodyOptions { Kind = BodyKind.Static };
        foreach (var (x, y, w, h) in Level.Solids)
        {
            space.AddBox(new FVec2(w, h), new FVec2(x, y), solid);
        }

        foreach (var (x, y, w, h) in Level.Platforms)
        {
            space.AddBox(new FVec2(Level.Tenths(w), Level.Tenths(h)), new FVec2(Level.Tenths(x), Level.Tenths(y)), solid);
        }

        // A trigger has no response: things pass through it and the space reports them entering and leaving. This one is a goal on the ground.
        Beacon = space.AddBox(new FVec2(Level.Tenths(16), Fixed.One), new FVec2(7, -4), new BodyOptions { Kind = BodyKind.Static, IsTrigger = true });
    }

    protected override void OnTick()
    {
        // The events of the last step, in the same order on every node.
        foreach (var e in PhysicsSpace2D.Of(World!)!.Events)
        {
            if (e.Kind == PhysicsEventKind.TriggerEnter && e.Involves(Beacon))
            {
                Entered++;
            }
        }
    }

    [View]
    public void Draw(Draw2D d)
    {
        d.FillRect(new Rect(-160, -90, 320, 180), Color.Rgb(0x1d2733), 0, -3);
        foreach (var (x, y, w, h) in Level.Solids)
        {
            Block(d, x, y, w, h);
        }

        foreach (var (x, y, w, h) in Level.Platforms)
        {
            Block(d, x / 10f, y / 10f, w / 10f, h / 10f);
        }

        d.FillRect(new Rect(6.2f * Level.Scale, 3.5f * Level.Scale, 1.6f * Level.Scale, Level.Scale), Entered > 0 ? Color.Rgb(0xf2c14e) : Color.Rgb(0x2a6f5e), 0, -1);

        // The development overlay (F3) shows what the physics world holds: outlines, sleeping bodies, contacts.
        if (d.DebugVisible && PhysicsSpace2D.Of(World!) is { } space)
        {
            _shapes.Clear();
            space.AddDebugShapes(_shapes);
            foreach (var l in _shapes.Lines)
            {
                d.Line(new Vector2(l.From.X * Level.Scale, -l.From.Y * Level.Scale), new Vector2(l.To.X * Level.Scale, -l.To.Y * Level.Scale), Color.Rgba(l.Rgba), 1.5f, 9);
            }
        }
    }

    [View]
    private static void Block(Draw2D d, float x, float y, float w, float h) =>
        d.FillRect(new Rect((x - (w / 2)) * Level.Scale, (-y - (h / 2)) * Level.Scale, w * Level.Scale, h * Level.Scale), Color.Rgb(0x34424f), 0, -2);
}

/// <summary>A box or a ball. Its body lives in the physics space; the entity copies what the view draws into synced properties, so replicas can draw it too.</summary>
[Entity]
public partial class Crate : Entity, IView2D
{
    [Synced] public partial BodyRef Body { get; internal set; }

    [Synced] public partial bool Round { get; internal set; }

    [Synced] public partial FVec2 Position { get; private set; }

    [Synced] public partial Fixed Angle { get; private set; }

    protected override void OnTick()
    {
        var space = PhysicsSpace2D.Of(World!)!;
        Position = space.Position(Body);
        Angle = space.Angle(Body);
    }

    [View]
    public void Draw(Draw2D d)
    {
        var c = new Vector2(Position.X.ToSingle() * Level.Scale, -Position.Y.ToSingle() * Level.Scale);
        if (Round)
        {
            d.FillCircle(c, 0.4f * Level.Scale, Color.Rgb(0xe07a5f), 2);
        }
        else
        {
            d.FillRect(new Rect(c.X - (0.4f * Level.Scale), c.Y - (0.4f * Level.Scale), 0.8f * Level.Scale, 0.8f * Level.Scale), Color.Rgb(0xc9a26b), -Angle.ToSingle(), 2);
        }
    }
}

/// <summary>
/// The player: a character capsule. Input arrives as commands; the simulation turns it into a move each tick. The vertical speed is the
/// hero's own synced value (gravity and jumps are game rules); the physics space does the colliding, sliding, stepping and pushing.
/// </summary>
[Entity]
public partial class Hero : Entity, IView2D
{
    private static Fixed Speed => Fixed.FromInt(5);

    private static Fixed Gravity => Fixed.FromInt(25);

    private static Fixed JumpSpeed => Fixed.FromInt(11);

    [Synced] public partial CharacterRef Body { get; internal set; }

    [Synced] public partial FVec2 Position { get; private set; }

    [Synced] public partial Fixed VerticalSpeed { get; private set; }

    [Synced] public partial int Direction { get; private set; }

    [Synced] public partial bool Grounded { get; private set; }

    /// <summary>Holds a direction: -1 left, 0 stop, 1 right.</summary>
    [Command]
    public void Walk(int direction) => Direction = Math.Clamp(direction, -1, 1);

    /// <summary>Jumps if standing on something.</summary>
    [Command]
    public bool Jump()
    {
        if (!Grounded)
        {
            return false;
        }

        VerticalSpeed = JumpSpeed;
        return true;
    }

    protected override void OnTick()
    {
        var space = PhysicsSpace2D.Of(World!)!;
        var dt = space.StepSeconds;
        var vy = VerticalSpeed - (Gravity * dt);
        var move = space.Move(Body, new FVec2(Speed * Direction * dt, vy * dt));
        if ((move.Grounded && vy < Fixed.Zero) || (move.HitCeiling && vy > Fixed.Zero))
        {
            vy = Fixed.Zero;
        }

        VerticalSpeed = vy;
        Grounded = move.Grounded;
        Position = move.Position;
    }

    [View]
    public void Draw(Draw2D d)
    {
        var c = new Vector2(Position.X.ToSingle() * Level.Scale, -Position.Y.ToSingle() * Level.Scale);
        d.FillRect(new Rect(c.X - 4.8f, c.Y - 8, 9.6f, 16), Color.Rgb(0xf2c14e), 0, 3);
        d.FillCircle(new Vector2(c.X, c.Y - 4), 3, Color.Rgb(0xfff1c2), 4);
    }
}

public sealed class PhysicsGame : GameDefinition
{
    public override string Name => "Physics";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.PhysicsExample.EntityTypes.Register(registry);

    // The space is part of the world, so it is added where the world is built, before a save is loaded: the same on every node.
    public override void RegisterSystems(World world) => PhysicsSpace2D.AddTo(world);

    public override void Setup(World world)
    {
        var space = PhysicsSpace2D.Of(world)!;
        var stage = world.Add(new Stage());
        stage.Build(space);

        // Three crates and two balls to push around.
        var crate = new BodyOptions { Density = Fixed.One, Friction = Fixed.FromRatio(7, 10) };
        var ball = new BodyOptions { Density = Fixed.One, Restitution = Fixed.FromRatio(1, 2) };
        foreach (var (x, y) in new[] { (0, -41), (0, -33), (12, -41) })
        {
            world.Add(new Crate { Body = space.AddBox(new FVec2(Level.Tenths(8), Level.Tenths(8)), new FVec2(Level.Tenths(x), Level.Tenths(y)), crate) });
        }

        foreach (var x in new[] { 3, 5 })
        {
            world.Add(new Crate { Round = true, Body = space.AddCircle(Level.Tenths(4), new FVec2(x, Fixed.FromInt(2)), ball) });
        }

        world.Add(new Hero { Body = space.AddCharacter(new FVec2(-8, Level.Tenths(-40)), new CharacterOptions { Radius = Level.Tenths(3), Height = Fixed.One }) });
    }

    public override void DefineActions(ActionMap actions) => actions.Context("play", c => c
        .Vector("move", Bind.Arrows, Bind.Wasd, Bind.LeftStick)
        .Button("jump", Bind.Key(Key.Space), Bind.Pad(GamepadButton.South)));

    // Views read state and send commands: held keys become a direction, Space a jump.
    public override void OnEachFrame(GameInstance game)
    {
        var hero = (Hero)game.World.Entities[^1];
        var direction = (int)MathF.Round(game.Actions.Vector("move").X);
        if (direction != hero.Direction)
        {
            hero.Send.Walk(direction);
        }

        if (game.Actions.Pressed("jump"))
        {
            hero.Send.Jump();
        }
    }

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column());
}
