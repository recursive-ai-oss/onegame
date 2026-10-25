using System.Runtime.CompilerServices;
using OneGame.Core;
using OneGame.Host;
using OneGame.Platform;
using OneGame.Sync;
using OneGame.UI.Kit;

namespace Lanterns.Views;

/// <summary>
/// What the screens know about the running game: which player is mine, which island I am on, the other players, the accounts. It reads the
/// replicas (and, for the island I am walking on, the predicted world, so my own steps show at once) and sends commands. It writes no state.
/// One session per game instance; it also works in single player, where the one replica is a local copy of the one island.
/// </summary>
[View]
internal sealed class Session
{
    private static readonly ConditionalWeakTable<GameInstance, Session> All = new();

    private readonly Dictionary<uint, Prediction> _predictions = [];
    private int _keysX, _keysY;
    private long _steerTick = -1;
    private int _steerX, _steerY;
    private uint _scope = Rules.IslandA;
    private Autopilot? _pilot;

    private Session(GameInstance game)
    {
        Game = game;
        Kit = Kits.For(game.Ui);
        if (game.Remote is { } remote && remote.Options.Name.StartsWith("bot", StringComparison.Ordinal))
        {
            _pilot = new Autopilot(this);
        }
    }

    public GameInstance Game { get; }

    public OneGame.UI.Kit.KitHost Kit { get; }

    public bool Networked => Game.Remote is not null;

    /// <summary>The partner picked in the swap dialog (a player id), or zero.</summary>
    public uint SwapTarget { get; set; }

    public int SwapGive { get; set; }

    public int SwapWant { get; set; } = 1;

    public bool SwapOpen { get; set; }

    /// <summary>Steering commands sent so far (tests compare it with the player's own count of applied ones).</summary>
    public int SteersSent { get; private set; }

    public string Notice { get; private set; } = string.Empty;

    public static Session For(GameInstance game) => All.GetValue(game, g => new Session(g));

    /// <summary>Runs once per frame: keeps predictions on and lets the autopilot (bots) play.</summary>
    public void Frame()
    {
        if (Networked)
        {
            foreach (var r in Game.Replicas)
            {
                if (r.Id.Value is Rules.IslandA or Rules.IslandB && !_predictions.ContainsKey(r.Id.Value))
                {
                    var world = new World(Game.Seed, NewRegistry(), WorldRole.Predicted);
                    _predictions[r.Id.Value] = r.EnablePrediction(world, new PredictionOptions { LeadTicks = 6 });
                }
            }
        }

        _pilot?.Step();
    }

    private static EntityRegistry NewRegistry()
    {
        var registry = new EntityRegistry();
        Worlds.Register(registry);
        return registry;
    }

    /// <summary>The world to draw for an island: the predicted one when there is one.</summary>
    public World WorldOf(ReplicaScope replica) => !Networked ? Game.World : _predictions.TryGetValue(replica.Id.Value, out var p) ? p.World : replica.World;

    public ReplicaScope? ReplicaOf(uint scope)
    {
        foreach (var r in Game.Replicas)
        {
            if (r.Id.Value == scope)
            {
                return r;
            }
        }

        return null;
    }

    /// <summary>My player, in whichever island copy holds it right now; null while I am being handed to the other island.</summary>
    public Player? Me
    {
        get
        {
            foreach (var r in Game.Replicas)
            {
                if (r.Id.Value is Rules.Master)
                {
                    continue;
                }

                foreach (var e in WorldOf(r).Entities)
                {
                    if (e is Player p && (Networked ? p.Mine : p.Owner == 1))
                    {
                        _scope = r.Id.Value;
                        return p;
                    }
                }
            }

            return null;
        }
    }

    /// <summary>The island being shown: where I am, or where I last was.</summary>
    public uint Scope
    {
        get
        {
            _ = Me;
            return _scope;
        }
    }

    public World? IslandWorld => ReplicaOf(Scope) is { } r ? WorldOf(r) : null;

