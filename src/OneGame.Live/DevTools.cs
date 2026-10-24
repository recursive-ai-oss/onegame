using System.Buffers;
using System.Text.Json.Serialization;
using OneGame.Control;
using OneGame.Host;
using OneGame.Testing;

namespace OneGame.Live;

/// <summary>
/// Turns the development options of a game's command line into actions: <c>--live</c>, <c>--scenario</c>, <c>--capture</c>,
/// <c>--sequence</c>, <c>--record</c> and <c>--replay</c>. A development build calls <see cref="Dev.Enable"/> at start-up; a release build
/// does not reference this assembly at all.
/// </summary>
internal sealed class DevTools : IDevTools
{
    private static readonly string[] Known =
    [
        "live", "realtime", "scenario", "artifacts", "update-references", "record", "capture", "sequence", "count", "every", "replay",
        "fixture", "screen", "after",
    ];

    public int Run(GameInstance game, GameArgs args, TextWriter output)
    {
        var json = args.Has("json");
        try
        {
            foreach (var name in args.Names)
            {
                if (name is "headless" or "frames" or "size" or "scale" or "seed" or "json" or "gpu")
                {
                    continue;
                }

                if (Array.IndexOf(Known, name) < 0)
                {
                    throw new FormatException($"Unknown option --{name}. Development options: {string.Join(" ", Known.Select(k => "--" + k))}.");
                }
            }

            var surface = new ControlSurface(game, args.Get("artifacts") ?? Directory.GetCurrentDirectory());
            if (args.Has("fixture"))
            {
                game.LoadFixture(args.Get("fixture") ?? throw new FormatException("--fixture needs a name."));
            }

            if (args.Has("screen"))
            {
                game.OpenScreen(args.Get("screen") ?? throw new FormatException("--screen needs a name."));
            }

            if (args.Has("live"))
            {
                return Live(game, surface, args, output, json);
            }

            if (args.Has("replay"))
            {
                return Replay(game, args, output, json);
            }

            if (args.Has("scenario"))
            {
                return Scenarios(game, surface, args, output, json);
            }

            var recorder = args.Has("record") ? new RunRecorder(game) : null;
            game.Step((int)Math.Min(args.GetInt("after", args.GetInt("frames", 0)), int.MaxValue));
            string? message = null;
            double? scale = double.TryParse(args.Get("scale"), NumberStyles.Float, CultureInfo.InvariantCulture, out var sc) ? sc : null;
            if (args.Has("capture"))
            {
                var path = args.Get("capture") ?? throw new FormatException("--capture needs a file name.");
                message = surface.Call("capture_frame", JsonSerializer.Serialize(new CaptureRequest(path, args.Get("size"), scale), LiveJson.Default.CaptureRequest));
            }
            else if (args.Has("sequence"))
            {
                var dir = args.Get("sequence") ?? throw new FormatException("--sequence needs a folder name.");
                message = surface.Call(
                    "capture_sequence",
                    JsonSerializer.Serialize(new SequenceRequest(dir, (int)args.GetInt("count", 30), (int)args.GetInt("every", 1), args.Get("size"), scale), LiveJson.Default.SequenceRequest));
            }

            if (recorder is not null)
            {
                var recording = recorder.Stop();
                var recordPath = Path.GetFullPath(args.Get("record") ?? throw new FormatException("--record needs a file name."), surface.OutputDirectory);
                recording.Save(recordPath);
                message = JsonSerializer.Serialize(new RecordSummary(recordPath, recording.Ticks, recording.Commands.Count), LiveJson.Default.RecordSummary);
            }

            Print(output, json, message ?? surface.Call("hash"));
            return ExitCodes.Ok;
        }
        catch (Exception ex) when (ex is ControlException or FormatException or ArgumentException or IOException or InvalidDataException or KeyNotFoundException or ScenarioException)
        {
            var (exit, code) = ex switch
            {
                ControlException c => (ExitCodes.Failed, c.Code),
                FormatException or ArgumentException => (ExitCodes.Usage, "bad_args"),
                ScenarioException => (ExitCodes.Usage, "bad_scenario"),
                _ => (ExitCodes.RuntimeError, "runtime_error"),
            };
            return GameApp.Fail(json, output, Console.Error, exit, code, ex.Message);
        }
    }

    private static void Print(TextWriter output, bool json, string resultJson) =>
        output.WriteLine(json ? "{\"ok\":true,\"result\":" + resultJson + "}" : resultJson);

    // ---- live control ----

    private static int Live(GameInstance game, ControlSurface surface, GameArgs args, TextWriter output, bool json)
    {
        using var server = new LiveServer(surface);
        server.Start(args.Get("live") ?? LiveServer.DefaultPath());
        var hello = JsonSerializer.Serialize(new LiveHello(true, server.Path!, Environment.ProcessId, game.Definition.Name, McpProtocol.LatestVersion), LiveJson.Default.LiveHello);
        output.WriteLine(json ? hello : $"listening on {server.Path} (MCP, newline-delimited JSON-RPC); connect with: onegame mcp --socket {server.Path}");
        output.Flush();
        if (args.Has("realtime"))
        {
            game.RunRealtime(args.GetInt("frames", 0), () => server.Pump());
        }
        else
        {
            // Paused: nothing advances until a client calls step, but requests, window events and redraws keep being served.
            var lastDraw = Environment.TickCount64;
            while (!game.QuitRequested)
            {
                server.Pump();
                game.DrainInput();
                if (!game.Options.Headless && Environment.TickCount64 - lastDraw >= 16)
                {
                    game.Redraw();
                    lastDraw = Environment.TickCount64;
                }

                server.WaitForWork(4);
            }
        }

        return ExitCodes.Ok;
    }

