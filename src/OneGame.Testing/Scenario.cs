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

    /// <summary>Tap an element with a finger.</summary>
    Tap,

    /// <summary>Tap at window coordinates.</summary>
    TapAt,

    /// <summary>One touch action (down, move, up, cancel) for a finger, for gestures and multi-touch.</summary>
    Touch,

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

    /// <summary>Start a new game, optionally with another seed.</summary>
    Reset,

    /// <summary>Set the UI scale override, or clear it.</summary>
    UiScale,

    /// <summary>Run frames.</summary>
    Step,

    /// <summary>Scroll the wheel.</summary>
    Wheel,

    /// <summary>Save a capture.</summary>
    Capture,

    /// <summary>Check an expectation now.</summary>
    Expect,

    /// <summary>Move or reset the 3D camera (a view-only override).</summary>
    Camera,

    /// <summary>Drive the game's in-app purchases (and script the fake store).</summary>
    Store,

    /// <summary>Drive the harness's stand-in Steam Input controller (connect, press, release, analog), then run two frames so the game sees it.</summary>
    SteamInput,

    /// <summary>Wait until a node of a process cluster reaches a tick (cluster scenarios only).</summary>
    Wait,

    /// <summary>Kill a node's process outright (cluster scenarios only).</summary>
    Kill,

    /// <summary>Start a node that was killed, with the same arguments, data folder and ports (cluster scenarios only).</summary>
    Start,

    /// <summary>Kill a node and start it again (cluster scenarios only).</summary>
    Restart,
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

    /// <summary>The finger of a <see cref="StepKind.Touch"/> step.</summary>
    public int Finger { get; init; }

    /// <summary>Key name, controller action, screen, fixture or typed text, by kind.</summary>
    public string? Text { get; init; }

    /// <summary>Modifier keys held during a key press.</summary>
    public string[] Modifiers { get; init; } = [];

    /// <summary>Entity spec for commands and patches.</summary>
    public string? Entity { get; init; }

    /// <summary>Hot table name, for a <c>set:</c> of a table cell (instead of <see cref="Entity"/>).</summary>
    public string? Table { get; init; }

    /// <summary>Row of <see cref="Table"/>, from 0.</summary>
    public int? Row { get; init; }

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

    /// <summary>The node of a process cluster the step is aimed at; null for a single game.</summary>
    public string? Node { get; init; }

    /// <summary>The arguments of the <c>camera</c> operation, as JSON, for a <see cref="StepKind.Camera"/> step.</summary>
    public string? ControlArgs { get; init; }

    /// <summary>The tick a <see cref="StepKind.Wait"/> step waits for.</summary>
    public long Tick { get; init; }

    /// <summary>How long a <see cref="StepKind.Wait"/> step may take, in milliseconds.</summary>
    public int TimeoutMs { get; init; } = 30_000;
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

    /// <summary>The entity under a pixel of the 3D view: <see cref="ScenarioExpectation.Entity"/> is what must be there, or <c>none</c>.</summary>
    Pick,

    /// <summary>Client replicas have the server's state hash at the replica's tick (cluster scenarios only).</summary>
    Agree,

    /// <summary>Nodes have filed no drift reports (cluster scenarios only).</summary>
    Drift,

    /// <summary>The purchase service's state: what is owned, pending or unfinished.</summary>
    Store,

    /// <summary>The state of Steam Input: the active action set, the number of controllers, actions held down, the on-screen keyboard.</summary>
    SteamInput,

    /// <summary>Sounds the game asked the audio service to play: how many, by name, optionally within the last few frames.</summary>
    Sound,
}

/// <summary>A check on the game's state. Unset fields are not checked.</summary>
public sealed record ScenarioExpectation(ExpectKind Kind, int Line)
{
    /// <summary>The checks of a <see cref="ExpectKind.Store"/> expectation: a key (owned, not-owned, pending, connection, unfinished, products, recent) and its value.</summary>
    public (string Key, string Value)[] StoreChecks { get; init; } = [];

    /// <summary>Entity spec (state).</summary>
    public string? Entity { get; init; }

    /// <summary>Hot table name (state), instead of <see cref="Entity"/>.</summary>
    public string? Table { get; init; }

    /// <summary>Row of <see cref="Table"/>, from 0 (state). Without it the property is looked up in the whole table.</summary>
    public int? Row { get; init; }

    /// <summary>Pixels from the left of the 3D view (pick).</summary>
    public double PickX { get; init; }

