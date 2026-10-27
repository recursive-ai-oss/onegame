using System.Runtime.InteropServices;

namespace OneGame.Audio.Tests;

/// <summary>
/// Runs the real ALSA backend against alsa-lib's own <c>file</c> PCM, which needs no sound card and no privileges: alsa-lib consumes every frame
/// and appends it to a file. That lets the test check the device side of the hand-over (frames written, frames played, bytes on disk),
/// not just the output's own state.
/// </summary>
public class AlsaFileDeviceTests
{
    private static bool WaitFor(Func<bool> condition, int milliseconds = 8000)
    {
        var end = DateTime.UtcNow.AddMilliseconds(milliseconds);
        while (DateTime.UtcNow < end)
        {
            if (condition())
            {
                return true;
            }

            Thread.Sleep(10);
        }

        return condition();
    }

    [Fact]
    public void FramesReachTheAlsaDeviceAndTheReportCountsThem()
    {
        if (!OperatingSystem.IsLinux() || !NativeLibrary.TryLoad("libasound.so.2", out var lib))
        {
            return;
        }

        NativeLibrary.Free(lib);
        var dir = Path.Combine(Path.GetTempPath(), "onegame-alsa-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "out.raw");
        try
        {
            var backend = new AlsaBackend();
            using var output = AudioOutput.Create(backend, new AudioDeviceOptions { DeviceName = $"file:{file},raw", SampleRate = 48_000, Channels = 2 }, OutputLimiterMode.Clamp);
            var core = new TestCore(output.Format.SampleRate, 2, 8, 4);
            output.Start(core);
            Assert.Equal(AudioOutputStatus.Running, output.Status);
            core.Play(new VoiceRequest { Source = core.LoadClip(new MemoryStream(TestCore.Constant(0.5f, 48_000))) });

            // The file PCM is not paced, so the audio thread runs ahead and the device can consume plenty of leading silence before the
            // voice starts. Wait for the peak as well as the frame count; the peak is sticky once a loud block has been consumed.
            Assert.True(WaitFor(() => output.DeviceStatus() is { FramesPlayed: > 24_000, PeakPlayed: > 0.2f }), "the device never took the tone: " + output.DeviceStatus());
            var s = output.DeviceStatus();
            Assert.Equal("alsa", s.Backend);
            Assert.True(s.FramesWritten >= s.FramesPlayed, s.ToString());
            Assert.InRange(s.PeakPlayed, 0.2f, 0.51f);
            Assert.Equal(0, s.AudioThreadAllocatedBytes);

            output.Dispose();
            var bytes = new FileInfo(file).Length;
            Assert.True(bytes >= 24_000 * 2 * 4, $"only {bytes} bytes reached the file");
            var data = File.ReadAllBytes(file);
            Assert.Contains(data, b => b != 0);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
