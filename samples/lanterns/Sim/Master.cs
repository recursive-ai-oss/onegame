using OneGame.Core;
using OneGame.Sync;

namespace Lanterns;

/// <summary>Swap progress, as shown to the two players.</summary>
public static class SwapStatus
{
    /// <summary>Nothing under way.</summary>
    public const int None = 0;

    /// <summary>You offered; the other player has not answered.</summary>
    public const int Offered = 1;

    /// <summary>Somebody offered you a swap.</summary>
    public const int Incoming = 2;

    /// <summary>Accepted; the islands are moving the lanterns.</summary>
    public const int Trading = 3;

    /// <summary>The last swap went through.</summary>
    public const int Done = 4;

    /// <summary>The last swap did not happen (declined, or a lantern was not there). Nothing was lost.</summary>
    public const int Failed = 5;
}

/// <summary>The master's one entity for the server's own bookkeeping.</summary>
[Entity]
public partial class Market : Entity
{
    /// <summary>Owned by server code, so only the server can update accounts.</summary>
    [Synced, OwnerKey] public partial uint Keeper { get; set; }

    /// <summary>Swaps that went through, in total.</summary>
    [Synced] public partial long Settled { get; set; }

    /// <summary>The next swap number.</summary>
    [Synced] public partial long NextTx { get; set; }

    /// <summary>Records what an island reports about a player: which island, how many lanterns. Creates the account the first time.</summary>
    [Command(OwnerOnly = true)]
    public bool Observe(int player, string name, int island, int total)
    {
        Account? found = null;
        foreach (var e in World!.Entities)
        {
            if (e is Account a && a.Owner == (uint)player)
            {
                found = a;
            }
        }

        if (found is null)
        {
            World.Add(new Account { Owner = (uint)player, Name = name, Island = (uint)island, Total = total });
            return true;
        }

        found.Island = (uint)island;
        found.Total = total;
        found.Name = name;
        return true;
    }
}

/// <summary>
/// One player's record on the master: name, where they are, lanterns ever held (kept across restarts), and the swap they are in. A swap is
/// settled by the islands' escrow, driven from the master; this entity only asks for it and shows how it went.
/// </summary>
[Entity]
public partial class Account : Entity
{
    /// <summary>The player.</summary>
    [Synced, OwnerKey] public partial uint Owner { get; set; }

    /// <summary>Display name.</summary>
    [Synced] public partial string Name { get; set; }

    /// <summary>The island the player was last seen on, or zero.</summary>
    [Synced] public partial uint Island { get; set; }

    /// <summary>Lanterns held when last seen. The leaderboard sorts on this.</summary>
    [Synced] public partial int Total { get; set; }

    /// <summary>A <see cref="SwapStatus"/> value (owner only).</summary>
    [Synced(Visibility = Visibility.Owner)] public partial int Status { get; set; }

    /// <summary>The other player in the swap (owner only).</summary>
    [Synced(Visibility = Visibility.Owner)] public partial uint Partner { get; set; }

    /// <summary>The colour the proposer gives (owner only).</summary>
    [Synced(Visibility = Visibility.Owner)] public partial int Give { get; set; }

    /// <summary>The colour the proposer asks for (owner only).</summary>
    [Synced(Visibility = Visibility.Owner)] public partial int Want { get; set; }

    /// <summary>The swap number once it was accepted (owner only).</summary>
    [Synced(Visibility = Visibility.Owner)] public partial long Tx { get; set; }

    /// <summary>Offers a swap: one lantern of colour <paramref name="give"/> for one of colour <paramref name="want"/>.</summary>
    [Command(OwnerOnly = true)]
    public CommandReply Propose(int to, int give, int want)
    {
        if (give is < 0 or >= Rules.Colors || want is < 0 or >= Rules.Colors)
        {
            return CommandReply.Reject("no such colour");
        }

        if (Busy(this))
        {
            return CommandReply.Reject("finish the swap you are in first");
        }

        var other = Find((uint)to);
        if (other is null || other == this)
        {
            return CommandReply.Reject("no such player");
        }

        if (Busy(other))
        {
            return CommandReply.Reject("that player is busy");
        }

        if (Island == 0 || other.Island == 0)
        {
            return CommandReply.Reject("both players must be on an island");
        }

        Status = SwapStatus.Offered;
        Partner = other.Owner;
        Give = give;
        Want = want;
        other.Status = SwapStatus.Incoming;
        other.Partner = Owner;
        other.Give = give;
        other.Want = want;
        return CommandReply.Ok;
    }

    /// <summary>Accepts the offer made to this player: the lanterns move through the islands' escrow.</summary>
    [Command(OwnerOnly = true)]
    public CommandReply Accept()
    {
        if (Status != SwapStatus.Incoming || Find(Partner) is not { } proposer || proposer.Status != SwapStatus.Offered || proposer.Partner != Owner)
        {
            return CommandReply.Reject("no offer to accept");
        }

        var market = MarketOf(World!);
        var desk = Escrow.DeskOf(World!);
        if (market is null || desk is null)
        {
            return CommandReply.Reject("swaps are not available here");
        }

        var tx = (ulong)(market.NextTx + 1);
        var reply = desk.Open(
            tx,
            proposer.Island, proposer.Owner, Owner, Rules.Kind(proposer.Give), 1,
            Island, Owner, proposer.Owner, Rules.Kind(proposer.Want), 1);
        if (!reply.Accepted)
        {
            return reply;
        }

        market.NextTx = (long)tx;
        Status = SwapStatus.Trading;
        Tx = (long)tx;
        proposer.Status = SwapStatus.Trading;
        proposer.Tx = (long)tx;
        return CommandReply.Ok;
    }

    /// <summary>Declines the offer made to this player, or withdraws this player's own offer.</summary>
    [Command(OwnerOnly = true)]
    public CommandReply Decline()
    {
        if (Status is not (SwapStatus.Incoming or SwapStatus.Offered))
        {
            return CommandReply.Reject("nothing to decline");
        }

        var other = Find(Partner);
        var mine = Status;
        Status = mine == SwapStatus.Incoming ? SwapStatus.None : SwapStatus.Failed;
        if (other is not null && other.Partner == Owner)
        {
            other.Status = mine == SwapStatus.Incoming ? SwapStatus.Failed : SwapStatus.None;
        }

        return CommandReply.Ok;
    }

    /// <inheritdoc />
    protected override void OnTick()
    {
        if (Status != SwapStatus.Trading || World!.Role != WorldRole.Authority)
        {
            return;
        }

        var trade = EscrowBook.Of(World)?.FindTrade((ulong)Tx);
        if (trade is { IsFinal: true })
        {
            Status = trade.State == EscrowState.Settled ? SwapStatus.Done : SwapStatus.Failed;
            if (Status == SwapStatus.Done && Partner > Owner && MarketOf(World!) is { } market)
            {
                market.Settled++; // counted once per swap: by the account with the lower player id
            }

            Tx = 0;
        }
    }

    private static bool Busy(Account a) => a.Status is SwapStatus.Offered or SwapStatus.Incoming or SwapStatus.Trading;

    private Account? Find(uint owner)
    {
        foreach (var e in World!.Entities)
        {
            if (e is Account a && a.Owner == owner)
            {
                return a;
            }
        }

        return null;
    }

    private static Market? MarketOf(World world)
    {
        foreach (var e in world.Entities)
        {
            if (e is Market m)
            {
                return m;
            }
        }

        return null;
    }
}
