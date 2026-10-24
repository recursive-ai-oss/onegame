namespace OneGame.Platform.Tests;

/// <summary>The Win32 message translation and the key tables are pure logic, so they run here without Windows.</summary>
public class Win32TranslatorTests
{
    private sealed class Sink : IWin32MessageSink
    {
        public List<PlatformEvent> Events { get; } = [];

        public List<string> Calls { get; } = [];

        public int WindowId => 3;

        public KeyModifiers Modifiers { get; set; }

        public void Post(in PlatformEvent e) => Events.Add(e);

        public void Resized(int width, int height, bool minimized) => Calls.Add($"size {width}x{height} min={minimized}");

        public void DpiChanged(int dpi) => Calls.Add($"dpi {dpi}");

        public void FocusChanged(bool focused) => Calls.Add($"focus {focused}");

        public void BeginMouseTracking() => Calls.Add("track");
    }

    // lParam of WM_KEYDOWN: repeat count in bits 0-15, scan code 16-23, extended flag 24, previous state 30.
    private static nint KeyParam(int scan, bool extended = false, bool wasDown = false) =>
        1 | (scan << 16) | (extended ? 1 << 24 : 0) | (wasDown ? 1 << 30 : 0);

    private static nint Pos(int x, int y) => (nint)(((y & 0xFFFF) << 16) | (x & 0xFFFF));

    [Fact]
    public void KeysUseScanCodesNotVirtualKeysAndReportRepeat()
    {
        var sink = new Sink { Modifiers = KeyModifiers.Shift };
        var t = new Win32Translator(sink);

        Assert.True(t.Translate(Win32Translator.WmKeyDown, 0x41, KeyParam(0x1E)));
        Assert.True(t.Translate(Win32Translator.WmKeyDown, 0x41, KeyParam(0x1E, wasDown: true)));
        Assert.True(t.Translate(Win32Translator.WmKeyUp, 0x41, KeyParam(0x1E)));
        t.Translate(Win32Translator.WmKeyDown, 0x25, KeyParam(0x4B, extended: true)); // left arrow
        t.Translate(Win32Translator.WmKeyDown, 0x25, KeyParam(0x4B)); // numpad 4 with Num Lock off sends the same VK without the extended flag
        t.Translate(Win32Translator.WmKeyDown, 0x13, KeyParam(0x45)); // Pause reports scan code 0x45, the Num Lock code

        Assert.Equal(
            [Key.A, Key.A, Key.A, Key.ArrowLeft, Key.Keypad4, Key.Pause],
            sink.Events.Select(e => e.Key));
        Assert.Equal([false, true, false], sink.Events.Take(3).Select(e => e.IsRepeat));
        Assert.Equal(PlatformEventKind.KeyUp, sink.Events[2].Kind);
        Assert.All(sink.Events, e => Assert.Equal(KeyModifiers.Shift, e.Modifiers));
        Assert.All(sink.Events, e => Assert.Equal(3, e.WindowId));
    }

    [Fact]
    public void AltKeysStillReachTheDefaultProcedureSoAltF4Works()
    {
        var sink = new Sink();
        var t = new Win32Translator(sink);
        Assert.False(t.Translate(Win32Translator.WmSysKeyDown, 0x73, KeyParam(0x3E)));
        Assert.Equal(Key.F4, sink.Events.Single().Key);
    }

    [Fact]
    public void CharMessagesBecomeTextIncludingSurrogatePairs()
    {
        var sink = new Sink();
        var t = new Win32Translator(sink);

        t.Translate(Win32Translator.WmChar, 'a', 0);
        t.Translate(Win32Translator.WmChar, '\r', 0); // Enter is a key, not text
        t.Translate(Win32Translator.WmChar, 0x1B, 0);
        t.Translate(Win32Translator.WmChar, 0xD83D, 0); // U+1F600 as a surrogate pair
        t.Translate(Win32Translator.WmChar, 0xDE00, 0);
        t.Translate(Win32Translator.WmChar, 0xDE00, 0); // a stray low surrogate is dropped
        t.Translate(Win32Translator.WmChar, 'é', 0);

        Assert.Equal(["a", "\U0001F600", "é"], sink.Events.Select(e => e.Text));
    }

