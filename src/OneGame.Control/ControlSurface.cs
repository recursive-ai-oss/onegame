using System.Security.Cryptography;
using OneGame.Core;
using OneGame.Host;
using OneGame.Platform;
using OneGame.UI;

namespace OneGame.Control;

/// <summary>
/// The control layer: every operation of <see cref="ControlOps"/> run against one <see cref="GameInstance"/>. The command line, the test
/// API and live control (MCP) all call <see cref="Invoke"/>; none of them reaches into the game any other way.
/// </summary>
public sealed class ControlSurface : IControlEndpoint
{
    private const int MaxSnapshotBytes = 64 * 1024 * 1024;
    private int _gamepad = -1;

    /// <summary>Creates a control surface over a game.</summary>
    /// <param name="game">The game to control.</param>
    /// <param name="outputDirectory">Where captures and snapshots are written and read. Relative paths in operations resolve here and may not leave it. Defaults to the current directory.</param>
    public ControlSurface(GameInstance game, string? outputDirectory = null)
    {
        Game = game ?? throw new ArgumentNullException(nameof(game));
        OutputDirectory = Path.GetFullPath(outputDirectory ?? Directory.GetCurrentDirectory());
    }

    /// <inheritdoc />
    public IReadOnlyList<ControlOp> Operations => ControlOps.All;

    /// <inheritdoc />
    public string Instructions => "Controls one running onegame game. Call info first. Mutating tools (command_send, input_*, step) act on the live game; step advances time. State reads are JSON.";

    /// <summary>The controlled game.</summary>
    public GameInstance Game { get; }

    /// <summary>The folder operations may write to and read from.</summary>
    public string OutputDirectory { get; }

    /// <summary>Runs an operation and returns its JSON text.</summary>
    /// <exception cref="ControlException">The operation is unknown, its arguments are wrong, or it failed.</exception>
    public string Call(string op, string? argsJson = null) => Invoke(op, argsJson).Json;

