namespace OneGame.Audio.Tests;

public class ServiceCaptureTests
{
    private const int Rate = 48_000;

    private static AudioService Capture(OutputLimiterMode limiter = OutputLimiterMode.Clamp, bool pauseOnFocusLoss = true) =>
        AudioService.Create(new AudioServiceOptions
        {
            Capture = true,
            CoreFactory = TestCore.Factory,
            Limiter = limiter,
            PauseOnFocusLoss = pauseOnFocusLoss,
            Device = new AudioDeviceOptions { SampleRate = Rate, Channels = 2 },
        });

    private static Sound Load(AudioService s, byte[] data) => s.LoadSound(new MemoryStream(data));

    [Fact]
    public void CaptureHasTheRequestedFormatAndNoDevice()
    {
        using var s = Capture();
        Assert.Equal(new AudioDeviceFormat(Rate, 2), s.Format);
        Assert.NotNull(s.Capture);
        s.Advance(100);
        Assert.Equal(100, s.Capture!.FramesCaptured);
        Assert.All(s.Capture.Samples.ToArray(), x => Assert.Equal(0f, x));
    }

    [Fact]
    public void SumsGainsPansAndBusesMatchExpectedSamples()
    {
        using var s = Capture();
        var a = Load(s, TestCore.Constant(0.25f, 100));
        var b = Load(s, TestCore.Constant(0.125f, 100));
        s.Play(a, new PlayOptions { Volume = 0.5f, Pan = -0.5f, Bus = AudioBus.Sfx });
        s.Play(b, new PlayOptions { Volume = 1f, Pan = 1f, Bus = AudioBus.Ui });
        s.SetBusVolume(AudioBus.Ui, 0.5f);
        s.MasterVolume = 0.5f;
        s.Advance(200);

        var x = s.Capture!.Samples.ToArray();
        // a: 0.25 * 0.5 gain * master 0.5 = 0.0625; pan -0.5 leaves left at 1 and right at 0.5.
        // b: 0.125 * ui 0.5 * master 0.5 = 0.03125; pan +1 leaves right at 1 and left at 0.
        Assert.Equal(0.0625f, x[0], 6);
        Assert.Equal((0.0625f * 0.5f) + 0.03125f, x[1], 6);
        Assert.Equal(0.0625f, x[(99 * 2)], 6);
        Assert.Equal(0f, x[100 * 2], 6);
        Assert.Equal(0f, x[(199 * 2) + 1], 6);
    }

    [Fact]
    public void StartAtFrameIsFrameExact()
    {
        using var s = Capture();
        var a = Load(s, TestCore.Constant(0.5f, 10));
        s.Play(a, new PlayOptions { StartAtFrame = 1234 });
        s.Advance(2000);
        var x = s.Capture!.Samples;
        for (var f = 0; f < 2000; f++)
        {
            var expect = f is >= 1234 and < 1244 ? 0.5f : 0f;
            Assert.True(Math.Abs(x[f * 2] - expect) < 1e-6f, $"frame {f}: {x[f * 2]} != {expect}");
        }
    }

    [Fact]
    public void ClampKeepsSummedVoicesInRange()
    {
        using var s = Capture(OutputLimiterMode.Clamp);
        var a = Load(s, TestCore.Constant(0.8f, 50));
        s.Play(a);
        s.Play(a);
        s.Play(Load(s, TestCore.Constant(-0.8f, 50)), new PlayOptions { Pan = 0 });
        s.Play(Load(s, TestCore.Constant(-0.8f, 50)));
        s.Play(Load(s, TestCore.Constant(-0.8f, 50)));
        s.Advance(50);
        Assert.Equal(-0.8f * 3 + 1.6f, s.Capture!.Samples[0], 5); // inside range: untouched

        s.Capture.Clear();
        s.Play(Load(s, TestCore.Constant(-0.8f, 50)));
        s.Play(Load(s, TestCore.Constant(-0.8f, 50)));
        s.Advance(50);
        Assert.Equal(-1f, s.Capture.Samples[0]);
        Assert.True(s.Capture.Peak <= 1f);
    }

    [Fact]
    public void SoftKneePassesQuietSamplesAndStaysBelowOne()
    {
        using var s = Capture(OutputLimiterMode.SoftKnee);
        var quiet = Load(s, TestCore.Constant(0.5f, 10));
        s.Play(quiet);
        s.Advance(10);
        Assert.Equal(0.5f, s.Capture!.Samples[0]);

        s.Capture.Clear();
        s.Play(Load(s, TestCore.Constant(3f, 10)));
        s.Advance(10);
        Assert.InRange(s.Capture.Samples[0], 0.99f, 1f);
    }

    [Fact]
    public void LimiterIsMonotonicAndHandlesNaN()
    {
        var last = -2f;
        for (var x = -4f; x <= 4f; x += 0.01f)
        {
            var y = OutputLimiter.Apply(x, OutputLimiterMode.SoftKnee);
            Assert.InRange(y, -1f, 1f);
            Assert.True(y >= last);
            last = y;
        }

        Assert.Equal(0f, OutputLimiter.Apply(float.NaN, OutputLimiterMode.Clamp));
        Assert.Equal(1f, OutputLimiter.Apply(float.PositiveInfinity, OutputLimiterMode.Clamp));
        Assert.Equal(-1f, OutputLimiter.Apply(float.NegativeInfinity, OutputLimiterMode.SoftKnee));
    }

