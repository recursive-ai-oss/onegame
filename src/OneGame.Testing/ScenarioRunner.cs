using System.Diagnostics;
using System.Text.Json.Serialization;
using OneGame.Control;
using OneGame.Host;
using OneGame.UI;

namespace OneGame.Testing;

/// <summary>Outcome of one step.</summary>
/// <param name="Index">Position in the scenario, from 0.</param>
/// <param name="Kind">The step's verb.</param>
/// <param name="Line">Line in the scenario file.</param>
/// <param name="Ok">False when the step failed; the scenario stops there.</param>
/// <param name="Error">What went wrong, with the control error code.</param>
/// <param name="Node">The cluster node the step was aimed at, or null.</param>
public sealed record StepResult(int Index, string Kind, int Line, bool Ok, string? Error, string? Node = null);

/// <summary>Outcome of one expectation. On failure <see cref="Expected"/> and <see cref="Actual"/> are the diff.</summary>
/// <param name="Index">Position among the final expectations, or the step index for inline ones.</param>
/// <param name="Kind">The expectation's kind.</param>
/// <param name="Line">Line in the scenario file.</param>
/// <param name="Passed">Whether it held.</param>
/// <param name="Target">What was looked at, such as <c>Shop#1.Gold</c> or <c>#buy</c>.</param>
/// <param name="Expected">What the scenario said.</param>
/// <param name="Actual">What the game had.</param>
/// <param name="Message">A sentence on a failure, or a note on a pass.</param>
/// <param name="Node">The cluster node the expectation looked at, or null.</param>
public sealed record ExpectationResult(int Index, string Kind, int Line, bool Passed, string? Target, string? Expected, string? Actual, string? Message, string? Node = null);

/// <summary>Outcome of a scenario.</summary>
/// <param name="Name">The scenario's name.</param>
/// <param name="Source">The file it came from.</param>
/// <param name="Passed">True when every step ran and every expectation held.</param>
/// <param name="Tick">The world tick at the end.</param>
/// <param name="Hash">The state hash at the end.</param>
/// <param name="ElapsedMs">Wall time of the run in milliseconds.</param>
/// <param name="Steps">Every step.</param>
/// <param name="Expectations">Every expectation that was checked.</param>
public sealed record ScenarioResult(
    string Name, string? Source, bool Passed, long Tick, string Hash, long ElapsedMs, StepResult[] Steps, ExpectationResult[] Expectations);

/// <summary>Source-generated JSON for scenario results.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ScenarioResult))]
[JsonSerializable(typeof(ScenarioResult[]))]
public sealed partial class ScenarioJson : JsonSerializerContext
{
}

/// <summary>Settings for running scenarios.</summary>
public sealed record ScenarioRunOptions
{
    /// <summary>Folder that relative paths in the scenario (snapshots, reference images) are read from. Defaults to the scenario file's folder.</summary>
    public string? BaseDirectory { get; init; }

    /// <summary>Where actual and diff images of failed capture checks, and captures saved by steps, are written.</summary>
    public string? ArtifactsDirectory { get; init; }

    /// <summary>Write reference images that are missing or differ instead of failing. Also set by the <c>ONEGAME_UPDATE_REFERENCES=1</c> environment variable.</summary>
    public bool UpdateReferences { get; init; }

    /// <summary>Loosens every capture check to at least this per-channel tolerance. References are drawn by one GPU driver; another driver may place anti-aliased edge pixels differently.</summary>
    public int MinCaptureTolerance { get; init; }

    /// <summary>Loosens every capture check to at least this fraction of differing pixels.</summary>
    public double MinCaptureMaxDiff { get; init; }
}

/// <summary>Runs scenarios against a game through the control layer, the same operations an agent calls over MCP.</summary>
public sealed class ScenarioRunner
{
    private readonly ControlSurface _surface;
    private readonly ScenarioRunOptions _options;

