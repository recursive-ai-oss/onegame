using OneGame.Sync;
using OneGame.Control;
using OneGame.Core;
using OneGame.Host;
using OneGame.UI;

namespace OneGame.Testing;

/// <summary>Options for <see cref="TestCluster"/>.</summary>
public sealed record TestOptions
{
    /// <summary>Window width in pixels.</summary>
    public int Width { get; init; } = 1280;

    /// <summary>Window height in pixels.</summary>
    public int Height { get; init; } = 720;

    /// <summary>UI scale.</summary>
    public float Scale { get; init; } = 1f;

    /// <summary>World seed; the game's own when null.</summary>
    public ulong? Seed { get; init; }

    /// <summary>Graphics device policy for captures. Auto uses a device when there is one; Off always renders on the CPU.</summary>
    public GpuMode Gpu { get; init; } = GpuMode.Auto;

    /// <summary>Where the 2D renderer gets compiled shaders; the renderer's own shaders when null.</summary>
    public OneGame.Render2D.IShaderSource? Shaders { get; init; }

    /// <summary>Folder for files that operations write (captures, snapshots). A fresh temporary folder when null.</summary>
    public string? OutputDirectory { get; init; }
}

/// <summary>A headless game under test, driven through the control layer like a live one. One node for now.</summary>
public sealed class TestClient : IDisposable
{
    internal TestClient(GameInstance game, string outputDirectory)
    {
        Game = game;
        Control = new ControlSurface(game, outputDirectory);
    }

    /// <summary>The running game.</summary>
    public GameInstance Game { get; }

    /// <summary>The control layer over the game.</summary>
    public ControlSurface Control { get; }

    /// <summary>The world's tick.</summary>
    public long Tick => Game.World.Tick;

    /// <summary>The world's state hash.</summary>
    public ulong Hash => Game.World.ComputeHash();

    /// <summary>Runs a control operation and returns its JSON.</summary>
    public string Call(string op, string? argsJson = null) => Control.Call(op, argsJson);

    /// <summary>Runs frames.</summary>
    public TestClient Step(int frames = 1)
    {
        Game.Step(frames);
        return this;
    }

    /// <summary>Clicks an element by id with pointer events.</summary>
    public TestClient Click(string id)
    {
        Control.Call("ui_click", Quote("id", id));
        return this;
    }

    /// <summary>Clicks a window position.</summary>
    public TestClient ClickAt(double x, double y)
    {
        Control.Call("input_pointer", string.Create(CultureInfo.InvariantCulture, $$"""{"action":"click","x":{{x}},"y":{{y}}}"""));
        return this;
    }

    /// <summary>Presses a key by name (<c>Enter</c>, <c>Escape</c>, <c>a</c>).</summary>
    public TestClient Press(string key)
    {
        Control.Call("input_key", Quote("key", key));
        return this;
    }

    /// <summary>Types text.</summary>
    public TestClient Type(string text)
    {
        Control.Call("input_text", Quote("text", text));
        return this;
    }

    /// <summary>Sends a command to an entity (id, type name or Type#n) as a player would; it runs on the next tick.</summary>
    public TestClient Send(string entity, string command, params string[] args)
    {
        Control.Call("command_send", JsonSerializer.Serialize(new CommandArgs(entity, command, args), TestJson.Default.CommandArgs));
        return this;
    }

    /// <summary>Reads one synced property as text.</summary>
    public string Get(string entity, string property)
    {
        using var doc = JsonDocument.Parse(Control.Call("state_query", JsonSerializer.Serialize(new QueryArgs(entity, property), TestJson.Default.QueryArgs)));
        return doc.RootElement.ValueKind == JsonValueKind.String ? doc.RootElement.GetString()! : doc.RootElement.GetRawText();
    }

    /// <summary>Finds a UI element by id.</summary>
    public Element? Find(string id)
    {
        Game.Ui.Update();
        return Game.Ui.Find(id);
    }

    /// <summary>Renders the current state offscreen.</summary>
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
/// An in-process cluster of games for tests. Today it is one authority node; when the sync stack gains more nodes they appear in
/// <see cref="Nodes"/> and the same client calls drive each.
/// </summary>
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

    /// <summary>The nodes. <c>Nodes[0]</c> is the authority.</summary>
    public IReadOnlyList<TestClient> Nodes => _nodes;

    /// <summary>The first node.</summary>
    public TestClient Client => _nodes[0];

    /// <summary>The folder operations write to.</summary>
    public string OutputDirectory => _output;

    /// <summary>Starts a cluster with one headless node.</summary>
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
        new(GameInstance.Create(game, new GameOptions { Headless = true, Width = options.Width, Height = options.Height, Scale = options.Scale, Seed = options.Seed, Gpu = options.Gpu, Shaders = options.Shaders }), output);

    /// <summary>Runs a scenario against the first node.</summary>
    public ScenarioResult Run(Scenario scenario, ScenarioRunOptions? options = null) =>
        new ScenarioRunner(Client.Control, options ?? new ScenarioRunOptions { ArtifactsDirectory = Path.Combine(_output, "artifacts") }).Run(scenario);

    /// <summary>Reads and runs a scenario file against the first node.</summary>
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
