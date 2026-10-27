using OneGame.Core;
using OneGame.Sync;

namespace OneGame.Corpus;

/// <summary>
/// A one-node game: an authority scope and one player node subscribed to it over a tapped loopback link. The bot plays through the replica, so
/// its commands travel the same way a client's do; the frames the authority sends to that link are the corpus's frames.
/// </summary>
internal abstract class SoloRun
{
    private const int SaveEvery = 40;
    private const int LateJoinEvery = 45;
    private const int RecordingTicks = 500;
    private const int MaxTicks = 400_000;

    protected abstract string Game { get; }

    protected abstract int SampleTicks { get; }

    protected abstract void Register(EntityRegistry registry);

    /// <summary>Adds the starting entities to the authority world.</summary>
    protected abstract void Seed(World world);

    /// <summary>One tick of the bot: reads the replica and sends commands.</summary>
    protected abstract void Play(ReplicaScope view, Rng rng, long tick);

    protected void Execute(CorpusOptions options, Sink sink)
    {
        var scopeId = new ScopeId(1);
        var cluster = new Cluster(new ClusterOptions { PieceBytes = options.PieceBytes, FatalDrift = true });
        var capture = new Capture(sink, Game, () => cluster.Tick);
        var rng = new Rng(Rng.Mix(options.Seed, 0x5010));

        World NewWorld(WorldRole role)
        {
            var registry = new EntityRegistry();
            Register(registry);
            return new World(Rng.Mix(options.Seed, 0xD1CE), registry, role);
        }

        var world = NewWorld(WorldRole.Authority);
        Seed(world);
        var host = cluster.AddNode("host");
        var scope = cluster.AddScope(host, scopeId, world);
        var player = cluster.AddNode("player");
        var (clientEnd, serverEnd) = Loopback.CreatePair();
        var link = player.ConnectTo(host, Trust.Player(scopeId), clientEnd, capture.ServerEnd(serverEnd));
        var view = player.Subscribe(link, scopeId, NewWorld(WorldRole.Replica));
        cluster.Settle();

        var archivist = new Archivist(cluster, capture, new Rng(Rng.Mix(options.Seed, 0xA4C)), [new Archivist.Target(scope, () => NewWorld(WorldRole.Replica), _ => Trust.Player(scopeId), "host")], options.Sample ? 400 : RecordingTicks);
        var ticks = options.Ticks ?? (options.Sample ? SampleTicks : MaxTicks);
        for (var tick = 1; tick <= ticks; tick++)
        {
            Play(view, rng, tick);
            cluster.Step();
            if (tick % SaveEvery == 0)
            {
                archivist.StoreSave(scope.Save());
            }

            if (tick % 200 == 0)
            {
                capture.EntitySchema = MaxSchema(scope.World);
            }

            archivist.Recordings();
            if (tick % LateJoinEvery == 0)
            {
                archivist.LateJoin();
            }

            if (options.Ticks is null && !options.Sample && sink.Full(Game, Kinds.Frames))
            {
                break;
            }
        }

        archivist.Finish();
        Console.Error.WriteLine($"{Game}: {cluster.Tick} ticks");
    }

    internal static uint MaxSchema(World world)
    {
        var max = 1u;
        foreach (var e in world.Entities)
        {
            max = Math.Max(max, e.SchemaVersion);
        }

        return max;
    }
}