    /// <summary>Runs an operation. <paramref name="argsJson"/> is a JSON object (or null for none).</summary>
    /// <exception cref="ControlException">The operation is unknown, its arguments are wrong, or it failed.</exception>
    public ControlResult Invoke(string op, string? argsJson = null)
    {
        ArgumentNullException.ThrowIfNull(op);
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argsJson) ? "{}" : argsJson, new JsonDocumentOptions { MaxDepth = 16 });
        }
        catch (JsonException ex)
        {
            throw new ControlException("bad_args", "The arguments are not valid JSON: " + ex.Message, ex);
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ControlException("bad_args", "The arguments must be a JSON object.");
            }

            var a = new Args(doc.RootElement);
            try
            {
                return Dispatch(op, a);
            }
            catch (KeyNotFoundException ex)
            {
                throw new ControlException("not_found", ex.Message, ex);
            }
            catch (InvalidDataException ex)
            {
                throw new ControlException("bad_data", ex.Message, ex);
            }
            catch (ReplicaWriteException ex)
            {
                throw new ControlException("replica", ex.Message, ex);
            }
        }
    }

    private ControlResult Dispatch(string op, Args a) => op switch
    {
        "info" => Json(Info(), ControlJson.Default.InfoResult),
        "hash" or "reset" or "quit" or "step" or "screen_open" or "fixture_load" or "snapshot_load" => Json(Mutate(op, a), ControlJson.Default.StateResult),
        "state_query" => new ControlResult(StateQuery(a)),
        "state_patch" => Json(StatePatch(a), ControlJson.Default.PatchResult),
        "commands_list" => Json(CommandsList(a), ControlJson.Default.CommandsResultArray),
        "command_send" => Json(CommandSend(a), ControlJson.Default.SendResult),
        "input_pointer" => Json(InputPointer(a), ControlJson.Default.InputResult),
        "input_key" => Json(InputKey(a), ControlJson.Default.InputResult),
        "input_text" => Json(InputText(a), ControlJson.Default.InputResult),
        "input_nav" => Json(InputNav(a), ControlJson.Default.InputResult),
        "input_composition" => Json(InputComposition(a), ControlJson.Default.InputResult),
        "clipboard_get" => Json(ClipboardGet(a), ControlJson.Default.ClipboardResult),
        "clipboard_set" => Json(ClipboardSet(a), ControlJson.Default.ClipboardResult),
        "ui_tree" => new ControlResult(UiTree(a)),
        "ui_query" => new ControlResult(UiQuery(a)),
        "ui_invoke" => Json(UiInvoke(a), ControlJson.Default.InvokeResult),
        "ui_click" => Json(UiClick(a), ControlJson.Default.InputResult),
        "ui_hit" => Json(UiHit(a), ControlJson.Default.HitResult),
        "snapshot_save" => Json(SnapshotSave(a), ControlJson.Default.SnapshotResult),
        "capture_frame" => CaptureFrame(a),
        "capture_sequence" => Json(CaptureSequence(a), ControlJson.Default.SequenceResult),
        _ => throw new ControlException("unknown_op", $"Unknown operation '{op}'. Known: {string.Join(", ", ControlOps.All.Select(o => o.Name))}."),
    };

    private static ControlResult Json<T>(T value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> info) =>
        new(JsonSerializer.Serialize(value, info));

    private string HashText() => Game.World.ComputeHash().ToString("x16", CultureInfo.InvariantCulture);

    private StateResult State() => new(Game.Frame, Game.World.Tick, HashText());

    private InfoResult Info() => new(
        Game.Definition.Name, Game.World.Tick, Game.Frame, HashText(), Game.Window.Width, Game.Window.Height, Game.UiScale,
        Game.Options.Headless, Game.CurrentScreen, Game.World.Entities.Count, [.. Game.ScreenNames], [.. Game.FixtureNames], Game.Stage.Name, Game.AdapterName, Game.DriftReports.Count,
        Game.Platform.Backend.ToString().ToLowerInvariant(), Game.PresentedFrames);

    private StateResult Mutate(string op, Args a)
    {
        switch (op)
        {
            case "step":
                var frames = a.Int("frames", 1);
                if (frames is < 1 or > 1_000_000)
                {
                    throw new ControlException("bad_args", "frames must be between 1 and 1000000.");
                }

                Game.Step(frames);
                break;
            case "screen_open":
                Game.OpenScreen(a.Required("name"));
                Game.Ui.Update();
                break;
            case "fixture_load":
                Game.LoadFixture(a.Required("name"));
                break;
            case "reset":
                Game.Reset(a.Str("seed") is { } seed ? (ulong.TryParse(seed, NumberStyles.None, CultureInfo.InvariantCulture, out var s) ? s : throw new ControlException("bad_args", "seed must be a whole number.")) : null);
                break;
            case "quit":
                Game.Platform.RequestQuit();
                break;
            case "snapshot_load":
                Game.LoadSnapshot(ReadSnapshotArgument(a));
                break;
            default:
                break;
        }

        return State();
    }

    // ---- state ----

    private Entity ResolveEntity(string? spec)
    {
        if (string.IsNullOrWhiteSpace(spec))
        {
            throw new ControlException("bad_args", "entity is required: a numeric id, a type name, or Type#n.");
        }

        if (ulong.TryParse(spec, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
        {
            return Game.World.Find(new EntityId(id)) ?? throw new ControlException("not_found", $"No entity with id {id}. Present: {Describe(Game.World.Entities)}.");
        }

        var name = spec;
        var nth = 1;
        var hash = spec.IndexOf('#', StringComparison.Ordinal);
        if (hash > 0)
        {
            name = spec[..hash];
            if (!int.TryParse(spec.AsSpan(hash + 1), NumberStyles.None, CultureInfo.InvariantCulture, out nth) || nth < 1)
            {
                throw new ControlException("bad_args", $"'{spec}': after # give the position of the entity among those of that type, from 1.");
            }
        }

        var seen = 0;
        foreach (var e in Game.World.Entities)
        {
            if (!e.IsRemoved && (e.TypeName == name || e.TypeName.EndsWith("." + name, StringComparison.Ordinal)) && ++seen == nth)
            {
                return e;
            }
        }

        throw new ControlException("not_found", $"No entity '{spec}'. Present: {Describe(Game.World.Entities)}.");
    }

    private static string Describe(IReadOnlyList<Entity> entities)
    {
        if (entities.Count == 0)
        {
            return "none";
        }

        var sb = new StringBuilder();
        for (var i = 0; i < entities.Count && i < 20; i++)
        {
            sb.Append(i > 0 ? ", " : "").Append(entities[i].TypeName).Append('#').Append(entities[i].Id.Value.ToString(CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    private string StateQuery(Args a)
    {
        var spec = a.Str("entity");
        if (spec is null)
        {
            return Game.World.DumpJson();
        }

        var e = ResolveEntity(spec);
        var j = new JsonOut();
        e.WriteJson(j);
        var text = j.ToString();
        if (a.Str("property") is not { } property)
        {
            return text;
        }

        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.TryGetProperty(property, out var v)
            ? v.GetRawText()
            : throw new ControlException("not_found", $"{e.TypeName} has no property '{property}'. It has: {PropertyNames(e)}.");
    }

    private static string PropertyNames(Entity e)
    {
        var names = new string[e.PropertyCount];
        for (var i = 0; i < names.Length; i++)
        {
            names[i] = e.PropertyName(i);
        }

        return string.Join(", ", names);
    }

    private PatchResult StatePatch(Args a)
    {
        var e = ResolveEntity(a.Str("entity"));
        var property = a.Required("property");
        var value = a.Required("value");
        if (!e.TrySetFromText(property, value))
        {
            throw new ControlException("rejected", $"{e.TypeName}.{property} cannot be set to '{value}': the property is unknown, not a scalar, or the text does not parse. Properties: {PropertyNames(e)}.");
        }

        return new PatchResult(Label(e), property, value, HashText());
    }

    private CommandsResult[] CommandsList(Args a)
    {
        if (a.Str("entity") is { } spec)
        {
            var e = ResolveEntity(spec);
            return [new CommandsResult(Label(e), e.TypeName, e.CommandSignatures)];
        }

        var seen = new HashSet<uint>();
        var list = new List<CommandsResult>();
        foreach (var e in Game.World.Entities)
        {
            if (seen.Add(e.TypeId))
            {
                list.Add(new CommandsResult(Label(e), e.TypeName, e.CommandSignatures));
            }
        }

        return [.. list];
    }

    private static string Label(Entity e) => e.TypeName + "#" + e.Id.Value.ToString(CultureInfo.InvariantCulture);

    private SendResult CommandSend(Args a)
    {
        var e = ResolveEntity(a.Str("entity"));
        var command = a.Required("command");
        var args = a.Strings("args");
        var status = e.SendFromText(command, args);
        switch (status)
        {
            case CommandStatus.UnknownCommand:
                throw new ControlException("not_found", $"{e.TypeName} has no command '{command}'. It accepts: {string.Join("; ", e.CommandSignatures)}.");
            case CommandStatus.Malformed:
                throw new ControlException("bad_args", $"The arguments [{string.Join(", ", args)}] do not fit {command}. Signatures: {string.Join("; ", e.CommandSignatures)}.");
            default:
                return new SendResult(Label(e), command, status.ToString());
        }
    }

    // ---- input ----

    private InputResult Done()
    {
        Game.DrainInput();
        return new InputResult(Game.Ui.Focused?.ToString(), Game.Ui.Hovered?.ToString(), Game.World.Tick);
    }

    private InputResult InputPointer(Args a)
    {
        var inj = Game.Platform.Injector;
        var id = Game.Window.Id;
        var x = (float)a.Double("x", Game.Platform.Input.PointerX);
        var y = (float)a.Double("y", Game.Platform.Input.PointerY);
        var button = a.Str("button") switch
        {
            null or "left" => MouseButton.Left,
            "right" => MouseButton.Right,
            "middle" => MouseButton.Middle,
            var other => throw new ControlException("bad_args", $"button is left, right or middle, got '{other}'."),
        };
        switch (a.Required("action"))
        {
            case "move":
                inj.PointerMove(id, x, y);
                break;
            case "down":
                inj.PointerMove(id, x, y);
                inj.PointerButton(id, button, true);
                break;
            case "up":
                inj.PointerMove(id, x, y);
                inj.PointerButton(id, button, false);
                break;
            case "click":
                ClickAt(x, y, button);
                break;
            case "wheel":
                inj.PointerMove(id, x, y);
                inj.Wheel(id, (float)a.Double("dx", 0), (float)a.Double("dy", 0));
                break;
            case var other:
                throw new ControlException("bad_args", $"action is move, down, up, click or wheel, got '{other}'.");
        }

        return Done();
    }

    private void ClickAt(float x, float y, MouseButton button)
    {
        var inj = Game.Platform.Injector;
        var id = Game.Window.Id;
        inj.PointerMove(id, x, y);
        inj.PointerButton(id, button, true);
        inj.PointerButton(id, button, false);
    }

    private InputResult InputKey(Args a)
    {
        var name = a.Required("key");
        if (!KeyNames.TryParse(name, out var key))
        {
            throw new ControlException("bad_args", $"'{name}' is not a key. Use a DOM name (Enter, Escape, ArrowLeft, a, F5) or a key name (Digit1, LeftShift).");
        }

        var inj = Game.Platform.Injector;
        var id = Game.Window.Id;
        var mods = new List<Key>();
        foreach (var m in a.Strings("modifiers"))
        {
            if (!KeyNames.TryParse(m, out var mk) || mk is not (Key.LeftShift or Key.LeftControl or Key.LeftAlt or Key.LeftMeta))
            {
                throw new ControlException("bad_args", $"modifier '{m}' is not Shift, Control, Alt or Meta.");
            }

            mods.Add(mk);
        }

        foreach (var m in mods)
        {
            inj.Key(id, m, true);
        }

        switch (a.Str("action"))
        {
            case null or "press":
                inj.KeyPress(id, key);
                break;
            case "down":
                inj.Key(id, key, true);
                break;
            case "up":
                inj.Key(id, key, false);
                break;
            case var other:
                throw new ControlException("bad_args", $"action is press, down or up, got '{other}'.");
        }

        for (var i = mods.Count - 1; i >= 0; i--)
        {
            inj.Key(id, mods[i], false);
        }

        return Done();
    }

    private InputResult InputText(Args a)
    {
        Game.Platform.Injector.Text(Game.Window.Id, a.Required("text"));
        return Done();
    }

    private InputResult InputComposition(Args a)
    {
        var text = a.Required("text");
        Game.Platform.Injector.Composition(
            Game.Window.Id, text, a.Str("cursorStart") is null ? null : a.Int("cursorStart", 0), a.Str("cursorEnd") is null ? null : a.Int("cursorEnd", 0));
        return Done();
    }

    private static ClipboardSelection Selection(Args a) => a.Str("selection") switch
    {
        null or "clipboard" => ClipboardSelection.Clipboard,
        "primary" => ClipboardSelection.Primary,
        var other => throw new ControlException("bad_args", $"selection is clipboard or primary, got '{other}'."),
    };

    private ClipboardResult ClipboardGet(Args a)
    {
        var selection = Selection(a);
        var text = Game.Platform.Clipboard.GetText(selection);
        return new ClipboardResult(text, Game.Platform.Clipboard.Supports(selection));
    }

    private ClipboardResult ClipboardSet(Args a)
    {
        var text = a.Required("text");
        var ok = Game.Platform.Clipboard.SetText(text, Selection(a));
        return new ClipboardResult(ok ? text : null, ok);
    }

    private InputResult InputNav(Args a)
    {
        var inj = Game.Platform.Injector;
        if (_gamepad < 0)
        {
            _gamepad = inj.GamepadConnect("Harness Gamepad");
        }

        var button = a.Required("action") switch
        {
            "up" => GamepadButton.DPadUp,
            "down" => GamepadButton.DPadDown,
            "left" => GamepadButton.DPadLeft,
            "right" => GamepadButton.DPadRight,
            "next" => GamepadButton.RightShoulder,
            "previous" => GamepadButton.LeftShoulder,
            "activate" => GamepadButton.South,
            "cancel" => GamepadButton.East,
            var other => throw new ControlException("bad_args", $"action is up, down, left, right, next, previous, activate or cancel, got '{other}'."),
        };
        inj.GamepadButton(_gamepad, button, true);
        inj.GamepadButton(_gamepad, button, false);
        return Done();
    }

    // ---- ui ----

    private Element FindElement(Args a)
    {
        Game.Ui.Update();
        if (a.Str("id") is { } id)
        {
            return Game.Ui.Find(id) ?? throw new ControlException("not_found", $"No element with id '{id}'. Use ui_query to list elements, for example selector 'Button'.");
        }

        if (a.Str("selector") is { } selector)
        {
            var found = Query(selector);
            return found.Count > 0 ? found[0] : throw new ControlException("not_found", $"Nothing matches '{selector}'.");
        }

        throw new ControlException("bad_args", "Give id or selector.");
    }

    private List<Element> Query(string selector)
    {
        try
        {
            return Game.Ui.Query(selector);
        }
        catch (Exception ex) when (ex is ArgumentException or UiStyleException)
        {
            throw new ControlException("bad_args", $"Bad selector '{selector}': {ex.Message}", ex);
        }
    }

    private string UiTree(Args a)
    {
        Element? from = null;
        if (a.Str("id") is { } id)
        {
            from = Game.Ui.Find(id) ?? throw new ControlException("not_found", $"No element with id '{id}'.");
        }

        return Game.Ui.DumpJson(from, a.Bool("allStyles", false), a.Bool("children", true));
    }

    private string UiQuery(Args a)
    {
        Game.Ui.Update();
        var sb = new StringBuilder("[");
        var first = true;
        foreach (var e in Query(a.Required("selector")))
        {
            sb.Append(first ? "" : ",").Append(Game.Ui.DumpJson(e, false, false));
            first = false;
        }

        return sb.Append(']').ToString();
    }

    private InvokeResult UiInvoke(Args a)
    {
        var e = FindElement(a);
        var ok = Game.Ui.Invoke(e);
        Game.Ui.Update();
        return new InvokeResult(e.ToString(), ok, Game.World.Tick);
    }

    private InputResult UiClick(Args a)
    {
        var e = FindElement(a);
        var b = e.AbsoluteBounds;
        var x = b.X + (b.Width / 2);
        var y = b.Y + (b.Height / 2);
        var hit = Game.Ui.HitTest(x, y);
        if (hit is null || !(hit == e || e.Contains(hit)))
        {
            throw new ControlException("obscured", $"{e} is not reachable at its center ({x:0.#}, {y:0.#}); the pointer would hit {hit?.ToString() ?? "nothing"}. Open the right screen or scroll it into view.");
        }

        ClickAt(x, y, MouseButton.Left);
        return Done();
    }

    private HitResult UiHit(Args a)
    {
        Game.Ui.Update();
        var hit = Game.Ui.HitTest((float)a.Double("x", 0), (float)a.Double("y", 0));
        return new HitResult(hit?.ToString(), hit?.Id);
    }

    // ---- snapshots ----

    private string ResolveOutput(string path)
    {
        var full = Path.GetFullPath(path, OutputDirectory);
        var root = OutputDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(root, StringComparison.Ordinal))
        {
            throw new ControlException("path_outside", $"'{path}' is outside the output folder {OutputDirectory}. Use a relative path.");
        }

        return full;
    }

    private SnapshotResult SnapshotSave(Args a)
    {
        var bytes = Game.SaveSnapshot();
        if (a.Str("path") is { } path)
        {
            var full = ResolveOutput(path);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllBytes(full, bytes);
            return new SnapshotResult(full, bytes.Length, null, HashText(), Game.World.Tick);
        }

        return new SnapshotResult(null, bytes.Length, Convert.ToBase64String(bytes), HashText(), Game.World.Tick);
    }

    private byte[] ReadSnapshotArgument(Args a)
    {
        if (a.Str("path") is { } path)
        {
            var full = ResolveOutput(path);
            var info = new FileInfo(full);
            if (!info.Exists)
            {
                throw new ControlException("not_found", $"No snapshot file {full}.");
            }

            return info.Length > MaxSnapshotBytes ? throw new ControlException("too_large", "The snapshot file is larger than 64 MiB.") : File.ReadAllBytes(full);
        }

        if (a.Str("base64") is { } b64)
        {
            if (b64.Length > MaxSnapshotBytes / 3 * 4)
            {
                throw new ControlException("too_large", "The snapshot is larger than 64 MiB.");
            }

            var buffer = new byte[(b64.Length / 4 * 3) + 3];
            return Convert.TryFromBase64String(b64, buffer, out var written) ? buffer[..written] : throw new ControlException("bad_args", "base64 is not valid base64.");
        }

        throw new ControlException("bad_args", "Give path or base64.");
    }

    // ---- capture ----

    private (int W, int H, float Scale) CaptureSize(Args a)
    {
        var size = a.Str("size");
        int w = Game.Window.Width, h = Game.Window.Height;
        if (size is not null && !Resolution.TryParse(size, out w, out h))
        {
            throw new ControlException("bad_args", $"'{size}' is not a size. Use WIDTHxHEIGHT or a name: 360p 480p 720p 1080p 1440p 4k deck phone tablet.");
        }

        var scale = (float)a.Double("scale", Game.UiScale);
        return scale is > 0 and <= 16 ? (w, h, scale) : throw new ControlException("bad_args", "scale must be between 0 and 16.");
    }

    private static string Sha256(byte[] data) => Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();

    private ControlResult CaptureFrame(Args a)
    {
        CapturedFrame frame;
        int w, h;
        float scale;
        if (a.Bool("presented", false))
        {
            frame = Game.CapturePresented() ?? throw new ControlException("rejected", "There is no presented frame: the game is headless or has no graphics device. Capture without 'presented' to render offscreen.");
            (w, h, scale) = (frame.Width, frame.Height, Game.UiScale);
        }
        else
        {
            (w, h, scale) = CaptureSize(a);
            frame = Game.Capture(w, h, scale);
        }

        var png = Png.Encode(frame);
        string? written = null;
        if (a.Str("path") is { } path)
        {
            written = ResolveOutput(path);
            Directory.CreateDirectory(Path.GetDirectoryName(written)!);
            File.WriteAllBytes(written, png);
        }

        return new ControlResult(
            JsonSerializer.Serialize(new CaptureResult(written, w, h, scale, Sha256(png), frame.Renderer, Game.World.Tick, HashText()), ControlJson.Default.CaptureResult),
            png);
    }

    private SequenceResult CaptureSequence(Args a)
    {
        var (w, h, scale) = CaptureSize(a);
        var count = a.Int("count", 1);
        var every = a.Int("ticksPerFrame", 1);
        if (count is < 1 or > 3600 || every < 1)
        {
            throw new ControlException("bad_args", "count is 1..3600 and ticksPerFrame at least 1.");
        }

        var fps = a.Int("fps", Math.Max(1, GameInstance.TickRate / every));
        var dir = ResolveOutput(a.Required("dir"));
        Directory.CreateDirectory(dir);
        var frames = new SequenceFrame[count];
        var renderer = "";
        for (var i = 0; i < count; i++)
        {
            if (i > 0)
            {
                Game.Step(every);
            }

            var frame = Game.Capture(w, h, scale);
            renderer = frame.Renderer;
            var png = Png.Encode(frame);
            var file = "frame-" + i.ToString("D4", CultureInfo.InvariantCulture) + ".png";
            File.WriteAllBytes(Path.Combine(dir, file), png);
            frames[i] = new SequenceFrame(file, Game.World.Tick, HashText(), Sha256(png));
        }

        var manifest = new SequenceManifest(Game.Definition.Name, w, h, scale, fps, every, renderer, frames);
        var manifestPath = Path.Combine(dir, "manifest.json");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, ControlJson.Default.SequenceManifest));
        return new SequenceResult(dir, manifestPath, count, w, h, fps);
    }
}

/// <summary>Reads typed arguments out of a JSON object, with errors that name the argument.</summary>
internal readonly struct Args
{
    private readonly JsonElement _o;

    public Args(JsonElement o) => _o = o;

    private bool TryGet(string name, out JsonElement v) => _o.TryGetProperty(name, out v) && v.ValueKind != JsonValueKind.Null;

    public string? Str(string name)
    {
        if (!TryGet(name, out var v))
        {
            return null;
        }

        return v.ValueKind switch
        {
            JsonValueKind.String => v.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => v.GetRawText(),
            _ => throw new ControlException("bad_args", $"{name} must be a string."),
        };
    }

    public string Required(string name) => Str(name) ?? throw new ControlException("bad_args", $"{name} is required.");

    public int Int(string name, int fallback) =>
        !TryGet(name, out var v) ? fallback : v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n) ? n : throw new ControlException("bad_args", $"{name} must be an integer.");

    public double Double(string name, double fallback) =>
        !TryGet(name, out var v) ? fallback : v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var n) ? n : throw new ControlException("bad_args", $"{name} must be a number.");

    public bool Bool(string name, bool fallback) =>
        !TryGet(name, out var v) ? fallback : v.ValueKind is JsonValueKind.True or JsonValueKind.False ? v.GetBoolean() : throw new ControlException("bad_args", $"{name} must be true or false.");

    public string[] Strings(string name)
    {
        if (!TryGet(name, out var v))
        {
            return [];
        }

        if (v.ValueKind != JsonValueKind.Array)
        {
            throw new ControlException("bad_args", $"{name} must be an array.");
        }

        var list = new List<string>();
        foreach (var item in v.EnumerateArray())
        {
            list.Add(item.ValueKind == JsonValueKind.String ? item.GetString() ?? "" : item.GetRawText());
        }

        return [.. list];
    }
}
