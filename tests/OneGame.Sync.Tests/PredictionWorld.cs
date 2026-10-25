namespace OneGame.Sync.Tests;

/// <summary>A runner on a track. It moves by its velocity every tick; players steer it, dash, and ask to be teleported.</summary>
[Entity]
public partial class Runner : Entity
{
    [Synced, OwnerKey] public partial uint Player { get; set; }

    [Synced] public partial FVec2 Position { get; set; }

    [Synced] public partial FVec2 Velocity { get; set; }

    [Synced] public partial int Energy { get; set; }

    [Synced] public partial int Steers { get; set; }

    /// <summary>Counts ticks on the authority only; no replica ever holds it.</summary>
    [Synced(Visibility = Visibility.Hidden)] public partial int Secret { get; set; }

    /// <summary>Whether the runner keeps a trail. Changing a list sends a whole new list, which allocates on the receiving side.</summary>
    [Synced] public partial bool Tracking { get; set; }

    [Synced] public SyncList<int> Trail { get; } = new();

    [Command(Predicted = true)]
    public void Steer(int dx, int dy)
    {
        Velocity = new FVec2(dx, dy);
        Steers++;
    }

    /// <summary>Spends energy for a jump forward. The authority rejects it when the energy is gone, which a replica may not know yet.</summary>
    [Command(Predicted = true)]
    public CommandReply Dash(int dx)
    {
        if (Energy < 20)
        {
            return CommandReply.Reject("too tired");
        }

        Energy -= 20;
        Position += new FVec2(dx, 0);
        return CommandReply.Ok;
    }

    /// <summary>Predicted, but refused before it runs when the sender does not own the runner.</summary>
    [Command(Predicted = true, OwnerOnly = true)]
    public void Teleport(int x, int y) => Position = new FVec2(x, y);

    /// <summary>Not predicted: the answer is only ever the authority's.</summary>
    [Command]
    public void Rest() => Energy += 50;

    protected override void OnTick()
    {
        Position += Velocity;
        Secret++;
        if (World!.Tick % 10 == 0)
        {
            Energy += 1;
        }

        if (Tracking && World.Tick % 16 == 0)
        {
            Trail.Add((int)World.Tick);
            if (Trail.Count > 8)
            {
                Trail.RemoveAt(0);
            }
        }
    }
}

/// <summary>Moves between two points over a span of ticks, set once. Nothing about it is sent while it moves.</summary>
[Entity]
public partial class Drifter : Entity
{
    [Synced] public partial Timed<FVec2> Glide { get; set; }

    [Synced] public partial int Moves { get; set; }

    [Command]
    public void GlideTo(int x, int y, int ticks)
    {
        var from = Glide.ValueAt(World!.Tick);
        Glide = new Timed<FVec2>(from, new FVec2(x, y), World.Tick, World.Tick + ticks);
        Moves++;
    }
}

/// <summary>One row of the track's marker table.</summary>
[HotData(Name = "markers")]
public struct MarkerRow
{
    public Fixed X { get; set; }

    public int Age { get; set; }
}

/// <summary>Ages the markers every tick.</summary>
public sealed class MarkerSystem(MarkerRowTable table) : ISimSystem
{
    public void Tick(World world)
    {
        var age = table.Age;
        for (var i = 0; i < age.Length; i++)
        {
            age[i]++;
        }
    }
}

/// <summary>Builds the worlds the prediction tests use.</summary>
internal static class Track
{
    public const ulong Seed = 99;

    public static EntityRegistry Registry()
    {
        var registry = new EntityRegistry();
        registry.Add(new Runner().TypeId, "Runner", static () => new Runner());
        registry.Add(new Drifter().TypeId, "Drifter", static () => new Drifter());
        return registry;
    }

    public static World NewWorld(WorldRole role)
    {
        var world = new World(Seed, Registry(), role);
        var table = world.AddTable(new MarkerRowTable());
        world.AddSystem(new MarkerSystem(table));
        return world;
    }

    /// <summary>An authority world with <paramref name="runners"/> runners (owners 1, 2, ...) and <paramref name="markers"/> marker rows.</summary>
    public static World NewAuthority(int runners = 4, int markers = 50)
    {
        var world = NewWorld(WorldRole.Authority);
        for (var i = 0; i < runners; i++)
        {
            world.Add(new Runner { Player = (uint)(i + 1), Position = new FVec2(i * 10, 0), Energy = 60, Secret = 7, Tracking = true });
        }

        var table = (MarkerRowTable)world.Tables[0];
        for (var i = 0; i < markers; i++)
        {
            table.Add(new MarkerRow { X = i, Age = 0 });
        }

        return world;
    }
}

/// <summary>
/// Delays every message by a fixed number of steps in each direction, for tests that need a known round trip. Advance <see cref="Now"/>
/// once per step.
/// </summary>
internal sealed class LagLine
{
    public long Now { get; set; }

    /// <summary>Called with the size of every message the client end receives.</summary>
    public Action<int>? OnClientReceive { get; set; }

    public (IMessageChannel Client, IMessageChannel Server) Create(int stepsEachWay)
    {
        var (a, b) = Loopback.CreatePair();
        return (new End(this, a, stepsEachWay, true), new End(this, b, stepsEachWay, false));
    }

    private sealed class End(LagLine line, IMessageChannel inner, int delay, bool isClient) : IMessageChannel
    {
        private readonly Queue<(long Due, byte[] Bytes)> _in = new();
        private byte[] _current = [];

        public void Send(ReadOnlySpan<byte> message) => inner.Send(message);

        public bool TryReceive(out ReadOnlySpan<byte> message)
        {
            while (inner.TryReceive(out var m))
            {
                _in.Enqueue((line.Now + delay, m.ToArray()));
            }

            if (_in.Count > 0 && _in.Peek().Due <= line.Now)
            {
                _current = _in.Dequeue().Bytes;
                if (isClient)
                {
                    line.OnClientReceive?.Invoke(_current.Length);
                }

                message = _current;
                return true;
            }

            message = default;
            return false;
        }
    }
}
