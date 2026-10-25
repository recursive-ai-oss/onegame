using OneGame.Core;

namespace Lanterns.Views;

/// <summary>
/// Plays for a client whose name starts with "bot": gathers a few lanterns, walks over the bridge, gathers on the other island, and offers
/// or accepts swaps. It uses only what a player can: the replicas it can see, and commands.
/// </summary>
[View]
internal sealed class Autopilot(Session session)
{
    private const int GatherGoal = 3;
    private const int ProposeEveryFrames = 900;

    private uint _island;
    private int _baseline;
    private long _frames;
    private long _lastPropose = -ProposeEveryFrames;
    private int _lastStatus;

    public void Step()
    {
        _frames++;
        if (session.Me is not { } me)
        {
            return;
        }

        if (me.Island != _island)
        {
            _island = me.Island;
            _baseline = me.Total;
        }

        Swaps(me);
        var (tx, ty) = Target(me);
        session.Steer(Math.Sign(tx - me.X) * (Math.Abs(tx - me.X) > Rules.Speed ? 1 : 0), Math.Sign(ty - me.Y) * (Math.Abs(ty - me.Y) > Rules.Speed ? 1 : 0), every: true);
    }

    private (int X, int Y) Target(Player me)
    {
        var bridgeX = me.Island == Rules.IslandA ? Rules.Width + 8 : -8;
        var bridgeY = (Rules.BridgeTop + Rules.BridgeBottom) / 2;
        if (me.Total - _baseline >= GatherGoal || me.Pending > 0)
        {
            // A swap holds the player in place; walking at the bridge edge is harmless until it settles.
            return (bridgeX, bridgeY);
        }

        Lantern? best = null;
        var bestDistance = int.MaxValue;
        foreach (var l in session.LanternsHere)
        {
            var d = Math.Abs(l.X - me.X) + Math.Abs(l.Y - me.Y);
            if (d < bestDistance || (d == bestDistance && l.Id.Value < best!.Id.Value))
            {
                best = l;
                bestDistance = d;
            }
        }

        return best is null ? (bridgeX, bridgeY) : (best.X, best.Y);
    }

    private void Swaps(Player me)
    {
        if (session.MyAccount is not { } account)
        {
            return;
        }

        if (account.Status == SwapStatus.Incoming && _lastStatus != SwapStatus.Incoming)
        {
            account.Send.Accept();
        }
        else if (account.Status == SwapStatus.None || account.Status >= SwapStatus.Done)
        {
            if (_frames - _lastPropose >= ProposeEveryFrames && me.Total >= 2 && Colors(me) is var (most, least))
            {
                var other = session.Board().FirstOrDefault(b => b.Id != me.Owner && b.Island != 0 && b.Island != me.Island);
                if (other.Id != 0)
                {
                    _lastPropose = _frames;
                    account.Send.Propose((int)other.Id, most, least);
                }
            }
        }

        _lastStatus = account.Status;
    }

    // The colour I hold most of, to give, and the one I hold least of, to ask for.
    private static (int Most, int Least)? Colors(Player me)
    {
        int most = 0, least = 0;
        for (var c = 1; c < Rules.Colors; c++)
        {
            most = me.Count(c) > me.Count(most) ? c : most;
            least = me.Count(c) < me.Count(least) ? c : least;
        }

        return most == least ? null : (most, least);
    }
}
