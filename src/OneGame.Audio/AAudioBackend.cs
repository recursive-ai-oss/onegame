using Dev = OneAudio.Devices;

namespace OneGame.Audio;

/// <summary>
/// Android output through AAudio (Android 8.0 and later), provided by the device package: shared low-latency float streams driven by the system's
/// data callback, with the device clock, xrun counts and reopening on a route change reported by the package.
/// </summary>
public sealed class AAudioBackend : IAudioBackend, IDevicesHolder
{
    private readonly DevicesBackend _backend = new(new Dev.AAudioBackend());

    /// <inheritdoc/>
    public string Name => _backend.Name;

    /// <inheritdoc/>
    public bool IsManual => false;

    /// <inheritdoc/>
    public IReadOnlyList<AudioDeviceInfo> ListDevices() => _backend.ListDevices();

    /// <inheritdoc/>
    public bool TryOpen(AudioDeviceOptions options, out IAudioSink? sink, out string reason) => _backend.TryOpen(options, out sink, out reason);

    DevicesBackend IDevicesHolder.Devices => _backend;
}
