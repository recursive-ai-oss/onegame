using System.Globalization;

namespace OneGame.Audio.Tests;

/// <summary>
/// Plays through the real operating-system backend. Skipped unless ONEGAME_AUDIO_DEVICE_TESTS=1, because a test machine may have no sound
/// hardware or a person nearby. ONEGAME_AUDIO_SOAK_SECONDS sets the soak length (default 10); ONEGAME_AUDIO_REPORT names a file for the numbers.
/// </summary>
public class RealDeviceTests
{
    private static bool Enabled => Environment.GetEnvironmentVariable("ONEGAME_AUDIO_DEVICE_TESTS") == "1";

    private static void Report(string text)
    {
        var path = Environment.GetEnvironmentVariable("ONEGAME_AUDIO_REPORT");
        if (!string.IsNullOrEmpty(path))
        {
            File.AppendAllText(path, text + Environment.NewLine);
        }

        Console.Error.WriteLine(text);
    }

    [Fact]
    public void ToneThenSoakOnTheRealDevice()
    {
        if (!Enabled)
        {
            return;
        }

        var backend = AudioService.DefaultBackend();
        var devices = backend.ListDevices();
        Report($"backend={backend.Name} devices={devices.Count} first={(devices.Count > 0 ? devices[0].Name : string.Empty)}");

        using var service = AudioService.Create(new AudioServiceOptions
        {
            Backend = backend,
            CoreFactory = TestCore.Factory,
            Device = new AudioDeviceOptions { LatencyMilliseconds = 40 },
        });
        var output = service.Output;
        Report($"status={output.Status} error={output.LastError}");
        Assert.Equal(AudioOutputStatus.Running, output.Status);

        var rate = service.Format.SampleRate;
        Report($"format={rate}Hz x{service.Format.Channels} period={output.PeriodFrames} buffer={output.BufferFrames} frames");

        // A quiet half-second A440, then a ten-second (default) soak of a barely audible loop with a sound started every 100 ms.
        var tone = service.LoadSound(new MemoryStream(TestCore.Sine(rate, 440, 0.05f, rate / 2)));
        service.Play(tone);
        Thread.Sleep(700);

        var soak = service.LoadSound(new MemoryStream(TestCore.Sine(rate, 220, 0.02f, rate / 10)));
        var seconds = int.Parse(Environment.GetEnvironmentVariable("ONEGAME_AUDIO_SOAK_SECONDS") ?? "10", CultureInfo.InvariantCulture);
        var end = DateTime.UtcNow.AddSeconds(seconds);
        var latency = new List<double>();
        while (DateTime.UtcNow < end)
        {
            service.Play(soak);
            service.Update();
            latency.Add(output.LatencyMilliseconds);
            Thread.Sleep(100);
        }

        Report($"latency_ms min={latency.Min():F1} avg={latency.Average():F1} max={latency.Max():F1}");
        Report($"underruns={output.Underruns} losses={output.DeviceLosses} faults={output.RenderFaults} audioThreadAllocatedBytes={output.AudioThreadAllocatedBytes} framesRendered={output.FramesRendered}");
        Assert.Equal(0, output.Underruns);
        Assert.Equal(0, output.AudioThreadAllocatedBytes);
        Assert.Equal(0, output.DeviceLosses);
        Assert.True(output.FramesRendered > rate * (seconds - 1));
    }
}
