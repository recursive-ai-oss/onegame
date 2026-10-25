using System.Diagnostics;
using System.Net.Sockets;
using OneGame.Host;

namespace OneGame.Cli.Verbs;

/// <summary>
/// An MCP server on standard input and output for one running development game. It is a byte pipe to the game's local socket: the game
/// itself speaks MCP (one JSON-RPC message per line), so an agent's MCP client can be configured with this command and nothing else.
/// </summary>
internal sealed class McpVerb : IVerb
{
    public string Name => "mcp";

    public string Summary => "MCP server on stdio for a running Debug game: --socket <path>, or --project <dir> to start one, or no option to attach to the only live game; --cluster <file|folder> serves every node of a process cluster (tools take a 'node' argument).";

    public int Run(string[] args, bool json, TextWriter output, TextWriter error)
    {
        string? socket = null, projectArg = null, clusterArg = null;
        var headless = false;
        var realtime = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--socket" when i + 1 < args.Length:
                    socket = args[++i];
                    break;
                case "--project" when i + 1 < args.Length:
                    projectArg = args[++i];
                    break;
                case "--cluster" when i + 1 < args.Length:
                    clusterArg = args[++i];
                    break;
                case "--headless":
                    headless = true;
                    break;
                case "--realtime":
                    realtime = true;
                    break;
                default:
                    throw new CliException(ExitCodes.Usage, "bad_args", $"Unknown option {args[i]}. Usage: onegame mcp [--socket <path> | --project <dir> [--headless] [--realtime] | --cluster <file|folder>].");
            }
        }

        if (clusterArg is not null)
        {
            return ClusterMcp.Serve(clusterArg, Console.OpenStandardInput(), Console.OpenStandardOutput());
        }

        Process? child = null;
        try
        {
            if (socket is null && projectArg is not null)
            {
                child = StartGame(Projects.Find(projectArg), headless, realtime, error, out socket);
            }

            socket ??= Discover();
            using var client = Connect(socket);
            Bridge(client, quitWhenDone: child is not null);
            return ExitCodes.Ok;
        }
        finally
        {
            if (child is not null)
            {
                try
                {
                    // The game was asked to quit when the agent went away; give it a moment, then make sure.
                    if (!child.WaitForExit(5000))
                    {
                        child.Kill(true);
                    }
                }
                catch (InvalidOperationException)
                {
                    // Already gone.
                }

                child.Dispose();
            }
        }
    }

    private static string PrivateDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "onegame-live");
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(dir);
        }
        else
        {
            Directory.CreateDirectory(dir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        return dir;
    }

    private static Process StartGame(ProjectRef project, bool headless, bool realtime, TextWriter error, out string socket)
    {
        var build = Projects.Build(project, "Debug");
        if (!build.Ok)
        {
            Projects.PrintBuild(build, false, error, error);
            throw new CliException(ExitCodes.BuildFailed, "build_failed", "The game did not build; fix the errors above.");
        }

        socket = Path.Combine(PrivateDirectory(), Guid.NewGuid().ToString("N")[..12] + ".sock");
        List<string> args = ["run", "--no-build", "--project", project.File, "-c", "Debug", "--", "--live", socket, "--json"];
        if (headless)
        {
            args.Add("--headless");
        }

        if (realtime)
        {
            args.Add("--realtime");
        }

        var child = Process.Start(Proc.Start(Proc.Dotnet, args, project.Directory))
            ?? throw new CliException(ExitCodes.RuntimeError, "start_failed", "Could not start the game.");
        child.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                error.WriteLine(e.Data);
            }
        };
        child.BeginErrorReadLine();

        // The game prints one JSON line when it is listening.
        var started = Environment.TickCount64;
        while (Environment.TickCount64 - started < 60_000)
        {
            var read = child.StandardOutput.ReadLineAsync();
            if (!read.Wait(1000))
            {
                if (child.HasExited)
                {
                    break;
                }

                continue;
            }

            var line = read.Result;
            if (line is null)
            {
                break;
            }

            if (line.StartsWith('{') && line.Contains("\"live\"", StringComparison.Ordinal))
            {
                _ = Task.Run(async () =>
                {
                    // Keep the pipe drained so the game never blocks on its output.
                    while (await child.StandardOutput.ReadLineAsync().ConfigureAwait(false) is not null)
                    {
                    }
                });
                return child;
            }
        }

        child.Kill(true);
        throw new CliException(ExitCodes.RuntimeError, "start_failed", "The game did not start listening within 60 seconds. Run 'onegame run --live' to see why.");
    }

    private static string Discover()
    {
        var dir = Path.Combine(Path.GetTempPath(), "onegame-live");
        var live = new List<string>();
        if (Directory.Exists(dir))
        {
            foreach (var path in Directory.GetFiles(dir, "*.sock"))
            {
                try
                {
                    using var probe = Connect(path);
                    live.Add(path);
                }
                catch (CliException)
                {
                    // A leftover socket from a game that is gone.
                }
            }
        }

        return live.Count switch
        {
            1 => live[0],
            0 => throw new CliException(ExitCodes.RuntimeError, "no_live_game", "No game is listening. Start one with 'onegame run --live' (or use 'onegame mcp --project <dir>')."),
            _ => throw new CliException(ExitCodes.Usage, "many_live_games", $"{live.Count} games are listening; choose one with --socket: {string.Join(", ", live)}"),
        };
    }

    private static Socket Connect(string path)
    {
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            socket.Connect(new UnixDomainSocketEndPoint(path));
            return socket;
        }
        catch (SocketException ex)
        {
            socket.Dispose();
            throw new CliException(ExitCodes.RuntimeError, "connect_failed", $"Cannot connect to {path}: {ex.Message}");
        }
    }

    private static void Bridge(Socket socket, bool quitWhenDone)
    {
        using var network = new NetworkStream(socket, ownsSocket: false);
        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();
        var upstream = new Thread(() =>
        {
            var buffer = new byte[16 * 1024];
            try
            {
                int n;
                while ((n = stdin.Read(buffer, 0, buffer.Length)) > 0)
                {
                    network.Write(buffer, 0, n);
                }

                // The agent closed its end. A game this process started goes with it.
                if (quitWhenDone)
                {
                    var quit = Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\",\"id\":\"bye\",\"method\":\"tools/call\",\"params\":{\"name\":\"quit\",\"arguments\":{}}}\n");
                    network.Write(quit, 0, quit.Length);
                }

                socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
            {
                // The game went away first.
            }
        })
        { IsBackground = true };
        upstream.Start();

        var down = new byte[16 * 1024];
        try
        {
            int n;
            while ((n = network.Read(down, 0, down.Length)) > 0)
            {
                stdout.Write(down, 0, n);
                stdout.Flush();
            }
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
        {
            // Closed.
        }
    }
}
