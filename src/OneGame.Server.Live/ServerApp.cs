using System.Text.Json.Serialization;
using OneGame.Live;

namespace OneGame.Server.Live;

/// <summary>The line printed on standard output once the server is up, so a launcher can find its live socket and listening ports.</summary>
/// <param name="Ready">Always true.</param>
/// <param name="Pid">The process id.</param>
/// <param name="Live">The live-control socket path, or null when none was asked for.</param>
/// <param name="Listen">The bound listening addresses.</param>
public sealed record ServerReadyLine(bool Ready, int Pid, string? Live, ListenEndpoint[] Listen);

/// <summary>The line printed when the server has stopped.</summary>
/// <param name="Stopped">Always true.</param>
/// <param name="Tick">The last tick.</param>
/// <param name="Scopes">The scopes' final state.</param>
public sealed record ServerStoppedLine(bool Stopped, long Tick, ScopeInfo[] Scopes);

/// <summary>Source-generated JSON for the lines a server process prints.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ServerReadyLine))]
[JsonSerializable(typeof(ServerStoppedLine))]
public sealed partial class ServerAppJson : JsonSerializerContext
{
}

/// <summary>
/// The whole <c>Main</c> of a dedicated server: parse the command line, build a <see cref="ServerHost"/>, listen for clients, serve
/// live control, run until stopped. A game's server project calls <c>ServerApp.Run(setup, args)</c> with the game's entity types; the
/// <c>onegame-server</c> tool calls it with a plugin loader and no game types.
/// </summary>
public static class ServerApp
{
    private const string Usage = "usage: <server> [--config <server.yaml>] [--name N] [--scope ID[:SEED]]... [--listen udp:PORT|ws:PORT[@ADDRESS]]... [--tick-rate N] [--hash-history N]\n"
        + "          [--option KEY=VALUE]... [--live [socket]] [--ticks N] [--manual] [--parent PID]\n"
        + "  --config        the server configuration (YAML); the other options override it\n"
        + "  --listen        accept clients on a reliable-UDP or WebSocket port (0 picks a free one; the bound ports are printed)\n"
        + "  --option        a KEY=VALUE setting for the game's own server code (repeatable)\n"
        + "  --live          listen for live control (MCP, JSON lines) on a Unix socket; the path is printed in the ready line\n"
        + "  --ticks N       run N ticks as fast as possible, then stop (without --live) or keep serving (with --live)\n"
        + "  --manual        do not tick on the clock; only the 'step' operation advances the server\n"
        + "  --parent PID    stop when that process is gone";

    /// <summary>Runs a server process and returns its exit code.</summary>
    /// <param name="setup">The game's entity types and world setup, or null for a server that only runs plugins.</param>
    /// <param name="args">The command line.</param>
    /// <param name="loader">Loads plugins; null for a server that cannot (a NativeAOT build).</param>
    public static int Run(ServerSetup? setup, string[] args, IPluginLoader? loader = null) => Run(setup, args, loader, Console.Out, Console.Error);

