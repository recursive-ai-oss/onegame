using Bricks;
using OneGame.Core;
using OneGame.Sync;

namespace OneGame.Corpus;

/// <summary>Bricks: a bot that serves, follows the lowest ball with a changing aim, nudges the paddle with keys now and then, and starts a new game on a random level when one ends.</summary>
internal sealed class BricksRun : SoloRun
{
    private int _offset;
    private int _lastBounces = -1;
    private long _nextAction;
    private long _pausedUntil;
    private long _lastSteer = long.MinValue / 2;

    public static void Run(CorpusOptions options, Sink sink) => new BricksRun().Execute(options, sink);

    protected override string Game => "bricks";

    protected override int SampleTicks => 3000;

    protected override void Register(EntityRegistry registry) =>
        new BricksGame(new GameServices { CreateStore = _ => new OneGame.Storage.MemoryByteStore() }).RegisterEntities(registry);

    protected override void Seed(World world) => world.Add(new Court());

    protected override void Play(ReplicaScope view, Rng rng, long tick)
    {
        if (view.World.Entities.Count == 0 || view.World.Entities[0] is not Court court || tick < _nextAction)
        {
            return;
        }

        if (court.Paused)
        {
            if (tick >= _pausedUntil)
            {
                court.Send.Pause(false);
                _nextAction = tick + 4;
            }

            return;
        }

        switch (court.Phase)
        {
            case CourtPhase.Idle or CourtPhase.Over:
                court.Send.NewGame(rng.Range(1, Levels.Count + 1));
                _nextAction = tick + 20 + rng.Range(0, 40);
                break;
            case CourtPhase.Serve:
                court.Send.Launch();
                _nextAction = tick + 10 + rng.Range(0, 30);
                break;
            case CourtPhase.Playing when court.Balls.Count > 0:
                var lowest = court.Balls[0];
                for (var i = 1; i < court.Balls.Count; i++)
                {
                    if (court.Balls[i].Y > lowest.Y)
                    {
                        lowest = court.Balls[i];
                    }
                }

                if (court.PaddleBounces != _lastBounces)
                {
                    _lastBounces = court.PaddleBounces;
                    _offset = rng.Range(-30, 31);
                }

                var aim = lowest.X + (lowest.Vy > Fixed.Zero ? Fixed.FromInt(_offset) : Fixed.Zero);
                if (Math.Abs(aim.Raw - _lastSteer) > 2 * Fixed.One.Raw)
                {
                    court.Send.Steer(aim);
                    _lastSteer = aim.Raw;
                }

                if (rng.OneIn(900))
                {
                    court.Send.Push(rng.Range(-100, 101));
                }
                else if (rng.OneIn(2500))
                {
                    court.Send.Pause(true);
                    _pausedUntil = tick + rng.Range(10, 60);
                }

                _nextAction = tick + 2;
                break;
            default:
                break;
        }
    }
}
