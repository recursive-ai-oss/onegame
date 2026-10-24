using System.Buffers;
using OneGame.Core;

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
    private static readonly string[] BaseOptions = ["headless", "frames", "size", "scale", "seed", "json", "gpu"];

    /// <summary>
    /// Set by development builds before <see cref="Run(GameDefinition, string[])"/> so the extra options work. A release build leaves it
    /// null, and those options then fail with <see cref="ExitCodes.NoDevTools"/>.
    /// </summary>
    public static Func<IDevTools>? DevTools { get; set; }

    /// <summary>
    /// Runs a game. Options: <c>--headless</c>, <c>--frames N</c> (stop after N frames), <c>--size 1280x720</c>, <c>--scale 1.5</c>
    /// (headless UI scale), <c>--seed N</c>, <c>--gpu auto|off|required</c>, <c>--json</c> (print one result object).
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

        try
        {
            using var instance = GameInstance.Create(game, options);
            if (extra.Length > 0)
            {
                return DevTools!().Run(instance, parsed, output);
            }

            var frames = parsed.GetInt("frames", 0);
            if (options.Headless)
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
