using OneYaml;

namespace OneGame.Testing;

/// <summary>A scenario file is wrong. The message names the line and what to change.</summary>
public sealed class ScenarioException : Exception
{
    /// <summary>Creates the exception.</summary>
    public ScenarioException(string message, string? source, int line)
        : base(source is null ? $"line {line}: {message}" : $"{source}:{line}: {message}")
    {
        Source = source;
        Line = line;
    }

    /// <summary>The file, when known.</summary>
    public new string? Source { get; }

    /// <summary>The line of the problem, 1-based; 0 when unknown.</summary>
    public int Line { get; }
}

/// <summary>What a scenario step does.</summary>
public enum StepKind
{
    /// <summary>Click an element with pointer events.</summary>
    Click,

    /// <summary>Click at window coordinates.</summary>
    ClickAt,

    /// <summary>Invoke an element without a pointer.</summary>
    Invoke,

    /// <summary>Press a key.</summary>
    Press,

    /// <summary>Type text.</summary>
    Type,

    /// <summary>A controller action.</summary>
    Nav,

    /// <summary>Send a command.</summary>
    Command,

    /// <summary>Patch a property.</summary>
    Set,

    /// <summary>Open a screen.</summary>
    Screen,

    /// <summary>Load a fixture.</summary>
    Fixture,

    /// <summary>Load a snapshot file.</summary>
    Snapshot,

    /// <summary>Run frames.</summary>
    Step,

    /// <summary>Scroll the wheel.</summary>
    Wheel,

    /// <summary>Save a capture.</summary>
    Capture,

    /// <summary>Check an expectation now.</summary>
    Expect,
}

/// <summary>One step of a scenario. Only the fields its <see cref="Kind"/> uses are set.</summary>
public sealed record ScenarioStep(StepKind Kind, int Line)
{
    /// <summary>Element id.</summary>
    public string? Id { get; init; }

    /// <summary>Element selector.</summary>
    public string? Selector { get; init; }

    /// <summary>Window x.</summary>
    public double X { get; init; }

    /// <summary>Window y.</summary>
    public double Y { get; init; }

    /// <summary>Wheel delta x.</summary>
    public double Dx { get; init; }

    /// <summary>Wheel delta y.</summary>
    public double Dy { get; init; }

    /// <summary>Key name, controller action, screen, fixture or typed text, by kind.</summary>
    public string? Text { get; init; }

    /// <summary>Modifier keys held during a key press.</summary>
    public string[] Modifiers { get; init; } = [];

    /// <summary>Entity spec for commands and patches.</summary>
    public string? Entity { get; init; }

    /// <summary>Command name or property name.</summary>
    public string? Name { get; init; }

    /// <summary>Command arguments, or the value to set (first element).</summary>
    public string[] Args { get; init; } = [];

    /// <summary>Frames to run.</summary>
    public int Count { get; init; } = 1;

    /// <summary>File path for a capture or snapshot.</summary>
    public string? Path { get; init; }

    /// <summary>Capture size.</summary>
    public string? Size { get; init; }

    /// <summary>Capture UI scale.</summary>
    public double? Scale { get; init; }

    /// <summary>The expectation of an <see cref="StepKind.Expect"/> step.</summary>
    public ScenarioExpectation? Expect { get; init; }
}

/// <summary>What an expectation looks at.</summary>
public enum ExpectKind
{
    /// <summary>A synced property.</summary>
    State,

    /// <summary>UI elements.</summary>
    Ui,

    /// <summary>The tick number.</summary>
    Tick,

    /// <summary>The state hash.</summary>
    Hash,

    /// <summary>A captured frame against a reference image.</summary>
    Capture,
}

/// <summary>A check on the game's state. Unset fields are not checked.</summary>
public sealed record ScenarioExpectation(ExpectKind Kind, int Line)
{
    /// <summary>Entity spec (state).</summary>
    public string? Entity { get; init; }

    /// <summary>Property name (state).</summary>
    public string? Property { get; init; }

    /// <summary>The value must equal this text (numbers compare as numbers).</summary>
    public string? EqualTo { get; init; }

