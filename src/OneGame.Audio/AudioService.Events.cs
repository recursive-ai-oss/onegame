namespace OneGame.Audio;

/// <summary>One sound the game asked the service to start, as the event log keeps it.</summary>
/// <param name="Sequence">Counts up from 1 for the life of the service and never repeats, even after old entries were dropped.</param>
/// <param name="Frame">The game frame the sound was requested in (from <see cref="AudioService.FrameClock"/>), or -1 when no game clock is attached.</param>
/// <param name="Sound">The name the clip was loaded under (<see cref="AudioService.LoadSound"/>), or the name given to <see cref="AudioService.PlayMusic"/>.</param>
/// <param name="Bus">The bus index it plays on.</param>
/// <param name="Volume">The requested linear volume.</param>
/// <param name="Loop">Whether it loops.</param>
/// <param name="Music">True for a music track.</param>
/// <param name="Started">False when the service dropped the request (command queue full).</param>
public sealed record PlayedSound(long Sequence, long Frame, string Sound, int Bus, float Volume, bool Loop, bool Music, bool Started);

public sealed partial class AudioService
{
    // A scenario asks about sounds played in the last few frames; this is far more than any scenario reads, and it keeps a long-running game bounded.
    private const int EventLogCapacity = 4096;

    private readonly Queue<PlayedSound> _events = new();
    private readonly object _eventLock = new();
    private long _eventSequence;

    /// <summary>Tells the log which game frame it is, so a test can ask for sounds played within a window of frames. The control layer binds it to the game's frame counter when the game's control surface is created or given the service (<c>ControlSurface.Audio</c>); a game never needs to. Sounds played while no clock is bound are stamped -1.</summary>
    public Func<long>? FrameClock { get; set; }

    /// <summary>The sounds played so far, oldest first, up to the last 4096. Works with every backend, with or without a device, so tests can check what a game played without listening.</summary>
    /// <returns>A copy of the log.</returns>
    public IReadOnlyList<PlayedSound> GetPlayLog()
    {
        lock (_eventLock)
        {
            return [.. _events];
        }
    }

    /// <summary>Empties the log. Sequence numbers keep counting.</summary>
    public void ClearPlayLog()
    {
        lock (_eventLock)
        {
            _events.Clear();
        }
    }

    private void RecordPlay(string sound, VoiceId voice, float volume, bool loop, int bus, bool music)
    {
        var frame = FrameClock?.Invoke() ?? -1;
        lock (_eventLock)
        {
            if (_events.Count >= EventLogCapacity)
            {
                _events.Dequeue();
            }

            _events.Enqueue(new PlayedSound(++_eventSequence, frame, sound, bus, volume, loop, music, voice.IsValid));
        }
    }
}
