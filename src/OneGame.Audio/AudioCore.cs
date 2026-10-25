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

    /// <summary>Turns on or retunes a low-pass or high-pass filter on a bus (12 dB per octave).</summary>
    /// <param name="bus">The bus index.</param>
    /// <param name="kind">Which filter.</param>
    /// <param name="cutoffHz">The corner frequency.</param>
    /// <param name="q">Resonance; 0 or less picks a flat (Butterworth) response.</param>
    /// <param name="glideFrames">Frames over which the change glides.</param>
    /// <returns>Whether the command was queued. Cores without filters return false.</returns>
    bool SetBusFilter(int bus, AudioFilterKind kind, float cutoffHz, float q, int glideFrames) => false;

    /// <summary>Turns a bus filter off.</summary>
    /// <param name="bus">The bus index.</param>
    /// <param name="kind">Which filter.</param>
    /// <param name="glideFrames">Frames to glide to transparent first.</param>
    /// <returns>Whether the command was queued. Cores without filters return false.</returns>
    bool ClearBusFilter(int bus, AudioFilterKind kind, int glideFrames) => false;

    /// <summary>Like <see cref="SetBusFilter"/> for one voice.</summary>
    /// <param name="voice">The voice.</param>
    /// <param name="kind">Which filter.</param>
    /// <param name="cutoffHz">The corner frequency.</param>
    /// <param name="q">Resonance; 0 or less picks a flat response.</param>
    /// <param name="glideFrames">Frames over which the change glides.</param>
    /// <returns>Whether the command was queued. Cores without filters return false.</returns>
    bool SetVoiceFilter(VoiceId voice, AudioFilterKind kind, float cutoffHz, float q, int glideFrames) => false;

    /// <summary>Turns a voice filter off.</summary>
    /// <param name="voice">The voice.</param>
    /// <param name="kind">Which filter.</param>
    /// <param name="glideFrames">Frames to glide to transparent first.</param>
    /// <returns>Whether the command was queued. Cores without filters return false.</returns>
    bool ClearVoiceFilter(VoiceId voice, AudioFilterKind kind, int glideFrames) => false;

    /// <summary>Puts a reverb insert on a bus, or retunes the one there.</summary>
    /// <param name="bus">The bus index.</param>
    /// <param name="options">The reverb.</param>
    /// <param name="glideFrames">Frames over which changes glide.</param>
    /// <returns>Whether the command was queued. Cores without reverb return false, and so does a core with no free reverb slot for a bus that has none.</returns>
    bool SetBusReverb(int bus, ReverbOptions options, int glideFrames) => false;

    /// <summary>Removes a bus's reverb; its tail stops at once.</summary>
    /// <param name="bus">The bus index.</param>
    /// <returns>Whether the command was queued. Cores without reverb return false.</returns>
    bool ClearBusReverb(int bus) => false;

    /// <summary>Reverb slots not yet used by a bus. Cores without reverb report 0.</summary>
    int FreeReverbSlots => 0;

    /// <summary>Loudness meter slots not yet used. Cores without bus meters report 0.</summary>
    int FreeBusMeters => 0;

    /// <summary>Starts measuring a bus's loudness, after its effects and gain and before ducking.</summary>
    /// <param name="bus">The bus index.</param>
    /// <returns>True when the bus is metered or the start was queued. False when no meter slot is free, the command queue was full, or the core has no bus meters; nothing changed then.</returns>
    bool EnableBusMeter(int bus) => false;

    /// <summary>Stops measuring a bus and frees its slot.</summary>
    /// <param name="bus">The bus index.</param>
    /// <returns>Whether the command was queued.</returns>
    bool DisableBusMeter(int bus) => false;

    /// <summary>Starts a metered bus's measurement over.</summary>
    /// <param name="bus">The bus index.</param>
    /// <returns>Whether the command was queued.</returns>
    bool ResetBusMeter(int bus) => false;

    /// <summary>Reads a metered bus.</summary>
    /// <param name="bus">The bus index.</param>
    /// <param name="reading">The reading; silence and no data are -infinity.</param>
    /// <returns>False when the bus is not metered.</returns>
    bool TryGetBusLoudness(int bus, out LoudnessReading reading)
    {
        reading = default;
        return false;
    }

    /// <summary>Switches the master limiter on, retunes it, or (with <see langword="null"/>) off.</summary>
    /// <param name="options">Ceiling and release, or null for off.</param>
    /// <returns>Whether the command was queued. False when the core was built without a limiter.</returns>
    bool SetLimiter(LimiterOptions? options) => false;

    /// <summary>Whether the core was built with a master limiter (a lookahead above zero).</summary>
    bool HasLimiter => false;

    /// <summary>Gain reduction of the master limiter in the last render pass, in dB (0 or negative).</summary>
    double LimiterGainReductionDb => 0;

    /// <summary>Reads where a streaming voice is. Positions are in source time, ahead of the speakers by the mixer's and device's buffering.</summary>
    /// <param name="voice">A voice started from a stream.</param>
    /// <param name="positionSeconds">Seconds into the stream; wraps with a loop.</param>
    /// <param name="lengthSeconds">The stream's length, or a negative number when unknown.</param>
    /// <returns>Whether the voice is a live stream.</returns>
    bool TryGetStreamPosition(VoiceId voice, out double positionSeconds, out double lengthSeconds)
    {
        positionSeconds = 0;
        lengthSeconds = -1;
        return false;
    }

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

    /// <summary>Builds a core with the full settings. The default ignores the effect settings and calls the four-number overload.</summary>
    /// <param name="settings">The format, sizes and effect capacity.</param>
    /// <returns>The core.</returns>
    IAudioCore Create(AudioCoreSettings settings) => Create(settings.SampleRate, settings.Channels, settings.MaxVoices, settings.Buses);
}