    /// <summary>Creates a runner for a control surface.</summary>
    public ScenarioRunner(ControlSurface surface, ScenarioRunOptions? options = null)
    {
        _surface = surface ?? throw new ArgumentNullException(nameof(surface));
        _options = options ?? new ScenarioRunOptions();
    }

    /// <summary>Runs one scenario. Steps stop at the first failure; expectations are checked only when every step ran.</summary>
    public ScenarioResult Run(Scenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        var clock = Stopwatch.StartNew();
        var baseDir = _options.BaseDirectory ?? (scenario.SourcePath is null ? Directory.GetCurrentDirectory() : Path.GetDirectoryName(Path.GetFullPath(scenario.SourcePath))!);
        var update = _options.UpdateReferences || Environment.GetEnvironmentVariable("ONEGAME_UPDATE_REFERENCES") == "1";
        var ctx = new Context(baseDir, update);
        var steps = new List<StepResult>();
        var results = new List<ExpectationResult>();
        var game = _surface.Game;

        var setup = Guarded(() =>
        {
            if (scenario.Window is not null)
            {
                var (w, h) = Resolution.Parse(scenario.Window);
                game.Resize(w, h);
            }

            if (scenario.Seed is { } seed)
            {
                _surface.Call("reset", Obj(("seed", seed.ToString(CultureInfo.InvariantCulture))));
            }

            if (scenario.Fixture is not null)
            {
                _surface.Call("fixture_load", Obj(("name", scenario.Fixture)));
            }

            if (scenario.Snapshot is not null)
            {
                LoadSnapshot(scenario.Snapshot, baseDir);
            }
        });
        var failed = setup is not null;
        if (failed)
        {
            steps.Add(new StepResult(-1, "setup", 0, false, setup));
        }

        for (var i = 0; i < scenario.Steps.Count && !failed; i++)
        {
            var step = scenario.Steps[i];
            var index = i;
            var error = Guarded(() => Execute(step, index, ctx, scenario, results));
            steps.Add(new StepResult(i, step.Kind.ToString().ToLowerInvariant(), step.Line, error is null, error));
            failed = error is not null;
        }

        if (!failed)
        {
            for (var i = 0; i < scenario.Expect.Count; i++)
            {
                results.Add(Check(scenario.Expect[i], i, scenario.Name, ctx, scenario));
            }
        }

        var passed = !failed && results.TrueForAll(r => r.Passed);
        return new ScenarioResult(
            scenario.Name, scenario.SourcePath, passed, game.World.Tick, game.World.ComputeHash().ToString("x16", CultureInfo.InvariantCulture),
            clock.ElapsedMilliseconds, [.. steps], [.. results]);
    }

    private sealed record Context(string BaseDir, bool Update);

    private static string? Guarded(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (ControlException ex)
        {
            return $"{ex.Code}: {ex.Message}";
        }
        catch (Exception ex) when (ex is ArgumentException or UiStyleException or KeyNotFoundException or InvalidDataException or IOException)
        {
            return ex.Message;
        }
    }

    private void LoadSnapshot(string path, string baseDir)
    {
        var full = Path.GetFullPath(path, baseDir);
        if (!File.Exists(full))
        {
            throw new ControlException("not_found", $"No snapshot file {full}.");
        }

        _surface.Call("snapshot_load", Obj(("base64", Convert.ToBase64String(File.ReadAllBytes(full)))));
    }

    internal static string Obj(params (string Key, object? Value)[] fields)
    {
        var sb = new StringBuilder("{");
        for (var i = 0; i < fields.Length; i++)
        {
            sb.Append(i > 0 ? "," : "").Append(JsonSerializer.Serialize(fields[i].Key, ScenarioHelperJson.Default.String)).Append(':');
            switch (fields[i].Value)
            {
                case null:
                    sb.Append("null");
                    break;
                case string s:
                    sb.Append(JsonSerializer.Serialize(s, ScenarioHelperJson.Default.String));
                    break;
                case string[] arr:
                    sb.Append(JsonSerializer.Serialize(arr, ScenarioHelperJson.Default.StringArray));
                    break;
                case bool b:
                    sb.Append(b ? "true" : "false");
                    break;
                case IFormattable f:
                    sb.Append(f.ToString(null, CultureInfo.InvariantCulture));
                    break;
                default:
                    throw new InvalidOperationException("Unsupported argument type.");
            }
        }

        return sb.Append('}').ToString();
    }

