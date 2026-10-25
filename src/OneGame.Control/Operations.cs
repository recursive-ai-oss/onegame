using System.Text.Json.Serialization;

namespace OneGame.Control;

/// <summary>A control operation failed in a way the caller can act on. <see cref="Code"/> is stable; the message says what to change.</summary>
public sealed class ControlException : Exception
{
    /// <summary>Creates the exception.</summary>
    public ControlException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>Creates the exception.</summary>
    public ControlException(string code, string message, Exception inner)
        : base(message, inner)
    {
        Code = code;
    }

    /// <summary>A short stable identifier such as <c>unknown_op</c>, <c>bad_args</c>, <c>not_found</c> or <c>rejected</c>.</summary>
    public string Code { get; }
}

/// <summary>One operation of the control layer: the name every face uses, what it does, and its arguments as a JSON schema.</summary>
/// <param name="Name">The operation name, also the MCP tool name.</param>
/// <param name="Description">What it does, for an agent that reads the tool list.</param>
/// <param name="InputSchema">JSON schema of the arguments object.</param>
/// <param name="ChangesState">True when the operation can change the simulation (as opposed to only reading it).</param>
public sealed record ControlOp(string Name, string Description, string InputSchema, bool ChangesState);

/// <summary>Anything the MCP face can drive: a list of operations and a way to run one. A game's <see cref="ControlSurface"/> is one; a server's is another.</summary>
public interface IControlEndpoint
{
    /// <summary>The operations, each of which is an MCP tool.</summary>
    IReadOnlyList<ControlOp> Operations { get; }

    /// <summary>A sentence or two for an agent that connects: where to start and what changes state.</summary>
    string Instructions { get; }

    /// <summary>Runs an operation; <paramref name="argsJson"/> is a JSON object or null.</summary>
    /// <exception cref="ControlException">The operation is unknown, its arguments are wrong, or it failed.</exception>
    ControlResult Invoke(string op, string? argsJson = null);
}

/// <summary>What an operation returns: a JSON text and, for captures, the PNG bytes.</summary>
/// <param name="Json">The result object as JSON text.</param>
/// <param name="Png">PNG bytes of a captured frame, when the operation made one.</param>
public sealed record ControlResult(string Json, byte[]? Png = null);

/// <summary>Result of <c>info</c>. <c>Drift</c> counts drift reports filed by the local replica; it must stay 0.</summary>
public sealed record InfoResult(
    string Game, long Tick, long Frame, string Hash, int Width, int Height, float Scale, bool Headless, string? Screen,
    int Entities, string[] Screens, string[] Fixtures, string Stage, string? Adapter, int Drift, string Backend, long Presented);

/// <summary>Result of <c>net_wait</c>: whether the tick was reached before the timeout, and where the replica is.</summary>
/// <param name="Reached">True when the replica reached the tick.</param>
/// <param name="Tick">The replica's tick now.</param>
/// <param name="Hash">Its state hash now.</param>
public sealed record NetWaitResult(bool Reached, long Tick, string Hash);

/// <summary>Result of <c>net_diverge</c>: which entity was corrupted in the local replica.</summary>
/// <param name="Entity">The entity, as <c>Type#id</c>.</param>
/// <param name="Tick">The replica's tick.</param>
/// <param name="Hash">The replica's hash after the change.</param>
public sealed record NetDivergeResult(string Entity, long Tick, string Hash);

/// <summary>Result of <c>clipboard_get</c> and <c>clipboard_set</c>. <c>Text</c> is null when the clipboard holds no text.</summary>
public sealed record ClipboardResult(string? Text, bool Ok);

/// <summary>Result of <c>step</c>, <c>hash</c> and the operations that change the world.</summary>
public sealed record StateResult(long Frame, long Tick, string Hash);

/// <summary>Result of <c>state_patch</c>.</summary>
public sealed record PatchResult(string Entity, string Property, string Value, string Hash);

/// <summary>Result of <c>command_send</c>: <c>Queued</c> means it runs on the next tick.</summary>
public sealed record SendResult(string Entity, string Command, string Status);

/// <summary>One entity type's commands.</summary>
public sealed record CommandsResult(string Entity, string Type, string[] Commands);

/// <summary>Result of the input operations.</summary>
public sealed record InputResult(string? Focused, string? Hovered, long Tick);

/// <summary>Result of <c>ui_invoke</c>.</summary>
public sealed record InvokeResult(string Target, bool Invoked, long Tick);

/// <summary>Result of <c>ui_hit</c>.</summary>
public sealed record HitResult(string? Element, string? Id);

/// <summary>Result of <c>capture_frame</c>.</summary>
public sealed record CaptureResult(string? Path, int Width, int Height, float Scale, string Sha256, string Renderer, long Tick, string Hash);

/// <summary>One frame of a captured sequence.</summary>
public sealed record SequenceFrame(string File, long Tick, string Hash, string Sha256);

/// <summary>The manifest of a captured sequence (also written to <c>manifest.json</c> beside the frames).</summary>
public sealed record SequenceManifest(
    string Game, int Width, int Height, float Scale, int Fps, int TicksPerFrame, string Renderer, SequenceFrame[] Frames);

