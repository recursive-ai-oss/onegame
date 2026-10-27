using OneAudio.Loudness;
using Dev = OneAudio.Devices;

namespace OneGame.Audio;

// The device-package mode of AudioOutput: the same members, answered from OneAudio.Devices' output device instead of our own thread and sink.
public sealed partial class AudioOutput
{
    private Dev.IAudioDeviceBackend? _devBackend;
    private Dev.AudioOutputDevice? _dev;

    private static AudioOutputStatus StatusOf(Dev.DeviceState state) => state switch
    {
        Dev.DeviceState.Created => AudioOutputStatus.Created,
        Dev.DeviceState.NoDevice => AudioOutputStatus.NoDevice,
        Dev.DeviceState.Stopped => AudioOutputStatus.Stopped,
        _ => AudioOutputStatus.Running,
    };

    private void StartOnDevices(IAudioRenderSource source)
    {
        var options = new Dev.DeviceStreamOptions
        {
            DeviceId = _deviceName,
            SampleRate = Format.SampleRate,
            Channels = Format.Channels,
            LatencyMilliseconds = _options.LatencyMilliseconds,
            Limiter = _limiter == OutputLimiterMode.Clamp ? Dev.OutputLimiterMode.Clamp : Dev.OutputLimiterMode.SoftKnee,
            Metering = Metering && Format.SampleRate >= LoudnessMeter.MinSampleRate,
            MeasureRenderAllocations = true,
        };
        var device = Dev.AudioOutputDevice.Open(new DeviceSource(this, source), options, _devBackend!);
        device.Event += OnDeviceEvent;
        _dev = device;
        if (_devBackend is IPumpedBackend pumped)
        {
            pumped.SetPump(Pump);
        }

        device.Start();
    }

    private void OnDeviceEvent(Dev.DeviceEvent e)
    {
        // The system taking the device (a call, another app) is not our own Suspended; the service reads IsDeviceStopped for both.
        if (e.Kind == Dev.DeviceEventKind.Interrupted)
        {
            Volatile.Write(ref _systemSuspended, 1);
        }
        else if (e.Kind == Dev.DeviceEventKind.Resumed)
        {
            Volatile.Write(ref _systemSuspended, 0);
        }
    }

    private void ApplyDeviceSuspend()
    {
        if (_dev is not { } device)
        {
            return;
        }

        if (Suspended)
        {
            device.Suspend();
        }
        else
        {
            device.Resume();
        }
    }

    private void PumpDevice(Dev.AudioOutputDevice device)
    {
        if (_devBackend!.IsManual)
        {
            device.Pump();
        }

        if (_devBackend is IPumpedDeviceBackend pumped && !IsDeviceStopped)
        {
            pumped.PumpNow();
        }
    }

    private AudioDeviceStatus DeviceStatusOnDevices()
    {
        var d = _dev!;
        var c = d.Counters;
        return new AudioDeviceStatus(
            BackendName, Status.ToString(), d.State.ToString().ToLowerInvariant(), DeviceName, LastError, Suspended, Paused, c.BufferFrames, c.PeriodFrames, Math.Round(c.LatencyMilliseconds, 2),
            c.Underruns, c.Underruns, c.DeviceLosses, c.Reopens, Suspensions, c.FramesRendered, c.FramesWritten, c.FramesPlayed, c.PeakPlayed,
            c.RenderAllocatedBytes, d.Detail);
    }

    private void DisposeOnDevices()
    {
        lock (_lifecycle)
        {
            _dev?.Dispose();
            Volatile.Write(ref _status, (int)AudioOutputStatus.Stopped);
        }
    }

    // The package pulls from this on its device thread. Pause feeds silence and holds the mixer's clock, as the output always did.
    private sealed class DeviceSource(AudioOutput owner, IAudioRenderSource source) : OneAudio.IAudioSource
    {
        public int SampleRate => source.SampleRate;

        public int Channels => source.Channels;

        public void Render(Span<float> interleaved)
        {
            if (owner.Paused)
            {
                interleaved.Clear();
                return;
            }

            source.Render(interleaved);
        }
    }
}

/// <summary>Implemented by a device backend that is pumped by a timer on a page with no audio thread.</summary>
internal interface IPumpedDeviceBackend
{
    /// <summary>Renders the frames the device wants right now, on the caller's thread; returns how many.</summary>
    int PumpNow();
}