    public Island? Island => IslandWorld is { } w ? Lanterns.Island.Of(w) : null;

    public IEnumerable<Player> PlayersHere => IslandWorld is { } w ? w.Entities.OfType<Player>() : [];

    public IEnumerable<Lantern> LanternsHere => IslandWorld is { } w ? w.Entities.OfType<Lantern>().Where(l => !l.Taken) : [];

    /// <summary>Every account the master knows, best first. In single player: the players on the island.</summary>
    public IReadOnlyList<(uint Id, string Name, int Total, uint Island)> Board()
    {
        var list = new List<(uint, string, int, uint)>();
        if (ReplicaOf(Rules.Master) is { } master)
        {
            foreach (var a in master.World.Entities.OfType<Account>())
            {
                list.Add((a.Owner, a.Name, a.Total, a.Island));
            }
        }
        else
        {
            foreach (var p in PlayersHere)
            {
                list.Add((p.Owner, p.Name, p.Total, p.Island));
            }
        }

        list.Sort(static (a, b) => b.Item3 != a.Item3 ? b.Item3.CompareTo(a.Item3) : a.Item1.CompareTo(b.Item1));
        return list;
    }

    public Account? MyAccount
    {
        get
        {
            if (Me is not { } me || ReplicaOf(Rules.Master) is not { } master)
            {
                return null;
            }

            return master.World.Entities.OfType<Account>().FirstOrDefault(a => a.Owner == me.Owner);
        }
    }

    /// <summary>Walks. A heading is sent once per server tick at most, and only when it changed unless <paramref name="every"/> asks for each tick.</summary>
    public void Steer(int dx, int dy, bool every = false)
    {
        if (Me is not { } me || ReplicaOf(Scope) is not { } replica)
        {
            return;
        }

        var tick = replica.LastTick;
        var changed = dx != _steerX || dy != _steerY || me.Dx != dx || me.Dy != dy;
        if (tick == _steerTick || !(changed || every))
        {
            return;
        }

        _steerTick = tick;
        _steerX = dx;
        _steerY = dy;
        me.Send.Steer(dx, dy);
        SteersSent++;
    }

    /// <summary>Asks for the swap picked in the dialog.</summary>
    public void SendSwap()
    {
        if (SwapTarget == 0 || Me is not { } me)
        {
            Notice = "Pick a player first.";
            return;
        }

        if (Networked)
        {
            if (MyAccount is not { } account)
            {
                Notice = "The master does not know you yet.";
                return;
            }

            account.Send.Propose((int)SwapTarget, SwapGive, SwapWant);
        }
        else
        {
            me.Send.SwapLocal((int)SwapTarget, SwapGive, SwapWant);
        }

        Notice = "Offer sent.";
    }

    public void Accept() => MyAccount?.Send.Accept();

    public void Decline() => MyAccount?.Send.Decline();

    public void HandleInput(in PlatformEvent e)
    {
        if (e.Kind is not (PlatformEventKind.KeyDown or PlatformEventKind.KeyUp) || e.IsRepeat)
        {
            return;
        }

        var down = e.Kind == PlatformEventKind.KeyDown ? 1 : 0;
        switch (e.Key)
        {
            case Key.ArrowLeft or Key.A:
                _keysX = down == 1 ? -1 : (_keysX == -1 ? 0 : _keysX);
                break;
            case Key.ArrowRight or Key.D:
                _keysX = down == 1 ? 1 : (_keysX == 1 ? 0 : _keysX);
                break;
            case Key.ArrowUp or Key.W:
                _keysY = down == 1 ? -1 : (_keysY == -1 ? 0 : _keysY);
                break;
            case Key.ArrowDown or Key.S:
                _keysY = down == 1 ? 1 : (_keysY == 1 ? 0 : _keysY);
                break;
            default:
                return;
        }

        Steer(_keysX, _keysY);
    }
}
