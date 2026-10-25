using System.Runtime.CompilerServices;

namespace OneGame.Audio.Tests;

/// <summary>
/// Filters, reverb, the master limiter, FLAC and MP3, stream positions and loudness, through the service on oneaudio's mixer,
/// recorded by the capture sink at 48 kHz stereo.
/// </summary>
public class MixingTests
{
    private const int Rate = 48_000;

    // A mono source is centred with an equal-power law: -3 dB in each channel.
    private const double Centre = 0.70710678;

    private static string Asset(string name, [CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "testdata", "audio", name));

    private static AudioService Service(AudioServiceOptions? o = null) => AudioService.Create((o ?? new AudioServiceOptions()) with
    {
        Capture = true,
        Limiter = OutputLimiterMode.Clamp,
        Device = new AudioDeviceOptions { SampleRate = Rate, Channels = 2 },
    });

    // One second of a sine as mono 16-bit PCM. A whole number of cycles at the test frequencies, so a looped clip has no seam.
    private static byte[] SineWav(double hz, double amplitude, double seconds = 1)
    {
        var frames = (int)(seconds * Rate);
        var wav = RealMixerTests.Wav(0, frames);
        for (var i = 0; i < frames; i++)
        {
            var v = (short)Math.Round(Math.Sin(2 * Math.PI * hz * i / Rate) * amplitude * 32767);
            BitConverter.TryWriteBytes(wav.AsSpan(44 + (i * 2)), v);
        }

        return wav;
    }

    private static Sound Load(AudioService s, byte[] wav) => s.LoadSound(new MemoryStream(wav));

    private static double Rms(AudioService s, double fromSeconds, double toSeconds)
    {
        var all = s.Capture!.Samples;
        var from = (int)(fromSeconds * Rate);
        var to = Math.Min((int)(toSeconds * Rate), s.Capture.FramesCaptured);
        double sum = 0;
        for (var i = from; i < to; i++)
        {
            sum += all[i * 2] * (double)all[i * 2];
        }

        return Math.Sqrt(sum / (to - from));
    }

    private static double Db(double ratio) => 20 * Math.Log10(ratio);

    // The level of a looped sine on a bus, measured in the settled second half of a one-second run.
    private static double SineLevelDb(double hz, Action<AudioService>? setup)
    {
        using var s = Service();
        setup?.Invoke(s);
        s.Play(Load(s, SineWav(hz, 0.5)), new PlayOptions { Loop = true });
        s.Advance(Rate);
        return Db(Rms(s, 0.5, 1.0) / (0.5 * Centre / Math.Sqrt(2)));
    }

    [Fact]
    public void LowPassAt1kHzTakesAbout24DbOffA4kHzToneAndLeavesALowOneAlone()
    {
        // A second-order Butterworth is 10*log10(1 + (f/fc)^4) down: 24.1 dB at four times the cutoff.
        Assert.Equal(0, SineLevelDb(4000, null), 0.1);
        var cut = SineLevelDb(4000, s => s.SetBusFilter(AudioBus.Sfx, AudioFilterKind.LowPass, 1000f));
        Assert.InRange(cut, -25.1, -23.1);
        var low = SineLevelDb(250, s => s.SetBusFilter(AudioBus.Sfx, AudioFilterKind.LowPass, 1000f));
        Assert.InRange(low, -0.3, 0.1);
    }

    [Fact]
    public void HighPassAt1kHzTakesAbout24DbOffA250HzToneAndLeavesAHighOneAlone()
    {
        var cut = SineLevelDb(250, s => s.SetBusFilter(AudioBus.Sfx, AudioFilterKind.HighPass, 1000f));
        Assert.InRange(cut, -25.1, -23.1);
        var high = SineLevelDb(4000, s => s.SetBusFilter(AudioBus.Sfx, AudioFilterKind.HighPass, 1000f));
        Assert.InRange(high, -0.3, 0.1);
    }

    [Fact]
    public void ClearingAFilterRestoresTheTone()
    {
        var level = SineLevelDb(4000, s =>
        {
            s.SetBusFilter(AudioBus.Sfx, AudioFilterKind.LowPass, 1000f);
            s.Advance(480);
            s.ClearBusFilter(AudioBus.Sfx, AudioFilterKind.LowPass);
        });
        Assert.InRange(level, -0.3, 0.1);
    }

