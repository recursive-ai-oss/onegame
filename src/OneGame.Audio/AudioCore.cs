namespace OneGame.Audio;

/// <summary>Identifies one playing sound inside a core. Ids are never reused, so a stale id is simply ignored.</summary>
/// <param name="Id">The id; 0 or less is invalid.</param>
public readonly record struct VoiceId(long Id)
{
    /// <summary>Whether the id refers to a voice that was accepted for playing.</summary>
    public bool IsValid => Id > 0;
}

/// <summary>What a core reports back to the game thread.</summary>
public enum AudioEventKind
{
    /// <summary>A voice reached the end of its source (or finished a fade-out).</summary>
    VoiceEnded,

    /// <summary>A voice was cut to make room for a higher-priority one.</summary>
    VoiceStolen,

    /// <summary>A play request was dropped because every voice was busy with something more important.</summary>
    PlayDropped,

    /// <summary>A streaming voice ran out of decoded data and played silence.</summary>
    Underrun,
}

/// <summary>One thing that happened on the audio thread.</summary>
/// <param name="Kind">What happened.</param>
/// <param name="Voice">The voice it happened to.</param>
/// <param name="Value">A kind-specific number, such as the frames lost to an underrun.</param>
public readonly record struct AudioEvent(AudioEventKind Kind, VoiceId Voice, long Value);

/// <summary>A request to start one voice.</summary>
public readonly record struct VoiceRequest
{
    /// <summary>Creates a request with unit gain, centred, on bus 0.</summary>
    public VoiceRequest()
    {
    }

    /// <summary>The source object obtained from the core's <see cref="IAudioCore.LoadClip"/> or <see cref="IAudioCore.OpenStream"/>.</summary>
    public object? Source { get; init; }

    /// <summary>Linear gain.</summary>
    public float Gain { get; init; } = 1f;

    /// <summary>Stereo position, -1 (left) to 1 (right).</summary>
    public float Pan { get; init; }

    /// <summary>Whether to restart at the end.</summary>
    public bool Loop { get; init; }

    /// <summary>The bus index the voice is summed into.</summary>
    public int Bus { get; init; }

    /// <summary>Higher priorities steal voices from lower ones when all voices are busy.</summary>
    public int Priority { get; init; }

    /// <summary>The frame on the core's timeline to start at (0 = as soon as possible). Frame-exact.</summary>
    public long StartAtFrame { get; init; }

    /// <summary>Frames over which the voice fades in from silence.</summary>
    public int FadeInFrames { get; init; }

    /// <summary>Playback speed and pitch multiplier; 1 is the source's own.</summary>
    public float Pitch { get; init; } = 1f;

    /// <summary>Reserve what the core needs to change the pitch later with <see cref="IAudioCore.SetPitch"/>.</summary>
    public bool VariablePitch { get; init; }
}

/// <summary>
/// The mixer behind the audio service: renders interleaved float frames and is controlled from the game thread.
/// The control calls are thread-safe and never block. A full command queue makes a call return false (or an invalid id).
/// </summary>
public interface IAudioCore : IAudioRenderSource, IDisposable
{
    /// <summary>Decodes a whole file into memory. Throws for malformed or unsupported data.</summary>
    /// <param name="data">The encoded file.</param>
    /// <returns>A source that can be played any number of times at once.</returns>
    object LoadClip(Stream data);

    /// <summary>Opens a file for streaming playback. The source plays once at a time; it takes ownership of the stream.</summary>
    /// <param name="data">The encoded file.</param>
    /// <param name="looping">Whether the decoder wraps at the end.</param>
    /// <returns>A streaming source.</returns>
    object OpenStream(Stream data, bool looping);

    /// <summary>The length of a source in seconds, or a negative number when unknown.</summary>
    /// <param name="source">A source from this core.</param>
    /// <returns>Seconds, or a negative number.</returns>
    double DurationSeconds(object source);

    /// <summary>Starts a voice.</summary>
    /// <param name="request">The request.</param>
    /// <returns>The voice, or an invalid id when the command queue is full.</returns>
    VoiceId Play(in VoiceRequest request);

    /// <summary>Fades out and removes a voice.</summary>
    /// <param name="voice">The voice.</param>
    /// <param name="fadeOutFrames">Fade length; 0 stops at once.</param>
    /// <returns>Whether the command was queued.</returns>
    bool StopVoice(VoiceId voice, int fadeOutFrames);

    /// <summary>Stops every voice.</summary>
    /// <param name="fadeOutFrames">Fade length; 0 stops at once.</param>
    /// <returns>Whether the command was queued.</returns>
    bool StopAll(int fadeOutFrames);

    /// <summary>Changes a voice's gain.</summary>
    /// <param name="voice">The voice.</param>
    /// <param name="gain">Linear target gain.</param>
    /// <param name="rampFrames">Frames to reach it; 0 jumps.</param>
    /// <returns>Whether the command was queued.</returns>
    bool SetGain(VoiceId voice, float gain, int rampFrames);

