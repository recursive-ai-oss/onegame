namespace OneGame.Sync.Tests;

/// <summary>A hero in a match. Some of what it holds is for everyone, some for its team, some for its owner alone, and some never leaves the authority.</summary>
[Entity]
public partial class Hero : Entity
{
    [Synced, OwnerKey] public partial uint Player { get; set; }

    [Synced, TeamKey] public partial uint Team { get; set; }

    [Synced] public partial string Name { get; set; }

    [Synced] public partial FVec2 Position { get; set; }

    [Synced(Visibility = Visibility.Team)] public partial int Stamina { get; set; }

    [Synced(Visibility = Visibility.Owner)] public partial int Gold { get; set; }

    [Synced(Visibility = Visibility.Owner)] public partial string Stash { get; set; }

    [Synced(Visibility = Visibility.Owner)] public SyncList<int> Hand { get; } = new();

    [Synced(Visibility = Visibility.Hidden)] public partial int Plot { get; set; }

    [Command]
    public CommandReply Spend(int amount)
    {
        if (amount <= 0)
        {
            return CommandReply.Reject("amount must be positive");
        }

        if (amount > Gold)
        {
            return CommandReply.Reject("not enough gold");
        }

        Gold -= amount;
        return CommandReply.Ok;
    }

    [Command(OwnerOnly = true)]
    public void Rename(string name) => Name = name;

    [Command]
    public void Walk(int dx, int dy) => Position += new FVec2(dx, dy);

    [Command]
    public bool Hoard(string what)
    {
        Stash = what;
        return true;
    }

    /// <summary>What the owner's screen shows. Reads the owner-only gold, so it is only meaningful on the owner's replica.</summary>
    [View]
    public string Purse => Gold.ToString(System.Globalization.CultureInfo.InvariantCulture);

    protected override void OnTick()
    {
        var rng = World!.Rng["hero"];
        Gold += 1 + rng.NextInt(3);
        Stamina = (Stamina + 7) % 100;
        Plot += rng.NextInt(5);
        if (World.Tick % 25 == 0)
        {
            Hand.Add(rng.NextInt(52));
            if (Hand.Count > 5)
            {
                Hand.RemoveAt(0);
            }
        }
    }
}

/// <summary>A player's bank account on the master: the balance is public, the notes are the owner's, the audit counter stays on the master.</summary>
[Entity]
public partial class Account : Entity
{
    [Synced, OwnerKey] public partial uint Player { get; set; }

    [Synced] public partial int Balance { get; set; }

    [Synced(Visibility = Visibility.Owner)] public partial string Notes { get; set; }

    [Synced(Visibility = Visibility.Hidden)] public partial int Audit { get; set; }

    [Command]
    public void Deposit(int amount) => Balance += amount;

    [Command]
    public CommandReply Withdraw(int amount) => amount > Balance ? CommandReply.Reject("insufficient funds") : Take(amount);

    [Command(OwnerOnly = true)]
    public void Note(string text) => Notes = text;

    private CommandReply Take(int amount)
    {
        Balance -= amount;
        return CommandReply.Ok;
    }

    protected override void OnTick()
    {
        Audit++;
        if (World!.Tick % 10 == 0)
        {
            Balance += 1;
        }
    }
}

/// <summary>Exists only for its owner.</summary>
[Entity(Visibility = Visibility.Owner)]
public partial class Diary : Entity
{
    [Synced, OwnerKey] public partial uint Player { get; set; }

    [Synced] public partial string Text { get; set; }
}

/// <summary>Exists only for one team (and its owner).</summary>
[Entity(Visibility = Visibility.Team)]
public partial class Intel : Entity
{
    [Synced, TeamKey] public partial uint Team { get; set; }

    [Synced] public partial int Sighted { get; set; }

    protected override void OnTick() => Sighted += 3;
}

/// <summary>Never leaves the authority.</summary>
[Entity(Visibility = Visibility.Hidden)]
public partial class Director : Entity
{
    [Synced] public partial int Mood { get; set; }

    protected override void OnTick() => Mood += 1;
}

/// <summary>Rows only the authority sees.</summary>
[HotData(Name = "secrets", Visibility = Visibility.Hidden)]
public struct SecretRow
{
    public Fixed X { get; set; }
    public int Weight { get; set; }
}

/// <summary>Builds the worlds the visibility tests use.</summary>
internal static class Arena
{
    public const ulong Seed = 7;

    public static EntityRegistry Registry()
    {
        var registry = new EntityRegistry();
        registry.Add(new Hero().TypeId, "Hero", static () => new Hero());
        registry.Add(new Account().TypeId, "Account", static () => new Account());
        registry.Add(new Diary().TypeId, "Diary", static () => new Diary());
        registry.Add(new Intel().TypeId, "Intel", static () => new Intel());
        registry.Add(new Director().TypeId, "Director", static () => new Director());
        return registry;
    }

    public static World Empty(WorldRole role, ulong seed = Seed)
    {
        var world = new World(seed, Registry(), role);
        world.AddTable(new SecretRowTable());
        return world;
    }

    /// <summary>A match with two heroes of different teams, one intel report per team, a diary for each, and a director.</summary>
    public static World Match(ulong seed = Seed, uint first = 1, uint second = 2)
    {
        var world = Empty(WorldRole.Authority, seed);
        world.Add(new Director { Mood = 1 });
        world.Add(new Hero { Player = first, Team = 10, Name = "Ada", Position = new FVec2(1, 1), Stash = "ruby-hoard-7731", Plot = 41 });
        world.Add(new Hero { Player = second, Team = 20, Name = "Bo", Position = new FVec2(5, 5), Stash = "emerald-cache-2208", Plot = 42 });
        world.Add(new Intel { Team = 10, Sighted = 100 });
        world.Add(new Intel { Team = 20, Sighted = 200 });
        world.Add(new Diary { Player = first, Text = "ada-diary-secret" });
        world.Add(new Diary { Player = second, Text = "bo-diary-secret" });
        var table = (SecretRowTable)world.Tables[0];
        table.Add(new SecretRow { X = 3, Weight = 9 });
        return world;
    }

    /// <summary>A master with one account per listed player.</summary>
    public static World Master(params uint[] players)
    {
        var world = Empty(WorldRole.Authority, Seed + 1);
        foreach (var p in players)
        {
            world.Add(new Account { Player = p, Balance = 100 * (int)p, Notes = "notes-of-player-" + (char)('a' + (int)p) });
        }

        return world;
    }
}

/// <summary>Records every message a channel end receives, so a test can look at the exact bytes that were delivered.</summary>
internal sealed class TapChannel(IMessageChannel inner) : IMessageChannel
{
    public List<byte[]> Received { get; } = [];

    public void Send(ReadOnlySpan<byte> message) => inner.Send(message);

    public bool TryReceive(out ReadOnlySpan<byte> message)
    {
        if (!inner.TryReceive(out message))
        {
            return false;
        }

        Received.Add(message.ToArray());
        return true;
    }

    /// <summary>True if any received message contains <paramref name="text"/> as UTF-8.</summary>
    public bool Contains(string text)
    {
        var needle = System.Text.Encoding.UTF8.GetBytes(text);
        return Received.Any(m => m.AsSpan().IndexOf(needle) >= 0);
    }
}
