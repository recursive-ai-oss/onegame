using OneGame.Core;
using OneGame.Sync;

namespace Lanterns;

/// <summary>
/// One person on an island. Everyone sees where the player is and how many lanterns they hold; only the owner sees which colours and whether
/// a swap is under way. When the player walks onto the bridge the entity is handed to the other island's server (see <see cref="Dest"/>).
/// </summary>
[Entity]
public partial class Player : Entity
{
    /// <summary>The player's identity; the key that decides who sees the private members.</summary>
    [Synced, OwnerKey] public partial uint Owner { get; set; }

    /// <summary>Display name.</summary>
    [Synced] public partial string Name { get; set; }

    /// <summary>The scope of the island the player is on.</summary>
    [Synced] public partial uint Island { get; set; }

    /// <summary>While the player waits on the bridge to be handed over: the scope it is going to. Zero otherwise.</summary>
    [Synced] public partial uint Dest { get; set; }

    /// <summary>Position in units.</summary>
    [Synced] public partial int X { get; set; }

    /// <summary>Position in units.</summary>
    [Synced] public partial int Y { get; set; }

    /// <summary>Heading, -1, 0 or 1.</summary>
    [Synced] public partial int Dx { get; set; }

    /// <summary>Heading, -1, 0 or 1.</summary>
    [Synced] public partial int Dy { get; set; }

    /// <summary>How many steering commands this player has had applied, ever. It travels with the player across islands, so it shows that none was lost or run twice.</summary>
    [Synced] public partial uint Moves { get; set; }

    /// <summary>Lanterns held, all colours. Everyone sees it.</summary>
    [Synced] public partial int Total { get; set; }

    /// <summary>True in the owner's own copy: how a client finds out which player it is.</summary>
    [Synced(Visibility = Visibility.Owner)] public partial bool Mine { get; set; }

    /// <summary>Red lanterns held (owner only).</summary>
    [Synced(Visibility = Visibility.Owner)] public partial int Red { get; set; }

    /// <summary>Blue lanterns held (owner only).</summary>
    [Synced(Visibility = Visibility.Owner)] public partial int Blue { get; set; }

    /// <summary>Gold lanterns held (owner only).</summary>
    [Synced(Visibility = Visibility.Owner)] public partial int Gold { get; set; }

    /// <summary>Swap steps in progress that hold or will return this player's lanterns. The player stays put while it is above zero.</summary>
    [Synced(Visibility = Visibility.Owner)] public partial int Pending { get; set; }

    /// <summary>True for a computer-run walker (single player).</summary>
    [Synced] public partial bool Auto { get; set; }

    /// <summary>Sets the heading. Run on the owner's screen at once and again on the server, so walking feels immediate.</summary>
    [Command(OwnerOnly = true, Predicted = true)]
    public bool Steer(int dx, int dy)
    {
        Dx = Math.Clamp(dx, -1, 1);
        Dy = Math.Clamp(dy, -1, 1);
        Moves++;
        return true;
    }

    /// <summary>Single player only: swaps one lantern with another player on the same island, at once.</summary>
    [Command(OwnerOnly = true)]
    public CommandReply SwapLocal(int other, int give, int want)
    {
        if (give is < 0 or >= Rules.Colors || want is < 0 or >= Rules.Colors)
        {
            return CommandReply.Reject("no such colour");
        }

        Player? partner = null;
        foreach (var e in World!.Entities)
        {
            if (e is Player p && p.Owner == (uint)other && p != this)
            {
                partner = p;
            }
        }

        if (partner is null)
        {
            return CommandReply.Reject("nobody to swap with");
        }

        if (Count(give) < 1 || partner.Count(want) < 1)
        {
            return CommandReply.Reject("a lantern is missing");
        }

        Change(give, -1);
        Change(want, 1);
        partner.Change(want, -1);
        partner.Change(give, 1);
        return CommandReply.Ok;
    }

    /// <summary>How many lanterns of a colour are held.</summary>
    public int Count(int color) => color switch { 0 => Red, 1 => Blue, _ => Gold };

