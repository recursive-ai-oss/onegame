using OneGame.Core;
using OneGame.Host;

namespace OneGame.Testing;

/// <summary>One command as it ran: the tick, the target, the command and its encoded arguments.</summary>
/// <param name="Tick">The tick the command ran in.</param>
/// <param name="Entity">The entity id.</param>
/// <param name="CommandId">The command id.</param>
/// <param name="Payload">The encoded arguments.</param>
public sealed record RecordedCommand(long Tick, ulong Entity, uint CommandId, byte[] Payload);

/// <summary>
/// A recorded run of one world: the snapshot it started from, every command that ran with its tick, and the state hash after each tick.
/// Replaying the commands against the snapshot must reproduce every hash. The file is untrusted input when read.
/// </summary>
public sealed class Recording
{
    private const uint Magic = 0x4352474F; // "OGRC"
    private const int Version = 1;
    private const int MaxCommands = 1 << 22;
    private const int MaxTicks = 1 << 24;
    private const int MaxSnapshotBytes = 64 * 1024 * 1024;

    /// <summary>The game's name.</summary>
    public string Game { get; init; } = "";

    /// <summary>The world seed.</summary>
    public ulong Seed { get; init; }

    /// <summary>The tick the recording starts at.</summary>
    public long StartTick { get; init; }

    /// <summary>The world snapshot at <see cref="StartTick"/>.</summary>
    public byte[] Snapshot { get; init; } = [];

    /// <summary>The commands that ran, in order.</summary>
    public IReadOnlyList<RecordedCommand> Commands { get; init; } = [];

    /// <summary>The state hash after tick <c>StartTick + i</c> finished.</summary>
    public IReadOnlyList<ulong> Hashes { get; init; } = [];

    /// <summary>Number of ticks recorded.</summary>
    public int Ticks => Hashes.Count;

    /// <summary>Serializes the recording.</summary>
    public byte[] ToBytes()
    {
        var w = new SyncWriter(Snapshot.Length + 1024);
        w.WriteFixed32(Magic);
        w.WriteUInt32(Version);
        w.WriteString(Game);
        w.WriteUInt64(Seed);
        w.WriteInt64(StartTick);
        w.WriteBytes(Snapshot);
        w.WriteUInt32((uint)Commands.Count);
        foreach (var c in Commands)
        {
            w.WriteInt64(c.Tick);
            w.WriteUInt64(c.Entity);
            w.WriteFixed32(c.CommandId);
            w.WriteBytes(c.Payload);
        }

        w.WriteUInt32((uint)Hashes.Count);
        foreach (var h in Hashes)
        {
            w.WriteFixed64(h);
        }

        return w.ToArray();
    }

    /// <summary>Reads a recording. Sizes are checked against limits before anything is allocated.</summary>
    /// <exception cref="InvalidDataException">The bytes are not a recording this version understands.</exception>
    public static Recording FromBytes(ReadOnlySpan<byte> bytes)
    {
        var r = new SyncReader(bytes);
        if (r.ReadFixed32() != Magic)
        {
            throw new InvalidDataException("This is not a recording file (bad header).");
        }

        var version = r.ReadUInt32();
        if (r.Ok && version != Version)
        {
            throw new InvalidDataException($"The recording is format {version}; this build reads format {Version}.");
        }

        var game = r.ReadString();
        var seed = r.ReadUInt64();
        var start = r.ReadInt64();
        var snapshot = r.ReadBytes(MaxSnapshotBytes).ToArray();
        var commandCount = r.ReadCount(MaxCommands);
        var commands = new List<RecordedCommand>(Math.Min(commandCount, 4096));
        for (var i = 0; i < commandCount && r.Ok; i++)
        {
            var tick = r.ReadInt64();
            var entity = r.ReadUInt64();
            var id = r.ReadFixed32();
            var payload = r.ReadBytes(SyncLimits.MaxCommandBytes).ToArray();
            commands.Add(new RecordedCommand(tick, entity, id, payload));
        }

        var hashCount = r.ReadCount(MaxTicks);
        var hashes = new List<ulong>(Math.Min(hashCount, 4096));
        for (var i = 0; i < hashCount && r.Ok; i++)
        {
            hashes.Add(r.ReadFixed64());
        }

        r.ExpectEnd();
        if (!r.Ok)
        {
            throw new InvalidDataException($"The recording is damaged: {r.Error}.");
        }

        return new Recording { Game = game, Seed = seed, StartTick = start, Snapshot = snapshot, Commands = commands, Hashes = hashes };
    }