    [Fact]
    public void PointerMessagesCarryClientPixelsAndSignedCoordinates()
    {
        var sink = new Sink();
        var t = new Win32Translator(sink);

        t.Translate(Win32Translator.WmMouseMove, 0, Pos(50, 60));
        t.Translate(Win32Translator.WmMouseMove, 0, Pos(-5, 7)); // a captured pointer can leave the window to the left
        t.Translate(Win32Translator.WmLButtonDown, 1, Pos(50, 60));
        t.Translate(Win32Translator.WmLButtonUp, 0, Pos(50, 60));
        t.Translate(Win32Translator.WmRButtonDown, 2, Pos(1, 2));
        t.Translate(Win32Translator.WmMButtonUp, 0, Pos(1, 2));
        t.Translate(Win32Translator.WmXButtonDown, (nuint)(1 << 16), Pos(1, 2));
        t.Translate(Win32Translator.WmXButtonUp, (nuint)(2 << 16), Pos(1, 2));
        t.Translate(Win32Translator.WmMouseLeave, 0, 0);

        Assert.Equal(["track"], sink.Calls);
        Assert.Equal(PlatformEventKind.PointerInside, sink.Events[0].Kind);
        Assert.True(sink.Events[0].Focused);
        Assert.Equal((50f, 60f), (sink.Events[1].X, sink.Events[1].Y));
        Assert.Equal((-5f, 7f), (sink.Events[2].X, sink.Events[2].Y));
        Assert.Equal([MouseButton.Left, MouseButton.Left, MouseButton.Right, MouseButton.Middle, MouseButton.Back, MouseButton.Forward], sink.Events.Skip(3).Take(6).Select(e => e.Button));
        Assert.Equal(PlatformEventKind.PointerButtonDown, sink.Events[3].Kind);
        Assert.Equal(PlatformEventKind.PointerButtonUp, sink.Events[4].Kind);
        Assert.Equal(PlatformEventKind.PointerInside, sink.Events[^1].Kind);
        Assert.False(sink.Events[^1].Focused);
    }

    [Fact]
    public void WheelMessagesBecomeNotchesWithPositiveAwayFromTheUser()
    {
        var sink = new Sink();
        var t = new Win32Translator(sink);
        t.Translate(Win32Translator.WmMouseWheel, (nuint)(120 << 16), 0);
        t.Translate(Win32Translator.WmMouseWheel, (nuint)((uint)unchecked((ushort)-240) << 16), 0);
        t.Translate(Win32Translator.WmMouseHWheel, (nuint)(60 << 16), 0);

        Assert.Equal((0f, 1f), (sink.Events[0].DeltaX, sink.Events[0].DeltaY));
        Assert.Equal((0f, -2f), (sink.Events[1].DeltaX, sink.Events[1].DeltaY));
        Assert.Equal((0.5f, 0f), (sink.Events[2].DeltaX, sink.Events[2].DeltaY));
    }

    [Fact]
    public void WindowMessagesReachTheWindowNotTheEventQueue()
    {
        var sink = new Sink();
        var t = new Win32Translator(sink);
        Assert.True(t.Translate(Win32Translator.WmSize, 0, Pos(800, 600)));
        Assert.True(t.Translate(Win32Translator.WmSize, 1, Pos(0, 0)));
        Assert.True(t.Translate(Win32Translator.WmDpiChanged, (nuint)((144 << 16) | 144), 0));
        Assert.True(t.Translate(Win32Translator.WmSetFocus, 0, 0));
        Assert.True(t.Translate(Win32Translator.WmKillFocus, 0, 0));
        Assert.True(t.Translate(Win32Translator.WmClose, 0, 0));
        Assert.False(t.Translate(0x0014 /* WM_ERASEBKGND */, 0, 0));

        Assert.Equal(["size 800x600 min=False", "size 0x0 min=True", "dpi 144", "focus True", "focus False"], sink.Calls);
        Assert.Equal(PlatformEventKind.WindowCloseRequested, sink.Events.Single().Kind);
    }