    /// <summary>The value must be at least this (state, numeric).</summary>
    public double? Min { get; init; }

    /// <summary>The value must be at most this (state, numeric).</summary>
    public double? Max { get; init; }

    /// <summary>Element id (ui).</summary>
    public string? Id { get; init; }

    /// <summary>Selector (ui).</summary>
    public string? Selector { get; init; }

    /// <summary>Number of matching elements (ui).</summary>
    public int? Count { get; init; }

    /// <summary>The first match's text (ui).</summary>
    public string? Text { get; init; }

    /// <summary>The first match's text contains this (ui).</summary>
    public string? Contains { get; init; }

    /// <summary>The first match is visible or hidden (ui).</summary>
    public bool? Visible { get; init; }

    /// <summary>The first match is enabled or disabled (ui).</summary>
    public bool? Enabled { get; init; }

    /// <summary>The first match has focus or not (ui).</summary>
    public bool? Focused { get; init; }

    /// <summary>Reference PNG, relative to the scenario file (capture).</summary>
    public string? Reference { get; init; }

    /// <summary>Capture size.</summary>
    public string? Size { get; init; }

    /// <summary>Capture UI scale.</summary>
    public double? Scale { get; init; }

    /// <summary>Per-channel difference (0-255) a pixel may have and still count as equal.</summary>
    public int Tolerance { get; init; }

    /// <summary>Fraction (0-1) of pixels that may differ by more than the tolerance.</summary>
    public double MaxDiff { get; init; }
}

/// <summary>A scenario: where to start, what to do, and what must hold afterwards.</summary>
public sealed record Scenario
{
    /// <summary>The scenario's name.</summary>
    public required string Name { get; init; }

    /// <summary>The file it was read from.</summary>
    public string? SourcePath { get; init; }

    /// <summary>A sentence about what the scenario shows.</summary>
    public string? Description { get; init; }

    /// <summary>Fixture to load first.</summary>
    public string? Fixture { get; init; }

    /// <summary>Snapshot file to load first, relative to the scenario file.</summary>
    public string? Snapshot { get; init; }

    /// <summary>Window size to run at, such as <c>800x600</c>.</summary>
    public string? Window { get; init; }

    /// <summary>World seed, when it should differ from the game's.</summary>
    public ulong? Seed { get; init; }

    /// <summary>The steps, in order.</summary>
    public IReadOnlyList<ScenarioStep> Steps { get; init; } = [];

    /// <summary>Expectations checked after the last step.</summary>
    public IReadOnlyList<ScenarioExpectation> Expect { get; init; } = [];
}

/// <summary>Reads scenario files. They are strict: a key the format does not know is an error with its line.</summary>
public static class ScenarioParser
{
    private const int MaxBytes = 1024 * 1024;
    private static readonly string[] TopKeys = ["name", "description", "fixture", "snapshot", "window", "seed", "steps", "expect"];

    /// <summary>Reads a scenario file.</summary>
    /// <exception cref="ScenarioException">The file is not a valid scenario.</exception>
    public static Scenario Load(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists)
        {
            throw new ScenarioException("the scenario file does not exist", path, 0);
        }

        if (info.Length > MaxBytes)
        {
            throw new ScenarioException("the scenario file is larger than 1 MiB", path, 0);
        }

