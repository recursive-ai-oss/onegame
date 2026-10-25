using System.Diagnostics;

namespace OneGame.Audio.Tests;

public class OutputTests
{
    private static readonly float[] Impulse = [1f];

    private static bool WaitFor(Func<bool> condition, int milliseconds = 6000)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < milliseconds)
        {
            if (condition())
            {
                return true;
            }

            Thread.Sleep(5);
        }

        return condition();
    }

    private static AudioOutput Start(FakeBackend backend, out TestCore core, int rate = 48_000)
    {
        var output = AudioOutput.Create(backend, new AudioDeviceOptions { SampleRate = rate, Channels = 2 }, OutputLimiterMode.Clamp);
        core = new TestCore(output.Format.SampleRate, 2, 8, 4);
        output.Start(core);
        return output;
    }

    [Fact]
    public void RealTimeThreadFeedsTheDeviceAtPace()
    {
        var backend = new FakeBackend();
        using var output = Start(backend, out var core);
        Thread.Sleep(500);
        var frames = backend.TotalFrames;
        Assert.InRange(frames, 48_000 * 0.3, 48_000 * 0.8);
        Assert.Equal(AudioOutputStatus.Running, output.Status);
        Assert.Equal(0, output.Underruns);
        Assert.True(core.Timeline > 0);
    }

    [Fact]
    public void MixerFormatFollowsTheDevice()
    {
        var backend = new FakeBackend(44_100);
        using var output = AudioOutput.Create(backend, new AudioDeviceOptions { SampleRate = 48_000, Channels = 2 });
        Assert.Equal(new AudioDeviceFormat(44_100, 2), output.Format);
        var wrong = new TestCore(48_000, 2, 4, 4);
        Assert.Throws<ArgumentException>(() => output.Start(wrong));
    }

    [Fact]
    public void NoDeviceRunsSilentlyAtRealTimeAndNeverThrows()
    {
        var backend = new FakeBackend { Available = false };
        using var output = Start(backend, out var core);
        core.Play(new VoiceRequest { Source = Impulse });
        Thread.Sleep(400);
        Assert.Equal(AudioOutputStatus.NoDevice, output.Status);
        Assert.Equal("unplugged", output.LastError);
        // The clock keeps moving at about real time so the game behaves the same without sound.
        Assert.InRange(core.Timeline, 48_000 * 0.2, 48_000 * 0.8);
        Assert.True(output.SilentFrames > 0);
        Assert.Equal(0, output.BufferFrames);
    }

    [Fact]
    public void DeviceThatAppearsLaterIsPickedUp()
    {
        var backend = new FakeBackend { Available = false };
        using var output = Start(backend, out _);
        Thread.Sleep(100);
        backend.Available = true;
        Assert.True(WaitFor(() => output.Status == AudioOutputStatus.Running));
        Assert.Equal(1, output.Reopens);
        Assert.True(WaitFor(() => backend.TotalFrames > 4800));
    }

    [Fact]
    public void DeviceLossFallsBackToSilenceAndReconnects()
    {
        var backend = new FakeBackend();
        using var output = Start(backend, out var core);
        Assert.True(WaitFor(() => backend.TotalFrames > 4800));

        backend.Unplug();
        Assert.True(WaitFor(() => output.DeviceLosses == 1 && output.Status == AudioOutputStatus.NoDevice));
        var t0 = core.Timeline;
        Thread.Sleep(300);
        Assert.True(core.Timeline > t0, "the mixer clock must keep running while the device is gone");

        backend.Available = true;
        Assert.True(WaitFor(() => output.Status == AudioOutputStatus.Running && output.Reopens == 1));
        var frames = backend.Current!.Frames;
        Assert.True(WaitFor(() => backend.Current!.Frames > frames + 2400));
        Assert.Equal(0, output.RenderFaults);
    }

    [Fact]
    public void ReconnectingToADifferentRateIsRefused()
    {
        var backend = new FakeBackend(48_000);
        using var output = Start(backend, out _);
        Assert.True(WaitFor(() => backend.TotalFrames > 2400));
        backend.DeviceRate = 44_100;
        backend.Unplug();
        Assert.True(WaitFor(() => output.DeviceLosses == 1));
        backend.Available = true;
        Assert.True(WaitFor(() => output.LastError?.Contains("expected 48000", StringComparison.Ordinal) == true));
        Assert.Equal(AudioOutputStatus.NoDevice, output.Status);
        backend.DeviceRate = 48_000;
        Assert.True(WaitFor(() => output.Status == AudioOutputStatus.Running));
    }

    [Fact]
    public void SelectDeviceReopensWithTheNewName()
    {
        var backend = new FakeBackend();
        using var output = Start(backend, out _);
        Assert.True(WaitFor(() => backend.Opens == 1 && backend.TotalFrames > 2400));
        output.SelectDevice("fake0");
        Assert.True(WaitFor(() => backend.Opens == 2));
        Assert.Equal("fake0", backend.LastRequestedDevice);
        Assert.Equal("fake0", output.DeviceName);
        Assert.Equal(AudioOutputStatus.Running, output.Status);
    }

    private sealed class Throwing : IAudioRenderSource
    {
        public int SampleRate => 48_000;

        public int Channels => 2;

        public void Render(Span<float> interleaved) => throw new InvalidOperationException("bad mixer");
    }

    [Fact]
    public void ExceptionsInTheSourceNeverEscapeTheAudioThread()
    {
        var backend = new FakeBackend();
        using var output = AudioOutput.Create(backend, new AudioDeviceOptions());
        output.Start(new Throwing());
        Assert.True(WaitFor(() => output.RenderFaults > 5));
        Assert.Equal(AudioOutputStatus.Running, output.Status);
        Assert.Equal(0f, backend.Current!.Peak);
    }

    [Fact]
    public void PausedOutputStillFeedsTheDeviceButHoldsTheMixer()
    {
        var backend = new FakeBackend();
        using var output = Start(backend, out var core);
        Assert.True(WaitFor(() => core.Timeline > 2400));
        output.Paused = true;
        Thread.Sleep(50);
        var t = core.Timeline;
        var f = backend.TotalFrames;
        Thread.Sleep(200);
        Assert.Equal(t, core.Timeline);
        Assert.True(backend.TotalFrames > f);
    }

    [Fact]
    public void SteadyStateAudioThreadAllocatesNothing()
    {
        var backend = new FakeBackend();
        using var output = Start(backend, out var core);
        var tone = TestCore.Sine(48_000, 440, 0.2f, 4800);
        var clip = core.LoadClip(new MemoryStream(tone));
        core.Play(new VoiceRequest { Source = clip, Loop = true, Gain = 0.5f });
        Thread.Sleep(1500);
        Assert.True(backend.TotalFrames > 48_000);
        Assert.True(output.AudioThreadAllocatedBytes == 0, $"the audio thread allocated {output.AudioThreadAllocatedBytes} bytes");
        Assert.Equal(0, output.Underruns);
    }

    [Fact]
    public void StartTwiceOrAfterDisposeThrows()
    {
        var backend = new FakeBackend();
        var output = Start(backend, out var core);
        Assert.Throws<InvalidOperationException>(() => output.Start(core));
        output.Dispose();
        Assert.Equal(AudioOutputStatus.Stopped, output.Status);
        output.Dispose();
    }
}