    /// <summary>Changes a voice's stereo position.</summary>
    /// <param name="voice">The voice.</param>
    /// <param name="pan">-1 to 1.</param>
    /// <returns>Whether the command was queued.</returns>
    bool SetPan(VoiceId voice, float pan);

    /// <summary>Changes a voice's looping.</summary>
    /// <param name="voice">The voice.</param>
    /// <param name="looping">Whether to loop.</param>
    /// <returns>Whether the command was queued.</returns>
    bool SetLoop(VoiceId voice, bool looping);

    /// <summary>Sets a bus's gain.</summary>
    /// <param name="bus">The bus index.</param>
    /// <param name="gain">Linear gain.</param>
    /// <returns>Whether the command was queued.</returns>
    bool SetBusGain(int bus, float gain);

    /// <summary>Fades one voice out while another fades in over the same frames.</summary>
    /// <param name="from">The voice to fade out; may be invalid.</param>
    /// <param name="incoming">The voice to start.</param>
    /// <param name="frames">The fade length.</param>
    /// <returns>The new voice, or an invalid id when the queue is full.</returns>
    VoiceId Crossfade(VoiceId from, in VoiceRequest incoming, int frames);

    /// <summary>Changes a voice's playback speed and pitch. Only voices started with a pitch other than 1 or with <see cref="VoiceRequest.VariablePitch"/> respond.</summary>
    /// <param name="voice">The voice.</param>
    /// <param name="pitch">Speed multiplier; 1 is the source's own.</param>
    /// <param name="rampFrames">Frames to glide; 0 jumps.</param>
    /// <returns>Whether the command was queued. Cores without pitch support return false.</returns>
    bool SetPitch(VoiceId voice, float pitch, int rampFrames) => false;

    /// <summary>Mutes or unmutes a bus without touching its gain.</summary>
    /// <param name="bus">The bus index.</param>
    /// <param name="muted">Whether to silence the bus.</param>
    /// <returns>Whether the command was queued. Cores without bus mute return false.</returns>
    bool SetBusMute(int bus, bool muted) => false;

    /// <summary>Turns <paramref name="targetBus"/> down while <paramref name="sourceBus"/> is loud.</summary>
    /// <param name="targetBus">The bus that is turned down.</param>
    /// <param name="sourceBus">The bus whose level is watched.</param>
    /// <param name="settings">Threshold, depth and timing.</param>
    /// <returns>Whether the command was queued. Cores without ducking return false.</returns>
    bool SetDucking(int targetBus, int sourceBus, DuckingOptions settings) => false;

    /// <summary>Removes the ducking of a bus.</summary>
    /// <param name="targetBus">The bus.</param>
    /// <returns>Whether the command was queued. Cores without ducking return false.</returns>
    bool ClearDucking(int targetBus) => false;

    /// <summary>Takes the next event the audio thread produced.</summary>
    /// <param name="audioEvent">The event.</param>
    /// <returns>Whether there was one.</returns>
    bool TryGetEvent(out AudioEvent audioEvent);

    /// <summary>Feeds streaming sources from their decoders. Called off the audio thread, repeatedly, by the service.</summary>
    void Pump();

    /// <summary>How many voices are playing now.</summary>
    int ActiveVoices { get; }

    /// <summary>Commands dropped because the queue was full.</summary>
    long DroppedCommands { get; }
}

/// <summary>Creates the mixer once the device has fixed the sample rate and channel count.</summary>
public interface IAudioCoreFactory
{
    /// <summary>Builds a core that renders at the given format.</summary>
    /// <param name="sampleRate">Frames per second.</param>
    /// <param name="channels">Samples per frame.</param>
    /// <param name="maxVoices">The most simultaneous voices.</param>
    /// <param name="buses">The number of buses.</param>
    /// <returns>The core.</returns>
    IAudioCore Create(int sampleRate, int channels, int maxVoices, int buses);
}

/// <summary>How one bus is turned down by the level of another, for example music under spoken lines.</summary>
public readonly record struct DuckingOptions
{
    /// <summary>Creates the defaults: duck 12 dB when the watched bus passes -30 dBFS, 10 ms attack, 250 ms release.</summary>
    public DuckingOptions()
    {
    }

    /// <summary>Level of the watched bus, in dBFS, above which ducking starts.</summary>
    public float ThresholdDb { get; init; } = -30f;

    /// <summary>How far the target is turned down, in dB (positive).</summary>
    public float ReductionDb { get; init; } = 12f;

    /// <summary>Milliseconds to reach full reduction.</summary>
    public float AttackMs { get; init; } = 10f;

    /// <summary>Milliseconds to recover once the watched bus is quiet.</summary>
    public float ReleaseMs { get; init; } = 250f;
}