    [Fact]
    public void RawMouseInputGivesRelativeMotionOnly()
    {
        var raw = new byte[48];
        BitConverter.TryWriteBytes(raw.AsSpan(0), 0u); // RIM_TYPEMOUSE
        BitConverter.TryWriteBytes(raw.AsSpan(24 + 12), 15);
        BitConverter.TryWriteBytes(raw.AsSpan(24 + 16), -7);
        Assert.True(Win32Translator.TryReadRawMouseDelta(raw, out var dx, out var dy));
        Assert.Equal((15, -7), (dx, dy));

        BitConverter.TryWriteBytes(raw.AsSpan(24), (ushort)1); // MOUSE_MOVE_ABSOLUTE
        Assert.False(Win32Translator.TryReadRawMouseDelta(raw, out _, out _));
        BitConverter.TryWriteBytes(raw.AsSpan(0), 1u); // keyboard
        Assert.False(Win32Translator.TryReadRawMouseDelta(raw, out _, out _));
        Assert.False(Win32Translator.TryReadRawMouseDelta(raw.AsSpan(0, 10), out _, out _));
    }

    [Fact]
    public void ScanCodeTablesAgreeBetweenWindowsAndLinux()
    {
        // Windows set-1 scan codes below 0x59 coincide with Linux key codes for the main block, so both tables must give the same key.
        for (var code = 1; code < 0x59; code++)
        {
            var win = KeyTables.FromWindowsScan(code, extended: false);
            var linux = KeyTables.FromEvdev(code);
            if (win != Key.Unknown && linux != Key.Unknown)
            {
                Assert.Equal(linux, win);
            }
        }

        Assert.Equal(Key.RightControl, KeyTables.FromWindowsScan(0x1D, true));
        Assert.Equal(Key.LeftControl, KeyTables.FromWindowsScan(0x1D, false));
        Assert.Equal(Key.KeypadEnter, KeyTables.FromWindowsScan(0x1C, true));
        Assert.Equal(Key.Enter, KeyTables.FromWindowsScan(0x1C, false));
        Assert.Equal(Key.RightAlt, KeyTables.FromWindowsScan(0x38, true));
        Assert.Equal(Key.Delete, KeyTables.FromWindowsScan(0x53, true));
        Assert.Equal(Key.KeypadDecimal, KeyTables.FromWindowsScan(0x53, false));
        Assert.Equal(Key.Q, KeyTables.FromEvdev(16));
        Assert.Equal(Key.RightMeta, KeyTables.FromEvdev(126));
    }
}

public class XInputPadTests
{
    [Fact]
    public void XInputStateMapsToTheStandardLayoutWithYDownAndTriggersFromZero()
    {
        var events = new List<PlatformEvent>();
        var pad = new XInputPad(9, 0.1f, 0.03f, events.Add);

        pad.Update(0x1000 | 0x0001 | 0x0010, 0, 0, 0, 0, 0, 0); // A, dpad up, start
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.GamepadButtonDown, GamepadButton: GamepadButton.South, GamepadId: 9 });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.GamepadButtonDown, GamepadButton: GamepadButton.DPadUp });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.GamepadButtonDown, GamepadButton: GamepadButton.Start });
        Assert.DoesNotContain(events, e => e.Kind == PlatformEventKind.GamepadAxisMoved);

        events.Clear();
        pad.Update(0x2000, 255, 0, 32767, 32767, 0, -32768); // B only; left trigger full; left stick right+up; right stick down
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.GamepadButtonUp, GamepadButton: GamepadButton.South });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.GamepadButtonDown, GamepadButton: GamepadButton.East });
        Assert.Contains(events, e => e is { GamepadAxis: GamepadAxis.LeftTrigger, Value: > 0.99f });
        Assert.Contains(events, e => e is { GamepadAxis: GamepadAxis.LeftX, Value: > 0f });
        Assert.Contains(events, e => e is { GamepadAxis: GamepadAxis.LeftY, Value: < 0f }); // XInput's up is negative here
        Assert.Contains(events, e => e is { GamepadAxis: GamepadAxis.RightY, Value: > 0f });
        Assert.DoesNotContain(events, e => e.GamepadAxis == GamepadAxis.RightTrigger);
    }
}
