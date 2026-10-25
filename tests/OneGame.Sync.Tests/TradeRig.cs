namespace OneGame.Sync.Tests;

/// <summary>A player's holdings on a game server: gold (kind 1) and swords (kind 2).</summary>
[Entity]
public partial class Purse : Entity
{
    [Synced, OwnerKey] public partial uint Player { get; set; }

    [Synced] public partial long Gold { get; set; }

    [Synced] public partial int Swords { get; set; }
}

internal sealed class PurseHandler : IEscrowHandler
{
    public const uint GoldKind = 1;
    public const uint SwordKind = 2;

    public bool TryDebit(World world, uint owner, uint kind, long amount)
    {
        if (Find(world, owner) is not { } p)
        {
            return false;
        }

        switch (kind)
        {
            case GoldKind when p.Gold >= amount:
                p.Gold -= amount;
                return true;
            case SwordKind when p.Swords >= amount:
                p.Swords -= (int)amount;
                return true;
            default:
                return false;
        }
    }

    public bool Credit(World world, uint owner, uint kind, long amount)
    {
        if (Find(world, owner) is not { } p)
        {
            return false;
        }

        switch (kind)
        {
            case GoldKind:
                p.Gold += amount;
                return true;
            case SwordKind:
                p.Swords += (int)amount;
                return true;
            default:
                return false;
        }
    }

    public static Purse? Find(World world, uint owner)
    {
        foreach (var e in world.Entities)
        {
            if (e is Purse p && p.Player == owner)
            {
                return p;
            }
        }

        return null;
    }
}

/// <summary>Drops, repeats and delays the command traffic of a channel (answers and refusals too), the way a bad network would, while the snapshot and frame stream stays in order.</summary>
internal sealed class ChaosChannel(IMessageChannel inner, Random rng, Func<long> clock, double drop, double duplicate, int maxDelay) : IMessageChannel
{
    private readonly List<(long At, byte[] Message)> _later = [];

    public int Dropped { get; private set; }

    public int Duplicated { get; private set; }

    public int Delayed { get; private set; }

    public void Send(ReadOnlySpan<byte> message)
    {
        var type = message[0];
        if (type is not (3 or 19 or 21))
        {
            inner.Send(message);
            return;
        }

        if (rng.NextDouble() < drop)
        {
            Dropped++;
            return;
        }

        var copies = rng.NextDouble() < duplicate ? 2 : 1;
        Duplicated += copies - 1;
        for (var i = 0; i < copies; i++)
        {
            var delay = maxDelay == 0 ? 0 : rng.Next(0, maxDelay + 1);
            if (delay == 0)
            {
                inner.Send(message);
            }
            else
            {
                Delayed++;
                _later.Add((clock() + delay, message.ToArray()));
            }
        }
    }

    public bool TryReceive(out ReadOnlySpan<byte> message)
    {
        for (var i = 0; i < _later.Count; i++)
        {
            if (_later[i].At <= clock())
            {
                inner.Send(_later[i].Message);
                _later.RemoveAt(i--);
            }
        }

        return inner.TryReceive(out message);
    }
}

/// <summary>A coordinator ("master") and two game servers, each holding one player's purse, joined by lossy links; the master's driver runs trades between them.</summary>
internal sealed class TradeRig
{
    public static readonly ScopeId MasterScope = new(100);
    public static readonly ScopeId ScopeA = new(1);
    public static readonly ScopeId ScopeB = new(2);
    public const uint Seller = 1;
    public const uint Buyer = 2;

    private readonly Random _rng;
    private readonly double _drop;
    private readonly double _dup;
    private readonly int _delay;
    private readonly Dictionary<ScopeId, byte[]> _saves = [];
    private readonly Dictionary<ScopeId, ReplicaScope> _replicas = [];
    private readonly Dictionary<ScopeId, (Node Node, AuthorityScope Scope)> _parts = [];
    private readonly List<ChaosChannel> _channels = [];
    private readonly int _saveEvery;
    private readonly Durability _durability;
    private long _tick;

