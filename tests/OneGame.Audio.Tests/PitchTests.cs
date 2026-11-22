namespace OneGame.Audio.Tests;

/// <summary>Extreme pitch values must never stop the game: they play at a clamped speed, and a mixer fault is logged, not thrown.</summary>
public class PitchTests
{
    private static AudioService Service() => AudioService.Create(new AudioServiceOptions
    {
        Capture = true,
        Limiter = OutputLimiterMode.Clamp,
        Device = new AudioDeviceOptions { SampleRate = 48_000, Channels = 2 },
    });

    [Theory]
    [InlineData(4f)]
    [InlineData(2.2f)]
    [InlineData(16f)]
    [InlineData(1000f)]
    [InlineData(0.0001f)]
    [InlineData(0f)]
    [InlineData(-3f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void ExtremePitchAt44100PlaysWithoutThrowing(float pitch)
    {
        using var s = Service();
        using var ms = new MemoryStream(RealMixerTests.Wav(8000, 44_100, 44_100));
        var sound = s.LoadSound(ms);
        var voice = s.Play(sound, new PlayOptions { Pitch = pitch, VariablePitch = true });
        s.Advance(24_000);
        s.SetPitch(voice, pitch);
        s.Advance(24_000);
        Assert.Contains(s.Capture!.Samples.ToArray(), x => Math.Abs(x) > 0.01f);
    }

    [Fact]
    public void ResetGivesBackTheVoicesAndTheCommandQueue()
    {
        using var s = Service();
        using var ms = new MemoryStream(RealMixerTests.Wav(8000, 48_000 * 3, 48_000));
        var sound = s.LoadSound(ms);
        var dropped = 0;
        for (var i = 0; i < 3000; i++)
        {
            if (!s.Play(sound).IsValid)
            {
                dropped++;
            }
        }

        Assert.True(dropped > 0, "the queue should have filled while nothing renders");
        s.Reset();
        Assert.True(s.Play(sound).IsValid);
        s.Advance(480);
        Assert.Equal(1, s.Core.ActiveVoices);
    }

    [Fact]
    public void ThePumpDoesNotThrowWhenTheMixerFaults()
    {
        var output = AudioOutput.Create(new CaptureAudioBackend(), new AudioDeviceOptions { SampleRate = 48_000, Channels = 2 }, OutputLimiterMode.Clamp);
        output.NoteFault("boom");
        Assert.Equal("boom", output.LastError);
        Assert.Equal(1, output.RenderFaults);
        output.Dispose();
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(4801)]
    public void ScheduledMusicStartsOnTheRequestedFrame(int frame)
    {
        using var s = Service();
        var voice = s.PlayMusic(new MemoryStream(RealMixerTests.Wav(16000, 48_000, 48_000)), new MusicOptions { Loop = false, StartAtFrame = frame });
        Assert.True(voice.IsValid);
        s.Advance(frame + 480);
        var samples = s.Capture!.Samples.ToArray();
        var first = Array.FindIndex(samples, x => Math.Abs(x) > 1e-6f) / 2;
        Assert.Equal(frame, first);
    }

    // A click at input frame 0 and silence after it, so the output's peak is where the track's first sample landed.
    private static byte[] Click(int rate, int frames)
    {
        var wav = RealMixerTests.Wav(0, frames, rate);
        System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(wav.AsSpan(44), 30000);
        return wav;
    }

    [Theory]
    [InlineData(22_050, 1000, true)]
    [InlineData(22_050, 4801, true)]
    [InlineData(44_100, 1000, true)]
    [InlineData(44_100, 4801, true)]
    [InlineData(22_050, 1000, false)]
    [InlineData(44_100, 7777, false)]
    public void AClickAtAnyRateLandsWithinAFrameOfTheRequest(int rate, int frame, bool music)
    {
        using var s = Service();
        using var wav = new MemoryStream(Click(rate, rate / 2));
        if (music)
        {
            Assert.True(s.PlayMusic(wav, new MusicOptions { Loop = false, StartAtFrame = frame }).IsValid);
        }
        else
        {
            Assert.True(s.Play(s.LoadSound(wav), new PlayOptions { StartAtFrame = frame }).IsValid);
        }

        s.Advance(frame + 4800);
        var all = s.Capture!.Samples.ToArray();
        var peak = 0;
        for (var i = 0; i < all.Length / 2; i++)
        {
            if (Math.Abs(all[i * 2]) > Math.Abs(all[peak * 2]))
            {
                peak = i;
            }
        }

        Assert.InRange(peak, frame - 1, frame + 1);
    }
}