    /// <summary>Writes the recording to a file.</summary>
    public void Save(string path)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllBytes(path, ToBytes());
    }

    /// <summary>Reads a recording file.</summary>
    /// <exception cref="InvalidDataException">The file is not a recording, or is too large.</exception>
    public static Recording Load(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists)
        {
            throw new FileNotFoundException($"No recording at {path}.", path);
        }

        return info.Length > MaxSnapshotBytes + (256L * 1024 * 1024)
            ? throw new InvalidDataException("The recording file is too large.")
            : FromBytes(File.ReadAllBytes(path));
    }

    /// <summary>
    /// Replays the recording on a game and compares the state hash after every tick. The game must be a fresh instance of the same
    /// definition; its world is replaced by the recorded snapshot.
    /// </summary>
    public ReplayResult Replay(GameInstance game)
    {
        ArgumentNullException.ThrowIfNull(game);
        game.LoadSnapshot(Snapshot);
        var world = game.World;
        var next = 0;
        for (var i = 0; i < Hashes.Count; i++)
        {
            var tick = StartTick + i;
            while (next < Commands.Count && Commands[next].Tick == tick)
            {
                var c = Commands[next++];
                world.Submit(new EntityId(c.Entity), c.CommandId, c.Payload);
            }

            world.Step();
            var actual = world.ComputeHash();
            if (actual != Hashes[i])
            {
                return new ReplayResult(false, i, tick, Hashes[i], actual);
            }
        }

        return new ReplayResult(true, Hashes.Count, StartTick + Hashes.Count, Hashes.Count > 0 ? Hashes[^1] : 0, world.ComputeHash());
    }
}

/// <summary>The outcome of replaying a <see cref="Recording"/>.</summary>
/// <param name="Identical">True when every tick's hash matched.</param>
/// <param name="TicksChecked">Ticks that matched (or, on a mismatch, the index of the first one that did not).</param>
/// <param name="Tick">The tick of the first mismatch, or the tick after the last replayed one.</param>
/// <param name="Expected">The recorded hash at that point.</param>
/// <param name="Actual">The replayed hash at that point.</param>
public sealed record ReplayResult(bool Identical, int TicksChecked, long Tick, ulong Expected, ulong Actual);

/// <summary>Records a running game into a <see cref="Recording"/>. Start it before the interesting input; stop it to get the result.</summary>
public sealed class RunRecorder : IDisposable
{
    private readonly GameInstance _game;
    private readonly List<RecordedCommand> _commands = [];
    private readonly List<ulong> _hashes = [];
    private readonly byte[] _snapshot;
    private readonly long _startTick;
    private readonly CommandObserver? _previous;
    private World _world;
    private bool _stopped;

    /// <summary>Starts recording from the game's current state.</summary>
    public RunRecorder(GameInstance game)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _world = game.World;
        _snapshot = game.SaveSnapshot();
        _startTick = game.World.Tick;
        _previous = _world.Observer;
        _world.Observer = OnCommand;
        game.TickCompleted += OnTick;
        game.WorldReplaced += OnReplaced;
    }

    /// <summary>Set when the world was replaced during recording (a fixture, snapshot or reset), which makes the log unusable.</summary>
    public string? Invalid { get; private set; }

    private void OnCommand(long tick, EntityId entity, uint commandId, ReadOnlySpan<byte> payload, CommandStatus status)
    {
        if (status != CommandStatus.Forwarded)
        {
            _commands.Add(new RecordedCommand(tick, entity.Value, commandId, payload.ToArray()));
        }

        _previous?.Invoke(tick, entity, commandId, payload, status);
    }

    private void OnTick(World world) => _hashes.Add(world.ComputeHash());

    private void OnReplaced(World world)
    {
        Invalid = "The world was replaced while recording (fixture, snapshot or reset). Start the recording after loading it.";
        _world = world;
    }

    /// <summary>Stops and returns the recording.</summary>
    /// <exception cref="InvalidOperationException">The world was replaced during the recording.</exception>
    public Recording Stop()
    {
        Dispose();
        if (Invalid is not null)
        {
            throw new InvalidOperationException(Invalid);
        }

        return new Recording
        {
            Game = _game.Definition.Name,
            Seed = _game.Seed,
            StartTick = _startTick,
            Snapshot = _snapshot,
            Commands = _commands,
            Hashes = _hashes,
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_stopped)
        {
            return;
        }

        _stopped = true;
        _game.TickCompleted -= OnTick;
        _game.WorldReplaced -= OnReplaced;
        if (_world.Observer == OnCommand)
        {
            _world.Observer = _previous;
        }
    }
}
