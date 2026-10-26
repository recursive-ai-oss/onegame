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

    private static string HerePath([System.Runtime.CompilerServices.CallerFilePath] string here = "") => here;

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
            Device = new AudioDeviceOptions { LatencyMilliseconds = 40 },
        });
        var output = service.Output;
        Report($"status={output.Status} error={output.LastError}");
        Assert.Equal(AudioOutputStatus.Running, output.Status);

        var rate = service.Format.SampleRate;
        Report($"format={rate}Hz x{service.Format.Channels} period={output.PeriodFrames} buffer={output.BufferFrames} frames");

        // A quiet Vorbis tone (44.1 kHz, converted to the device rate), then a ten-second (default) soak: a quiet looping Vorbis stream
        // with a pitched Opus clip started every 100 ms.
        var dir = Path.Combine(Path.GetDirectoryName(HerePath())!, "..", "..", "testdata", "audio");
        service.Play(service.LoadSound(File.OpenRead(Path.Combine(dir, "tone44k.ogg"))), new PlayOptions { Volume = 0.15f });
        Thread.Sleep(700);

        service.PlayMusic(File.OpenRead(Path.Combine(dir, "tone44k.ogg")), new MusicOptions { Volume = 0.05f });
        var blip = service.LoadSound(File.OpenRead(Path.Combine(dir, "loop48k.opus")));
        var seconds = int.Parse(Environment.GetEnvironmentVariable("ONEGAME_AUDIO_SOAK_SECONDS") ?? "10", CultureInfo.InvariantCulture);
        var end = DateTime.UtcNow.AddSeconds(seconds);
        var latency = new List<double>();
        var n = 0;
        while (DateTime.UtcNow < end)
        {
            service.Play(blip, new PlayOptions { Volume = 0.1f, Pitch = 0.8f + ((n++ % 5) * 0.1f) });
            service.Update();
            latency.Add(output.LatencyMilliseconds);
            Thread.Sleep(100);
        }

        Report($"latency_ms min={latency.Min():F1} avg={latency.Average():F1} max={latency.Max():F1}");
        Report($"underruns={output.Underruns} losses={output.DeviceLosses} faults={output.RenderFaults} audioThreadAllocatedBytes={output.AudioThreadAllocatedBytes} framesRendered={output.FramesRendered}");
        var device = output.DeviceStatus();
        Report($"device written={device.FramesWritten} played={device.FramesPlayed} peak={device.PeakPlayed:F3} state={device.DeviceState}");
        Assert.True(device.FramesWritten > rate * (seconds - 1), "no frames reached the device");
        Assert.True(device.FramesPlayed > rate * (seconds - 2), "the device took no frames");
        Assert.True(device.PeakPlayed > 0.01f, "the device played only silence");
        Assert.Equal(0, output.Underruns);
        Assert.Equal(0, output.AudioThreadAllocatedBytes);
        Assert.Equal(0, output.DeviceLosses);
        Assert.True(output.FramesRendered > rate * (seconds - 1));
    }
}
