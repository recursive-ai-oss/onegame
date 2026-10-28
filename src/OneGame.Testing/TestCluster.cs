using OneGame.Sync;
using OneGame.Control;
using OneGame.Core;
using OneGame.Host;
using OneGame.UI;

namespace OneGame.Testing;

/// <summary>Options for <see cref="TestCluster.Start"/>: the headless window size, the UI scale, the seed and where captures and saves go. Every property has a default, so <c>new TestOptions()</c> is a normal 1280x720 test.</summary>
public sealed record TestOptions
{
    /// <summary>Window width in pixels (default 1280). Captures and UI layout use it.</summary>
    public int Width { get; init; } = 1280;

    /// <summary>Window height in pixels (default 720).</summary>
    public int Height { get; init; } = 720;

    /// <summary>UI scale: 1 draws the UI at its declared size, 2 at double size on the same window. Default 1.</summary>
    public float Scale { get; init; } = 1f;

    /// <summary>World seed; the game's own when null.</summary>
    public ulong? Seed { get; init; }

    /// <summary>Graphics device policy for captures. Auto uses a device when there is one; Off always renders on the CPU.</summary>
    public GpuMode Gpu { get; init; } = GpuMode.Auto;

    /// <summary>Where the 2D renderer gets compiled shaders; the renderer's own shaders when null.</summary>
    public OneGame.Render2D.IShaderSource? Shaders { get; init; }

    /// <summary>Folder for files that operations write (captures, snapshots). A fresh temporary folder when null.</summary>
    public string? OutputDirectory { get; init; }

    /// <summary>Where the game's saves and settings live. A test starts with memory (never the player's files); give a shared store, or a folder, to model a restart.</summary>
    public OneGame.Storage.IByteStore? Storage { get; init; }
}

/// <summary>
/// A headless game under test, driven through the same control layer a live game and an MCP client use: click, press, type, send a
/// command, step frames, read state, capture. Every driving call returns the client, so a test reads as one chain.
/// Get one from <see cref="TestCluster.Client"/>. Calls fail with <see cref="OneGame.Control.ControlException"/> (with a stable
/// <c>Code</c>) when the element, entity, command or key does not exist.
/// </summary>
/// <example>
/// <code>
/// using var cluster = TestCluster.Start(new BricksGame(), new TestOptions { Width = 540, Height = 960 });
/// var client = cluster.Client;
/// client.Click("play").Step(2);
/// client.Send("Court", "Launch").Step(20);
/// Assert.Equal("2", client.Get("Court", "Phase"));
/// </code>
/// </example>
public sealed class TestClient : IDisposable
{
    internal TestClient(GameInstance game, string outputDirectory)
    {
        Game = game;
        Control = new ControlSurface(game, outputDirectory);
    }

    /// <summary>The running game, for what the control layer does not cover: its platform injector, UI tree and world.</summary>
    public GameInstance Game { get; }

    /// <summary>The control layer over the game.</summary>
    public ControlSurface Control { get; }

    /// <summary>The world's tick: how many simulation ticks have run since the world was created or its snapshot was taken.</summary>
    public long Tick => Game.World.Tick;

    /// <summary>The hash of all synced state at this tick. Two runs that do the same things have the same hash on every tick; compare it to prove a change did not alter behaviour.</summary>
    public ulong Hash => Game.World.ComputeHash();

    /// <summary>Runs any control operation by name (the same ones live control lists) and returns its JSON result. Use it for what the typed calls below do not cover.</summary>
    /// <param name="op">The operation, for example <c>state_query</c>.</param>
    /// <param name="argsJson">The operation's arguments as a JSON object, or null for none.</param>
    /// <exception cref="OneGame.Control.ControlException">The operation is unknown or its arguments are wrong.</exception>
    public string Call(string op, string? argsJson = null) => Control.Call(op, argsJson);

    /// <summary>Runs frames: each is one fixed simulation tick plus input, UI update and layout, as in the running game. Time only passes when you call this.</summary>
    /// <param name="frames">How many frames to run; 1 by default.</param>
    public TestClient Step(int frames = 1)
    {
        Game.Step(frames);
        return this;
    }

    /// <summary>Clicks a UI element by its id, with real pointer move, down and up events at the centre of its box, so hover, press and click handlers run as for a player. Follow with <see cref="Step"/> so the effect shows.</summary>
    /// <param name="id">The element's <c>Id</c>.</param>
    /// <exception cref="OneGame.Control.ControlException">No visible element has that id.</exception>
    public TestClient Click(string id)
    {
        Control.Call("ui_click", Quote("id", id));
        return this;
    }

