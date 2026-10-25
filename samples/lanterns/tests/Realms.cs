using System.Net;
using System.Net.Sockets;
using Lanterns.Server;
using Lanterns.Views;
using OneGame.Core;
using OneGame.Host;
using OneGame.Server;
using OneGame.Sync;

namespace Lanterns.Tests;

/// <summary>Both islands and the master in one process, joined over real UDP sockets on the loopback, stepped in turns.</summary>
internal sealed class Realms : IDisposable
{
    private readonly int[] _peerPorts = [Free(), Free(), Free()];
    private readonly int[] _listenPorts = [Free(), Free(), Free()]; // fixed per scope, so a restarted server is found where it was
    private readonly string? _data;
    private readonly Dictionary<uint, (ServerHost Host, Realm Realm)> _servers = [];
    private readonly List<GameInstance> _clients = [];

    public Realms(string? data = null)
    {
        _data = data;
        foreach (var scope in new[] { Rules.IslandA, Rules.IslandB, Rules.Master })
        {
            Start(scope);
        }
    }

    public ServerHost A => _servers[Rules.IslandA].Host;

    public ServerHost B => _servers[Rules.IslandB].Host;

    public ServerHost M => _servers[Rules.Master].Host;

    public ServerHost Host(uint scope) => _servers[scope].Host;

    public Realm RealmOf(uint scope) => _servers[scope].Realm;

    public void Start(uint scope)
    {
        var options = new List<KeyValuePair<string, string>> { new("peers", _peerPorts[scope - 1].ToString(CultureInfo.InvariantCulture)) };
        foreach (var other in new[] { Rules.IslandA, Rules.IslandB, Rules.Master }.Where(s => s != scope))
        {
            options.Add(new("peer." + other.ToString(CultureInfo.InvariantCulture), "udp://127.0.0.1:" + _peerPorts[other - 1].ToString(CultureInfo.InvariantCulture)));
        }

        if (_data is not null)
        {
            options.Add(new("data", _data));
        }

        var config = new ServerConfig
        {
            Name = scope == Rules.Master ? "master" : scope == Rules.IslandA ? "islandA" : "islandB",
            Scopes = [new ScopeConfig(scope, scope * 11)],
            Listen = scope == Rules.Master ? [] : [new ListenConfig("udp", _listenPorts[scope - 1], "127.0.0.1")],
            Options = options,
            HashHistory = 64,
        };
        var realm = new Realm();
        var host = new ServerHost(config, realm.Create(), null, new ServerLog());
        host.Start();
        _servers[scope] = (host, realm);
    }

    /// <summary>Stops a server abruptly: its sockets close and nothing is saved on the way out.</summary>
    public void Kill(uint scope)
    {
        _servers[scope].Realm.Close();
        _servers[scope].Host.Stop();
        _servers.Remove(scope);
    }

    public GameInstance Client(string name, uint island)
    {
        var port = Host(island).Endpoints.First(e => e.Transport == "udp").Port;
        var also = new[] { Rules.IslandA, Rules.IslandB }.Where(s => s != island).Append(Rules.Master).ToArray();
        var game = GameInstance.Create(new LanternsGame(), new GameOptions
        {
            Headless = true,
            Gpu = GpuMode.Off,
            Connect = new ConnectOptions { Address = "udp://127.0.0.1:" + port.ToString(CultureInfo.InvariantCulture), Scope = island, AlsoScopes = also, Name = name },
        });
        _clients.Add(game);
        return game;
    }

    /// <summary>Runs <paramref name="ticks"/> rounds: every server ticks once, every client runs a frame.</summary>
    public void Run(int ticks, Action<int>? each = null)
    {
        for (var i = 0; i < ticks; i++)
        {
            foreach (var (host, _) in _servers.Values.ToArray())
            {
                host.PumpNetwork();
                host.StepOnce();
            }

            foreach (var c in _clients)
            {
                c.Step();
            }

            each?.Invoke(i);
            Thread.Sleep(1);
        }
    }

    /// <summary>Runs until the condition holds; fails the test after <paramref name="timeoutMs"/> of real time.</summary>
    public void RunUntil(Func<bool> condition, int timeoutMs, string what, Action<int>? each = null, Func<string>? why = null)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            Assert.True(clock.ElapsedMilliseconds < timeoutMs, "timed out waiting for " + what + (why is null ? string.Empty : ": " + why()));
            Run(1, each);
        }
    }

    /// <summary>All players on both islands.</summary>
    public IEnumerable<Player> Players() => new[] { Rules.IslandA, Rules.IslandB }.Where(_servers.ContainsKey).SelectMany(s => Host(s).Scopes[0].World.Entities.OfType<Player>());

    /// <summary>Lanterns held by players, taken out by a swap and not yet delivered, or gone with a player who disconnected.</summary>
    public long Held() => Players().Sum(p => (long)p.Total) + InFlight() + Departed();

    public long InFlight()
    {
        var books = new[] { Rules.IslandA, Rules.IslandB }.Where(_servers.ContainsKey).Select(s => EscrowBook.Of(Host(s).Scopes[0].World)!).ToList();
        return EscrowBook.InFlight(books).Values.Sum();
    }

    public long Collected() => new[] { Rules.IslandA, Rules.IslandB }.Where(_servers.ContainsKey).Sum(s => Island.Of(Host(s).Scopes[0].World)!.Collected);

    public long Departed() => new[] { Rules.IslandA, Rules.IslandB }.Where(_servers.ContainsKey).Sum(s => Island.Of(Host(s).Scopes[0].World)!.Departed);

    /// <summary>A player closes the game: the connection ends and the client is no longer run.</summary>
    public void Leave(GameInstance client)
    {
        client.Dispose();
        _clients.Remove(client);
    }

    public void Dispose()
    {
        foreach (var c in _clients)
        {
            c.Dispose();
        }

        foreach (var (host, realm) in _servers.Values)
        {
            realm.Close();
            host.Stop();
        }
    }

    private static int Free()
    {
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)udp.Client.LocalEndPoint!).Port;
    }
}
