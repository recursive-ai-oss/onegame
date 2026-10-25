namespace OneGame.Sync.Tests;

/// <summary>
/// One traveller moves from game server A (scope 1) to game server B (scope 2) while a client keeps hitting it. The rig counts every
/// command the client sent and what became of it, and watches that the traveller exists in at most one world at every tick.
/// </summary>
internal sealed class HandoffRig
{
    public const uint Player = 5;
    public static readonly ScopeId ScopeA = new(1);
    public static readonly ScopeId ScopeB = new(2);

    private readonly List<(ReplicaScope Replica, uint Sequence)> _sent = [];
    private readonly List<string> _violations = [];

    public HandoffRig(Wire? wire = null, ClusterOptions? options = null)
    {
        Cluster = new Cluster(options ?? new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        A = Cluster.AddNode("A");
        B = Cluster.AddNode("B");
        Client = Cluster.AddNode("client");
        ScopeOnA = Cluster.AddScope(A, ScopeA, Realm.NewWorld(WorldRole.Authority));
        ScopeOnB = Cluster.AddScope(B, ScopeB, Realm.NewWorld(WorldRole.Authority, 12));
        ScopeOnA.World.Add(new Beacon());
        ScopeOnB.World.Add(new Beacon());
        Traveler = ScopeOnA.World.Add(new Traveler { Player = Player, Name = "Wanda", Diary = "owner-only-diary-4417", Secret = 99 });
        Original = Traveler.Id;

        // The two game servers trust each other to hand entities over.
        LinkAB = wire is null
            ? Cluster.Connect(A, B, Trust.Owner(ScopeB))
            : wire.Connect(A, B, Trust.Owner(ScopeB));

        // The client reads and commands both scopes as the player, and finds the second one by itself when the traveller moves.
        var toA = Cluster.Connect(Client, A, Trust.Owner(ScopeA).As(Player));
        var toB = Cluster.Connect(Client, B, Trust.Owner(ScopeB).As(Player));
        Client.FollowResolver = scope => scope == ScopeB ? new FollowTarget(toB, Realm.NewWorld(WorldRole.Replica, 12)) : null;
        ViewOfA = Client.Subscribe(toA, ScopeA, Realm.NewWorld(WorldRole.Replica));
        Cluster.Settle();
    }

    public Cluster Cluster { get; }

    public Node A { get; }

    public Node B { get; }

    public Node Client { get; }

    public AuthorityScope ScopeOnA { get; }

    public AuthorityScope ScopeOnB { get; }

    public Link LinkAB { get; }

    public Traveler Traveler { get; }

    public EntityId Original { get; }

    public ReplicaScope ViewOfA { get; }

    public IReadOnlyList<string> Violations => _violations;

    public int Sent => _sent.Count;

    /// <summary>The traveller wherever the client currently sees it, with the replica that holds it.</summary>
    public (ReplicaScope Replica, Traveler Traveler)? Visible()
    {
        foreach (var r in Client.ReplicaScopes)
        {
            if (r.State == ReplicaState.Live && Realm.FindTraveler(r.World, Player) is { } t)
            {
                return (r, t);
            }
        }

        return null;
    }

    /// <summary>One cluster tick in which the client sends a hit if it can see the traveller.</summary>
    public void Tick(bool send = true)
    {
        if (send && Visible() is { } seen)
        {
            seen.Traveler.Send.Hit();
            _sent.Add((seen.Replica, seen.Replica.LastSequence));
        }

        Cluster.Step();
        var copies = Realm.CountTravelers(ScopeOnA.World, Player) + Realm.CountTravelers(ScopeOnB.World, Player);
        if (copies > 1)
        {
            _violations.Add($"tick {Cluster.Tick}: the traveller exists in {copies} worlds");
        }
    }

    public void Run(int ticks, bool send = true)
    {
        for (var i = 0; i < ticks; i++)
        {
            Tick(send);
        }
    }

    public bool RunUntil(Func<bool> condition, int maxTicks, bool send = true)
    {
        for (var i = 0; i < maxTicks; i++)
        {
            if (condition())
            {
                return true;
            }

            Tick(send);
        }

        return condition();
    }

    /// <summary>How many of the commands the client sent were answered as executed, rejected, refused, dropped, or are still waiting.</summary>
    public (int Executed, int Other, int Unanswered) Outcomes()
    {
        int executed = 0, other = 0, waiting = 0;
        foreach (var (replica, sequence) in _sent)
        {
            CommandResult? result = null;
            foreach (var r in replica.Results)
            {
                if (r.Sequence == sequence)
                {
                    result = r;
                }
            }

            if (result is null)
            {
                waiting++;
            }
            else if (result.Value.Verdict == CommandVerdict.Executed)
            {
                executed++;
            }
            else
            {
                other++;
            }
        }

        return (executed, other, waiting);
    }

    /// <summary>The traveller in whichever server's world holds it, or null.</summary>
    public Traveler? Authoritative()
        => Realm.FindTraveler(ScopeOnB.World, Player) ?? Realm.FindTraveler(ScopeOnA.World, Player);
}
