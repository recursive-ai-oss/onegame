namespace OneGame.Platform;

/// <summary>Finds gamepads, maps them onto the standard layout, and plays rumble. Created by <see cref="PlatformApp"/>.</summary>
public sealed class GamepadManager : IDisposable
{
    private const long ScanIntervalMilliseconds = 500;

    private readonly PlatformApp _app;
    private readonly bool _devicesEnabled;
    private readonly List<EvdevDevice> _evdev = [];
    private readonly HashSet<string> _rejected = [];
    private readonly Dictionary<int, GamepadState> _synthetic = [];
    private XInputGamepads? _xinput;
    private Cocoa.CocoaGamepads? _cocoa;
    private long _nextScan;
    private int _nextDeviceId = 1000;

    internal GamepadManager(PlatformApp app, bool devicesEnabled)
    {
        _app = app;
        _devicesEnabled = devicesEnabled;
        if (devicesEnabled && OperatingSystem.IsWindows())
        {
            _xinput = new XInputGamepads(app, () => _nextDeviceId++);
        }

        if (devicesEnabled && OperatingSystem.IsMacOS())
        {
            _cocoa = new Cocoa.CocoaGamepads(app, () => _nextDeviceId++);
        }
    }

    /// <summary>Mappings used to find a device's layout. Devices with no entry get the kernel's standard layout on Linux. Add database text here before pads are plugged in, or call <see cref="Rescan"/> after.</summary>
    public GamepadMappingDatabase Mappings { get; } = new();

    internal string DeviceDirectory { get; set; } = "/dev/input";

    /// <summary>Forgets devices that failed to open and looks for gamepads again on the next <see cref="PlatformApp.PumpEvents"/>.</summary>
    public void Rescan()
    {
        _rejected.Clear();
        _nextScan = 0;
    }

    /// <summary>
    /// Plays rumble on a gamepad for a while. Strengths run from 0 to 1: <paramref name="low"/> is the heavy low-frequency motor,
    /// <paramref name="high"/> the light one. Returns false when the device cannot rumble.
    /// </summary>
    public bool Rumble(int gamepadId, float low, float high, int durationMilliseconds)
    {
        foreach (var d in _evdev)
        {
            if (d.Pad?.Id == gamepadId)
            {
                return d.Rumble(low, high, durationMilliseconds);
            }
        }

        return _xinput?.Rumble(gamepadId, low, high, durationMilliseconds) ?? false;
    }

    internal void RegisterSynthetic(int id, string name)
    {
        _synthetic[id] = _app.Input.AddGamepad(id, name, isMapped: true, canRumble: false);
        _app.Post(new PlatformEvent { Kind = PlatformEventKind.GamepadConnected, GamepadId = id });
    }

    internal void Poll()
    {
        if (!_devicesEnabled)
        {
            return;
        }

        if (OperatingSystem.IsLinux())
        {
            var now = Environment.TickCount64;
            if (now >= _nextScan)
            {
                _nextScan = now + ScanIntervalMilliseconds;
                Scan();
            }

            for (var i = _evdev.Count - 1; i >= 0; i--)
            {
                if (!_evdev[i].Drain())
                {
                    Remove(i);
                }
            }
        }

        _xinput?.Poll();
        _cocoa?.Poll();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var d in _evdev)
        {
            d.Dispose();
        }

        _evdev.Clear();
        _xinput = null;
        _cocoa = null;
    }

    private void Scan()
    {
        string[] paths;
        try
        {
            paths = Directory.GetFiles(DeviceDirectory, "event*");
        }
        catch (IOException)
        {
            return;
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }

        foreach (var path in paths)
        {
            if (_rejected.Contains(path) || _evdev.Exists(d => d.Path == path))
            {
                continue;
            }

            var device = EvdevDevice.TryOpen(path);
            if (device is null)
            {
                // Keyboards, mice and unreadable nodes are not retried until the next Rescan.
                _rejected.Add(path);
                continue;
            }

            Add(device);
        }
    }

    private void Add(EvdevDevice device)
    {
        var id = _nextDeviceId++;
        var startup = new List<PlatformEvent>();
        var pad = new EvdevGamepad(id, device.Capabilities, Mappings, _app.Options.StickDeadZone, _app.Options.TriggerDeadZone, startup.Add);
        device.Pad = pad;
        _evdev.Add(device);
        _app.Input.AddGamepad(id, device.Capabilities.Name, pad.Mapped, device.Capabilities.CanRumble);
        _app.Post(new PlatformEvent { Kind = PlatformEventKind.GamepadConnected, GamepadId = id });

        // Resting positions found while attaching (a stick held off-centre) arrive after the connect event.
        foreach (var e in startup)
        {
            _app.Post(e);
        }

        pad.SetSink(e => _app.Post(e));
    }

    private void Remove(int index)
    {
        var device = _evdev[index];
        _evdev.RemoveAt(index);
        if (device.Pad is { } pad)
        {
            _app.Post(new PlatformEvent { Kind = PlatformEventKind.GamepadDisconnected, GamepadId = pad.Id });
        }

        device.Dispose();
    }
}