    /// <summary>Adds or removes lanterns of a colour and keeps <see cref="Total"/> right.</summary>
    public void Change(int color, int delta)
    {
        switch (color)
        {
            case 0:
                Red += delta;
                break;
            case 1:
                Blue += delta;
                break;
            default:
                Gold += delta;
                break;
        }

        Total += delta;
    }

    /// <inheritdoc />
    protected override void OnTick()
    {
        var w = World!;
        var island = Lanterns.Island.Of(w);
        if (island is null)
        {
            return;
        }

        if (Dest != 0)
        {
            if (Dest != island.ScopeNo)
            {
                return; // waiting on the bridge for the other server to take over
            }

            Island = Dest;
            Dest = 0;
            island.Arrivals++;
        }

        if (Auto)
        {
            Wander(w);
        }

        if (Dx != 0 || Dy != 0)
        {
            Walk(island);
        }

        Pickup(w, island);
    }

    private void Walk(Island island)
    {
        var nx = X + (Dx * Rules.Speed);
        var ny = Math.Clamp(Y + (Dy * Rules.Speed), 0, Rules.Height);
        var canCross = !Auto && Pending == 0 && island.Linked && Rules.OnBridge(ny);
        if (nx < 0)
        {
            if (canCross && island.ScopeNo == Rules.IslandB)
            {
                Cross(Rules.IslandA, Rules.Width - (2 * Rules.Speed), ny);
                return;
            }

            nx = 0;
        }
        else if (nx > Rules.Width)
        {
            if (canCross && island.ScopeNo == Rules.IslandA)
            {
                Cross(Rules.IslandB, 2 * Rules.Speed, ny);
                return;
            }

            nx = Rules.Width;
        }

        X = nx;
        Y = ny;
    }

    // The entity is already at its arrival place when it is handed over, so the other island's server has nothing to adjust.
    private void Cross(uint dest, int x, int y)
    {
        Dest = dest;
        X = x;
        Y = y;
        Dx = 0;
        Dy = 0;
    }

    private void Wander(World w)
    {
        if (w.Tick % 45 == (Owner % 45))
        {
            var rng = w.Rng["walkers"];
            Dx = rng.NextInt(-1, 2);
            Dy = rng.NextInt(-1, 2);
        }
    }

    private void Pickup(World w, Island island)
    {
        foreach (var e in w.Entities)
        {
            if (e is Lantern l && !l.Taken && Math.Abs(l.X - X) <= Rules.PickupReach && Math.Abs(l.Y - Y) <= Rules.PickupReach)
            {
                l.Taken = true;
                w.Remove(l.Id);
                Change(l.Color, 1);
                island.Collected++;
            }
        }
    }
}

/// <summary>How a swap reads and writes a player's lanterns. Used by the swap ledger on each island, and by the single-player rules.</summary>
[Sim]
public sealed class LanternPurse : IEscrowHandler
{
    /// <inheritdoc />
    public bool TryDebit(World world, uint owner, uint kind, long amount)
    {
        // A player who is already on the way to the other island cannot take part: the hand-over and the swap would race.
        if (Find(world, owner) is not { } p || p.Dest != 0 || kind is < 1 or > Rules.Colors || amount != 1 || p.Count((int)kind - 1) < 1)
        {
            return false;
        }

        p.Change((int)kind - 1, -1);
        p.Pending++;
        return true;
    }

    /// <inheritdoc />
    public bool Credit(World world, uint owner, uint kind, long amount)
    {
        if (Find(world, owner) is not { } p || kind is < 1 or > Rules.Colors || amount != 1)
        {
            return false;
        }

        p.Change((int)kind - 1, 1);
        p.Pending = p.Pending > 0 ? p.Pending - 1 : 0;
        return true;
    }

    private static Player? Find(World world, uint owner)
    {
        foreach (var e in world.Entities)
        {
            if (e is Player p && p.Owner == owner)
            {
                return p;
            }
        }

        return null;
    }
}
