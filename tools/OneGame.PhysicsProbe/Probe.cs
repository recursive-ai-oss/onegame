using System.Text;
using OneGame.Core;
using OneGame.Physics;

namespace OneGame.PhysicsProbe;

/// <summary>A scene that exercises every kind of contact the engine wraps, run for 900 ticks.</summary>
internal static class Probe
{
    public static string Run()
    {
        var world = new World(7, new EntityRegistry());
        var space = PhysicsSpace2D.AddTo(world);
        var still = new BodyOptions { Kind = BodyKind.Static };
        space.AddBox(new FVec2(60, 1), new FVec2(0, -1), still);
        space.AddBox(new FVec2(1, 20), new FVec2(-12, 8), still);
        space.AddBox(new FVec2(1, 20), new FVec2(12, 8), still);
        space.AddBox(new FVec2(Fixed.FromInt(5), Fixed.FromRatio(1, 2)), new FVec2(-4, Fixed.FromRatio(3, 2)), new BodyOptions { Kind = BodyKind.Static, Angle = Fixed.FromRatio(1, 5) });
        var trigger = space.AddBox(new FVec2(3, 3), new FVec2(6, 1), new BodyOptions { Kind = BodyKind.Static, IsTrigger = true });
        var pile = new List<BodyRef>();
        for (var i = 0; i < 12; i++)
        {
            pile.Add(space.AddBox(new FVec2(Fixed.FromRatio(9, 10), Fixed.FromRatio(11, 10)), new FVec2(-8 + (i % 4), 2 + (i / 4 * 2)), new BodyOptions { Friction = Fixed.FromRatio(7, 10) }));
        }

        var balls = new List<BodyRef>();
        for (var i = 0; i < 6; i++)
        {
            balls.Add(space.AddCircle(Fixed.FromRatio(2 + i, 5), new FVec2(2 + i, 6 + i), new BodyOptions { Restitution = Fixed.FromRatio(6, 10) }));
        }

        var hexagon = new FVec2[] { new(1, 0), new(Fixed.FromRatio(1, 2), Fixed.FromRatio(87, 100)), new(Fixed.FromRatio(-1, 2), Fixed.FromRatio(87, 100)), new(-1, 0), new(Fixed.FromRatio(-1, 2), Fixed.FromRatio(-87, 100)), new(Fixed.FromRatio(1, 2), Fixed.FromRatio(-87, 100)) };
        var poly = space.AddPolygon(hexagon, new FVec2(4, 9));
        var capsule = space.AddCapsule(Fixed.FromRatio(1, 2), Fixed.FromRatio(1, 4), new FVec2(0, 12));
        var hero = space.AddCharacter(new FVec2(-2, Fixed.FromRatio(11, 10)), new CharacterOptions { Height = Fixed.FromInt(2) });

        var text = new StringBuilder();
        var vy = Fixed.Zero;
        var dt = space.StepSeconds;
        var enters = 0;
        for (var t = 0; t < 900; t++)
        {
            var walk = t switch { < 200 => Fixed.FromInt(3), < 300 => Fixed.Zero, < 600 => Fixed.FromInt(-2), _ => Fixed.FromInt(4) };
            if (t is 210 or 640)
            {
                vy = Fixed.FromInt(9);
            }

            vy -= Fixed.FromInt(25) * dt;
            var move = space.Move(hero, new FVec2(walk * dt, vy * dt));
            if ((move.Grounded && vy < Fixed.Zero) || (move.HitCeiling && vy > Fixed.Zero))
            {
                vy = Fixed.Zero;
            }

            if (t == 450)
            {
                space.ApplyImpulse(pile[3], new FVec2(40, 10));
            }

            world.Step();
            foreach (var e in space.Events)
            {
                enters += e.Kind == PhysicsEventKind.TriggerEnter && e.Involves(trigger) ? 1 : 0;
            }

            if ((t + 1) % 50 == 0)
            {
                text.Append(t + 1).Append(' ').Append(world.ComputeHash().ToString("x16")).Append('\n');
            }
        }

        void Line(string name, FVec2 p) => text.Append(name).Append(' ').Append(p.X.Raw).Append(' ').Append(p.Y.Raw).Append('\n');
        Line("hero", space.Position(hero));
        Line("ball0", space.Position(balls[0]));
        Line("ball5", space.Position(balls[5]));
        Line("pile3", space.Position(pile[3]));
        Line("poly", space.Position(poly));
        Line("capsule", space.Position(capsule));
        text.Append("angle ").Append(space.Angle(pile[3]).Raw).Append('\n');
        text.Append("trigger-enters ").Append(enters).Append('\n');
        text.Append("space ").Append(space.Hash.ToString("x16")).Append('\n');
        return text.ToString();
    }
}