    /// <summary>Clicks a window position given in pixels from the top-left corner of the window (before UI scale).</summary>
    public TestClient ClickAt(double x, double y)
    {
        Control.Call("input_pointer", string.Create(CultureInfo.InvariantCulture, $$"""{"action":"click","x":{{x}},"y":{{y}}}"""));
        return this;
    }

    /// <summary>Presses and releases a key by name (<c>Enter</c>, <c>Escape</c>, <c>Space</c>, <c>a</c>). It goes through the game's input path, so focus, shortcuts and action maps see it.</summary>
    /// <exception cref="OneGame.Control.ControlException">The key name is not known.</exception>
    public TestClient Press(string key)
    {
        Control.Call("input_key", Quote("key", key));
        return this;
    }

    /// <summary>Types text into the focused text input, as committed characters (what an IME or keyboard produces).</summary>
    public TestClient Type(string text)
    {
        Control.Call("input_text", Quote("text", text));
        return this;
    }

    /// <summary>Sends a command to an entity as a player would; it is validated and runs on the next tick, so call <see cref="Step"/> before reading the result.</summary>
    /// <param name="entity">The entity: its type name (when there is one), an id, or <c>Type#n</c>.</param>
    /// <param name="command">The command method's name.</param>
    /// <param name="args">The arguments as text, in the command's parameter order; numbers use the invariant culture.</param>
    /// <exception cref="OneGame.Control.ControlException">The entity or command does not exist or the arguments do not parse.</exception>
    public TestClient Send(string entity, string command, params string[] args)
    {
        Control.Call("command_send", JsonSerializer.Serialize(new CommandArgs(entity, command, args), TestJson.Default.CommandArgs));
        return this;
    }

    /// <summary>Reads one synced property as text (strings as they are, everything else as its JSON form).</summary>
    /// <param name="entity">The entity, as for <see cref="Send"/>.</param>
    /// <param name="property">The property name; a path such as <c>Balls[0].Y</c> or <c>Balls.count</c> reaches into collections.</param>
    /// <exception cref="OneGame.Control.ControlException">The entity or property does not exist.</exception>
    public string Get(string entity, string property)
    {
        using var doc = JsonDocument.Parse(Control.Call("state_query", JsonSerializer.Serialize(new QueryArgs(entity, property), TestJson.Default.QueryArgs)));
        return doc.RootElement.ValueKind == JsonValueKind.String ? doc.RootElement.GetString()! : doc.RootElement.GetRawText();
    }

    /// <summary>Finds a UI element by id after bringing styles and layout up to date, or null when there is none. Read its computed box, text or state.</summary>
    public Element? Find(string id)
    {
        Game.Ui.Update();
        return Game.Ui.Find(id);
    }

    /// <summary>Renders the current state offscreen, with no window or display.</summary>
    /// <param name="size">The resolution as <c>WIDTHxHEIGHT</c>, for example <c>1280x720</c>.</param>
    /// <param name="scale">The UI scale for this capture.</param>
    /// <returns>The frame's RGBA pixels, size and the renderer that drew them; compare with <see cref="ImageCompare"/>.</returns>
    public CapturedFrame Capture(string size = "320x180", float scale = 1f) => Game.Capture(Resolution.Parse(size).Width, Resolution.Parse(size).Height, scale);

    private static string Quote(string key, string value) => "{" + JsonSerializer.Serialize(key, TestJson.Default.String) + ":" + JsonSerializer.Serialize(value, TestJson.Default.String) + "}";

    /// <inheritdoc />
    public void Dispose() => Game.Dispose();
}

internal sealed record CommandArgs(string Entity, string Command, string[] Args);

internal sealed record QueryArgs(string Entity, string Property);

[System.Text.Json.Serialization.JsonSourceGenerationOptions(PropertyNamingPolicy = System.Text.Json.Serialization.JsonKnownNamingPolicy.CamelCase)]
[System.Text.Json.Serialization.JsonSerializable(typeof(string))]
[System.Text.Json.Serialization.JsonSerializable(typeof(CommandArgs))]
[System.Text.Json.Serialization.JsonSerializable(typeof(QueryArgs))]
internal sealed partial class TestJson : System.Text.Json.Serialization.JsonSerializerContext
{
}

