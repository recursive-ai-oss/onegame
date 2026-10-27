using System.Runtime.InteropServices;

namespace OneGame.Audio.Tests;

/// <summary>Drives the browser backend through the device package's seam with a stand-in for the page's JavaScript.</summary>
public class WebDeviceSeamTests
{
    private sealed class FakePage : IWebAudioHost
    {
        public long WrittenFrames;
        public double ConsumedFrames;
        public float PeakWritten;
        public int Dry;
        public bool Active = true;
        public bool Closed;
        public Action? Timer;

        public string Create(int rate, int channels, int latencyMilliseconds) => string.Empty;

        public int Rate() => 48_000;

        public int TargetFrames() => 2400;

        public int Queued() => (int)(WrittenFrames - (long)ConsumedFrames);

        public void Write(ReadOnlySpan<byte> bytes)
        {
            var samples = MemoryMarshal.Cast<byte, float>(bytes);
            foreach (var s in samples)
            {
                PeakWritten = Math.Max(PeakWritten, Math.Abs(s));
            }

            WrittenFrames += samples.Length / 2;
        }

        public void SetActive(bool active) => Active = active;

        public string State() => Active ? "running" : "suspended";

        public string Stats() => "fake";

        public double Consumed() => ConsumedFrames;

        public int Underruns() => Dry;

        public void Close() => Closed = true;

        public void StartTimer(Action callback, int milliseconds) => Timer = callback;
    }

    private sealed class Constant(float value) : IAudioRenderSource
    {
        public int SampleRate => 48_000;

        public int Channels => 2;

        public void Render(Span<float> interleaved) => interleaved.Fill(value);
    }

    [Fact]
    public void ThePageTimerFeedsTheWorkletAndTheCountersFollowItsClock()
    {
        var page = new FakePage();
        var backend = new DevicesBackend(new WebAudioDeviceBackend(page));
        using var output = AudioOutput.Create(backend, new AudioDeviceOptions { SampleRate = 48_000, Channels = 2 }, OutputLimiterMode.Clamp);
        output.Start(new Constant(0.5f));
        Assert.NotNull(page.Timer);
        Assert.Equal(AudioOutputStatus.Running, output.Status);

        // The timer ticks; the worklet plays some of it and later runs dry once.
        page.Timer!();
        Assert.InRange(page.WrittenFrames, 1920, 2880);
        page.ConsumedFrames = page.WrittenFrames - 480;
        page.Timer();
        page.Dry = 1;
        page.ConsumedFrames = page.WrittenFrames;
        page.Timer();

        var s = output.DeviceStatus();
        Assert.Equal("webaudio", s.Backend);
        Assert.Equal(page.WrittenFrames, s.FramesWritten);
        Assert.True(s.FramesPlayed > 0 && s.FramesPlayed <= s.FramesWritten, s.ToString());
        Assert.InRange(s.PeakPlayed, 0.49f, 0.51f);
        Assert.Equal(0.5f, page.PeakWritten);
        Assert.Equal(1, s.Underruns);

        // Suspending stops the worklet and the timer renders nothing.
        output.Suspended = true;
        var before = page.WrittenFrames;
        page.ConsumedFrames = before;
        page.Timer();
        Assert.False(page.Active);
        Assert.Equal(before, page.WrittenFrames);
        output.Suspended = false;
        Assert.True(page.Active);
    }
}
