using Dev = OneAudio.Devices;

namespace OneGame.Audio.Tests;

/// <summary>AudioOutput on the device package's manual backend: the lifecycle and counters our service reads.</summary>
public class DevicePackageOutputTests
{
    private sealed class Constant(float value) : IAudioRenderSource
    {
        public int SampleRate => 48_000;

        public int Channels => 2;

        public void Render(Span<float> interleaved) => interleaved.Fill(value);
    }

    private static AudioOutput Open(Dev.ManualBackend manual, float value = 0.5f)
    {
        var output = AudioOutput.Create(new DevicesBackend(manual), new AudioDeviceOptions { SampleRate = 48_000, Channels = 2 }, OutputLimiterMode.Clamp);
        output.Start(new Constant(value));
        return output;
    }

    [Fact]
    public void AdvanceMovesTheDeviceClockAndTheCountersAgree()
    {
        var manual = new Dev.ManualBackend(48_000, 2);
        using var output = Open(manual);
        output.Advance(4800);
        var s = output.DeviceStatus();
        Assert.Equal(4800, s.FramesWritten);
        Assert.Equal(4800, s.FramesPlayed);
        Assert.InRange(s.PeakPlayed, 0.49f, 0.51f);
        Assert.Equal(4800, output.FramesRendered);
        Assert.Equal(0, output.AudioThreadAllocatedBytes);
    }

    [Fact]
    public void ALostDeviceReopensAndCountsAndSystemInterruptionStopsTheDevice()
    {
        var manual = new Dev.ManualBackend(48_000, 2);
        using var output = Open(manual);
        output.Advance(960);
        manual.SimulateLoss();
        output.Advance(960);
        Assert.Equal(1, output.DeviceLosses);

        manual.SimulateInterruption(true);
        output.Advance(480);
        Assert.True(output.IsDeviceStopped);
        manual.SimulateInterruption(false);
        output.Advance(480);
        Assert.False(output.IsDeviceStopped);
    }

    [Fact]
    public void PausedOutputFeedsSilenceAndHoldsTheMixerClock()
    {
        var manual = new Dev.ManualBackend(48_000, 2);
        using var output = Open(manual);
        output.Paused = true;
        output.Advance(4800);
        Assert.Equal(0f, output.DeviceStatus().PeakPlayed);
        output.Paused = false;
        output.Advance(4800);
        Assert.InRange(output.DeviceStatus().PeakPlayed, 0.49f, 0.51f);
    }
}