public class BackendLogicTests
{
    [Fact]
    public void WasapiFormatBytesDescribeFloatExtensible()
    {
        var b = new byte[WasapiLogic.ExtensibleFormatBytes];
        WasapiLogic.WriteFloatFormat(b, 48_000, 2);
        Assert.Equal(0xFFFE, BitConverter.ToUInt16(b, 0));
        Assert.Equal(2, BitConverter.ToUInt16(b, 2));
        Assert.Equal(48_000u, BitConverter.ToUInt32(b, 4));
        Assert.Equal(48_000u * 8, BitConverter.ToUInt32(b, 8));
        Assert.Equal(8, BitConverter.ToUInt16(b, 12));
        Assert.Equal(32, BitConverter.ToUInt16(b, 14));
        Assert.Equal(22, BitConverter.ToUInt16(b, 16));
        Assert.Equal(3u, BitConverter.ToUInt32(b, 20));
        Assert.Equal(new Guid("00000003-0000-0010-8000-00aa00389b71"), new Guid(b.AsSpan(24, 16)));
    }

    [Theory]
    [InlineData(1, 0x4u)]
    [InlineData(2, 0x3u)]
    [InlineData(6, 0x3Fu)]
    [InlineData(3, 0u)]
    public void WasapiChannelMasks(int channels, uint mask) => Assert.Equal(mask, WasapiLogic.ChannelMask(channels));

