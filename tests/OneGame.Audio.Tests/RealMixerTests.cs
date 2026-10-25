using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace OneGame.Audio.Tests;

/// <summary>The service on its default core, oneaudio's mixer and decoders, recorded through the capture sink at 48 kHz stereo.</summary>
public class RealMixerTests
{
    private const int Rate = 48_000;

    // A mono source is centred with an equal-power pan law: -3 dB in each channel.
    private const float Centre = 0.70710678f;

    private static string Asset(string name, [CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "testdata", "audio", name));

    // A mono 16-bit PCM WAV holding one constant sample value.
    internal static byte[] Wav(short value, int frames, int rate = Rate)
    {
        var b = new byte[44 + (frames * 2)];
        "RIFF"u8.CopyTo(b);
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(4), 36 + (frames * 2));
        "WAVEfmt "u8.CopyTo(b.AsSpan(8));
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(16), 16);
        BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan(20), 1);
        BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan(22), 1);
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(24), rate);
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(28), rate * 2);
        BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan(32), 2);
        BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan(34), 16);
        "data"u8.CopyTo(b.AsSpan(36));
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(40), frames * 2);
        for (var i = 0; i < frames; i++)
        {
            BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan(44 + (i * 2)), value);
        }

        return b;
    }

    private static AudioService Service(OutputLimiterMode limiter = OutputLimiterMode.Clamp) => AudioService.Create(new AudioServiceOptions
    {
        Capture = true,
        Limiter = limiter,
        Device = new AudioDeviceOptions { SampleRate = Rate, Channels = 2 },
    });

    private static float[] Left(AudioService s, int from, int count)
    {
        var all = s.Capture!.Samples;
        var r = new float[count];
        for (var i = 0; i < count; i++)
        {
            r[i] = all[(from + i) * 2];
        }

        return r;
    }

    // Frequency from upward zero crossings over the window.
    private static double Hz(float[] x)
    {
        var crossings = 0;
        for (var i = 1; i < x.Length; i++)
        {
            if (x[i - 1] < 0 && x[i] >= 0)
            {
                crossings++;
            }
        }

        return crossings * (double)Rate / x.Length;
    }

    private static float Peak(float[] x) => x.Max(Math.Abs);

    [Fact]
    public void VorbisAt44100PlaysIntoA48000Output()
    {
        float[] Run()
        {
            using var s = Service();
            var sound = s.LoadSound(File.OpenRead(Asset("tone44k.ogg")), "tone");
            Assert.Equal(0.5, sound.DurationSeconds, 2);
            s.Play(sound);
            s.Advance(Rate);
            s.Update();
            return Left(s, 0, Rate);
        }

        var x = Run();
        Assert.All(x, v => Assert.True(float.IsFinite(v)));
        var body = x[2400..21600]; // away from the converter's start and flush
        Assert.InRange(Peak(body), 0.33, 0.37);
        Assert.InRange(Hz(body), 436, 444);
        Assert.InRange(Peak(x[26000..]), 0, 0.001); // the clip is 0.5 s long; the rest is silence
        Assert.Equal(x, Run());
    }

    [Fact]
    public void StreamedVorbisPlaysAtTheOutputRateAndEnds()
    {
        using var s = Service();
        var ended = false;
        s.EventRaised += e => ended |= e.Kind == AudioEventKind.VoiceEnded;
        s.PlayMusic(File.OpenRead(Asset("tone44k.ogg")), new MusicOptions { Loop = false });
        s.Advance(Rate);
        s.Update();
        Assert.InRange(Hz(Left(s, 2400, 19200)), 436, 444);
        Assert.True(ended);
        Assert.False(s.CurrentMusic.IsValid);
    }

    [Fact]
    public void LoopingOpusClipWithPitchChange()
    {
        float[] Run()
        {
            using var s = Service();
            var sound = s.LoadSound(File.OpenRead(Asset("loop48k.opus")), "loop");
            var v = s.Play(sound, new PlayOptions { Loop = true, VariablePitch = true });
            s.Advance(Rate / 2); // 0.5 s: two loops at the clip's own pitch
            s.SetPitch(v, 2f);
            s.Advance(Rate / 2);
            return Left(s, 0, Rate);
        }

        var x = Run();
        Assert.All(x, v => Assert.True(float.IsFinite(v)));
        Assert.InRange(Hz(x[4800..21600]), 870, 890);       // looped past the 0.25 s clip, still 880 Hz
        Assert.InRange(Peak(x[4800..21600]), 0.33, 0.37);
        Assert.InRange(Hz(x[30000..46000]), 1740, 1780);    // an octave up after SetPitch
        Assert.Equal(x, Run());
    }

    [Fact]
    public void PitchAtPlayRaisesTheClip()
    {
        using var s = Service();
        s.Play(s.LoadSound(File.OpenRead(Asset("loop48k.opus"))), new PlayOptions { Loop = true, Pitch = 0.5f });
        s.Advance(Rate / 2);
        Assert.InRange(Hz(Left(s, 4800, 19200)), 430, 450);
    }

    [Fact]
    public void MusicCrossfadeMovesFromOneTrackToTheOther()
    {
        float[] Run()
        {
            using var s = Service();
            s.PlayMusic(new MemoryStream(Wav(8192, 4800)));
            s.Advance(4800);
            s.PlayMusic(new MemoryStream(Wav(-8192, 4800)), new MusicOptions { CrossfadeSeconds = 0.1 });
            s.Advance(9600);
            s.Update();
            Assert.Equal(0, ((OneAudioCore)s.Core).Mixer.UnderrunEvents);
            return Left(s, 0, 14400);
        }

        var x = Run();
        Assert.Equal(Centre * 0.25f, x[4000], 3);                  // first track alone
        Assert.True(Math.Abs(x[4800 + 2400]) < 0.2f, "midway the two tracks nearly cancel");
        Assert.Equal(-Centre * 0.25f, x[14000], 3);                // second track alone
        for (var i = 4800; i < 4800 + 4800; i++)
        {
            Assert.True(x[i] <= x[i - 1] + 1e-5f, "the level moves one way through the fade");
        }

        Assert.Equal(x, Run());
    }

    [Fact]
    public void BusVolumeAndMute()
    {
        using var s = Service();
        s.Play(s.LoadSound(new MemoryStream(Wav(16384, Rate))), new PlayOptions { Loop = true });
        s.Advance(2000);
        var full = Left(s, 1900, 1)[0];
        Assert.Equal(Centre * 0.5f, full, 3);

        s.SetBusVolume(AudioBus.Sfx, 0.5f);
        s.Advance(2000); // the mixer smooths gain changes over a few milliseconds
        Assert.Equal(Centre * 0.25f, Left(s, 3900, 1)[0], 3);

        s.SetBusMuted(AudioBus.Sfx, true);
        s.Advance(2000);
        Assert.Equal(0f, Left(s, 5900, 1)[0], 4);
        Assert.Equal(0.5f, s.GetBusVolume(AudioBus.Sfx)); // mute does not touch the volume

        s.SetBusMuted(AudioBus.Sfx, false);
        s.Advance(2000);
        Assert.Equal(Centre * 0.25f, Left(s, 7900, 1)[0], 3);

        // Another bus is unaffected by the sfx settings.
        s.Play(s.LoadSound(new MemoryStream(Wav(8192, Rate))), new PlayOptions { Bus = AudioBus.Ui, Loop = true });
        s.Advance(2000);
        Assert.Equal(Centre * 0.5f, Left(s, 9900, 1)[0], 3);
    }

    [Fact]
    public void StartIsFrameExact()
    {
        using var s = Service();
        var clip = s.LoadSound(new MemoryStream(Wav(16384, 480)));
        s.Play(clip, new PlayOptions { StartAtFrame = 100 });
        s.Advance(1000);
        var x = s.Capture!.Samples;
        Assert.Equal(0f, x[99 * 2]);
        Assert.Equal(Centre * 0.5f, x[100 * 2], 4);
        Assert.Equal(Centre * 0.5f, x[579 * 2], 4);
        Assert.Equal(0f, x[580 * 2]);
    }

    [Fact]
    public void LimiterKeepsSummedOverloadsInRange()
    {
        float PeakOf(OutputLimiterMode mode)
        {
            using var s = Service(mode);
            var clip = s.LoadSound(new MemoryStream(Wav(26000, 4800))); // 0.79 each, 1.12 summed after the pan law
            s.Play(clip);
            s.Play(clip);
            s.Advance(4800);
            Assert.All(s.Capture!.Samples.ToArray(), v => Assert.True(float.IsFinite(v)));
            return s.Capture.Peak;
        }

        Assert.Equal(1f, PeakOf(OutputLimiterMode.Clamp));
        Assert.InRange(PeakOf(OutputLimiterMode.SoftKnee), 0.95f, 0.9999f);
    }

    [Fact]
    public void DuckingTurnsTheMusicBusDownWhileSfxPlay()
    {
        using var s = Service();
        s.PlayMusic(new MemoryStream(Wav(13107, Rate * 3))); // 0.4
        s.Advance(2000);
        Assert.Equal(Centre * 0.4f, Left(s, 1900, 1)[0], 3);

        s.SetDucking(AudioBus.Music, AudioBus.Sfx, new DuckingOptions { ReductionDb = 12, AttackMs = 5 });
        s.Play(s.LoadSound(new MemoryStream(Wav(16384, Rate))), new PlayOptions { Loop = true });
        s.Advance(9600);
        var ducked = Left(s, 9500, 1)[0];
        Assert.InRange(ducked, Centre * (0.5f + (0.4f * 0.2f)), Centre * (0.5f + (0.4f * 0.3f))); // sfx 0.5 plus music at about -12 dB

        s.ClearDucking(AudioBus.Music);
        s.Advance(96000);
        Assert.Equal(Centre * 0.9f, Left(s, 105000, 1)[0], 3);
    }

    [Fact]
    public void BadContentFailsOnTheGameThreadWithTypedErrors()
    {
        using var s = Service();
        Assert.Throws<AudioLoadException>(() => s.LoadSound(new MemoryStream([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12])));
        Assert.Throws<AudioLoadException>(() => s.PlayMusic(new MemoryStream(new byte[64])));

        var ogg = File.ReadAllBytes(Asset("tone44k.ogg"));
        Assert.Throws<AudioLoadException>(() => s.LoadSound(new MemoryStream(ogg[..(ogg.Length / 2)])));

        var clip = s.LoadSound(new MemoryStream(Wav(1000, 100)));
        Assert.ThrowsAny<ArgumentException>(() => s.Play(clip, new PlayOptions { Volume = float.NaN }));
        Assert.ThrowsAny<ArgumentException>(() => s.Play(clip, new PlayOptions { Pitch = 1000f }));
        Assert.ThrowsAny<ArgumentException>(() => s.Play(clip, new PlayOptions { Pan = float.NaN }));

        // The service is still healthy afterwards.
        s.Play(clip);
        s.Advance(480);
        Assert.True(s.Capture!.Peak > 0);
    }

    [Fact]
    public void ClipsLongerThanTheLimitAreRefused()
    {
        using var s = AudioService.Create(new AudioServiceOptions { Capture = true, MaxClipSeconds = 0.5 });
        Assert.Throws<AudioLoadException>(() => s.LoadSound(new MemoryStream(Wav(1, Rate))));
        s.LoadSound(new MemoryStream(Wav(1, Rate / 4)));
    }

    [Fact]
    public void EndedStreamsAreReleasedAndTheServiceDisposesCleanly()
    {
        using var s = Service();
        for (var i = 0; i < 20; i++)
        {
            s.Play(s.LoadSound(new MemoryStream(Wav(100, 50))));
            s.PlayMusic(new MemoryStream(Wav(1000, 4800)), new MusicOptions { Loop = false });
            s.Advance(4900);
            s.Update();
        }

        Assert.Equal(0, s.Core.ActiveVoices);
    }
}
