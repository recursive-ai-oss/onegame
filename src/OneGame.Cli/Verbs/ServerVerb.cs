using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using OneGame.Host;

namespace OneGame.Cli.Verbs;

/// <summary>
/// Game servers: start one from a configuration, and look at or change a running one (plugins included) through its live socket.
/// Every client subcommand prints the operation's JSON result.
/// </summary>
internal sealed class ServerVerb : IVerb
{
    public string Name => "server";

    public string Summary => "Game servers: run --config f.yaml | info | hash | step N | plugins | load <name> <path> | reload <name> | unload <name> | send <entity> <command> | state | log | stop  (--socket <path>).";

    public int Run(string[] args, bool json, TextWriter output, TextWriter error)
    {
        if (args.Length == 0)
        {
            throw Usage();
        }

        var sub = args[0];
        var rest = args[1..];
        if (sub == "run")
        {
            return RunServer(rest, error);
        }

        string? socket = null;
        var positional = new List<string>();
        var named = new Dictionary<string, string>(StringComparer.Ordinal);
        var settings = new List<(string, string)>();
        for (var i = 0; i < rest.Length; i++)
        {
            if (rest[i] == "--socket" && i + 1 < rest.Length)
            {
                socket = rest[++i];
            }
            else if (rest[i] == "--set" && i + 1 < rest.Length)
            {
                var kv = rest[++i];
                var eq = kv.IndexOf('=', StringComparison.Ordinal);
                if (eq <= 0)
                {
                    throw new CliException(ExitCodes.Usage, "bad_args", "--set needs name=value.");
                }

                settings.Add((kv[..eq], kv[(eq + 1)..]));
            }
            else if (rest[i].StartsWith("--", StringComparison.Ordinal) && i + 1 < rest.Length)
            {
                named[rest[i][2..]] = rest[++i];
            }
            else
            {
                positional.Add(rest[i]);
            }
        }

        string P(int i, string what) => i < positional.Count ? positional[i] : throw new CliException(ExitCodes.Usage, "bad_args", $"Missing {what}. {Summary}");

        var (op, opArgs) = sub switch
        {
            "info" => ("server_info", "{}"),
            "hash" => ("hash", "{}"),
            "step" => ("step", Obj(("ticks", positional.Count > 0 ? Num(P(0, "tick count")) : "1"))),
            "plugins" => ("plugin_list", "{}"),
            "load" => ("plugin_load", LoadArgs(P(0, "plugin name"), P(1, "plugin path"), named, settings)),
            "reload" => ("plugin_reload", named.TryGetValue("path", out var np) ? Obj(("name", Str(P(0, "plugin name"))), ("path", Str(Path.GetFullPath(np)))) : Obj(("name", Str(P(0, "plugin name"))))),
            "unload" => ("plugin_unload", Obj(("name", Str(P(0, "plugin name"))))),
            "send" => ("command_send", SendArgs(P(0, "entity id"), P(1, "command"), positional.Skip(2), named)),
            "state" => ("state_query", StateArgs(named)),
            "log" => ("log_tail", Obj(("max", positional.Count > 0 ? Num(P(0, "line count")) : "50"))),
            "stop" => ("shutdown", "{}"),
            "call" => (P(0, "operation name"), positional.Count > 1 ? positional[1] : "{}"),
            _ => throw Usage(),
        };

        socket ??= Discover();
        output.WriteLine(Call(socket, op, opArgs));
        _ = json;
        return ExitCodes.Ok;
    }

    private static CliException Usage() =>
        new(ExitCodes.Usage, "bad_args", "Usage: onegame server run --config <file> [--live [socket]] [--ticks N] [--manual] [--runner <dll>], or onegame server <info|hash|step|plugins|load|reload|unload|send|state|log|stop|call> [--socket <path>].");

    private static string Str(string s) => JsonSerializer.Serialize(s, CliJson.Default.String);

    private static string Num(string s) =>
        long.TryParse(s, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var n)
            ? n.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : throw new CliException(ExitCodes.Usage, "bad_args", $"'{s}' is not a whole number.");

    private static string Obj(params (string Key, string RawValue)[] fields) =>
        "{" + string.Join(",", fields.Select(f => Str(f.Key) + ":" + f.RawValue)) + "}";

    private static string LoadArgs(string name, string path, Dictionary<string, string> named, List<(string, string)> settings)
    {
        var fields = new List<(string, string)> { ("name", Str(name)), ("path", Str(Path.GetFullPath(path))) };
        if (named.TryGetValue("scopes", out var scopes))
        {
            fields.Add(("scopes", "[" + string.Join(",", scopes.Split(',').Select(Num)) + "]"));
        }

        if (settings.Count > 0)
        {
            fields.Add(("settings", Obj([.. settings.Select(s => (s.Item1, Str(s.Item2)))])));
        }

        return Obj([.. fields]);
    }