/// <summary>Result of <c>capture_sequence</c>.</summary>
public sealed record SequenceResult(string Directory, string Manifest, int Frames, int Width, int Height, int Fps);

/// <summary>Result of <c>snapshot_save</c>.</summary>
public sealed record SnapshotResult(string? Path, int Bytes, string? Base64, string Hash, long Tick);

/// <summary>Source-generated JSON metadata for control results; nothing here uses reflection.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(InfoResult))]
[JsonSerializable(typeof(StateResult))]
[JsonSerializable(typeof(PatchResult))]
[JsonSerializable(typeof(SendResult))]
[JsonSerializable(typeof(CommandsResult))]
[JsonSerializable(typeof(CommandsResult[]))]
[JsonSerializable(typeof(InputResult))]
[JsonSerializable(typeof(InvokeResult))]
[JsonSerializable(typeof(HitResult))]
[JsonSerializable(typeof(ClipboardResult))]
[JsonSerializable(typeof(CaptureResult))]
[JsonSerializable(typeof(SequenceManifest))]
[JsonSerializable(typeof(SequenceResult))]
[JsonSerializable(typeof(SnapshotResult))]
[JsonSerializable(typeof(OneGame.Host.NetStatus))]
[JsonSerializable(typeof(NetWaitResult))]
[JsonSerializable(typeof(NetDivergeResult))]
[JsonSerializable(typeof(ShadersResult))]
public sealed partial class ControlJson : JsonSerializerContext
{
}

/// <summary>The operation table. Every face (CLI, test API, MCP) is a view of this list.</summary>
public static class ControlOps
{
    private const string NoArgs = """{"type":"object","properties":{}}""";

