using System.Buffers;
using OneGame.Core;
using OneGame.Gpu;
using OneGame.Platform;

namespace OneGame.Host;

/// <summary>Process exit codes shared by games and the <c>onegame</c> command.</summary>
public static class ExitCodes
{
    /// <summary>Success.</summary>
    public const int Ok = 0;

    /// <summary>The run completed but a check failed (a scenario expectation, a replay mismatch, a test).</summary>
    public const int Failed = 1;

    /// <summary>Bad command line.</summary>
    public const int Usage = 2;

    /// <summary>The build failed.</summary>
    public const int BuildFailed = 3;

    /// <summary>The game or tool crashed or could not start (no display, no project, unreadable file).</summary>
    public const int RuntimeError = 4;

    /// <summary>The requested feature exists only in development builds and this build has none of it.</summary>
    public const int NoDevTools = 5;
}

/// <summary>The command line of a game: <c>--name value</c>, <c>--name=value</c> and bare <c>--flag</c>.</summary>
public sealed class GameArgs
{
    private readonly Dictionary<string, string?> _values = new(StringComparer.Ordinal);

    /// <summary>Parses arguments. Anything not starting with <c>--</c> and not the value of an option is a positional argument.</summary>
    public GameArgs(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var positional = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (!a.StartsWith("--", StringComparison.Ordinal))
            {
                positional.Add(a);
                continue;
            }

            var eq = a.IndexOf('=', StringComparison.Ordinal);
            if (eq > 0)
            {
                _values[a[2..eq]] = a[(eq + 1)..];
            }
            else if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                _values[a[2..]] = args[++i];
            }
            else
            {
                _values[a[2..]] = null;
            }
        }

        Positional = positional;
    }

    /// <summary>Arguments that are not options.</summary>
    public IReadOnlyList<string> Positional { get; }

    /// <summary>The option names given.</summary>
    public IEnumerable<string> Names => _values.Keys;

    /// <summary>Whether the option was given.</summary>
    public bool Has(string name) => _values.ContainsKey(name);

    /// <summary>The option's value, or null if absent or bare.</summary>
    public string? Get(string name) => _values.GetValueOrDefault(name);

    /// <summary>The option as a non-negative integer, or <paramref name="fallback"/> when absent.</summary>
    /// <exception cref="FormatException">The value is not an integer.</exception>
    public long GetInt(string name, long fallback)
    {
        var v = Get(name);
        if (v is null)
        {
            return fallback;
        }

        return long.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n >= 0
            ? n
            : throw new FormatException($"--{name} needs a whole number, got '{v}'.");
    }
}

/// <summary>
/// The hook through which development builds add their tools (live control, scenarios, capture, replay). Release builds never set
/// <see cref="GameApp.DevTools"/>, so none of that code is part of them.
/// </summary>
public interface IDevTools
{
    /// <summary>Runs a development command line against a game and returns the exit code.</summary>
    int Run(GameInstance game, GameArgs args, TextWriter output);
}

/// <summary>The entry point of a game: parse the command line, create the instance, run it.</summary>
public static class GameApp
{
    private static readonly string[] BaseOptions = ["headless", "frames", "size", "scale", "seed", "json", "gpu", "backend", "display", "present", "connect", "scope", "also-scopes", "name", "net", "realtime", "until-tick", "no-present"];

    /// <summary>
    /// Set by development builds before <see cref="Run(GameDefinition, string[])"/> so the extra options work. A release build leaves it
    /// null, and those options then fail with <see cref="ExitCodes.NoDevTools"/>.
    /// </summary>
    public static Func<IDevTools>? DevTools { get; set; }

