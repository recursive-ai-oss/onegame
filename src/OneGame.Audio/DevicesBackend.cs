using Dev = OneAudio.Devices;

namespace OneGame.Audio;

/// <summary>
/// An <see cref="IAudioBackend"/> that stands for one of the device package's backends. <see cref="AudioOutput"/> does not open sinks on it: it hands
/// the mixer to the package's output device, which owns the thread, the limiter, the counters and the reopening.
/// </summary>
internal sealed class DevicesBackend(Dev.IAudioDeviceBackend inner) : IAudioBackend
{
    public Dev.IAudioDeviceBackend Inner { get; } = inner;

    public string Name => Inner.Name;

    public bool IsManual => Inner.IsManual;

    public IReadOnlyList<AudioDeviceInfo> ListDevices()
    {
        var result = new List<AudioDeviceInfo>();
        foreach (var d in Inner.List(Dev.DeviceDirection.Output))
        {
            result.Add(new AudioDeviceInfo(d.Id, d.Name, d.IsDefault));
        }

        return result;
    }

    public bool TryOpen(AudioDeviceOptions options, out IAudioSink? sink, out string reason)
    {
        sink = null;
        reason = "This backend is driven by the device package; AudioOutput opens it.";
        return false;
    }
}

/// <summary>Linux output through ALSA, provided by the device package. Kept as a type of this assembly so code that named it keeps working.</summary>
public sealed class AlsaBackend : IAudioBackend
{
    private readonly DevicesBackend _backend = new(new Dev.AlsaBackend());

    /// <inheritdoc/>
    public string Name => _backend.Name;

    /// <inheritdoc/>
    public bool IsManual => false;

    /// <inheritdoc/>
    public IReadOnlyList<AudioDeviceInfo> ListDevices() => _backend.ListDevices();

    /// <inheritdoc/>
    public bool TryOpen(AudioDeviceOptions options, out IAudioSink? sink, out string reason) => _backend.TryOpen(options, out sink, out reason);

    internal DevicesBackend Devices => _backend;
}
