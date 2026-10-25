namespace OneGame.Sync.Tests;

/// <summary>A traveller that counts the hits it takes: the unit the hand-off tests move between game servers and count exactly.</summary>
[Entity]
public partial class Traveler : Entity
{
    [Synced, OwnerKey] public partial uint Player { get; set; }

    [Synced] public partial string Name { get; set; }

    [Synced] public partial int Hits { get; set; }

    [Synced(Visibility = Visibility.Owner)] public partial string Diary { get; set; }

    [Synced(Visibility = Visibility.Hidden)] public partial int Secret { get; set; }

    [Command]
    public void Hit() => Hits++;

    [Command(OwnerOnly = true)]
    public void Write(string text) => Diary = text;
}

/// <summary>Scenery that changes every tick, so hashes move and a missed frame cannot hide.</summary>
[Entity]
public partial class Beacon : Entity
{
    [Synced] public partial int Pulse { get; set; }

    protected override void OnTick() => Pulse += 1;
}

/// <summary>Builds the worlds of the hand-off, relay and trade tests.</summary>
internal static class Realm
{
    public static EntityRegistry Registry()
    {
        var registry = new EntityRegistry();
        registry.Add(new Traveler().TypeId, "Traveler", static () => new Traveler());
        registry.Add(new Beacon().TypeId, "Beacon", static () => new Beacon());
        return registry;
    }

    /// <summary>An authority world with a transfer log, or a bare replica world.</summary>
    public static World NewWorld(WorldRole role, ulong seed = 11)
    {
        var world = new World(seed, Registry(), role);
        if (role == WorldRole.Authority)
        {
            TransferLog.AddTo(world);
        }

        return world;
    }

    public static Traveler? FindTraveler(World world, uint player)
    {
        foreach (var e in world.Entities)
        {
            if (e is Traveler t && t.Player == player)
            {
                return t;
            }
        }

        return null;
    }

    public static int CountTravelers(World world, uint player)
    {
        var n = 0;
        foreach (var e in world.Entities)
        {
            if (e is Traveler t && t.Player == player)
            {
                n++;
            }
        }

        return n;
    }
}

/// <summary>Wraps one end of a message pipe with switches that make the other node look dead: nothing sent is delivered, and what was already queued vanishes.</summary>
internal sealed class GateChannel(IMessageChannel inner, Func<bool> blockedSend, Func<bool> blockedReceive) : IMessageChannel
{
    public int Dropped { get; private set; }

    public void Send(ReadOnlySpan<byte> message)
    {
        if (blockedSend())
        {
            Dropped++;
            return;
        }

        inner.Send(message);
    }

    public bool TryReceive(out ReadOnlySpan<byte> message)
    {
        while (inner.TryReceive(out message))
        {
            if (!blockedReceive())
            {
                return true;
            }

            Dropped++;
        }

        message = default;
        return false;
    }
}

/// <summary>Switches for a link between two nodes: which direction currently loses everything.</summary>
internal sealed class Wire
{
    /// <summary>Messages from the first node to the second are lost.</summary>
    public bool CutForward { get; set; }

    /// <summary>Messages from the second node to the first are lost.</summary>
    public bool CutBackward { get; set; }

    public bool Dead
    {
        set
        {
            CutForward = value;
            CutBackward = value;
        }
    }

    /// <summary>Joins <paramref name="client"/> to <paramref name="server"/> over a loopback pair whose directions this wire can cut.</summary>
    public Link Connect(Node client, Node server, Trust trust, string? name = null)
    {
        var (a, b) = Loopback.CreatePair();
        var clientEnd = new GateChannel(a, () => CutForward, () => CutBackward);
        var serverEnd = new GateChannel(b, () => CutBackward, () => CutForward);
        return client.ConnectTo(server, trust, clientEnd, serverEnd, name);
    }
}