    // ---- scenarios ----

    private static int Scenarios(GameInstance game, ControlSurface surface, GameArgs args, TextWriter output, bool json)
    {
        var target = args.Get("scenario") ?? throw new FormatException("--scenario needs a file or a folder.");
        var files = new List<string>();
        if (Directory.Exists(target))
        {
            files.AddRange(Directory.GetFiles(target, "*.yaml", SearchOption.AllDirectories));
            files.AddRange(Directory.GetFiles(target, "*.yml", SearchOption.AllDirectories));
            files.Sort(StringComparer.Ordinal);
        }
        else
        {
            files.Add(target);
        }

        var artifacts = Path.GetFullPath(args.Get("artifacts") ?? "artifacts");
        var options = new ScenarioRunOptions { ArtifactsDirectory = artifacts, UpdateReferences = args.Has("update-references") };
        var main = game.CurrentScreen ?? "main";
        var width = game.Window.Width;
        var height = game.Window.Height;
        var results = new List<ScenarioResult>();
        RunRecorder? recorder = null;
        foreach (var file in files)
        {
            // Every scenario starts from the same place: a fresh world, the first screen, the original window size.
            game.Reset();
            game.OpenScreen(main);
            game.Resize(width, height);
            var scenario = ScenarioParser.Load(file);
            if (args.Has("record") && files.Count == 1)
            {
                recorder = new RunRecorder(game);
            }

            results.Add(new ScenarioRunner(surface, options).Run(scenario));
        }

        if (recorder is not null)
        {
            recorder.Stop().Save(Path.GetFullPath(args.Get("record") ?? throw new FormatException("--record needs a file name."), surface.OutputDirectory));
        }

        var failed = results.Count(r => !r.Passed);
        if (json)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var w = new Utf8JsonWriter(buffer))
            {
                w.WriteStartObject();
                w.WriteBoolean("ok", failed == 0);
                w.WriteNumber("passed", results.Count - failed);
                w.WriteNumber("failed", failed);
                w.WritePropertyName("scenarios");
                w.WriteRawValue(JsonSerializer.Serialize([.. results], ScenarioJson.Default.ScenarioResultArray));
                w.WriteEndObject();
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
        }
        else
        {
            foreach (var r in results)
            {
                output.WriteLine($"{(r.Passed ? "PASS" : "FAIL")} {r.Name} ({r.ElapsedMs} ms, tick {r.Tick}, hash {r.Hash})");
                foreach (var s in r.Steps.Where(s => !s.Ok))
                {
                    output.WriteLine($"  step {s.Index} ({s.Kind}, line {s.Line}): {s.Error}");
                }

                foreach (var e in r.Expectations.Where(e => !e.Passed))
                {
                    output.WriteLine($"  expect {e.Kind} (line {e.Line}): {e.Message}");
                    if (e.Expected is not null || e.Actual is not null)
                    {
                        output.WriteLine($"    expected: {e.Expected}");
                        output.WriteLine($"    actual:   {e.Actual}");
                    }
                }
            }

            output.WriteLine($"{results.Count - failed} passed, {failed} failed");
        }

        return failed == 0 ? ExitCodes.Ok : ExitCodes.Failed;
    }

    // ---- replay ----

    private static int Replay(GameInstance game, GameArgs args, TextWriter output, bool json)
    {
        var path = args.Get("replay") ?? throw new FormatException("--replay needs a recording file.");
        var recording = Recording.Load(path);
        if (recording.Game != game.Definition.Name)
        {
            throw new InvalidDataException($"The recording is of '{recording.Game}', but this game is '{game.Definition.Name}'.");
        }

        var result = recording.Replay(game);
        var actual = result.Actual.ToString("x16", CultureInfo.InvariantCulture);
        var summary = new ReplaySummary(
            result.Identical, result.Identical, recording.Ticks, recording.Commands.Count, result.Identical ? null : result.Tick,
            result.Identical ? null : result.Expected.ToString("x16", CultureInfo.InvariantCulture), result.Identical ? null : actual, actual);
        output.WriteLine(json
            ? JsonSerializer.Serialize(summary, LiveJson.Default.ReplaySummary)
            : result.Identical
                ? $"replay identical: {recording.Ticks} ticks, {recording.Commands.Count} commands, final hash {actual}"
                : $"replay DIFFERS at tick {result.Tick}: recorded {summary.Expected}, replayed {actual}");
        return result.Identical ? ExitCodes.Ok : ExitCodes.Failed;
    }
}

/// <summary>Turns on the development tools of a game. The scaffolded game calls this from a module initializer in debug builds only.</summary>
public static class Dev
{
    /// <summary>Makes the development options (<c>--live</c>, <c>--scenario</c>, <c>--capture</c>, <c>--replay</c>...) work in <see cref="GameApp.Run(GameDefinition, string[])"/>.</summary>
    public static void Enable() => GameApp.DevTools = static () => new DevTools();
}

internal sealed record CaptureRequest(string Path, string? Size, double? Scale);

internal sealed record SequenceRequest(string Dir, int Count, int TicksPerFrame, string? Size, double? Scale);

internal sealed record RecordSummary(string Path, int Ticks, int Commands);

internal sealed record LiveHello(bool Live, string Socket, int Pid, string Game, string Protocol);

internal sealed record ReplaySummary(bool Ok, bool Identical, int Ticks, int Commands, long? Tick, string? Expected, string? Actual, string FinalHash);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(CaptureRequest))]
[JsonSerializable(typeof(SequenceRequest))]
[JsonSerializable(typeof(RecordSummary))]
[JsonSerializable(typeof(LiveHello))]
[JsonSerializable(typeof(ReplaySummary))]
internal sealed partial class LiveJson : JsonSerializerContext
{
}