    /// <summary>
    /// Runs a game. Options: <c>--headless</c>, <c>--frames N</c> (stop after N frames), <c>--size 1280x720</c>, <c>--scale 1.5</c>
    /// (headless UI scale), <c>--seed N</c>, <c>--gpu auto|off|required</c>, <c>--backend wayland|x11|cocoa</c> and <c>--display NAME</c> (which window system and display
    /// a windowed run opens on; the environment decides without them), <c>--present vsync|lowlatency|immediate</c>, <c>--json</c> (print one result object),
    /// <c>--connect udp://host:port|ws://host:port</c> (play on a server: the world is a replica of its scope), with <c>--scope N</c>, <c>--name N</c> and
    /// <c>--net latency=100,loss=0.02</c> (network conditions on this client's traffic). <c>--realtime</c> makes a headless run follow the clock instead of
    /// running its frames as fast as it can, and <c>--until-tick N</c> (a connected client; implies <c>--realtime</c>) stops once the replica reaches tick N or the
    /// connection closes; a release build has no live control, so this is how a connected client is run and checked.
    /// </summary>
    public static int Run(GameDefinition game, string[] args) => Run(game, args, Console.Out, Console.Error);

    /// <summary>Runs a game with explicit output streams.</summary>
    public static int Run(GameDefinition game, string[] args, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(args);
        var json = Array.IndexOf(args, "--json") >= 0;
        GameArgs parsed;
        GameOptions options;
        try
        {
            parsed = new GameArgs(args);
            options = ParseOptions(parsed);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return Fail(json, output, error, ExitCodes.Usage, "bad_args", ex.Message);
        }

        var extra = parsed.Names.Where(n => Array.IndexOf(BaseOptions, n) < 0).ToArray();
        if (extra.Length > 0 && DevTools is null)
        {
            return Fail(json, output, error, ExitCodes.NoDevTools, "no_dev_tools",
                $"{string.Join(", ", extra.Select(n => "--" + n))}: this build has no development tools. Build the Debug configuration (or set OneGameDev=true).");
        }

        if (OperatingSystem.IsBrowser())
        {
            // A browser cannot block: the page creates the GPU device asynchronously and drives the frames, so Main returns at once.
            BrowserHost.Start(game, options, parsed, extra.Length > 0 ? DevTools : null, output);
            return ExitCodes.Ok;
        }

        try
        {
            using var instance = GameInstance.Create(game, options);
            if (extra.Length > 0)
            {
                return DevTools!().Run(instance, parsed, output);
            }

            var frames = parsed.GetInt("frames", 0);
            var untilTick = parsed.GetInt("until-tick", 0);
            if (untilTick > 0 && options.Connect is null)
            {
                return Fail(json, output, error, ExitCodes.Usage, "bad_args", "--until-tick only applies with --connect <address>.");
            }

            if (options.Headless && (parsed.Has("realtime") || untilTick > 0))
            {
                var run = 0L;
                while (!instance.QuitRequested && (frames == 0 || run++ < frames) && instance.Remote is not { IsClosed: true } && (untilTick == 0 || instance.World.Tick < untilTick))
                {
                    instance.RunRealtime(1);
                }
            }
            else if (options.Headless)
            {
                instance.Step((int)Math.Min(frames, int.MaxValue));
            }
            else
            {
                instance.RunRealtime(frames);
            }

            WriteSummary(instance, json, output);
            return ExitCodes.Ok;
        }
        catch (Exception ex) when (ex is Platform.PlatformException or Gpu.GpuException or IOException or InvalidOperationException)
        {
            return Fail(json, output, error, ExitCodes.RuntimeError, "runtime_error", ex.Message);
        }
    }