    [Fact]
    public void AVoiceFilterMuffledOnlyThatVoice()
    {
        using var s = Service();
        var tone = Load(s, SineWav(4000, 0.5));
        var muffled = s.Play(tone, new PlayOptions { Loop = true, Pan = -1 });
        s.Play(tone, new PlayOptions { Loop = true, Pan = 1 });
        s.SetVoiceFilter(muffled, AudioFilterKind.LowPass, 1000f);
        s.Advance(Rate);
        var all = s.Capture!.Samples;
        double l = 0, r = 0;
        for (var i = Rate / 2; i < Rate; i++)
        {
            l += all[i * 2] * (double)all[i * 2];
            r += all[(i * 2) + 1] * (double)all[(i * 2) + 1];
        }

        Assert.InRange(Db(Math.Sqrt(l / r)), -25.1, -23.1);
    }

    [Fact]
    public void BadFilterSettingsThrowTypedErrors()
    {
        using var s = Service();
        Assert.Throws<ArgumentOutOfRangeException>(() => s.SetBusFilter(AudioBus.Sfx, AudioFilterKind.LowPass, 0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => s.SetBusFilter(AudioBus.Sfx, AudioFilterKind.LowPass, float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => s.SetBusFilter(AudioBus.Sfx, AudioFilterKind.LowPass, 500f, -1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => s.SetBusFilter(new AudioBus(99), AudioFilterKind.LowPass, 500f));
    }

    [Fact]
    public void ReverbMakesATailAfterADryImpulse()
    {
        double Tail(bool reverb)
        {
            using var s = Service();
            if (reverb)
            {
                s.SetBusReverb(AudioBus.Sfx, new ReverbOptions { Wet = 1f, Dry = 0.5f, DecaySeconds = 1f });
            }

            var impulse = RealMixerTests.Wav(0, 64);
            BitConverter.TryWriteBytes(impulse.AsSpan(44), (short)20000);
            s.Play(Load(s, impulse));
            s.Advance(Rate);
            Assert.True(Rms(s, 0, 0.01) > 0, "the impulse itself plays");
            return Rms(s, 0.1, 0.6);
        }

        Assert.Equal(0, Tail(false));
        var tail = Tail(true);
        Assert.True(tail > 1e-4, $"tail rms {tail}");
    }

    [Fact]
    public void ReverbTailKeepsRingingAndDiesOutAfterTheDecay()
    {
        using var s = Service();
        s.SetBusReverb(AudioBus.Sfx, new ReverbOptions { Wet = 1f, DecaySeconds = 0.5f });
        var impulse = RealMixerTests.Wav(0, 64);
        BitConverter.TryWriteBytes(impulse.AsSpan(44), (short)20000);
        s.Play(Load(s, impulse));
        s.Advance(Rate * 3);
        // RT60 is 0.5 s: by 2 s the tail is far below where it was at 0.2 s.
        Assert.True(Rms(s, 2.0, 2.5) < Rms(s, 0.2, 0.7) * 0.01);
        Assert.Single(s.Describe().Buses, b => b.Reverb is not null);
    }

    [Fact]
    public void MoreReverbsThanTheServiceWasBuiltForIsRefused()
    {
        using var s = Service(new AudioServiceOptions { MaxReverbs = 1 });
        Assert.True(s.SetBusReverb(AudioBus.Sfx, new ReverbOptions()));
        s.Advance(480);
        Assert.False(s.SetBusReverb(AudioBus.Ui, new ReverbOptions()));
        Assert.Null(s.Describe().Buses[2].Reverb);
        Assert.True(s.SetBusReverb(AudioBus.Sfx, new ReverbOptions { Wet = 0.1f }));
        Assert.ThrowsAny<ArgumentException>(() => s.SetBusReverb(AudioBus.Sfx, new ReverbOptions { DecaySeconds = 500f }));
    }

    [Fact]
    public void LimiterHoldsASixDbOverdriveUnderItsCeiling()
    {
        using var s = Service(new AudioServiceOptions { MasterLimiter = new LimiterOptions { CeilingDb = -3f, ReleaseMilliseconds = 50f }, LimiterLookaheadMilliseconds = 5 });
        var tone = Load(s, SineWav(1000, 0.9));
        // Two voices of 0.9 sum to 1.8, +6 dB over one, and about +4.6 dB over the ceiling after centring.
        s.Play(tone, new PlayOptions { Loop = true, Pan = -1 });
        s.Play(tone, new PlayOptions { Loop = true, Pan = -1 });
        s.Advance(Rate);
        var ceiling = Math.Pow(10, -3 / 20.0);
        var peak = s.Capture!.Samples.ToArray().Max(Math.Abs);
        Assert.True(peak <= ceiling + 1e-4, $"peak {peak} over ceiling {ceiling}");
        Assert.True(peak > ceiling * 0.9, $"peak {peak}: the limiter should sit at the ceiling, not far under");
        var status = s.Describe().Limiter;
        Assert.NotNull(status);
        Assert.True(status.GainReductionDb < -3, $"reduction {status.GainReductionDb}");

        // The same overdrive without a limiter hits the output clamp.
        using var raw = Service();
        var t2 = Load(raw, SineWav(1000, 0.9));
        raw.Play(t2, new PlayOptions { Loop = true, Pan = -1 });
        raw.Play(t2, new PlayOptions { Loop = true, Pan = -1 });
        raw.Advance(Rate);
        Assert.True(raw.Capture!.Samples.ToArray().Max(Math.Abs) > 0.99f);
        Assert.Null(raw.Describe().Limiter);
        Assert.Throws<InvalidOperationException>(() => raw.SetMasterLimiter(null));
    }

    [Fact]
    public void LimiterDelaysTheOutputByItsLookaheadAndCanBeSwitchedOff()
    {
        using var s = Service(new AudioServiceOptions { MasterLimiter = new LimiterOptions(), LimiterLookaheadMilliseconds = 5 });
        s.Play(Load(s, RealMixerTests.Wav(8000, 4800)));
        s.Advance(2400);
        var all = s.Capture!.Samples;
        var first = 0;
        while (all[first * 2] == 0)
        {
            first++;
        }

        Assert.InRange(first, 235, 245);
        s.SetMasterLimiter(null);
        Assert.False(s.Describe().Limiter!.Enabled);
    }

    [Theory]
    [InlineData("tone44k.flac")]
    [InlineData("tone44k.mp3")]
    public void FlacAndMp3ClipsDecodeAndPlay(string file)
    {
        using var s = Service();
        using var data = File.OpenRead(Asset(file));
        var sound = s.LoadSound(data, file);
        // MP3 carries encoder delay and padding, so its length is a little off.
        Assert.InRange(sound.DurationSeconds, 0.45, 0.58);
        s.Play(sound, new PlayOptions { Bus = AudioBus.Sfx });
        s.Advance(Rate / 2);
        var samples = s.Capture!.Samples;
        float peak = 0;
        var crossings = 0;
        var from = 4800;
        var to = 4800 + 24_000 - 4800;
        for (var i = from; i < to; i++)
        {
            peak = Math.Max(peak, Math.Abs(samples[i * 2]));
            if (samples[(i - 1) * 2] < 0 && samples[i * 2] >= 0)
            {
                crossings++;
            }
        }

        Assert.InRange(peak, 0.5 * Centre * 0.9, 0.5 * Centre * 1.1);
        var hz = crossings * (double)Rate / (to - from);
        Assert.InRange(hz, 430, 450);
    }

    [Fact]
    public void ADamagedFlacFailsWithATypedError()
    {
        using var s = Service();
        var bytes = File.ReadAllBytes(Asset("tone44k.flac"));
        Assert.Throws<AudioLoadException>(() => s.LoadSound(new MemoryStream(bytes, 0, 20)));
    }

    [Fact]
    public void AStreamedTrackReportsItsLengthAndPosition()
    {
        using var s = Service();
        var voice = s.PlayMusic(File.OpenRead(Asset("track48k.flac")), new MusicOptions { Loop = false });
        var start = s.GetStreamPosition(voice);
        Assert.NotNull(start);
        Assert.Equal(3.0, start.Value.LengthSeconds, 0.01);
        Assert.Equal(0, start.Value.PositionSeconds, 0.01);
        s.Advance(Rate);
        var p = s.GetStreamPosition(voice)!.Value;
        Assert.Equal(1.0, p.PositionSeconds, 0.05);
        s.Advance(Rate);
        Assert.Equal(2.0, s.GetStreamPosition(voice)!.Value.PositionSeconds, 0.05);

        var music = s.Describe().Music;
        Assert.NotNull(music);
        Assert.Equal(3.0, music.LengthSeconds!.Value, 0.01);

        s.Advance(Rate * 2);
        s.Update();
        Assert.Null(s.GetStreamPosition(voice));
        Assert.Null(s.Describe().Music);
    }

    [Fact]
    public void ALoopingStreamsPositionWraps()
    {
        using var s = Service();
        var voice = s.PlayMusic(File.OpenRead(Asset("track48k.flac")));
        s.Advance(Rate * 4);
        Assert.Equal(1.0, s.GetStreamPosition(voice)!.Value.PositionSeconds, 0.1);
    }

    [Fact]
    public void LoudnessMeterReadsACalibratedTone()
    {
        using var s = Service();
        // A mono 1 kHz sine at -20 dBFS peak, centred (-3 dB per channel), is -23.0 LUFS: EBU R 128's own reference level (BS.1770-5 §2, Tech 3341 test signals).
        s.Play(Load(s, SineWav(1000, 0.1)), new PlayOptions { Loop = true });
        s.Advance(Rate * 6);
        var l = s.Loudness;
        Assert.Equal(-23.0, l.IntegratedLufs, 0.2);
        Assert.Equal(-23.0, l.MomentaryLufs, 0.2);
        Assert.Equal(-23.0, l.ShortTermLufs, 0.2);
        Assert.Equal(-23.0, l.TruePeakDbtp, 0.3);
        Assert.Equal(6 * Rate, l.Frames);
    }

    [Fact]
    public void LoudnessResetsAndSilenceReadsAsNull()
    {
        using var s = Service();
        Assert.Null(s.Describe().Loudness.IntegratedLufs);
        s.Play(Load(s, SineWav(1000, 0.1)), new PlayOptions { Loop = true });
        s.Advance(Rate * 2);
        Assert.NotNull(s.Describe().Loudness.IntegratedLufs);
        s.StopAll();
        s.ResetLoudness();
        s.Advance(Rate);
        var after = s.Describe().Loudness;
        Assert.Null(after.IntegratedLufs);
        Assert.Equal(1.0, after.MeasuredSeconds, 0.01);
    }

    [Fact]
    public void TheMusicBusReadsTheToneAndTheSfxBusReadsSilence()
    {
        using var s = Service();
        s.Play(Load(s, SineWav(1000, 0.1)), new PlayOptions { Loop = true, Bus = AudioBus.Music });
        s.Advance(Rate * 6);
        var music = s.GetBusLoudness(AudioBus.Music)!.Value;
        Assert.Equal(-23.0, music.IntegratedLufs, 0.2);
        Assert.Equal(-23.0, music.MomentaryLufs, 0.2);
        Assert.Equal(-23.0, music.TruePeakDbtp, 0.3);
        Assert.Equal(6 * Rate, music.Frames);
        Assert.Equal(double.NegativeInfinity, s.GetBusLoudness(AudioBus.Sfx)!.Value.IntegratedLufs);
        var d = s.Describe();
        Assert.Equal(-23.0, d.Buses[0].Loudness!.IntegratedLufs!.Value, 0.2);
        Assert.Null(d.Buses[1].Loudness!.IntegratedLufs);
        Assert.Null(d.Buses[3].Loudness); // not one of the three metered at startup
        Assert.Null(s.GetBusLoudness(new AudioBus(3)));

        s.ResetLoudness();
        s.Advance(Rate);
        Assert.Equal(Rate, s.GetBusLoudness(AudioBus.Music)!.Value.Frames);
    }

    [Fact]
    public void DuckingLowersTheOutputButNotTheMusicBusReading()
    {
        using var s = Service();
        s.Play(Load(s, SineWav(1000, 0.1)), new PlayOptions { Loop = true, Bus = AudioBus.Music });
        s.Advance(Rate * 2);
        var before = s.GetBusLoudness(AudioBus.Music)!.Value.MomentaryLufs;
        var outBefore = s.Loudness.MomentaryLufs;
        s.SetDucking(AudioBus.Music, AudioBus.Sfx, new DuckingOptions { ThresholdDb = -40f, ReductionDb = 12f, AttackMs = 5f, ReleaseMs = 250f });
        s.Play(Load(s, SineWav(440, 0.5)), new PlayOptions { Loop = true, Bus = AudioBus.Sfx });
        s.Advance(Rate * 2);
        // The mix at the output has the music turned down; the bus meter sits before ducking, so it does not move.
        Assert.Equal(before, s.GetBusLoudness(AudioBus.Music)!.Value.MomentaryLufs, 0.05);
        Assert.Equal(-23.0, s.GetBusLoudness(AudioBus.Music)!.Value.MomentaryLufs, 0.2);
        Assert.True(s.GetBusLoudness(AudioBus.Sfx)!.Value.MomentaryLufs > -30);
        Assert.NotEqual(outBefore, s.Loudness.MomentaryLufs);
    }

    [Fact]
    public void MoreMetersAndReverbsThanSlotsReturnFalseAndNeverThrow()
    {
        using var s = Service(new AudioServiceOptions { MaxBusMeters = 4, MaxReverbs = 1 });
        Assert.Equal(1, s.FreeBusMeters);
        Assert.True(s.EnableBusMeter(new AudioBus(3)));
        Assert.True(s.EnableBusMeter(new AudioBus(3))); // already metered
        Assert.Equal(0, s.FreeBusMeters);
        Assert.False(s.EnableBusMeter(new AudioBus(4)));
        Assert.Null(s.GetBusLoudness(new AudioBus(4)));
        Assert.True(s.DisableBusMeter(AudioBus.Ui));
        Assert.True(s.EnableBusMeter(new AudioBus(4)));
        s.Advance(480);
        Assert.Null(s.GetBusLoudness(AudioBus.Ui));
        Assert.NotNull(s.GetBusLoudness(new AudioBus(4)));

        Assert.Equal(1, s.FreeReverbSlots);
        Assert.True(s.SetBusReverb(AudioBus.Sfx, new ReverbOptions()));
        Assert.Equal(0, s.FreeReverbSlots);
        Assert.False(s.SetBusReverb(AudioBus.Ui, new ReverbOptions()));
        s.ClearBusReverb(AudioBus.Sfx);
        Assert.True(s.SetBusReverb(AudioBus.Ui, new ReverbOptions()));

        using var none = Service(new AudioServiceOptions { MaxBusMeters = 0, MaxReverbs = 0 });
        Assert.False(none.EnableBusMeter(AudioBus.Music));
        Assert.False(none.SetBusReverb(AudioBus.Music, new ReverbOptions()));
        Assert.Null(none.GetBusLoudness(AudioBus.Music));
    }

    [Fact]
    public void MeteringCanBeSwitchedOff()
    {
        using var s = Service(new AudioServiceOptions { Metering = false });
        s.Play(Load(s, SineWav(1000, 0.1)), new PlayOptions { Loop = true });
        s.Advance(Rate);
        Assert.Equal(double.NegativeInfinity, s.Loudness.IntegratedLufs);
    }

    [Fact]
    public void DescribeListsTheBusSettings()
    {
        using var s = Service();
        s.SetBusVolume(AudioBus.Music, 0.5f);
        s.SetBusFilter(AudioBus.Music, AudioFilterKind.LowPass, 800f);
        s.SetBusReverb(AudioBus.Sfx, new ReverbOptions { Wet = 0.5f });
        s.SetDucking(AudioBus.Music, AudioBus.Ui);
        s.SetBusMuted(AudioBus.Ui, true);
        var d = s.Describe();
        Assert.Equal("music", d.Buses[0].Name);
        Assert.Equal(0.5f, d.Buses[0].Volume);
        Assert.Equal(800f, d.Buses[0].LowPass!.CutoffHz);
        Assert.Equal(2, d.Buses[0].Ducking!.WatchedBus);
        Assert.Equal(0.5f, d.Buses[1].Reverb!.Value.Wet);
        Assert.True(d.Buses[2].Muted);

        s.SetDucking(AudioBus.Music, AudioBus.Ui, DuckingOptions.None);
        s.ClearBusFilter(AudioBus.Music, AudioFilterKind.LowPass);
        Assert.Null(s.Describe().Buses[0].Ducking);
        Assert.Null(s.Describe().Buses[0].LowPass);
    }

    [Fact]
    public void DuckingFollowsTheDocumentedExponentialRelease()
    {
        using var s = Service();
        s.SetDucking(AudioBus.Music, AudioBus.Ui, new DuckingOptions { ThresholdDb = -30f, ReductionDb = 12f, AttackMs = 5f, ReleaseMs = 250f });
        var music = Load(s, SineWav(250, 0.25, 4));
        var ui = Load(s, SineWav(1000, 0.5, 0.5));
        s.Play(music, new PlayOptions { Bus = AudioBus.Music, Loop = true });
        s.Advance(Rate / 2);
        var open = Level(s, 0.3, 0.5);
        s.Play(ui, new PlayOptions { Bus = AudioBus.Ui });
        s.Advance(Rate / 2);
        s.Advance(Rate);
        // While the ui sound played the music sat 12 dB down; afterwards it returns with a 250 ms time constant (after 1 s, 4 time constants, 98%).
        var recovered = Level(s, 1.9, 2.0);
        Assert.InRange(recovered - open, -0.6, 0.1);
        Assert.True(s.Capture!.FramesCaptured == 2 * Rate);
    }

    // The 250 Hz music level (the ui tone is 1 kHz) measured with a Goertzel filter at 250 Hz.
    private static double Level(AudioService s, double from, double to)
    {
        var all = s.Capture!.Samples;
        var a = (int)(from * Rate);
        var b = (int)(to * Rate);
        double re = 0, im = 0;
        for (var i = a; i < b; i++)
        {
            var ph = 2 * Math.PI * 250 * i / Rate;
            re += all[i * 2] * Math.Cos(ph);
            im += all[i * 2] * Math.Sin(ph);
        }

        return Db(2 * Math.Sqrt((re * re) + (im * im)) / (b - a));
    }
}