    /// <summary>Pixels from the top of the 3D view (pick).</summary>
    public double PickY { get; init; }

    /// <summary>The scope to read on a server that hosts several (cluster scenarios; 1 when absent).</summary>
    public int? Scope { get; init; }

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

    /// <summary>The first match is a toggle control that is switched on or off (ui); a control that is not a toggle never matches.</summary>
    public bool? Pressed { get; init; }

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

    /// <summary>The node of a process cluster the expectation looks at; null for a single game.</summary>
    public string? Node { get; init; }

    /// <summary>Cluster state expectations: keep checking for up to this many milliseconds before failing, since a command takes real time to reach the server.</summary>
    public int WithinMs { get; init; }

    /// <summary>The server whose recorded hashes <see cref="ExpectKind.Agree"/> compares against.</summary>
    public string? Server { get; init; }

    /// <summary>The nodes <see cref="ExpectKind.Agree"/> and <see cref="ExpectKind.Drift"/> look at; empty means all that apply.</summary>
    public string[] Nodes { get; init; } = [];

    /// <summary>The action set that must be active (steam-input).</summary>
    public string? ActiveSet { get; init; }

    /// <summary>The sound's name (sound): the name the clip was loaded under, with or without its file extension.</summary>
    public string? SoundName { get; init; }

    /// <summary>Only sounds requested in the last this-many frames count (sound); 0 counts every sound since the scenario began.</summary>
    public int SoundFrames { get; init; }

    /// <summary>A digital action that must be down on some controller (steam-input).</summary>
    public string? DownAction { get; init; }

    /// <summary>A digital action that must not be down on any controller (steam-input).</summary>
    public string? UpAction { get; init; }
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

        private string NumberText(YamlNode at, string text, string what) =>
            double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && double.IsFinite(d)
                ? d.ToString("R", CultureInfo.InvariantCulture)
                : throw Error(at, $"{what} must hold numbers");

        private bool Bool(YamlNode node, string what) => Text(node, what) switch
        {
            "true" => true,
            "false" => false,
            _ => throw Error(node, $"{what} must be true or false"),
        };

        // A step or expectation is one verb key; a cluster scenario may add a 'node' key beside it to aim it at a node.
        private ((YamlNode Key, YamlNode Value) Verb, string? Node) SplitVerb(YamlNode node, string what)
        {
            if (node.Kind != YamlNodeKind.Mapping || node.Count is not (1 or 2))
            {
                throw Error(node, what);
            }

            (YamlNode Key, YamlNode Value)? verb = null;
            string? target = null;
            foreach (var e in node.Entries)
            {
                if (node.Count == 2 && e.Key.GetValue<string>() == "node")
                {
                    target = Text(e.Value, "node");
                }
                else if (verb is null)
                {
                    verb = (e.Key, e.Value);
                }
                else
                {
                    throw Error(node, what);
                }
            }

            return verb is null ? throw Error(node, what) : (verb.Value, target);
        }

        public ScenarioStep Step(YamlNode node)
        {
            var (verb, target) = SplitVerb(node, "a step is a mapping with one key, the verb: click, invoke, press, type, nav, command, set, screen, fixture, snapshot, reset, ui-scale, step, wheel, capture, store, wait, kill, start, restart or expect (plus node: for a cluster)");
            var step = StepCore(verb);
            return target is null ? step : step with { Node = target };
        }