    private static GameOptions ParseOptions(GameArgs a)
    {
        var o = new GameOptions { Headless = a.Has("headless") };
        if (a.Get("size") is { } size)
        {
            var (w, h) = Resolution.Parse(size);
            o = o with { Width = w, Height = h };
        }

        if (a.Get("scale") is { } scale)
        {
            o = o with { Scale = float.TryParse(scale, NumberStyles.Float, CultureInfo.InvariantCulture, out var s) && s > 0 ? s : throw new FormatException($"--scale needs a positive number, got '{scale}'.") };
        }

        if (a.Has("seed"))
        {
            o = o with { Seed = (ulong)a.GetInt("seed", 0) };
        }

        if (a.Get("backend") is { } backend)
        {
            o = o with
            {
                Backend = backend switch
                {
                    "wayland" => PlatformBackendKind.Wayland,
                    "x11" => PlatformBackendKind.X11,
                    "cocoa" => PlatformBackendKind.Cocoa,
                    var other => throw new FormatException($"--backend is wayland, x11 or cocoa, got '{other}'."),
                },
            };
        }

        if (a.Has("no-present"))
        {
            o = o with { PresentFrames = false };
        }

        if (a.Get("display") is { } display)
        {
            o = o with { DisplayName = display };
        }

        if (a.Get("present") is { } present)
        {
            o = o with
            {
                PresentMode = present switch
                {
                    "vsync" => PresentMode.Vsync,
                    "lowlatency" => PresentMode.LowLatency,
                    "immediate" => PresentMode.Immediate,
                    var other => throw new FormatException($"--present is vsync, lowlatency or immediate, got '{other}'."),
                },
            };
        }

        if (a.Get("connect") is { } address)
        {
            o = o with
            {
                Connect = new ConnectOptions
                {
                    Address = address,
                    Scope = (uint)a.GetInt("scope", 1),
                    AlsoScopes = a.Get("also-scopes") is { } also ? [.. also.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(t => uint.Parse(t, CultureInfo.InvariantCulture))] : [],
                    Name = a.Get("name") ?? "client",
                    Conditions = a.Get("net") is { } net ? RemoteSession.ParseConditions(net) : null,
                },
            };
        }
        else if (a.Has("scope") || a.Has("also-scopes") || a.Has("net") || a.Has("name"))
        {
            throw new FormatException("--scope, --also-scopes, --name and --net only apply with --connect <address>.");
        }

        return a.Get("gpu") switch
        {
            null or "auto" => o,
            "off" => o with { Gpu = GpuMode.Off },
            "required" => o with { Gpu = GpuMode.Required },
            var other => throw new FormatException($"--gpu is auto, off or required, got '{other}'."),
        };
    }

    private static void WriteSummary(GameInstance g, bool json, TextWriter output)
    {
        var hash = g.World.ComputeHash().ToString("x16", CultureInfo.InvariantCulture);
        if (!json)
        {
            output.WriteLine($"{g.Definition.Name}: {g.Frame} frames, tick {g.World.Tick}, hash {hash}");
            return;
        }

        var buffer = new ArrayBufferWriter<byte>();
        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();
            w.WriteBoolean("ok", true);
            w.WriteString("game", g.Definition.Name);
            w.WriteBoolean("headless", g.Options.Headless);
            w.WriteNumber("frames", g.Frame);
            w.WriteNumber("tick", g.World.Tick);
            w.WriteString("hash", hash);
            if (g.Remote is { } remote)
            {
                // What a connected client shows at exit: the same fields as the live net_status, so a cluster can compare it with its server.
                var net = remote.Status();
                w.WriteString("connection", net.Connection);
                w.WriteNumber("scope", net.Scope);
                w.WriteString("replica", net.Replica);
                w.WriteString("viewHash", net.Hash);
                w.WriteNumber("resyncs", net.Resyncs);
                w.WriteNumber("drift", net.Drift);
                w.WriteStartArray("followed");
                foreach (var f in net.Followed)
                {
                    w.WriteStartObject();
                    w.WriteNumber("scope", f.Scope);
                    w.WriteString("replica", f.Replica);
                    w.WriteNumber("tick", f.Tick);
                    w.WriteEndObject();
                }

                w.WriteEndArray();
            }

            w.WriteNumber("width", g.Window.Width);
            w.WriteNumber("height", g.Window.Height);
            w.WriteEndObject();
        }

        output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    /// <summary>Prints a structured error (a JSON object with <c>ok:false</c>) or a plain message, and returns the exit code.</summary>
    public static int Fail(bool json, TextWriter output, TextWriter error, int exitCode, string code, string message)
    {
        if (json)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var w = new Utf8JsonWriter(buffer))
            {
                w.WriteStartObject();
                w.WriteBoolean("ok", false);
                w.WriteStartObject("error");
                w.WriteString("code", code);
                w.WriteString("message", message);
                w.WriteEndObject();
                w.WriteEndObject();
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
        }
        else
        {
            error.WriteLine($"error: {message}");
        }

        return exitCode;
    }
}
