using System.Text;

namespace OneGame.Platform.Tests;

/// <summary>A virtual Xbox 360 pad made through /dev/uinput, so the real evdev path (device scan, ioctl probing, event reads) is exercised without hardware.</summary>
internal sealed unsafe class UinputPad : IDisposable
{
    private const uint UiSetEvBit = 0x40045564, UiSetKeyBit = 0x40045565, UiSetAbsBit = 0x40045567, UiDevCreate = 0x5501, UiDevDestroy = 0x5502;
    private int _fd;

    private UinputPad(int fd, string name)
    {
        _fd = fd;
        Name = name;
    }

    public string Name { get; }

    public static UinputPad? TryCreate(string name)
    {
        var fd = Libc.OpenNonBlocking("/dev/uinput", write: true);
        if (fd < 0)
        {
            return null;
        }

        void Set(uint request, int value) => Libc.Ioctl(fd, request, (void*)value);
        Set(UiSetEvBit, 1);
        Set(UiSetEvBit, 3);
        foreach (var code in new[] { 0x130, 0x131, 0x133, 0x134, 0x136, 0x137, 0x13a, 0x13b, 0x13c, 0x13d, 0x13e })
        {
            Set(UiSetKeyBit, code);
        }

        // struct uinput_user_dev: name[80], input_id {bus, vendor, product, version}, ff_effects_max, then absmax/absmin/absfuzz/absflat[64].
        var dev = new byte[80 + 8 + 4 + (64 * 4 * 4)];
        Encoding.ASCII.GetBytes(name).CopyTo(dev, 0);
        BitConverter.TryWriteBytes(dev.AsSpan(80), (ushort)3);
        BitConverter.TryWriteBytes(dev.AsSpan(82), (ushort)0x045e);
        BitConverter.TryWriteBytes(dev.AsSpan(84), (ushort)0x028e);
        BitConverter.TryWriteBytes(dev.AsSpan(86), (ushort)0x0114);
        foreach (var (code, min, max) in new[] { (0, -32768, 32767), (1, -32768, 32767), (2, 0, 255), (3, -32768, 32767), (4, -32768, 32767), (5, 0, 255), (0x10, -1, 1), (0x11, -1, 1) })
        {
            Set(UiSetAbsBit, code);
            BitConverter.TryWriteBytes(dev.AsSpan(92 + (code * 4)), max);
            BitConverter.TryWriteBytes(dev.AsSpan(92 + (64 * 4) + (code * 4)), min);
        }

        fixed (byte* p = dev)
        {
            if (Libc.Write(fd, p, (nuint)dev.Length) != dev.Length || Libc.Ioctl(fd, UiDevCreate, null) < 0)
            {
                _ = Libc.Close(fd);
                return null;
            }
        }

        return new UinputPad(fd, name);
    }

    public void Emit(int type, int code, int value)
    {
        Span<byte> e = stackalloc byte[24];
        e.Clear();
        BitConverter.TryWriteBytes(e[16..], (ushort)type);
        BitConverter.TryWriteBytes(e[18..], (ushort)code);
        BitConverter.TryWriteBytes(e[20..], value);
        fixed (byte* p = e)
        {
            Libc.Write(_fd, p, 24);
        }
    }

    public void Sync() => Emit(0, 0, 0);

    public void Dispose()
    {
        if (_fd >= 0)
        {
            Libc.Ioctl(_fd, UiDevDestroy, null);
            _ = Libc.Close(_fd);
            _fd = -1;
        }
    }
}

public class UinputGamepadTests
{
    [Fact]
    public void VirtualPadIsFoundMappedAndStreamsEventsThroughTheRealEvdevPath()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "evdev is Linux only");
        using var pad = UinputPad.TryCreate("OneGame Test Pad");
        Assert.SkipUnless(pad is not null, "/dev/uinput is not writable here");

        using var app = PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Headless });
        app.Gamepads.Mappings.AddText(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "testdata", "gamecontrollerdb-subset.txt")), "Linux");

        // The device node appears a moment after creation (udev sets its permissions), so keep scanning for a while.
        var events = new List<PlatformEvent>();
        var found = false;
        for (var attempt = 0; attempt < 40 && !found; attempt++)
        {
            app.Gamepads.Rescan();
            Pump.For(app, 100, events);
            found = events.Any(e => e.Kind == PlatformEventKind.GamepadConnected);
        }

        Assert.SkipUnless(found, "the virtual device node could not be opened (no permission on /dev/input)");
        var connected = events.First(e => e.Kind == PlatformEventKind.GamepadConnected);
        var state = app.Input.GetGamepad(connected.GamepadId)!;
        Assert.True(state.IsMapped);
        Assert.Equal("OneGame Test Pad", state.Name);

        pad!.Emit(1, 0x130, 1);
        pad.Sync();
        pad.Emit(3, 0x10, 1);
        pad.Sync();
        pad.Emit(3, 0, 32767);
        pad.Sync();
        events.Clear();
        Pump.Until(app, ev => ev.Any(e => e.GamepadAxis == GamepadAxis.LeftX && e.Kind == PlatformEventKind.GamepadAxisMoved), 3000, events);

        Assert.Contains(events, e => e is { Kind: PlatformEventKind.GamepadButtonDown, GamepadButton: GamepadButton.South });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.GamepadButtonDown, GamepadButton: GamepadButton.DPadRight });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.GamepadAxisMoved, GamepadAxis: GamepadAxis.LeftX, Value: > 0.99f });
        Assert.True(state.IsDown(GamepadButton.South));
        Assert.True(state.GetAxis(GamepadAxis.LeftX) > 0.99f);
    }
}
