using OneGame.Core;
using OneGame.Sync;

namespace OneGame.Host;

/// <summary>
/// A whole session: one <see cref="Recording"/> per world the game ran on. A game that starts a new campaign (<see cref="GameInstance.Reset"/>),
/// loads a fixture or loads a save replaces its world; each replacement ends one segment and starts the next from the new world's
/// full state (seed included), so replay reproduces resets exactly. A state patch from the harness ends a segment the same way.
/// A session without replacements or patches has one segment.
/// </summary>
public sealed class SessionRecording
{
    /// <summary>Creates a session from its segments, in the order they ran.</summary>
    public SessionRecording(IReadOnlyList<Recording> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);
        Segments = segments;
    }

    /// <summary>The recordings, in the order the worlds ran. Each starts from its own snapshot.</summary>
    public IReadOnlyList<Recording> Segments { get; }

    /// <summary>Simulation ticks over all segments.</summary>
    public long Ticks => Segments.Sum(s => s.EndTick - s.StartTick);

    /// <summary>Logged commands over all segments.</summary>
    public int Commands => Segments.Sum(s => s.Commands.Count);
}

/// <summary>Records a session, following the game across world replacements and state patches. Create one with <see cref="GameInstance.StartSessionRecording"/>.</summary>
public sealed class SessionRecorder
{
    private readonly GameInstance _game;
    private readonly RecordingOptions? _options;
    private readonly List<Recording> _done = [];
    private Recorder? _current;
    private SessionRecording? _result;

    internal SessionRecorder(GameInstance game, RecordingOptions? options)
    {
        _game = game;
        _options = options;
        _current = game.StartRecording(options);
        game.WorldReplaced += OnReplaced;
        game.WorldEdited += OnReplaced;
    }

    private void OnReplaced(World world)
    {
        EndCurrent();
        _current = _game.StartRecording(_options);
    }

    private void EndCurrent()
    {
        if (_current is null)
        {
            return;
        }

        var recording = _current.Stop();
        _current = null;

        // A world replaced before it ran a tick or took a command holds nothing worth replaying; the next segment starts from the
        // state that follows it.
        if (recording.EndTick > recording.StartTick || recording.Commands.Count > 0)
        {
            _done.Add(recording);
        }
    }

    /// <summary>Ends the recording and returns the session. A session in which nothing ran still has one (empty) segment.</summary>
    public SessionRecording Stop()
    {
        if (_result is null)
        {
            _game.WorldReplaced -= OnReplaced;
            _game.WorldEdited -= OnReplaced;
            var last = _current;
            EndCurrent();
            if (_done.Count == 0 && last is not null)
            {
                _done.Add(last.Recording);
            }

            _result = new SessionRecording(_done);
        }

        return _result;
    }
}

/// <summary>The outcome of replaying a <see cref="SessionRecording"/>.</summary>
/// <param name="Ok">True when every checkpoint of every segment matched.</param>
/// <param name="TicksRun">Ticks simulated over all segments.</param>
/// <param name="CheckpointsVerified">Checkpoints that matched.</param>
/// <param name="FinalHash">The hash of the last world the replay reached.</param>
/// <param name="Segment">Index of the segment that diverged, or -1.</param>
/// <param name="Divergence">The first mismatch, or null.</param>
public sealed record SessionReplayResult(bool Ok, long TicksRun, int CheckpointsVerified, ulong FinalHash, int Segment, Divergence? Divergence);