/// <summary>
/// The entry point for game tests: starts your game headless in this process and hands back a <see cref="TestClient"/> to drive it.
/// Dispose it when the test ends. It holds one authority node, so single-player rules, saves and UI are tested exactly as they run;
/// use <see cref="Launch"/> for several real processes over the real transport.
/// </summary>
/// <example>
/// A test that clicks through a screen, checks state and runs a scenario file:
/// <code>
/// [Fact]
/// public void PlayServesLevelOne()
/// {
///     using var cluster = TestCluster.Start(new BricksGame(), new TestOptions { Width = 540, Height = 960 });
///     cluster.Client.Click("play").Step(2);
///     Assert.Equal("1", cluster.Client.Get("Court", "Level"));
///
///     var result = cluster.Run("scenarios/02-brick-broken.yaml");
///     Assert.True(result.Passed, string.Join("; ", result.Expectations.Where(e => !e.Passed).Select(e => e.Expected + " but was " + e.Actual)));
/// }
/// </code>
/// </example>
public sealed class TestCluster : IDisposable
{
    private readonly List<TestClient> _nodes = [];
    private readonly string _output;
    private readonly bool _ownsOutput;

    private TestCluster(string output, bool ownsOutput)
    {
        _output = output;
        _ownsOutput = ownsOutput;
    }

    /// <summary>All nodes. <c>Nodes[0]</c> is the authority; today that is the only one.</summary>
    public IReadOnlyList<TestClient> Nodes => _nodes;

    /// <summary>The first (authority) node: the one most tests drive.</summary>
    public TestClient Client => _nodes[0];

    /// <summary>The folder that captures, snapshots and other files written by operations go to. A temporary folder deleted on dispose unless <see cref="TestOptions.OutputDirectory"/> named one.</summary>
    public string OutputDirectory => _output;

    /// <summary>Starts a cluster with one headless node running <paramref name="game"/>.</summary>
    /// <param name="game">The game definition, as the game's own <c>Program</c> would run it. Give it in-memory services (for example a <c>MemoryByteStore</c>) so tests never touch a player's saves.</param>
    /// <param name="options">Window size, UI scale, seed and folders; defaults when null.</param>
    /// <exception cref="ArgumentNullException"><paramref name="game"/> is null.</exception>
    public static TestCluster Start(GameDefinition game, TestOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        options ??= new TestOptions();
        var output = options.OutputDirectory ?? Path.Combine(Path.GetTempPath(), "onegame-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(output);
        var cluster = new TestCluster(output, options.OutputDirectory is null);
        cluster._nodes.Add(NewNode(game, options, output));
        return cluster;
    }

    /// <summary>
    /// Starts a cluster of real processes (dedicated servers and headless game clients) on this machine, talking over the real transport.
    /// Dispose it to stop them; see <see cref="ProcessCluster"/>.
    /// </summary>
    /// <param name="spec">The nodes.</param>
    /// <param name="parentPid">The process the nodes watch, so they stop if it dies; this process when null.</param>
    public static ProcessCluster Launch(ClusterSpec spec, int? parentPid = null) => ProcessCluster.Up(spec, parentPid);

    internal static TestClient NewNode(GameDefinition game, TestOptions options, string output) =>
        new(GameInstance.Create(game, new GameOptions { Headless = true, Width = options.Width, Height = options.Height, Scale = options.Scale, Seed = options.Seed, Gpu = options.Gpu, Shaders = options.Shaders, Storage = options.Storage }), output);

    /// <summary>Runs a scenario against the first node: its steps in order (a step that fails stops it), then its expectations. Failures are in the result, not thrown: check <see cref="ScenarioResult.Passed"/>.</summary>
    /// <param name="scenario">A parsed scenario.</param>
    /// <param name="options">Where captures and references live; a folder under <see cref="OutputDirectory"/> when null.</param>
    public ScenarioResult Run(Scenario scenario, ScenarioRunOptions? options = null) =>
        new ScenarioRunner(Client.Control, options ?? new ScenarioRunOptions { ArtifactsDirectory = Path.Combine(_output, "artifacts") }).Run(scenario);

    /// <summary>Reads a scenario YAML file and runs it against the first node.</summary>
    /// <param name="scenarioPath">Path of the <c>.yaml</c> file.</param>
    /// <param name="options">As for the other overload.</param>
    /// <exception cref="ScenarioException">The file does not parse; the message names the line.</exception>
    public ScenarioResult Run(string scenarioPath, ScenarioRunOptions? options = null) => Run(ScenarioParser.Load(scenarioPath), options);

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var n in _nodes)
        {
            n.Dispose();
        }

        if (_ownsOutput)
        {
            try
            {
                Directory.Delete(_output, true);
            }
            catch (IOException)
            {
                // A leftover temp folder is harmless.
            }
        }
    }
}

/// <summary>A determinism check found two runs that should be identical and are not.</summary>
public sealed class DeterminismException : Exception
{
    /// <summary>Creates the exception.</summary>
    public DeterminismException(string message)
        : base(message)
    {
    }
}