    [Fact]
    public void SameScenarioTwiceGivesIdenticalSamples()
    {
        float[] Run()
        {
            using var s = Capture(OutputLimiterMode.SoftKnee);
            var tone = Load(s, TestCore.Sine(Rate, 440, 0.7f, 4800));
            s.Play(tone, new PlayOptions { Loop = true, Pan = 0.3f });
            s.Play(tone, new PlayOptions { StartAtFrame = 777, Volume = 0.9f });
            s.Advance(1000);
            s.Update();
            s.Advance(3000);
            return s.Capture!.Samples.ToArray();
        }

        Assert.Equal(Run(), Run());
    }

    [Fact]
    public void FocusLossPausesAndHoldsTheClock()
    {
        using var s = Capture();
        var core = (TestCore)s.Core;
        s.Play(Load(s, TestCore.Constant(0.5f, 1000)));
        s.Advance(100);
        var timeline = core.Timeline;
        s.SetFocused(false);
        s.Advance(100);
        Assert.Equal(timeline, core.Timeline);
        Assert.All(s.Capture!.Samples[200..].ToArray(), x => Assert.Equal(0f, x));
        s.SetFocused(true);
        s.Advance(100);
        Assert.Equal(0.5f, s.Capture.Samples[400], 6);
    }

    [Fact]
    public void FocusLossCanBeDisabled()
    {
        using var s = Capture(pauseOnFocusLoss: false);
        s.Play(Load(s, TestCore.Constant(0.5f, 1000)));
        s.SetFocused(false);
        s.Advance(10);
        Assert.Equal(0.5f, s.Capture!.Samples[0], 6);
    }

    [Fact]
    public void MuteSilencesWithoutLosingVolumes()
    {
        using var s = Capture();
        s.Play(Load(s, TestCore.Constant(0.5f, 1000)));
        s.SetBusVolume(AudioBus.Sfx, 0.5f);
        s.Muted = true;
        s.Advance(10);
        Assert.Equal(0f, s.Capture!.Samples[0]);
        s.Muted = false;
        s.Advance(10);
        Assert.Equal(0.25f, s.Capture.Samples[20], 6);
        Assert.Equal(0.5f, s.GetBusVolume(AudioBus.Sfx));
    }

    [Fact]
    public void EndedVoicesRaiseEventsOnUpdate()
    {
        using var s = Capture();
        var seen = new List<AudioEvent>();
        s.EventRaised += seen.Add;
        var v = s.Play(Load(s, TestCore.Constant(0.5f, 100)));
        s.Advance(200);
        s.Update();
        Assert.Contains(seen, e => e.Kind == AudioEventKind.VoiceEnded && e.Voice == v);
    }

    [Fact]
    public void MusicCrossfadeEndsOnTheNewTrackAndForgetsTheOld()
    {
        using var s = Capture();
        s.PlayMusic(new MemoryStream(TestCore.Constant(0.4f, 20_000)));
        s.Advance(1000);
        Assert.Equal(0.4f, s.Capture!.Samples[1998], 5);
        var second = s.PlayMusic(new MemoryStream(TestCore.Constant(0.2f, 20_000)), new MusicOptions { CrossfadeSeconds = 0.1 });
        s.Advance(4800);
        Assert.Equal(second, s.CurrentMusic);
        s.Advance(100);
        // After the 0.1 s fade only the second track is left.
        var x = s.Capture.Samples;
        Assert.Equal(0.2f, x[(5900 - 1) * 2], 5);
        // Midway both tracks contribute: the old one is below its starting level.
        Assert.InRange(x[(1000 + 2400) * 2], 0.1f, 0.6f);
    }

    [Fact]
    public void StopMusicClearsTheCurrentTrack()
    {
        using var s = Capture();
        s.PlayMusic(new MemoryStream(TestCore.Constant(0.4f, 20_000)));
        s.StopMusic();
        s.Advance(10);
        Assert.False(s.CurrentMusic.IsValid);
        Assert.Equal(0f, s.Capture!.Samples[0]);
    }

    [Fact]
    public void WavWriterProducesAReadableFloatFile()
    {
        using var s = Capture();
        s.Play(Load(s, TestCore.Constant(0.5f, 10)));
        s.Advance(10);
        using var ms = new MemoryStream();
        s.Capture!.WriteWav(ms);
        var b = ms.ToArray();
        Assert.Equal(44 + (10 * 2 * 4), b.Length);
        Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(b, 0, 4));
        Assert.Equal(3, BitConverter.ToInt16(b, 20));
        Assert.Equal(Rate, BitConverter.ToInt32(b, 24));
        Assert.Equal(0.5f, BitConverter.ToSingle(b, 44));
    }

    [Fact]
    public void AdvanceNeedsCaptureMode()
    {
        using var s = AudioService.Create(new AudioServiceOptions { Backend = new FakeBackend() });
        Assert.Throws<InvalidOperationException>(() => s.Advance(10));
    }

    [Fact]
    public void RejectsSettingsOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AudioService.Create(new AudioServiceOptions { Capture = true, BusCount = 2 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => AudioService.Create(new AudioServiceOptions { Capture = true, Device = new AudioDeviceOptions { Channels = 0 } }));
        using var s = Capture();
        Assert.Throws<ArgumentOutOfRangeException>(() => s.SetBusVolume(new AudioBus(8), 1f));
    }
}