    private static string SendArgs(string entity, string command, IEnumerable<string> commandArgs, Dictionary<string, string> named)
    {
        var fields = new List<(string, string)> { ("entity", Num(entity)), ("command", Str(command)), ("args", "[" + string.Join(",", commandArgs.Select(Str)) + "]") };
        if (named.TryGetValue("scope", out var scope))
        {
            fields.Add(("scope", Num(scope)));
        }

        return Obj([.. fields]);
    }

    private static string StateArgs(Dictionary<string, string> named)
    {
        var fields = new List<(string, string)>();
        if (named.TryGetValue("scope", out var scope))
        {
            fields.Add(("scope", Num(scope)));
        }

        if (named.TryGetValue("entity", out var entity))
        {
            fields.Add(("entity", Num(entity)));
        }

        return Obj([.. fields]);
    }

    // ---- Talking to a running server ----

    private static string Call(string socketPath, string op, string argsJson)
    {
        using var socket = Connect(socketPath);
        using var stream = new NetworkStream(socket, ownsSocket: false);
        var request = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/call\",\"params\":{\"name\":" + Str(op) + ",\"arguments\":" + argsJson + "}}\n";
        var bytes = Encoding.UTF8.GetBytes(request);
        stream.Write(bytes, 0, bytes.Length);
        var line = new MemoryStream();
        var one = new byte[1];
        while (stream.Read(one, 0, 1) == 1 && one[0] != (byte)'\n')
        {
            line.WriteByte(one[0]);
            if (line.Length > 16 * 1024 * 1024)
            {
                throw new CliException(ExitCodes.RuntimeError, "bad_reply", "The server's reply is too long.");
            }
        }

        using var doc = JsonDocument.Parse(line.ToArray());
        var root = doc.RootElement;
        if (root.TryGetProperty("error", out var rpcError))
        {
            throw new CliException(ExitCodes.RuntimeError, "rpc_error", rpcError.TryGetProperty("message", out var m) ? m.GetString() ?? "error" : "error");
        }

        var result = root.GetProperty("result");
        var text = result.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
        if (result.TryGetProperty("isError", out var isError) && isError.GetBoolean())
        {
            using var inner = JsonDocument.Parse(text);
            var e = inner.RootElement.GetProperty("error");
            throw new CliException(ExitCodes.Failed, e.GetProperty("code").GetString() ?? "error", e.GetProperty("message").GetString() ?? "");
        }

        return text;
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
                    Call(path, "server_info", "{}");
                    live.Add(path);
                }
                catch (CliException)
                {
                    // Not a server, or a leftover socket.
                }
            }
        }

        return live.Count switch
        {
            1 => live[0],
            0 => throw new CliException(ExitCodes.RuntimeError, "no_live_server", "No game server is listening. Start one with 'onegame server run --config <file> --live'."),
            _ => throw new CliException(ExitCodes.Usage, "many_live_servers", $"{live.Count} servers are listening; choose one with --socket: {string.Join(", ", live)}"),
        };
    }

    // ---- Starting a server ----

    private static int RunServer(string[] args, TextWriter error)
    {
        string? runner = Environment.GetEnvironmentVariable("ONEGAME_SERVER");
        var pass = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--runner" && i + 1 < args.Length)
            {
                runner = args[++i];
            }
            else
            {
                pass.Add(args[i]);
            }
        }

        var info = new ProcessStartInfo { UseShellExecute = false };
        if (runner is null)
        {
            // Beside the tool, or in server-host/ where the tool package carries it.
            var beside = Path.Combine(AppContext.BaseDirectory, "onegame-server.dll");
            var packaged = ExportVerb.PackagedHost();
            if (File.Exists(beside))
            {
                runner = beside;
            }
            else if (packaged is not null)
            {
                runner = Path.Combine(packaged, "onegame-server.dll");
            }
        }

        if (runner is not null)
        {
            if (runner.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                info.FileName = Proc.Dotnet;
                info.ArgumentList.Add(runner);
            }
            else
            {
                info.FileName = runner;
            }
        }
        else
        {
            // From a source checkout: build and run the runner project.
            var root = typeof(ServerVerb).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(m => m.Key == "OneGameRoot")?.Value;
            var project = root is null ? null : Path.Combine(root, "src", "OneGame.Server.Runner");
            if (project is null || !Directory.Exists(project))
            {
                throw new CliException(ExitCodes.Usage, "no_runner", "Cannot find onegame-server. Pass --runner <onegame-server.dll> or set ONEGAME_SERVER.");
            }

            info.FileName = Proc.Dotnet;
            foreach (var a in new[] { "run", "--project", project, "--" })
            {
                info.ArgumentList.Add(a);
            }
        }

        foreach (var a in pass)
        {
            info.ArgumentList.Add(a);
        }

        using var process = Process.Start(info) ?? throw new CliException(ExitCodes.RuntimeError, "start_failed", "The server process did not start.");
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            error.WriteLine($"the server exited with code {process.ExitCode}");
        }

        return process.ExitCode;
    }
}