    public TradeRig(int seed = 1, double drop = 0, double duplicate = 0, int maxDelay = 0, long sellerGold = 10, int sellerSwords = 3, long buyerGold = 100, int buyerSwords = 0, int collectTicks = 300, int saveEvery = 1, Durability durability = Durability.AfterSave)
    {
        _durability = durability;
        _saveEvery = saveEvery;
        _rng = new Random(seed);
        _drop = drop;
        _dup = duplicate;
        _delay = maxDelay;
        CollectTicks = collectTicks;
        Cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 0 });
        var worldA = NewWorld(WorldRole.Authority, 21);
        worldA.Add(new Purse { Player = Seller, Gold = sellerGold, Swords = sellerSwords });
        var worldB = NewWorld(WorldRole.Authority, 22);
        worldB.Add(new Purse { Player = Buyer, Gold = buyerGold, Swords = buyerSwords });
        Start(ScopeA, "A", worldA);
        Start(ScopeB, "B", worldB);
        Start(MasterScope, "M", NewWorld(WorldRole.Authority, 23));
        Connect();
        Cluster.Settle();
        InitialGold = TotalGold();
        InitialSwords = TotalSwords();
    }

    public Cluster Cluster { get; }

    public int CollectTicks { get; }

    public long InitialGold { get; }

    public int InitialSwords { get; }

    public EscrowDriver Driver { get; private set; } = null!;

    public AuthorityScope Master => _parts[MasterScope].Scope;

    public AuthorityScope Seat(ScopeId scope) => _parts[scope].Scope;

    public Node NodeOf(ScopeId scope) => _parts[scope].Node;

    public int Dropped => _channels.Sum(c => c.Dropped);

    public int Duplicated => _channels.Sum(c => c.Duplicated);

    public int Delayed => _channels.Sum(c => c.Delayed);

    public long Now => _tick;

    public static World NewWorld(WorldRole role, ulong seed)
    {
        var registry = new EntityRegistry();
        registry.Add(new Purse().TypeId, "Purse", static () => new Purse());
        Escrow.Register(registry);
        var world = new World(seed, registry, role);
        if (role == WorldRole.Authority)
        {
            Escrow.Install(world, new PurseHandler()).CollectTicks = 300;
        }

        return world;
    }

    private void Start(ScopeId id, string name, World world)
    {
        var node = Cluster.AddNode(name);
        var scope = Cluster.AddScope(node, id, world);
        EscrowBook.Of(world)!.CollectTicks = CollectTicks;
        scope.Durability = _durability;
        _parts[id] = (node, scope);
        _saves[id] = scope.Save();
    }

    /// <summary>Joins the master to both game servers over lossy links and starts its driver.</summary>
    private void Connect()
    {
        var master = _parts[MasterScope].Node;
        foreach (var id in new[] { ScopeA, ScopeB })
        {
            Link(master, id);
        }

        Driver = new EscrowDriver(Master, scope => _replicas.GetValueOrDefault(scope));
    }

    private void Link(Node master, ScopeId id)
    {
        var (a, b) = Loopback.CreatePair();
        var clientEnd = new ChaosChannel(a, _rng, () => _tick, _drop, _dup, _delay);
        var serverEnd = new ChaosChannel(b, _rng, () => _tick, _drop, _dup, _delay);
        _channels.Add(clientEnd);
        _channels.Add(serverEnd);
        var link = master.ConnectTo(_parts[id].Node, Escrow.CoordinatorTrust(id), clientEnd, serverEnd, "M");
        _replicas[id] = master.Subscribe(link, id, NewWorld(WorldRole.Replica, 30));
    }

    public Purse PurseOf(ScopeId scope, uint player) => PurseHandler.Find(Seat(scope).World, player)!;

    public long TotalGold() => Purses().Sum(p => p.Gold);

    public int TotalSwords() => Purses().Sum(p => p.Swords);

    private IEnumerable<Purse> Purses()
    {
        foreach (var id in new[] { ScopeA, ScopeB })
        {
            foreach (var e in Seat(id).World.Entities)
            {
                if (e is Purse p)
                {
                    yield return p;
                }
            }
        }
    }

    /// <summary>Gold and swords in the purses plus what has left a purse and not arrived anywhere yet.</summary>
    public (long Gold, int Swords) Totals()
    {
        var flight = EscrowBook.InFlight(new[] { ScopeA, ScopeB }.Select(id => EscrowBook.Of(Seat(id).World)!));
        return (TotalGold() + flight.GetValueOrDefault(PurseHandler.GoldKind), TotalSwords() + (int)flight.GetValueOrDefault(PurseHandler.SwordKind));
    }

    public string Describe(ulong tx)
    {
        var t = Trade(tx);
        return t is null
            ? "no trade"
            : $"{t.State} opened {t.OpenedTick} changed {t.StateTick} saved {Master.SavedTick}; legs: {string.Join("; ", t.Legs.Select(l => l.Hold + "/" + l.Delivery))}; steps sent {Driver.StepsSent}; tick {_tick}";
    }

    public EscrowTrade? Trade(ulong tx) => EscrowBook.Of(Master.World)!.FindTrade(tx);

    /// <summary>Asks the coordinator to open the trade: the seller's sword for the buyer's gold. Safe to repeat.</summary>
    public void Open(ulong tx, long gold = 50)
    {
        Escrow.DeskOf(Master.World)!.Send.Open(tx, ScopeA.Value, Seller, Buyer, PurseHandler.SwordKind, 1, ScopeB.Value, Buyer, Seller, PurseHandler.GoldKind, gold);
    }

    /// <summary>One tick: the cluster steps, the driver sends what is due, every node saves.</summary>
    public void Tick()
    {
        _tick++;
        Cluster.Step();
        Driver.Pump();
        if (_tick % _saveEvery == 0)
        {
            foreach (var (id, part) in _parts)
            {
                _saves[id] = part.Scope.Save();
            }
        }

        var (gold, swords) = Totals();
        if (gold != InitialGold || swords != InitialSwords)
        {
            throw new InvalidOperationException($"tick {_tick}: gold {gold} (expected {InitialGold}), swords {swords} (expected {InitialSwords})");
        }
    }

    public bool RunUntil(Func<bool> done, int maxTicks, ulong? reopen = null)
    {
        for (var i = 0; i < maxTicks; i++)
        {
            if (done())
            {
                return true;
            }

            if (reopen is { } tx && i % 8 == 0 && Trade(tx) is null)
            {
                Open(tx);
            }

            Tick();
        }

        return done();
    }

    /// <summary>Kills a node and starts a new one from the last save it made: new connections, new replicas, a new driver for the master.</summary>
    public void Restart(ScopeId id)
    {
        var (node, _) = _parts[id];
        Cluster.RemoveNode(node);
        var role = NewWorld(WorldRole.Authority, id == ScopeA ? 21UL : id == ScopeB ? 22UL : 23UL);
        var born = Cluster.AddNode(node.Name + "'");
        var scope = Cluster.AddScope(born, id, role);
        EscrowBook.Of(role)!.CollectTicks = CollectTicks;
        scope.Durability = _durability;
        var loaded = scope.Load(_saves[id]);
        if (loaded.Status != SaveStatus.Ok)
        {
            throw new InvalidOperationException("restart: " + loaded.Status);
        }

        _parts[id] = (born, scope);
        var master = _parts[MasterScope].Node;
        if (id == MasterScope)
        {
            foreach (var other in new[] { ScopeA, ScopeB })
            {
                Link(master, other);
            }

            Driver = new EscrowDriver(Master, scope => _replicas.GetValueOrDefault(scope));
        }
        else
        {
            Link(master, id);
        }

        Cluster.Settle();
    }
}