    private void Execute(ScenarioStep s, int index, Context ctx, Scenario scenario, List<ExpectationResult> inline)
    {
        if (s.Node is not null || s.Kind == StepKind.Wait)
        {
            throw new ControlException("bad_args", $"Line {s.Line}: this step is aimed at a cluster node ('{s.Node}'). Run it against a process cluster (onegame cluster test), not a single game.");
        }

        switch (s.Kind)
        {
            case StepKind.Click:
                _surface.Call("ui_click", s.Id is not null ? Obj(("id", s.Id)) : Obj(("selector", s.Selector)));
                break;
            case StepKind.ClickAt:
                _surface.Call("input_pointer", Obj(("action", "click"), ("x", s.X), ("y", s.Y)));
                break;
            case StepKind.Tap:
                _surface.Call("ui_tap", s.Id is not null ? Obj(("id", s.Id)) : Obj(("selector", s.Selector)));
                break;
            case StepKind.TapAt:
                _surface.Call("input_touch", Obj(("action", "tap"), ("x", s.X), ("y", s.Y)));
                break;
            case StepKind.Touch:
                _surface.Call("input_touch", Obj(("action", s.Text), ("id", s.Finger), ("x", s.X), ("y", s.Y)));
                break;
            case StepKind.Invoke:
                _surface.Call("ui_invoke", s.Id is not null ? Obj(("id", s.Id)) : Obj(("selector", s.Selector)));
                break;
            case StepKind.Press:
                _surface.Call("input_key", Obj(("key", s.Text), ("modifiers", s.Modifiers)));
                break;
            case StepKind.Type:
                _surface.Call("input_text", Obj(("text", s.Text)));
                break;
            case StepKind.Nav:
                _surface.Call("input_nav", Obj(("action", s.Text)));
                break;
            case StepKind.Command:
                _surface.Call("command_send", Obj(("entity", s.Entity), ("command", s.Name), ("args", s.Args)));
                break;
            case StepKind.Set:
                _surface.Call("state_patch", Obj(("entity", s.Entity), ("property", s.Name), ("value", s.Args[0])));
                break;
            case StepKind.Screen:
                _surface.Call("screen_open", Obj(("name", s.Text)));
                break;
            case StepKind.Fixture:
                _surface.Call("fixture_load", Obj(("name", s.Text)));
                break;
            case StepKind.Snapshot:
                LoadSnapshot(s.Path!, ctx.BaseDir);
                break;
            case StepKind.Reset:
                _surface.Call("reset", Obj(("seed", s.Text)));
                break;
            case StepKind.UiScale:
                _surface.Call("ui_scale", s.Scale is { } scale ? Obj(("scale", scale)) : Obj(("reset", true)));
                break;
            case StepKind.Step:
                _surface.Call("step", Obj(("frames", s.Count)));
                break;
            case StepKind.Wheel:
                _surface.Call("input_pointer", Obj(("action", "wheel"), ("x", s.X), ("y", s.Y), ("dx", s.Dx), ("dy", s.Dy)));
                break;
            case StepKind.Capture:
                var root = Path.GetFullPath(_options.ArtifactsDirectory ?? ctx.BaseDir);
                var full = Path.GetFullPath(s.Path!, root);
                if (!full.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                {
                    throw new ControlException("path_outside", $"'{s.Path}' is outside {root}.");
                }

                var result = _surface.Invoke("capture_frame", Obj(("size", s.Size), ("scale", s.Scale)));
                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                File.WriteAllBytes(full, result.Png!);
                break;
            case StepKind.Camera:
                _surface.Call("camera", s.ControlArgs);
                break;
            case StepKind.Expect:
                var r = Check(s.Expect!, index, scenario.Name, ctx, scenario);
                inline.Add(r);
                if (!r.Passed)
                {
                    throw new ControlException("expectation_failed", r.Message ?? "an expectation failed");
                }

                break;
            default:
                throw new ControlException("bad_args", $"Unknown step {s.Kind}.");
        }
    }

    private ExpectationResult Check(ScenarioExpectation e, int index, string scenarioName, Context ctx, Scenario scenario)
    {
        try
        {
            if (e.Node is not null || e.WithinMs > 0 || e.Kind is ExpectKind.Agree or ExpectKind.Drift)
            {
                throw new ControlException("bad_args", $"Line {e.Line}: this expectation looks at a cluster ('{e.Node ?? e.Kind.ToString().ToLowerInvariant()}'). Run it against a process cluster (onegame cluster test), not a single game.");
            }

            return e.Kind switch
            {
                ExpectKind.State => CheckState(e, index),
                ExpectKind.Ui => CheckUi(e, index),
                ExpectKind.Tick => Compare(e, index, "tick", "tick", _surface.Game.World.Tick.ToString(CultureInfo.InvariantCulture), e.EqualTo!),
                ExpectKind.Hash => Compare(e, index, "hash", "hash", _surface.Game.World.ComputeHash().ToString("x16", CultureInfo.InvariantCulture), e.EqualTo!.ToLowerInvariant()),
                ExpectKind.Capture => CheckCapture(e, index, scenarioName, ctx, scenario),
                ExpectKind.Pick => CheckPick(e, index),
                _ => throw new ControlException("bad_args", "Unknown expectation."),
            };
        }
        catch (ControlException ex)
        {
            return new ExpectationResult(index, e.Kind.ToString().ToLowerInvariant(), e.Line, false, null, null, null, $"{ex.Code}: {ex.Message}");
        }
    }

    internal static ExpectationResult Compare(ScenarioExpectation e, int index, string kind, string target, string actual, string expected)
    {
        var ok = SameValue(actual, expected);
        return new ExpectationResult(index, kind, e.Line, ok, target, expected, actual, ok ? null : $"{target} is {actual}, expected {expected}");
    }

    private static bool SameValue(string actual, string expected) =>
        actual == expected
        || (double.TryParse(actual, NumberStyles.Float, CultureInfo.InvariantCulture, out var a)
            && double.TryParse(expected, NumberStyles.Float, CultureInfo.InvariantCulture, out var b)
            && Math.Abs(a - b) < 1e-9);

    private ExpectationResult CheckPick(ScenarioExpectation e, int index)
    {
        var json = _surface.Call("pick", Obj(("x", e.PickX), ("y", e.PickY), ("size", e.Size)));
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var hit = root.GetProperty("hit").GetBoolean();
        var target = $"pick({e.PickX.ToString(CultureInfo.InvariantCulture)}, {e.PickY.ToString(CultureInfo.InvariantCulture)})";
        var actual = hit ? root.GetProperty("address").GetString()! : "none";
        string expected;
        bool ok;
        if (e.Entity == "none")
        {
            expected = "none";
            ok = !hit;
        }
        else
        {
            var want = _surface.ResolveEntityId(e.Entity!);
            expected = e.Entity!;
            ok = hit && root.GetProperty("id").GetUInt64() == want.Value;
        }

        return new ExpectationResult(index, "pick", e.Line, ok, target, expected, actual, ok ? null : $"{target} is {actual}, expected {expected}");
    }

    private ExpectationResult CheckState(ScenarioExpectation e, int index)
    {
        var json = _surface.Call("state_query", Obj(("entity", e.Entity), ("property", e.Property)));
        using var doc = JsonDocument.Parse(json);
        var el = doc.RootElement;
        var actual = el.ValueKind == JsonValueKind.String ? el.GetString()! : el.GetRawText();
        var target = $"{e.Entity}.{e.Property}";
        if (e.EqualTo is not null)
        {
            return Compare(e, index, "state", target, actual, e.EqualTo);
        }

        if (!double.TryParse(actual, NumberStyles.Float, CultureInfo.InvariantCulture, out var n))
        {
            return new ExpectationResult(index, "state", e.Line, false, target, null, actual, $"{target} is '{actual}', which is not a number, so min and max cannot be checked");
        }

        if (e.Min is { } min && n < min)
        {
            return new ExpectationResult(index, "state", e.Line, false, target, $">= {min.ToString(CultureInfo.InvariantCulture)}", actual, $"{target} is {actual}, expected at least {min.ToString(CultureInfo.InvariantCulture)}");
        }

        if (e.Max is { } max && n > max)
        {
            return new ExpectationResult(index, "state", e.Line, false, target, $"<= {max.ToString(CultureInfo.InvariantCulture)}", actual, $"{target} is {actual}, expected at most {max.ToString(CultureInfo.InvariantCulture)}");
        }

        return new ExpectationResult(index, "state", e.Line, true, target, null, actual, null);
    }

    private ExpectationResult CheckUi(ScenarioExpectation e, int index)
    {
        var selector = e.Selector ?? UiTree.IdSelector(e.Id!);
        using var doc = JsonDocument.Parse(_surface.Call("ui_query", Obj(("selector", selector))));
        var items = doc.RootElement;
        var count = items.GetArrayLength();
        ExpectationResult Fail(string expected, string actual, string message) => new(index, "ui", e.Line, false, selector, expected, actual, message);

        if (e.Count is { } want && count != want)
        {
            return Fail($"{want} match(es)", $"{count} match(es)", $"'{selector}' matches {count} element(s), expected {want}");
        }

        var needsFirst = e.Text is not null || e.Contains is not null || e.Visible is not null || e.Enabled is not null || e.Focused is not null || e.Pressed is not null;
        if (!needsFirst)
        {
            return count > 0 || e.Count == 0
                ? new ExpectationResult(index, "ui", e.Line, true, selector, null, $"{count} match(es)", null)
                : Fail("at least 1 match", "0 matches", $"'{selector}' matches no element");
        }

        if (count == 0)
        {
            return Fail("an element", "no match", $"'{selector}' matches no element");
        }

        var first = items[0];
        var text = first.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString()! : "";
        if (e.Text is not null && text != e.Text)
        {
            return Fail(e.Text, text, $"'{selector}' text is \"{text}\", expected \"{e.Text}\"");
        }

        if (e.Contains is not null && !text.Contains(e.Contains, StringComparison.Ordinal))
        {
            return Fail($"contains \"{e.Contains}\"", text, $"'{selector}' text \"{text}\" does not contain \"{e.Contains}\"");
        }

        // (label, state name that is present when the property is the opposite of the label, wanted label value)
        (string Label, string State, bool? Wanted, bool Inverted)[] checks =
        [
            ("visible", "hidden", e.Visible, true),
            ("enabled", "disabled", e.Enabled, true),
            ("focused", "focus", e.Focused, false),
        ];
        foreach (var (label, state, wanted, inverted) in checks)
        {
            if (wanted is not { } w)
            {
                continue;
            }

            var actual = HasState(first, state) != inverted;
            if (actual != w)
            {
                return Fail($"{label}={Bool(w)}", $"{label}={Bool(actual)}", $"'{selector}' should be {label}={Bool(w)} but is {label}={Bool(actual)}");
            }
        }

        if (e.Pressed is { } wantPressed)
        {
            var have = first.TryGetProperty("pressed", out var pv) && pv.ValueKind is JsonValueKind.True or JsonValueKind.False ? pv.GetBoolean() : (bool?)null;
            if (have != wantPressed)
            {
                var shown = have is null ? "not a toggle" : "pressed=" + Bool(have.Value);
                return Fail($"pressed={Bool(wantPressed)}", shown, $"'{selector}' should be pressed={Bool(wantPressed)} but is {shown}");
            }
        }

        return new ExpectationResult(index, "ui", e.Line, true, selector, null, text, null);
    }

    private static string Bool(bool b) => b ? "true" : "false";

    private static bool HasState(JsonElement element, string state)
    {
        if (!element.TryGetProperty("states", out var states))
        {
            return false;
        }

        if (states.ValueKind == JsonValueKind.Array)
        {
            foreach (var s in states.EnumerateArray())
            {
                if (s.ValueKind == JsonValueKind.String && s.GetString() == state)
                {
                    return true;
                }
            }

            return false;
        }

        return states.ValueKind == JsonValueKind.Object && states.TryGetProperty(state, out var v) && v.ValueKind == JsonValueKind.True;
    }

    private ExpectationResult CheckCapture(ScenarioExpectation e, int index, string scenarioName, Context ctx, Scenario scenario)
    {
        var reference = Path.GetFullPath(e.Reference!, ctx.BaseDir);
        var result = _surface.Invoke("capture_frame", Obj(("size", e.Size ?? scenario.Window), ("scale", e.Scale)));
        var actual = Png.Decode(result.Png!, "capture");
        if (!File.Exists(reference))
        {
            if (ctx.Update)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(reference)!);
                File.WriteAllBytes(reference, result.Png!);
                return new ExpectationResult(index, "capture", e.Line, true, e.Reference, null, null, "reference written");
            }

            return new ExpectationResult(index, "capture", e.Line, false, e.Reference, "a reference image", "none", $"No reference image at {reference}. Run with ONEGAME_UPDATE_REFERENCES=1 to create it from the current frame, then look at it.");
        }