    [Fact]
    public void WasapiArithmeticAndErrorClasses()
    {
        Assert.Equal(400_000L, WasapiLogic.ToReferenceTime(40));
        Assert.Equal(300, WasapiLogic.WritableFrames(1000, 700, 480));
        Assert.Equal(480, WasapiLogic.WritableFrames(1000, 0, 480));
        Assert.Equal(0, WasapiLogic.WritableFrames(1000, 1200, 480));
        Assert.True(WasapiLogic.IsDeviceLost(unchecked((int)0x88890004)));
        Assert.False(WasapiLogic.IsDeviceLost(0));
        Assert.False(WasapiLogic.IsDeviceLost(unchecked((int)0x88890002)));
    }

    [Fact]
    public void WasapiBackendOffWindowsFailsCleanly()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var ok = new WasapiBackend().TryOpen(new AudioDeviceOptions(), out var sink, out var reason);
        Assert.False(ok);
        Assert.Null(sink);
        Assert.False(string.IsNullOrEmpty(reason));
        Assert.Empty(new WasapiBackend().ListDevices());
    }

    [Fact]
    public void AlsaRefusesAnUnknownDeviceWithAReason()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var ok = new AlsaBackend().TryOpen(new AudioDeviceOptions { DeviceName = "onegame_no_such_device" }, out var sink, out var reason);
        Assert.False(ok);
        Assert.Null(sink);
        Assert.False(string.IsNullOrEmpty(reason));
    }

    [Fact]
    public void ServiceWithAnUnopenableDeviceStillRunsSilently()
    {
        using var s = AudioService.Create(new AudioServiceOptions
        {
            Backend = new FakeBackend { Available = false },
            CoreFactory = TestCore.Factory,
        });
        var clip = s.LoadSound(new MemoryStream(TestCore.Constant(0.5f, 100)));
        Assert.True(s.Play(clip).IsValid);
        s.Update();
        Thread.Sleep(200);
        Assert.Equal(AudioOutputStatus.NoDevice, s.Output.Status);
        Assert.Equal(0, s.Output.RenderFaults);
    }

    [Fact]
    public void DefaultServiceNeverThrowsWhateverTheHostHas()
    {
        // Uses the real backend for this OS with the default mixer: no device, a sound server, or a bare card are all fine.
        using var s = AudioService.Create(new AudioServiceOptions { Device = new AudioDeviceOptions { DeviceName = "onegame_no_such_device" } });
        s.Play(s.LoadSound(new MemoryStream(RealMixerTests.Wav(0, 10))));
        s.Update();

        // ALSA and WASAPI refuse an unknown device name. The Apple and Android backends route by the system default and ignore the name, so they run.
        var routesBySystem = OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsAndroid();
        Assert.True(s.Output.Status == AudioOutputStatus.NoDevice || (routesBySystem && s.Output.Status == AudioOutputStatus.Running), s.Output.Status.ToString());
    }
}
