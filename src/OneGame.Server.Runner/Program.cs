using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using OneGame.Live;
using OneGame.Server;
using OneGame.Server.Live;
using OneGame.Server.Plugins;

namespace OneGame.Server.Runner;

/// <summary>The line printed when the live listener is up, so a script or the command line can find the socket.</summary>
internal sealed record ListeningLine(string Live, int Pid);

/// <summary>The line printed when the server has stopped.</summary>
internal sealed record StoppedLine(bool Stopped, long Tick, ScopeInfo[] Scopes);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ListeningLine))]
[JsonSerializable(typeof(StoppedLine))]
internal sealed partial class RunnerJson : JsonSerializerContext
{
}

internal static class Program
{
    private const string Usage = "usage: onegame-server --config <server.yaml> [--live [socket]] [--ticks N] [--manual]\n"
        + "  --config   the server configuration (YAML)\n"
        + "  --live     listen for live control (MCP, JSON lines) on a Unix socket; the path is printed as {\"live\":...}\n"
        + "  --ticks N  run N ticks as fast as possible, then stop (without --live) or keep serving (with --live)\n"
        + "  --manual   do not tick on the clock; only the 'step' operation advances the server";

    private static int Main(string[] args)
    {
        string? config = null, live = null;
        long ticks = 0;
        var manual = false;
        var wantLive = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--config" when i + 1 < args.Length:
                    config = args[++i];
                    break;
                case "--live":
                    wantLive = true;
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        live = args[++i];
                    }

                    break;
                case "--ticks" when i + 1 < args.Length && long.TryParse(args[i + 1], out ticks) && ticks >= 0:
                    i++;
                    break;
                case "--manual":
                    manual = true;
                    break;
                default:
                    Console.Error.WriteLine("error: unknown or incomplete option " + args[i]);
                    Console.Error.WriteLine(Usage);
                    return 2;
            }
        }

        if (config is null)
        {
            Console.Error.WriteLine(Usage);
            return 2;
        }

        ServerConfig settings;
        try
        {
            settings = ServerConfig.Load(config);
        }
        catch (ServerConfigException ex)
        {
            Console.Error.WriteLine("error: " + ex.Message);
            return 2;
        }

        var host = new ServerHost(settings, loader: new PluginLoader(), log: new ServerLog(Console.Error));
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            host.RequestStop();
        };

        host.Start();
        LiveServer? server = null;
        try
        {
            if (wantLive || settings.Live is not null)
            {
                server = new LiveServer(new ServerControl(host));
                server.Start(live ?? settings.Live ?? LiveServer.DefaultPath());
                Console.Out.WriteLine(JsonSerializer.Serialize(new ListeningLine(server.Path!, Environment.ProcessId), RunnerJson.Default.ListeningLine));
                Console.Out.Flush();
            }

            if (ticks > 0)
            {
                for (long i = 0; i < ticks && !host.StopRequested; i++)
                {
                    host.StepOnce();
                    server?.Pump();
                }
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
                    server.WaitForWork(50);
                    server.Pump();
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
            Console.Out.WriteLine(JsonSerializer.Serialize(new StoppedLine(true, host.Tick, [.. host.ScopeInfos()]), RunnerJson.Default.StoppedLine));
            return 0;
        }
        finally
        {
            server?.Dispose();
        }
    }
}