    /// <summary>All operations.</summary>
    public static IReadOnlyList<ControlOp> All { get; } =
    [
        new("info", "Describe the running game: tick, frame, state hash, window size, the open screen, and the screens and fixtures it declares.", NoArgs, false),
        new("hash", "The current tick and the 64-bit state hash (hex).", NoArgs, false),
        new("state_query",
            "Read synced state as JSON. With no arguments: the whole world (entities and tables). With entity: that entity. With property too: just that value. 'entity' is a numeric id, a type name (first of that type) or 'Type#n' (nth of that type, from 1).",
            """{"type":"object","properties":{"entity":{"type":"string"},"property":{"type":"string"}}}""", false),
        new("state_patch",
            "Set one scalar synced property from text (numbers, bool, string, Fixed like '12.5'; enums by number). The change bypasses commands, so it is not in a recorded command log.",
            """{"type":"object","properties":{"entity":{"type":"string"},"property":{"type":"string"},"value":{"type":"string"}},"required":["entity","property","value"]}""", true),
        new("commands_list", "List the commands an entity accepts, with parameter types. Without 'entity', every entity type present.",
            """{"type":"object","properties":{"entity":{"type":"string"}}}""", false),
        new("command_send",
            "Send a command to an entity as a player would. It is queued and runs on the next tick (use step). 'args' are the parameters as strings, in order.",
            """{"type":"object","properties":{"entity":{"type":"string"},"command":{"type":"string"},"args":{"type":"array","items":{"type":"string"}}},"required":["entity","command"]}""", true),
        new("net_status", "The connection of a game started with --connect: transport state, the replica's state, tick and hash, resyncs, drift count, and round-trip time, loss and retransmits. Fails with 'not_connected' for a game that runs its own world.", NoArgs, false),
        new("net_wait", "Pump the connection until the replica reaches 'tick' (a server tick number), or for 'timeoutMs' (default 10000, at most 120000). Returns whether it got there, and the tick and hash.",
            """{"type":"object","properties":{"tick":{"type":"integer","minimum":0},"timeoutMs":{"type":"integer","minimum":0,"maximum":120000}},"required":["tick"]}""", false),
        new("net_diverge", "Test hook: corrupt a replica entity locally (flip a bit of its serialized state) so that drift detection can be exercised. 'entity' defaults to the first entity. The replica reports drift at the server's next hash check.",
            """{"type":"object","properties":{"entity":{"type":"string"}}}""", true),
        new("input_pointer",
            "Inject pointer input through the platform event queue. action: move, down, up, click, wheel. Positions are window pixels.",
            """{"type":"object","properties":{"action":{"type":"string","enum":["move","down","up","click","wheel"]},"x":{"type":"number"},"y":{"type":"number"},"button":{"type":"string","enum":["left","right","middle"]},"dx":{"type":"number"},"dy":{"type":"number"}},"required":["action"]}""", true),
        new("input_key", "Inject a key. key is a DOM name (Enter, Escape, Tab, ArrowLeft, a, F5, ' ') or a key name (Digit1). action: press (default), down, up. modifiers: Shift, Control, Alt, Meta held during the press.",
            """{"type":"object","properties":{"key":{"type":"string"},"action":{"type":"string","enum":["press","down","up"]},"modifiers":{"type":"array","items":{"type":"string"}}},"required":["key"]}""", true),
        new("input_text", "Inject typed text (a TextInput event).", """{"type":"object","properties":{"text":{"type":"string"}},"required":["text"]}""", true),
        new("input_composition", "Inject an input method's pre-edit text (a TextComposition event), as an IME does while composing. An empty text ends the composition; send the finished text with input_text.",
            """{"type":"object","properties":{"text":{"type":"string"},"cursorStart":{"type":"integer"},"cursorEnd":{"type":"integer"}},"required":["text"]}""", true),
        new("clipboard_get", "Read the system clipboard's text (null when empty). selection: clipboard (default) or primary (X11 only).",
            """{"type":"object","properties":{"selection":{"type":"string","enum":["clipboard","primary"]}}}""", false),
        new("clipboard_set", "Put text on the system clipboard, so a paste (Control+V) can be tested. selection: clipboard (default) or primary (X11 only).",
            """{"type":"object","properties":{"text":{"type":"string"},"selection":{"type":"string","enum":["clipboard","primary"]}},"required":["text"]}""", false),
        new("input_nav", "Inject a controller action: up, down, left, right, next, previous, activate, cancel (a virtual gamepad's d-pad, shoulders, South and East buttons).",
            """{"type":"object","properties":{"action":{"type":"string","enum":["up","down","left","right","next","previous","activate","cancel"]}},"required":["action"]}""", true),
        new("step", "Run frames. Each frame handles input, runs one 60 Hz tick and updates the UI. Returns the new tick and state hash.",
            """{"type":"object","properties":{"frames":{"type":"integer","minimum":1,"maximum":1000000}}}""", true),
        new("ui_tree", "Dump the UI tree as JSON (kind, id, classes, states, text, boxes, computed style). 'id' limits it to a subtree.",
            """{"type":"object","properties":{"id":{"type":"string"},"allStyles":{"type":"boolean"},"children":{"type":"boolean"}}}""", false),
        new("ui_query", "Find UI elements by selector (Kind, #id, .class, :hover, :focus, descendant and > combinators). Returns each match without children.",
            """{"type":"object","properties":{"selector":{"type":"string"}},"required":["selector"]}""", false),
        new("ui_invoke", "Click a UI element without a pointer, by id or selector (first match).",
            """{"type":"object","properties":{"id":{"type":"string"},"selector":{"type":"string"}}}""", true),
        new("ui_click", "Click the center of a UI element with real pointer events (move, down, up), by id or selector (first match).",
            """{"type":"object","properties":{"id":{"type":"string"},"selector":{"type":"string"}}}""", true),
        new("ui_hit", "The element a pointer event at this position would reach.",
            """{"type":"object","properties":{"x":{"type":"number"},"y":{"type":"number"}},"required":["x","y"]}""", false),
        new("screen_open", "Replace the UI with a declared screen.", """{"type":"object","properties":{"name":{"type":"string"}},"required":["name"]}""", true),
        new("fixture_load", "Replace the world with a declared fixture.", """{"type":"object","properties":{"name":{"type":"string"}},"required":["name"]}""", true),
        new("reset", "Start a new game: a fresh world from the game's setup, optionally with another seed.", """{"type":"object","properties":{"seed":{"type":"integer","minimum":0}}}""", true),
        new("snapshot_save", "Write the world as a snapshot to 'path' (under the output folder), or return it as base64 when no path is given.",
            """{"type":"object","properties":{"path":{"type":"string"}}}""", false),
        new("snapshot_load", "Replace the world with a snapshot, from 'path' or 'base64'.",
            """{"type":"object","properties":{"path":{"type":"string"},"base64":{"type":"string"}}}""", true),
        new("shaders",
            "Hot-reload status of the game's registered shaders. Checks their files first: a changed one is recompiled and, when its interface still matches, swapped in; otherwise the old shader keeps running and the state says why (compile_error with file and line, or incompatible).",
            NoArgs, true),
        new("capture_frame",
            "Render the current state offscreen to a PNG. size is WxH or a name (360p 720p 1080p 1440p 4k deck phone tablet); scale is the UI scale. With 'path' the file is written under the output folder. With presented=true, a windowed game instead returns the frame it presents to its window (the swapchain image, at the window's size; size and scale are ignored).",
            """{"type":"object","properties":{"size":{"type":"string"},"scale":{"type":"number"},"path":{"type":"string"},"presented":{"type":"boolean"}}}""", false),
        new("capture_sequence",
            "Run frames and capture each one: 'count' captures, 'ticksPerFrame' ticks between them. Writes frame-NNNN.png and manifest.json (the video form: frames plus fps) into 'dir' under the output folder.",
            """{"type":"object","properties":{"dir":{"type":"string"},"count":{"type":"integer","minimum":1,"maximum":3600},"ticksPerFrame":{"type":"integer","minimum":1},"size":{"type":"string"},"scale":{"type":"number"},"fps":{"type":"integer","minimum":1}},"required":["dir","count"]}""", true),
        new("quit", "Ask the game to exit.", NoArgs, false),
    ];
}