        private ScenarioStep StepCore((YamlNode Key, YamlNode Value) entry)
        {
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
                case "tap":
                    if (v.Kind == YamlNodeKind.Scalar)
                    {
                        return new ScenarioStep(StepKind.Tap, line) { Id = Text(v, "tap") };
                    }

                    var tp = Map(v, ["id", "selector", "x", "y"], "tap");
                    if (tp.ContainsKey("x") || tp.ContainsKey("y"))
                    {
                        return tp.TryGetValue("x", out var tx) && tp.TryGetValue("y", out var ty)
                            ? new ScenarioStep(StepKind.TapAt, line) { X = Number(tx, "x"), Y = Number(ty, "y") }
                            : throw Error(v, "tap at a position needs both x and y");
                    }

                    return tp.Count == 1 ? new ScenarioStep(StepKind.Tap, line) { Id = OptText(v, "id"), Selector = OptText(v, "selector") } : throw Error(v, "tap needs one of id, selector or x and y");
                case "touch":
                    var tm = Map(v, ["action", "finger", "x", "y"], "touch");
                    return new ScenarioStep(StepKind.Touch, line)
                    {
                        Text = tm.TryGetValue("action", out var ta) ? Text(ta, "action") : throw Error(v, "touch needs an action: down, move, up or cancel"),
                        Finger = tm.TryGetValue("finger", out var tf) ? (int)Number(tf, "finger") : 0,
                        X = tm.TryGetValue("x", out var tcx) ? Number(tcx, "x") : 0,
                        Y = tm.TryGetValue("y", out var tcy) ? Number(tcy, "y") : 0,
                    };
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
                case "reset":
                    // reset: 77 or reset: {seed: 77}; reset: {} starts again with the current seed.
                    var rseed = v.Kind == YamlNodeKind.Scalar ? Text(v, "reset") : Map(v, ["seed"], "reset").TryGetValue("seed", out var rs) ? Text(rs, "seed") : null;
                    return rseed is null || ulong.TryParse(rseed, NumberStyles.None, CultureInfo.InvariantCulture, out _)
                        ? new ScenarioStep(StepKind.Reset, line) { Text = rseed }
                        : throw Error(v, "reset takes a whole number seed");
                case "ui-scale":
                    var usText = Text(v, "ui-scale");
                    return usText == "auto"
                        ? new ScenarioStep(StepKind.UiScale, line)
                        : new ScenarioStep(StepKind.UiScale, line) { Scale = Number(v, "ui-scale") };
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
                    var s = Map(v, ["entity", "table", "row", "property", "value"], "set");
                    if (s.ContainsKey("entity") == s.ContainsKey("table"))
                    {
                        throw Error(v, "set needs an entity, or a table with a row");
                    }

                    if (s.ContainsKey("table") && !s.ContainsKey("row"))
                    {
                        throw Error(v, "set on a table needs a row (from 0)");
                    }

                    return new ScenarioStep(StepKind.Set, line)
                    {
                        Entity = s.TryGetValue("entity", out var se) ? Text(se, "entity") : null,
                        Table = s.TryGetValue("table", out var st) ? Text(st, "table") : null,
                        Row = s.TryGetValue("row", out var sr) ? Whole(sr, "row") : null,
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
                case "camera":
                    if (v.Kind == YamlNodeKind.Scalar)
                    {
                        return Text(v, "camera") == "reset" ? new ScenarioStep(StepKind.Camera, line) { ControlArgs = "{\"reset\":true}" } : throw Error(v, "camera takes 'reset' or a mapping of position, target, fov, near, far, ortho-height");
                    }

                    var cam = Map(v, ["position", "target", "fov", "near", "far", "ortho-height"], "camera");
                    var json = new List<string>();
                    foreach (var key in new[] { "position", "target" })
                    {
                        if (cam.TryGetValue(key, out var list))
                        {
                            var parts = StringList(this, list, key);
                            if (parts.Length != 3)
                            {
                                throw Error(list, $"{key} is three numbers: [x, y, z]");
                            }

                            json.Add($"\"{key}\":[{string.Join(",", parts.Select(p => NumberText(list, p, key)))}]");
                        }
                    }

                    foreach (var (key, name) in new[] { ("fov", "fovDegrees"), ("near", "near"), ("far", "far"), ("ortho-height", "orthoHeight") })
                    {
                        if (cam.TryGetValue(key, out var n))
                        {
                            json.Add($"\"{name}\":{Number(n, key).ToString("R", CultureInfo.InvariantCulture)}");
                        }
                    }

                    return json.Count > 0 ? new ScenarioStep(StepKind.Camera, line) { ControlArgs = "{" + string.Join(",", json) + "}" } : throw Error(v, "camera needs at least one of position, target, fov, near, far, ortho-height");
                case "store":
                    var storeKeys = new[] { "action", "product", "outcome", "error", "approve" };
                    var stm = v.Kind == YamlNodeKind.Scalar ? new Dictionary<string, YamlNode>() : Map(v, storeKeys, "store");
                    var storeAction = v.Kind == YamlNodeKind.Scalar ? Text(v, "store") : stm.TryGetValue("action", out var storeActionNode) ? Text(storeActionNode, "action") : throw Error(v, "store needs an action: describe, connect, load, buy, finish, restore, script, resolve, revoke, disconnect or fail-connect");
                    return new ScenarioStep(StepKind.Store, line)
                    {
                        ControlArgs = ScenarioRunner.Obj(("action", storeAction), ("product", stm.TryGetValue("product", out var s_product) ? Text(s_product, "product") : null), ("outcome", stm.TryGetValue("outcome", out var s_outcome) ? Text(s_outcome, "outcome") : null), ("error", stm.TryGetValue("error", out var s_error) ? Text(s_error, "error") : null),
                            ("approve", stm.TryGetValue("approve", out var storeApprove) ? Bool(storeApprove, "approve") : null)),
                    };
                case "steam-input":
                    var si = Map(v, ["connect", "disconnect", "press", "release", "analog", "x", "y", "controller"], "steam-input");
                    var sj = new List<string>();
                    foreach (var key in new[] { "connect", "disconnect", "controller" })
                    {
                        if (si.TryGetValue(key, out var wn2))
                        {
                            sj.Add($"\"{key}\":{Whole(wn2, key)}");
                        }
                    }

                    foreach (var key in new[] { "press", "release", "analog" })
                    {
                        if (si.TryGetValue(key, out var tn))
                        {
                            sj.Add($"\"{key}\":\"{Text(tn, key).Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"");
                        }
                    }

                    foreach (var key in new[] { "x", "y" })
                    {
                        if (si.TryGetValue(key, out var an))
                        {
                            sj.Add($"\"{key}\":{Number(an, key).ToString("R", CultureInfo.InvariantCulture)}");
                        }
                    }

                    return sj.Count > 0 ? new ScenarioStep(StepKind.SteamInput, line) { ControlArgs = "{" + string.Join(",", sj) + "}" } : throw Error(v, "steam-input needs at least one of connect, disconnect, press, release, analog");
                case "expect":
                    return new ScenarioStep(StepKind.Expect, line) { Expect = Expectation(v) };
                case "wait":
                    var wt = Map(v, ["node", "tick", "timeout"], "wait");
                    return new ScenarioStep(StepKind.Wait, line)
                    {
                        Node = wt.TryGetValue("node", out var wn) ? Text(wn, "node") : throw Error(v, "wait needs a node"),
                        Tick = wt.TryGetValue("tick", out var wk) ? Whole(wk, "tick") : throw Error(v, "wait needs a tick"),
                        TimeoutMs = wt.TryGetValue("timeout", out var wo) ? Whole(wo, "timeout") : 30_000,
                    };
                case "kill":
                    return new ScenarioStep(StepKind.Kill, line) { Node = Text(v, "kill") };
                case "start":
                    return new ScenarioStep(StepKind.Start, line) { Node = Text(v, "start") };
                case "restart":
                    return new ScenarioStep(StepKind.Restart, line) { Node = Text(v, "restart") };
                default:
                    throw Error(entry.Key, $"unknown step '{verb}'; use click, invoke, press, type, nav, command, set, screen, fixture, snapshot, reset, ui-scale, step, wheel, capture, camera, steam-input, wait, kill, start, restart or expect");
            }
        }

        public ScenarioExpectation Expectation(YamlNode node)
        {
            var (verb, target) = SplitVerb(node, "an expectation is a mapping with one key: state, ui, tick, hash, capture, pick, steam-input, sound, agree or drift (plus node: for a cluster)");
            var e = ExpectationCore(verb);
            return target is null ? e : e with { Node = target };
        }

        private ScenarioExpectation ExpectationCore((YamlNode Key, YamlNode Value) entry)
        {
            var kind = entry.Key.GetValue<string>();
            var v = entry.Value;
            var line = entry.Key.Position.Line;
            switch (kind)
            {
                case "state":
                    var s = Map(v, ["entity", "table", "row", "property", "scope", "equals", "min", "max", "within"], "state");
                    if (s.ContainsKey("entity") == s.ContainsKey("table"))
                    {
                        throw Error(v, "state needs an entity, or a table");
                    }

                    return new ScenarioExpectation(ExpectKind.State, line)
                    {
                        Entity = s.TryGetValue("entity", out var e) ? Text(e, "entity") : null,
                        Table = s.TryGetValue("table", out var stTable) ? Text(stTable, "table") : null,
                        Row = s.TryGetValue("row", out var stRow) ? Whole(stRow, "row") : null,
                        Property = s.TryGetValue("property", out var p) ? Text(p, "property") : throw Error(v, "state needs a property"),
                        Scope = s.TryGetValue("scope", out var stScope) ? Whole(stScope, "scope") : null,
                        EqualTo = OptText(v, "equals"),
                        Min = s.TryGetValue("min", out var mn) ? Number(mn, "min") : null,
                        Max = s.TryGetValue("max", out var mx) ? Number(mx, "max") : null,
                        WithinMs = s.TryGetValue("within", out var wi) ? Whole(wi, "within") : 0,
                    };
                case "ui":
                    var u = Map(v, ["id", "selector", "count", "text", "contains", "visible", "enabled", "focused", "pressed"], "ui");
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
                        Pressed = u.TryGetValue("pressed", out var pr) ? Bool(pr, "pressed") : null,
                    };
                case "store":
                    var sx = Map(v, ["owned", "not-owned", "pending", "connection", "unfinished", "products", "recent"], "store");
                    return sx.Count > 0
                        ? new ScenarioExpectation(ExpectKind.Store, line) { StoreChecks = [.. sx.Select(kv => (kv.Key, Text(kv.Value, kv.Key)))] }
                        : throw Error(v, "store needs at least one of owned, not-owned, pending, connection, unfinished, products, recent");
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
                case "pick":
                    var pk = Map(v, ["x", "y", "size", "entity"], "pick");
                    return new ScenarioExpectation(ExpectKind.Pick, line)
                    {
                        PickX = pk.TryGetValue("x", out var px) ? Number(px, "x") : throw Error(v, "pick needs x and y"),
                        PickY = pk.TryGetValue("y", out var py) ? Number(py, "y") : throw Error(v, "pick needs x and y"),
                        Size = OptText(v, "size"),
                        Entity = pk.TryGetValue("entity", out var pe) ? Text(pe, "entity") : throw Error(v, "pick needs the entity expected there, or none"),
                    };
                case "sound":
                    var sd = Map(v, ["name", "count", "min", "max", "frames"], "sound");
                    var noBound = !sd.ContainsKey("count") && !sd.ContainsKey("min") && !sd.ContainsKey("max");
                    return new ScenarioExpectation(ExpectKind.Sound, line)
                    {
                        SoundName = sd.TryGetValue("name", out var sdn) ? Text(sdn, "name") : throw Error(v, "sound needs the name the clip was loaded under"),
                        Count = sd.TryGetValue("count", out var sdc) ? Whole(sdc, "count") : null,
                        Min = sd.TryGetValue("min", out var sdmin) ? Number(sdmin, "min") : (noBound ? 1 : null),
                        Max = sd.TryGetValue("max", out var sdmax) ? Number(sdmax, "max") : null,
                        SoundFrames = sd.TryGetValue("frames", out var sdf) ? Whole(sdf, "frames") : 0,
                    };
                case "steam-input":
                    var six = Map(v, ["set", "controllers", "down", "up", "keyboard"], "steam-input");
                    return six.Count == 0 ? throw Error(v, "steam-input needs at least one of set, controllers, down, up, keyboard") : new ScenarioExpectation(ExpectKind.SteamInput, line)
                    {
                        ActiveSet = OptText(v, "set"),
                        Count = six.TryGetValue("controllers", out var sxc) ? Whole(sxc, "controllers") : null,
                        DownAction = OptText(v, "down"),
                        UpAction = OptText(v, "up"),
                        Visible = six.TryGetValue("keyboard", out var sxk) ? Bool(sxk, "keyboard") : null,
                    };
                case "agree":
                    var ag = Map(v, ["server", "clients"], "agree");
                    return new ScenarioExpectation(ExpectKind.Agree, line)
                    {
                        Server = ag.TryGetValue("server", out var asv) ? Text(asv, "server") : throw Error(v, "agree needs a server"),
                        Nodes = ag.TryGetValue("clients", out var acl) ? StringList(this, acl, "clients") : [],
                    };
                case "drift":
                    var dr = Map(v, ["nodes", "equals"], "drift");
                    if (dr.TryGetValue("equals", out var deq) && Whole(deq, "equals") != 0)
                    {
                        throw Error(deq, "drift can only be expected to equal 0");
                    }

                    return new ScenarioExpectation(ExpectKind.Drift, line) { Nodes = dr.TryGetValue("nodes", out var dn) ? StringList(this, dn, "nodes") : [] };
                default:
                    throw Error(entry.Key, $"unknown expectation '{kind}'; use state, ui, tick, hash, capture, pick, agree or drift");
            }
        }
    }
}