        var expected = Png.Load(reference);
        var tolerance = Math.Max(e.Tolerance, _options.MinCaptureTolerance);
        var maxDiff = Math.Max(e.MaxDiff, _options.MinCaptureMaxDiff);
        var diff = ImageCompare.Compare(actual, expected, tolerance);
        if (diff.Within(maxDiff))
        {
            return new ExpectationResult(index, "capture", e.Line, true, e.Reference, null, $"{diff.DifferingPixels} px differ, max channel delta {diff.MaxChannelDelta}", null);
        }

        if (ctx.Update)
        {
            File.WriteAllBytes(reference, result.Png!);
            return new ExpectationResult(index, "capture", e.Line, true, e.Reference, null, null, "reference updated");
        }

        string? saved = null;
        if (_options.ArtifactsDirectory is { } dir)
        {
            Directory.CreateDirectory(dir);
            var stem = Path.Combine(dir, $"{scenarioName}.{index}");
            File.WriteAllBytes(stem + ".actual.png", result.Png!);
            if (diff.SameSize)
            {
                Png.Save(ImageCompare.DiffImage(actual, expected, tolerance), stem + ".diff.png");
            }

            saved = $" Wrote {stem}.actual.png" + (diff.SameSize ? $" and {stem}.diff.png." : ".");
        }

        var message = diff.SameSize
            ? $"The frame differs from {e.Reference}: {diff.DifferingPixels} px ({diff.Fraction.ToString("P3", CultureInfo.InvariantCulture)}) differ by more than {tolerance}, max channel delta {diff.MaxChannelDelta}; allowed {maxDiff.ToString("P3", CultureInfo.InvariantCulture)}.{saved}"
            : $"The frame is {actual.Width}x{actual.Height} but {e.Reference} is {expected.Width}x{expected.Height}.{saved}";
        return new ExpectationResult(
            index, "capture", e.Line, false, e.Reference, $"within {maxDiff.ToString(CultureInfo.InvariantCulture)} of reference",
            diff.SameSize ? $"{diff.DifferingPixels} px differ (max delta {diff.MaxChannelDelta})" : "different size", message);
    }
}

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(string[]))]
internal sealed partial class ScenarioHelperJson : JsonSerializerContext
{
}