    /// <summary>Runs a server process with explicit output streams.</summary>
    public static int Run(ServerSetup? setup, string[] args, IPluginLoader? loader, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(args);
        ServerConfig settings;
        string? live = null;
        long ticks = 0;
        var manual = false;
        var wantLive = false;
        var parent = 0;
        try
        {
            string? config = null;
            string? name = null;
            int? tickRate = null, history = null;
            var scopes = new List<ScopeConfig>();
            var listen = new List<ListenConfig>();
            var options = new List<KeyValuePair<string, string>>();
            for (var i = 0; i < args.Length; i++)
            {
                string Value() => i + 1 < args.Length ? args[++i] : throw new FormatException(args[i] + " needs a value");
                switch (args[i])
                {
                    case "--config":
                        config = Value();
                        break;
                    case "--name":
                        name = Value();
                        break;
                    case "--scope":
                        scopes.Add(ParseScope(Value()));
                        break;
                    case "--listen":
                        listen.Add(ParseListen(Value()));
                        break;
                    case "--option":
                        options.Add(ParseOption(Value()));
                        break;
                    case "--tick-rate":
                        tickRate = (int)Number(args[i], Value());
                        break;
                    case "--hash-history":
                        history = (int)Number(args[i], Value());
                        break;
                    case "--parent":
                        parent = (int)Number(args[i], Value());
                        break;
                    case "--ticks":
                        ticks = Number(args[i], Value());
                        break;
                    case "--manual":
                        manual = true;
                        break;
                    case "--live":
                        wantLive = true;
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                        {
                            live = args[++i];
                        }

                        break;
                    default:
                        throw new FormatException("unknown option " + args[i]);
                }
            }

            var baseConfig = config is null ? new ServerConfig() : ServerConfig.Load(config);
            settings = new ServerConfig
            {
                Name = name ?? baseConfig.Name,
                TickRate = tickRate ?? baseConfig.TickRate,
                MaxCatchUp = baseConfig.MaxCatchUp,
                Scopes = scopes.Count > 0 ? scopes : baseConfig.Scopes,
                Plugins = baseConfig.Plugins,
                Live = baseConfig.Live,
                Listen = listen.Count > 0 ? listen : baseConfig.Listen,
                ClientTrust = baseConfig.ClientTrust,
                HashHistory = history ?? baseConfig.HashHistory,
                Options = options.Count > 0 ? options : baseConfig.Options,
            };
        }
        catch (Exception ex) when (ex is FormatException or ServerConfigException)
        {
            error.WriteLine("error: " + ex.Message);
            error.WriteLine(Usage);
            return 2;
        }

        var host = new ServerHost(settings, setup, loader, new ServerLog(error));
        ConsoleCancelEventHandler onCancel = (_, e) =>
        {
            e.Cancel = true;
            host.RequestStop();
        };
        Console.CancelKeyPress += onCancel;
        ParentWatch? watch = parent > 0 ? new ParentWatch(parent, host.RequestStop) : null;
        LiveServer? server = null;
        try
        {
            host.Start();
            if (wantLive || settings.Live is not null)
            {
                server = new LiveServer(new ServerControl(host));
                server.Start(live ?? settings.Live ?? LiveServer.DefaultPath());
            }

            output.WriteLine(JsonSerializer.Serialize(new ServerReadyLine(true, Environment.ProcessId, server?.Path, [.. host.Endpoints]), ServerAppJson.Default.ServerReadyLine));
            output.Flush();

            for (long i = 0; i < ticks && !host.StopRequested; i++)
            {
                host.PumpNetwork();
                host.StepOnce();
                server?.Pump();
            }

            if (server is null)
            {
                if (ticks == 0 && !manual)
                {
                    host.Run();
                }
            }
            else if (manual || ticks > 0)
            {
                while (!host.StopRequested)
                {
                    host.PumpNetwork();
                    server.Pump();
                    server.WaitForWork(1);
                }
            }
            else
            {
                host.Run(between: () =>
                {
                    server.Pump();
                    server.WaitForWork(1);
                });
            }

            host.Stop();
            output.WriteLine(JsonSerializer.Serialize(new ServerStoppedLine(true, host.Tick, [.. host.ScopeInfos()]), ServerAppJson.Default.ServerStoppedLine));
            return 0;
        }
        catch (IOException ex)
        {
            error.WriteLine("error: " + ex.Message);
            return 4;
        }
        finally
        {
            Console.CancelKeyPress -= onCancel;
            watch?.Dispose();
            server?.Dispose();
        }
    }

    private static long Number(string option, string text) =>
        long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : throw new FormatException($"{option} needs a whole number, got '{text}'");

    private static KeyValuePair<string, string> ParseOption(string text)
    {
        var eq = text.IndexOf('=', StringComparison.Ordinal);
        return eq > 0 ? new KeyValuePair<string, string>(text[..eq], text[(eq + 1)..]) : throw new FormatException($"--option is KEY=VALUE, got '{text}'");
    }

    private static ScopeConfig ParseScope(string text)
    {
        var colon = text.IndexOf(':', StringComparison.Ordinal);
        var id = colon < 0 ? text : text[..colon];
        var seed = colon < 0 ? "1" : text[(colon + 1)..];
        return uint.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var i) && ulong.TryParse(seed, NumberStyles.None, CultureInfo.InvariantCulture, out var s)
            ? new ScopeConfig(i, s)
            : throw new FormatException($"--scope is ID or ID:SEED, got '{text}'");
    }

    private static ListenConfig ParseListen(string text)
    {
        var colon = text.IndexOf(':', StringComparison.Ordinal);
        if (colon < 0)
        {
            throw new FormatException($"--listen is udp:PORT or ws:PORT, got '{text}'");
        }

        var transport = text[..colon];
        var rest = text[(colon + 1)..];
        var at = rest.IndexOf('@', StringComparison.Ordinal);
        var port = at < 0 ? rest : rest[..at];
        var address = at < 0 ? "0.0.0.0" : rest[(at + 1)..];
        if (transport is not ("udp" or "ws") || !int.TryParse(port, NumberStyles.None, CultureInfo.InvariantCulture, out var p) || p > 65535)
        {
            throw new FormatException($"--listen is udp:PORT or ws:PORT (optionally @ADDRESS), got '{text}'");
        }

        return new ListenConfig(transport, p, address);
    }
}