/// <summary>The outcome of a determinism check.</summary>
/// <param name="Check">Which check ran.</param>
/// <param name="Identical">True when every frame's hash matched.</param>
/// <param name="Frames">Frames compared.</param>
/// <param name="FirstMismatchFrame">The first frame whose hash differed, or -1.</param>
/// <param name="Expected">The hash of the reference run at that frame.</param>
/// <param name="Actual">The hash of the other run at that frame.</param>
public sealed record DeterminismReport(string Check, bool Identical, int Frames, int FirstMismatchFrame, ulong Expected, ulong Actual)
{
    /// <summary>Throws <see cref="DeterminismException"/> unless the runs were identical.</summary>
    public void ThrowIfDifferent()
    {
        if (!Identical)
        {
            throw new DeterminismException($"{Check}: the runs differ at frame {FirstMismatchFrame} (hash {Expected:x16} vs {Actual:x16}).");
        }
    }
}

/// <summary>Checks that a scripted run is reproducible: twice from scratch, through a snapshot, and through a recording.</summary>
public static class Determinism
{
    /// <summary>Does something to the game before frame number <paramref name="frame"/> runs: send a command, click, press a key.</summary>
    public delegate void Drive(TestClient client, int frame);

    /// <summary>Runs the script twice in fresh games and compares the state hash after every frame.</summary>
    public static DeterminismReport RunTwice(Func<GameDefinition> make, Drive drive, int frames, TestOptions? options = null)
    {
        var a = Trace(make, drive, 0, frames, null, options, out _);
        var b = Trace(make, drive, 0, frames, null, options, out _);
        return Compare("run twice", a, b);
    }

    /// <summary>
    /// Runs the script straight through, and again with a snapshot taken after <paramref name="splitFrame"/> frames and restored into a
    /// new game that runs the rest. The hashes after the split must match.
    /// </summary>
    public static DeterminismReport SnapshotRestore(Func<GameDefinition> make, Drive drive, int frames, int splitFrame, TestOptions? options = null)
    {
        if (splitFrame < 1 || splitFrame >= frames)
        {
            throw new ArgumentOutOfRangeException(nameof(splitFrame), "The split must be inside the run.");
        }

        var straight = Trace(make, drive, 0, frames, null, options, out _);
        var first = Trace(make, drive, 0, splitFrame, null, options, out var snapshot);
        var rest = Trace(make, drive, splitFrame, frames, snapshot, options, out _);
        return Compare("snapshot and restore", straight, [.. first, .. rest]);
    }

    /// <summary>Records the script, replays the recording in a new game and compares every tick.</summary>
    public static DeterminismReport Replay(Func<GameDefinition> make, Drive drive, int frames, TestOptions? options = null)
    {
        Recording recording;
        using (var cluster = TestCluster.Start(make(), options))
        {
            var recorder = cluster.Client.Game.StartRecording();
            for (var f = 0; f < frames; f++)
            {
                drive(cluster.Client, f);
                cluster.Client.Step(1);
            }

            recording = recorder.Stop();
        }

        using var fresh = TestCluster.Start(make(), options);
        var result = fresh.Client.Game.Replay(recording);
        var ticks = (int)(recording.EndTick - recording.StartTick);
        return new DeterminismReport("replay", result.Ok, ticks, result.Ok ? -1 : (int)(result.Divergence!.Tick - recording.StartTick), result.Divergence?.ExpectedHash ?? 0, result.Divergence?.ActualHash ?? 0);
    }

    private static ulong[] Trace(Func<GameDefinition> make, Drive drive, int from, int to, byte[]? restore, TestOptions? options, out byte[] endSnapshot)
    {
        using var cluster = TestCluster.Start(make(), options);
        var client = cluster.Client;
        if (restore is not null)
        {
            client.Game.LoadSnapshot(restore);
        }

        var hashes = new ulong[to - from];
        for (var f = from; f < to; f++)
        {
            drive(client, f);
            client.Step(1);
            hashes[f - from] = client.Hash;
        }

        endSnapshot = client.Game.SaveSnapshot();
        return hashes;
    }

    private static DeterminismReport Compare(string check, ulong[] expected, ulong[] actual)
    {
        var n = Math.Min(expected.Length, actual.Length);
        for (var i = 0; i < n; i++)
        {
            if (expected[i] != actual[i])
            {
                return new DeterminismReport(check, false, n, i, expected[i], actual[i]);
            }
        }

        return new DeterminismReport(check, expected.Length == actual.Length, n, expected.Length == actual.Length ? -1 : n, 0, 0);
    }
}