/// <summary>What a core is built with.</summary>
/// <param name="SampleRate">Frames per second.</param>
/// <param name="Channels">Samples per frame.</param>
/// <param name="MaxVoices">The most simultaneous voices.</param>
/// <param name="Buses">The number of buses.</param>
public sealed record AudioCoreSettings(int SampleRate, int Channels, int MaxVoices, int Buses)
{
    /// <summary>Reverbs that can be active at once, 0 to 16. Each costs about 0.3 MB up front. Default 2.</summary>
    public int MaxReverbs { get; init; } = 2;

    /// <summary>Per-bus loudness meters that can run at once, 0 to 64. Each costs about 0.1 MB up front. Default 0.</summary>
    public int MaxBusMeters { get; init; }

    /// <summary>Lookahead of the master limiter in milliseconds, 0 to 100. 0 builds no limiter. The whole output is delayed by this much. Default 0.</summary>
    public int LimiterLookaheadMilliseconds { get; init; }
}

/// <summary>Which side of the spectrum a filter lets through.</summary>
public enum AudioFilterKind
{
    /// <summary>Passes frequencies below the cutoff.</summary>
    LowPass,

    /// <summary>Passes frequencies above the cutoff.</summary>
    HighPass,
}

/// <summary>A reverb insert on a bus: the bus signal becomes <c>Dry * signal + Wet * reverb</c>.</summary>
public readonly record struct ReverbOptions
{
    /// <summary>Creates a medium room: 1.5 s decay, wet 0.25.</summary>
    public ReverbOptions()
    {
    }

    /// <summary>Apparent room size, 0 to 1. Default 0.5.</summary>
    public float RoomSize { get; init; } = 0.5f;

    /// <summary>Time for the tail to fall 60 dB at low frequencies, in seconds (0.1 to 20). Default 1.5.</summary>
    public float DecaySeconds { get; init; } = 1.5f;

    /// <summary>High-frequency absorption, 0 to 1. Default 0.4.</summary>
    public float Damping { get; init; } = 0.4f;

    /// <summary>Delay before the tail starts, in milliseconds. Default 0.</summary>
    public float PreDelayMilliseconds { get; init; }

    /// <summary>Linear gain of the reverberated signal, 0 to 4. Default 0.25.</summary>
    public float Wet { get; init; } = 0.25f;

    /// <summary>Linear gain of the unprocessed signal, 0 to 4. Default 1.</summary>
    public float Dry { get; init; } = 1f;
}

/// <summary>The master limiter's settings.</summary>
public readonly record struct LimiterOptions
{
    /// <summary>Creates a -1 dBFS ceiling with a 100 ms release.</summary>
    public LimiterOptions()
    {
    }

    /// <summary>The highest sample magnitude allowed, in dBFS (-60 to 0). Default -1.</summary>
    public float CeilingDb { get; init; } = -1f;

    /// <summary>Time constant of the gain recovering after a peak, in milliseconds (1 to 10000). Default 100.</summary>
    public float ReleaseMilliseconds { get; init; } = 100f;
}

/// <summary>
/// How one bus is turned down by the level of another, for example music under spoken lines. The bus gain moves exponentially toward its goal, so the
/// attack and release times are time constants: after one release time 63% of the way back is done, after four times 98%, and it never quite arrives.
/// The watched level is taken after the watched bus's own gain and mute, so muting or fading it also stops the ducking.
/// </summary>
public readonly record struct DuckingOptions
{
    /// <summary>Creates the defaults: duck 12 dB when the watched bus passes -30 dBFS, 10 ms attack, 250 ms release.</summary>
    public DuckingOptions()
    {
    }

    /// <summary>Settings that mean no ducking; <see cref="AudioService.SetDucking"/> with them removes the link.</summary>
    public static DuckingOptions None => new() { Enabled = false };

    /// <summary>False for <see cref="None"/>; the other values are then ignored. Default true.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Level of the watched bus, in dBFS, above which ducking starts.</summary>
    public float ThresholdDb { get; init; } = -30f;

    /// <summary>How far the target is turned down, in dB (positive).</summary>
    public float ReductionDb { get; init; } = 12f;

    /// <summary>Time constant, in milliseconds, of the gain falling to the ducked level.</summary>
    public float AttackMs { get; init; } = 10f;

    /// <summary>Time constant, in milliseconds, of the gain recovering once the watched bus is quiet.</summary>
    public float ReleaseMs { get; init; } = 250f;
}
