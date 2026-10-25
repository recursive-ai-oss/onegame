using OneGame.Audio;

namespace Solitaire;

/// <summary>
/// Plays the game's sounds through the audio service. The clips are short tones and noise bursts made here, so the sample needs no audio files;
/// a game with recorded sounds would load them with <c>LoadSound</c> instead. Where the machine has no audio device, the service runs silently and nothing else changes.
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
            using var wav = new MemoryStream(Synthesize(e));
            _clips[e] = audio.LoadSound(wav, e.ToString());
        }
    }

    /// <summary>Opens the audio service. Returns null when it cannot be started, and the game then makes no sound.</summary>
    public static AudioSounds? TryCreate(AudioServiceOptions? options = null)
    {
        try
        {
            return new AudioSounds(AudioService.Create(options));
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or NotSupportedException or InvalidDataException or AudioLoadException or DllNotFoundException)
        {
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
    public void SetVolumes(float music, float effects)
    {
        _audio.SetBusVolume(AudioBus.Music, music);
        _audio.SetBusVolume(AudioBus.Sfx, effects);
        _audio.SetBusVolume(AudioBus.Ui, effects);
    }

    /// <inheritdoc />
    public void Dispose() => _audio.Dispose();

    /// <summary>The 16-bit mono WAV file of a sound.</summary>
    internal static byte[] Synthesize(SoundEvent sound)
    {
        var samples = sound switch
        {
            SoundEvent.Card => Burst(0.07, 900, 0.5),
            SoundEvent.Draw => Burst(0.10, 1400, 0.4),
            SoundEvent.Refuse => Tone([(180, 0.12)], 0.35),
            SoundEvent.Win => Tone([(523, 0.12), (659, 0.12), (784, 0.12), (1047, 0.35)], 0.4),
            _ => Tone([(880, 0.04)], 0.25),
        };
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

    // A short noise burst through a one-pole low-pass, fading out: the sound of a card on felt.
    private static short[] Burst(double seconds, double cutoff, double gain)
    {
        var n = (int)(seconds * Rate);
        var data = new short[n];
        uint state = 12345;
        var low = 0.0;
        var k = 2 * Math.PI * cutoff / Rate;
        for (var i = 0; i < n; i++)
        {
            state = (state * 1664525) + 1013904223;
            var noise = ((state >> 8) / (double)(1 << 24) * 2) - 1;
            low += k * (noise - low);
            var fade = 1.0 - ((double)i / n);
            data[i] = (short)(low * fade * fade * gain * 32000);
        }

        return data;
    }

    private static short[] Tone((double Hz, double Seconds)[] notes, double gain)
    {
        var data = new List<short>();
        foreach (var (hz, seconds) in notes)
        {
            var n = (int)(seconds * Rate);
            for (var i = 0; i < n; i++)
            {
                var fade = Math.Min(1.0, (n - i) / (0.02 * Rate));
                data.Add((short)(Math.Sin(2 * Math.PI * hz * i / Rate) * fade * gain * 32000));
            }
        }

        return [.. data];
    }
}