        return Parse(File.ReadAllText(path), path, Path.GetFileNameWithoutExtension(path));
    }

    /// <summary>Reads a scenario from text.</summary>
    /// <param name="yaml">The YAML text.</param>
    /// <param name="source">The file name for messages, or null.</param>
    /// <param name="defaultName">The name to use when the file has none.</param>
    /// <exception cref="ScenarioException">The text is not a valid scenario.</exception>
    public static Scenario Parse(string yaml, string? source = null, string defaultName = "scenario")
    {
        ArgumentNullException.ThrowIfNull(yaml);
        YamlDocument doc;
        try
        {
            doc = YamlDocument.Parse(yaml, new YamlDocumentOptions());
        }
        catch (YamlException ex)
        {
            throw new ScenarioException(ex.Message, source, ex.Line);
        }

        using (doc)
        {
            var p = new Reader(source);
            var root = doc.Root;
            p.Map(root, TopKeys, "the scenario");
            var steps = new List<ScenarioStep>();
            if (root.TryGetValue("steps", out var stepsNode) && !stepsNode.IsNull)
            {
                foreach (var item in p.Seq(stepsNode, "steps"))
                {
                    steps.Add(p.Step(item));
                }
            }

            var expect = new List<ScenarioExpectation>();
            if (root.TryGetValue("expect", out var expectNode) && !expectNode.IsNull)
            {
                foreach (var item in p.Seq(expectNode, "expect"))
                {
                    expect.Add(p.Expectation(item));
                }
            }

            return new Scenario
            {
                Name = p.OptText(root, "name") ?? defaultName,
                SourcePath = source,
                Description = p.OptText(root, "description"),
                Fixture = p.OptText(root, "fixture"),
                Snapshot = p.OptText(root, "snapshot"),
                Window = p.OptText(root, "window"),
                Seed = p.OptULong(root, "seed"),
                Steps = steps,
                Expect = expect,
            };
        }
    }

    private sealed class Reader(string? source)
    {
        public ScenarioException Error(YamlNode at, string message) => new(message, source, at.Position.Line);

        public Dictionary<string, YamlNode> Map(YamlNode node, string[] allowed, string what)
        {
            if (node.Kind != YamlNodeKind.Mapping)
            {
                throw Error(node, $"{what} must be a mapping of {string.Join(", ", allowed)}");
            }

            var map = new Dictionary<string, YamlNode>(StringComparer.Ordinal);
            foreach (var entry in node.Entries)
            {
                var key = entry.Key.GetValue<string>();
                if (Array.IndexOf(allowed, key) < 0)
                {
                    throw Error(entry.Key, $"unknown key '{key}' in {what}; allowed: {string.Join(", ", allowed)}");
                }

                map[key] = entry.Value;
            }

            return map;
        }

        public YamlNode.ItemEnumerable Seq(YamlNode node, string what)
        {
            if (node.Kind != YamlNodeKind.Sequence)
            {
                throw Error(node, $"{what} must be a list");
            }

            return node.Items;
        }

        public string Text(YamlNode node, string what)
        {
            if (node.Kind != YamlNodeKind.Scalar || node.IsNull)
            {
                throw Error(node, $"{what} must be a plain value");
            }

            return node.GetValue<string>();
        }

        public string? OptText(YamlNode map, string key) =>
            map.TryGetValue(key, out var n) && !n.IsNull ? Text(n, key) : null;

        public ulong? OptULong(YamlNode map, string key)
        {
            var t = OptText(map, key);
            return t is null ? null : ulong.TryParse(t, NumberStyles.None, CultureInfo.InvariantCulture, out var v) ? v : throw Error(map[key], $"{key} must be a whole number");
        }

        private static string[] StringList(Reader r, YamlNode node, string what)
        {
            if (node.Kind == YamlNodeKind.Scalar)
            {
                return [r.Text(node, what)];
            }

            var list = new List<string>();
            foreach (var item in r.Seq(node, what))
            {
                list.Add(r.Text(item, what));
            }

            return [.. list];
        }

        private double Number(YamlNode node, string what) =>
            double.TryParse(Text(node, what), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : throw Error(node, $"{what} must be a number");

        private int Whole(YamlNode node, string what) =>
            int.TryParse(Text(node, what), NumberStyles.None, CultureInfo.InvariantCulture, out var v) ? v : throw Error(node, $"{what} must be a whole number");

        private bool Bool(YamlNode node, string what) => Text(node, what) switch
        {
            "true" => true,
            "false" => false,
            _ => throw Error(node, $"{what} must be true or false"),
        };

        public ScenarioStep Step(YamlNode node)
        {
            if (node.Kind != YamlNodeKind.Mapping || node.Count != 1)
            {
                throw Error(node, "a step is a mapping with one key, the verb: click, invoke, press, type, nav, command, set, screen, fixture, snapshot, step, wheel, capture or expect");
            }

            var entry = node.Entries.First();
            var verb = entry.Key.GetValue<string>();
            var v = entry.Value;
            var line = entry.Key.Position.Line;
            switch (verb)
            {
                case "click":
                    if (v.Kind == YamlNodeKind.Scalar)
                    {
                        return new ScenarioStep(StepKind.Click, line) { Id = Text(v, "click") };
                    }

                    var c = Map(v, ["id", "selector", "x", "y"], "click");
                    if (c.ContainsKey("x") || c.ContainsKey("y"))
                    {
                        return c.TryGetValue("x", out var cx) && c.TryGetValue("y", out var cy)
                            ? new ScenarioStep(StepKind.ClickAt, line) { X = Number(cx, "x"), Y = Number(cy, "y") }
                            : throw Error(v, "click at a position needs both x and y");
                    }

                    return c.Count == 1 ? new ScenarioStep(StepKind.Click, line) { Id = OptText(v, "id"), Selector = OptText(v, "selector") } : throw Error(v, "click needs one of id, selector or x and y");
                case "invoke":
                    if (v.Kind == YamlNodeKind.Scalar)
                    {
                        return new ScenarioStep(StepKind.Invoke, line) { Id = Text(v, "invoke") };
                    }

                    Map(v, ["id", "selector"], "invoke");
                    return new ScenarioStep(StepKind.Invoke, line) { Id = OptText(v, "id"), Selector = OptText(v, "selector") };
                case "press":
                    if (v.Kind == YamlNodeKind.Scalar)
                    {
                        return new ScenarioStep(StepKind.Press, line) { Text = Text(v, "press") };
                    }

                    var k = Map(v, ["key", "modifiers"], "press");
                    return new ScenarioStep(StepKind.Press, line)
                    {
                        Text = k.TryGetValue("key", out var kn) ? Text(kn, "key") : throw Error(v, "press needs a key"),
                        Modifiers = k.TryGetValue("modifiers", out var mn) ? StringList(this, mn, "modifiers") : [],
                    };
                case "type":
                    return new ScenarioStep(StepKind.Type, line) { Text = Text(v, "type") };
                case "nav":
                    return new ScenarioStep(StepKind.Nav, line) { Text = Text(v, "nav") };
                case "screen":
                    return new ScenarioStep(StepKind.Screen, line) { Text = Text(v, "screen") };
                case "fixture":
                    return new ScenarioStep(StepKind.Fixture, line) { Text = Text(v, "fixture") };
                case "snapshot":
                    return new ScenarioStep(StepKind.Snapshot, line) { Path = Text(v, "snapshot") };
                case "step":
                    return new ScenarioStep(StepKind.Step, line) { Count = Whole(v, "step") };
                case "command":
                    var cm = Map(v, ["entity", "name", "args"], "command");
                    return new ScenarioStep(StepKind.Command, line)
                    {
                        Entity = cm.TryGetValue("entity", out var ce) ? Text(ce, "entity") : throw Error(v, "command needs an entity"),
                        Name = cm.TryGetValue("name", out var cn) ? Text(cn, "name") : throw Error(v, "command needs a name"),
                        Args = cm.TryGetValue("args", out var ca) ? StringList(this, ca, "args") : [],
                    };
                case "set":
                    var s = Map(v, ["entity", "property", "value"], "set");
                    return new ScenarioStep(StepKind.Set, line)
                    {
                        Entity = s.TryGetValue("entity", out var se) ? Text(se, "entity") : throw Error(v, "set needs an entity"),
                        Name = s.TryGetValue("property", out var sp) ? Text(sp, "property") : throw Error(v, "set needs a property"),
                        Args = [s.TryGetValue("value", out var sv) ? Text(sv, "value") : throw Error(v, "set needs a value")],
                    };
                case "wheel":
                    var wm = Map(v, ["x", "y", "dx", "dy"], "wheel");
                    return new ScenarioStep(StepKind.Wheel, line)
                    {
                        X = wm.TryGetValue("x", out var wx) ? Number(wx, "x") : 0,
                        Y = wm.TryGetValue("y", out var wy) ? Number(wy, "y") : 0,
                        Dx = wm.TryGetValue("dx", out var wdx) ? Number(wdx, "dx") : 0,
                        Dy = wm.TryGetValue("dy", out var wdy) ? Number(wdy, "dy") : 0,
                    };
                case "capture":
                    var cp = Map(v, ["path", "size", "scale"], "capture");
                    return new ScenarioStep(StepKind.Capture, line)
                    {
                        Path = cp.TryGetValue("path", out var pn) ? Text(pn, "path") : throw Error(v, "capture needs a path"),
                        Size = OptText(v, "size"),
                        Scale = cp.TryGetValue("scale", out var sn) ? Number(sn, "scale") : null,
                    };
                case "expect":
                    return new ScenarioStep(StepKind.Expect, line) { Expect = Expectation(v) };
                default:
                    throw Error(entry.Key, $"unknown step '{verb}'; use click, invoke, press, type, nav, command, set, screen, fixture, snapshot, step, wheel, capture or expect");
            }
        }

        public ScenarioExpectation Expectation(YamlNode node)
        {
            if (node.Kind != YamlNodeKind.Mapping || node.Count != 1)
            {
                throw Error(node, "an expectation is a mapping with one key: state, ui, tick, hash or capture");
            }

            var entry = node.Entries.First();
            var kind = entry.Key.GetValue<string>();
            var v = entry.Value;
            var line = entry.Key.Position.Line;
            switch (kind)
            {
                case "state":
                    var s = Map(v, ["entity", "property", "equals", "min", "max"], "state");
                    return new ScenarioExpectation(ExpectKind.State, line)
                    {
                        Entity = s.TryGetValue("entity", out var e) ? Text(e, "entity") : throw Error(v, "state needs an entity"),
                        Property = s.TryGetValue("property", out var p) ? Text(p, "property") : throw Error(v, "state needs a property"),
                        EqualTo = OptText(v, "equals"),
                        Min = s.TryGetValue("min", out var mn) ? Number(mn, "min") : null,
                        Max = s.TryGetValue("max", out var mx) ? Number(mx, "max") : null,
                    };
                case "ui":
                    var u = Map(v, ["id", "selector", "count", "text", "contains", "visible", "enabled", "focused"], "ui");
                    if (!u.ContainsKey("id") && !u.ContainsKey("selector"))
                    {
                        throw Error(v, "ui needs an id or a selector");
                    }

                    return new ScenarioExpectation(ExpectKind.Ui, line)
                    {
                        Id = OptText(v, "id"),
                        Selector = OptText(v, "selector"),
                        Count = u.TryGetValue("count", out var cn) ? Whole(cn, "count") : null,
                        Text = OptText(v, "text"),
                        Contains = OptText(v, "contains"),
                        Visible = u.TryGetValue("visible", out var vi) ? Bool(vi, "visible") : null,
                        Enabled = u.TryGetValue("enabled", out var en) ? Bool(en, "enabled") : null,
                        Focused = u.TryGetValue("focused", out var fo) ? Bool(fo, "focused") : null,
                    };
                case "tick":
                    return new ScenarioExpectation(ExpectKind.Tick, line) { EqualTo = Text(v, "tick") };
                case "hash":
                    return new ScenarioExpectation(ExpectKind.Hash, line) { EqualTo = Text(v, "hash") };
                case "capture":
                    var c = Map(v, ["reference", "size", "scale", "tolerance", "max-diff"], "capture");
                    return new ScenarioExpectation(ExpectKind.Capture, line)
                    {
                        Reference = c.TryGetValue("reference", out var r) ? Text(r, "reference") : throw Error(v, "capture needs a reference image path"),
                        Size = OptText(v, "size"),
                        Scale = c.TryGetValue("scale", out var sc) ? Number(sc, "scale") : null,
                        Tolerance = c.TryGetValue("tolerance", out var t) ? Whole(t, "tolerance") : 0,
                        MaxDiff = c.TryGetValue("max-diff", out var md) ? Number(md, "max-diff") : 0,
                    };
                default:
                    throw Error(entry.Key, $"unknown expectation '{kind}'; use state, ui, tick, hash or capture");
            }
        }
    }
}
