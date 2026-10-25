using OneGame.Audio;

namespace Bricks;

/// <summary>
/// Plays the game's sounds through the audio service. The clips are made here from sine sweeps and noise bursts when the game starts, so
/// the sample carries no audio files; a game with recorded sounds would load them with <c>LoadSound</c> the same way. Each clip is loaded
/// under its <see cref="SoundEvent"/> name, which is what a scenario's <c>sound:</c> expectation names. Where the machine has no audio device
/// the service runs silently and nothing else changes.
/// </summary>
public sealed class AudioSounds : ISoundSink, IDisposable
{
    private const int Rate = 44100;

    private readonly AudioService _audio;
    private readonly Dictionary<SoundEvent, Sound> _clips = [];

    private AudioSounds(AudioService audio)
    {
        _audio = audio;
        foreach (var e in Enum.GetValues<SoundEvent>())
        {
            using var wav = new MemoryStream(Wav(Synthesize(e)));
            _clips[e] = audio.LoadSound(wav, e.ToString());
        }
    }

    /// <summary>Opens the audio service. Returns null when it cannot be started, and the game then makes no sound.</summary>
    /// <param name="options">Service options; null opens the machine's default device. A test passes a capture service, which has no device.</param>
    public static AudioSounds? TryCreate(AudioServiceOptions? options = null)
    {
        try
        {
            return new AudioSounds(AudioService.Create(options));
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or NotSupportedException or InvalidDataException or AudioLoadException or DllNotFoundException)
        {
            Console.Error.WriteLine("bricks: no sound: " + ex.GetType().Name + ": " + ex.Message);
            return null;
        }
    }

    /// <summary>The service the sounds play through.</summary>
    internal AudioService Service => _audio;

    /// <inheritdoc />
    public void Play(SoundEvent sound, float volume)
    {
        if (_clips.TryGetValue(sound, out var clip))
        {
            _audio.Play(clip, new PlayOptions { Bus = sound == SoundEvent.Click ? AudioBus.Ui : AudioBus.Sfx, Volume = volume });
        }
    }

    /// <inheritdoc />
    public void Dispose() => _audio.Dispose();

    /// <summary>The samples of a sound, 16-bit mono at 44.1 kHz.</summary>
    internal static short[] Synthesize(SoundEvent sound) => sound switch
    {
        SoundEvent.Brick => Sweep(0.09, 880, 1320, 0.45),
        SoundEvent.Tough => Sweep(0.06, 520, 440, 0.4),
        SoundEvent.Clink => Sweep(0.05, 2400, 1900, 0.3),
        SoundEvent.Wall => Sweep(0.05, 300, 260, 0.35),
        SoundEvent.Paddle => Sweep(0.07, 200, 340, 0.5),
        SoundEvent.Catch => Join(Sweep(0.06, 660, 880, 0.4), Sweep(0.08, 880, 1320, 0.4)),
        SoundEvent.Lost => Sweep(0.45, 440, 90, 0.45),
        SoundEvent.Launch => Sweep(0.08, 300, 600, 0.35),
        SoundEvent.Clear => Join(Sweep(0.1, 523, 523, 0.4), Sweep(0.1, 659, 659, 0.4), Sweep(0.1, 784, 784, 0.4), Sweep(0.3, 1047, 1047, 0.4)),
        SoundEvent.Over => Join(Sweep(0.18, 392, 392, 0.4), Sweep(0.18, 330, 330, 0.4), Sweep(0.4, 262, 247, 0.4)),
        _ => Sweep(0.04, 880, 880, 0.25),
    };

    // A sine that glides from one pitch to another while it fades out (a short fade in avoids a click at the start).
    private static short[] Sweep(double seconds, double fromHz, double toHz, double gain)
    {
        var n = (int)(seconds * Rate);
        var data = new short[n];
        var phase = 0.0;
        for (var i = 0; i < n; i++)
        {
            var t = (double)i / n;
            phase += 2 * Math.PI * (fromHz + ((toHz - fromHz) * t)) / Rate;
            var fadeIn = Math.Min(1.0, i / (0.004 * Rate));
            var fadeOut = 1.0 - t;
            data[i] = (short)(Math.Sin(phase) * fadeIn * fadeOut * gain * 32000);
        }

        return data;
    }

    private static short[] Join(params short[][] parts) => [.. parts.SelectMany(p => p)];

    private static byte[] Wav(short[] samples)
    {
        using var stream = new MemoryStream();
        using var w = new BinaryWriter(stream);
        w.Write("RIFF"u8);
        w.Write(36 + (samples.Length * 2));
        w.Write("WAVEfmt "u8);
        w.Write(16);
        w.Write((short)1);
        w.Write((short)1);
        w.Write(Rate);
        w.Write(Rate * 2);
        w.Write((short)2);
        w.Write((short)16);
        w.Write("data"u8);
        w.Write(samples.Length * 2);
        foreach (var s in samples)
        {
            w.Write(s);
        }

        return stream.ToArray();
    }
}
