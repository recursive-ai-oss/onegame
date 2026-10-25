using OneGame.Core;

namespace Bricks.Tests;

/// <summary>
/// A player for tests: it serves, keeps the paddle under the first ball and meets the ball off-centre by a different amount each time,
/// so the bounces spread over the whole wall. It sends only commands, like any other player.
/// </summary>
internal static class Bot
{
    public static World NewWorld(ulong seed = 1)
    {
        var registry = new EntityRegistry();
        OneGame.Generated.Bricks.EntityTypes.Register(registry);
        var world = new World(seed, registry);
        world.Add(new Court());
        return world;
    }

    public static Court CourtOf(World world) => (Court)world.Entities[0];

    /// <summary>Plays one tick: decides, then steps the world.</summary>
    public static void Play(World world)
    {
        var court = CourtOf(world);
        if (court.Phase == CourtPhase.Serve)
        {
            court.Launch();
        }

        if (court.Phase == CourtPhase.Playing && court.Balls.Count > 0)
        {
            var lowest = court.Balls[0];
            for (var i = 1; i < court.Balls.Count; i++)
            {
                if (court.Balls[i].Y > lowest.Y)
                {
                    lowest = court.Balls[i];
                }
            }

            var offset = ((court.PaddleBounces * 37) % 61) - 30;
            court.Steer(lowest.X + (lowest.Vy > Fixed.Zero ? offset : 0));
        }

        world.Step();
    }
}
