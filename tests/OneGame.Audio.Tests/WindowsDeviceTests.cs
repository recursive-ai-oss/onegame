namespace OneGame.Audio.Tests;

/// <summary>
/// WASAPI through the device package, on Windows only. A CI runner may have no sound device (hosted runners usually have none), so the
/// open and the counters are checked only when an output device exists; enumeration always runs and the test says which case it was.
/// </summary>
public class WindowsDeviceTests
{
    private sealed class Tone : IAudioRenderSource
    {
        public int SampleRate => 48_000;

        public int Channels => 2;

        public void Render(Span<float> interleaved) => interleaved.Fill(0.25f);
    }

    [Fact]
    public void WasapiEnumeratesAndPlaysWhenAnOutputDeviceExists()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip("WASAPI exists on Windows only.");
        }

        var devices = new WasapiBackend().ListDevices();
        if (devices.Count == 0)
        {
            Assert.Skip("This machine has no WASAPI output device (a hosted CI runner without audio hardware). Enumeration ran and returned none.");
        }

        using var output = AudioOutput.Create(new WasapiBackend(), new AudioDeviceOptions { LatencyMilliseconds = 40 }, OutputLimiterMode.Clamp);
        output.Start(new Tone());
        Assert.Equal(AudioOutputStatus.Running, output.Status);
        var end = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < end && output.DeviceStatus().FramesPlayed < 9600)
        {
            Thread.Sleep(20);
        }

        var s = output.DeviceStatus();
        Assert.True(s.FramesWritten > 0, s.ToString());
        Assert.True(s.FramesPlayed > 0, s.ToString());
        Assert.True(s.PeakPlayed > 0.2f, s.ToString());
    }
}
