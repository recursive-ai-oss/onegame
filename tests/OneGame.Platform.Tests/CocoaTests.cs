using System.Runtime.InteropServices;
using System.Diagnostics;
using OneGame.Platform.Cocoa;

namespace OneGame.Platform.Tests;

/// <summary>The parts of the Cocoa backend that are plain logic: key tables, text filtering and the controller mapping. They run on any host.</summary>
public class CocoaLogicTests
{
    [Theory]
    [InlineData(0x00, Key.A)]
    [InlineData(0x0D, Key.W)]
    [InlineData(0x01, Key.S)]
    [InlineData(0x02, Key.D)]
    [InlineData(0x12, Key.Digit1)]
    [InlineData(0x1D, Key.Digit0)]
    [InlineData(0x24, Key.Enter)]
    [InlineData(0x33, Key.Backspace)]
    [InlineData(0x35, Key.Escape)]
    [InlineData(0x31, Key.Space)]
    [InlineData(0x7E, Key.ArrowUp)]
    [InlineData(0x7A, Key.F1)]
    [InlineData(0x75, Key.Delete)]
    [InlineData(0x37, Key.LeftMeta)]
    [InlineData(0x52, Key.Keypad0)]
    [InlineData(0x7F, Key.Unknown)]
    [InlineData(0x200, Key.Unknown)]
    public void VirtualKeyCodesNameThePhysicalKey(int code, Key expected) => Assert.Equal(expected, CocoaKeys.FromVirtualKey(code));

    [Fact]
    public void EveryLetterAndDigitKeyIsReachable()
    {
        var seen = new HashSet<Key>();
        for (var code = 0; code < 0x80; code++)
        {
            seen.Add(CocoaKeys.FromVirtualKey(code));
        }

        for (var k = Key.A; k <= Key.Z; k++)
        {
            Assert.Contains(k, seen);
        }

        for (var k = Key.Digit1; k <= Key.Digit0; k++)
        {
            Assert.Contains(k, seen);
        }
    }

    [Fact]
    public void ModifierFlagsBecomeKeyModifiers()
    {
        Assert.Equal(KeyModifiers.Shift | KeyModifiers.Meta, CocoaKeys.ModifiersFromFlags((1UL << 17) | (1UL << 20)));
        Assert.Equal(KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.CapsLock, CocoaKeys.ModifiersFromFlags((1UL << 18) | (1UL << 19) | (1UL << 16)));
        Assert.Equal(KeyModifiers.None, CocoaKeys.ModifiersFromFlags(0));
    }

    [Fact]
    public void ModifierKeysAreToldApartBySide()
    {
        // Both shifts down, then the left one released: the general bit stays set for the right one, the device bits say which.
        const ulong shift = 1UL << 17;
        Assert.True(CocoaKeys.IsDown(Key.LeftShift, shift | 0x2 | 0x4));
        Assert.True(CocoaKeys.IsDown(Key.RightShift, shift | 0x2 | 0x4));
        Assert.False(CocoaKeys.IsDown(Key.LeftShift, shift | 0x4));
        Assert.True(CocoaKeys.IsDown(Key.RightShift, shift | 0x4));
        Assert.False(CocoaKeys.IsDown(Key.RightControl, 0x2000 ^ 0x2000));

        // A source that sets only the general bit is read from it.
        Assert.True(CocoaKeys.IsDown(Key.LeftMeta, 1UL << 20));
        Assert.False(CocoaKeys.IsDown(Key.LeftMeta, 0));
        Assert.True(CocoaKeys.IsDown(Key.CapsLock, 1UL << 16));
    }

    [Theory]
    [InlineData("a", KeyModifiers.None, "a")]
    [InlineData("A", KeyModifiers.Shift, "A")]
    [InlineData("é", KeyModifiers.Alt, "é")]
    [InlineData("😀", KeyModifiers.None, "😀")]
    [InlineData("c", KeyModifiers.Meta, null)]
    [InlineData("c", KeyModifiers.Control, null)]
    [InlineData("\r", KeyModifiers.None, null)]
    [InlineData("\u001b", KeyModifiers.None, null)]
    [InlineData("\u007f", KeyModifiers.None, null)]
    [InlineData("\uF700", KeyModifiers.None, null)]
    [InlineData("", KeyModifiers.None, null)]
    public void TypedTextDropsShortcutsAndControlCodes(string text, KeyModifiers modifiers, string? expected) =>
        Assert.Equal(expected, CocoaKeys.TypedText(text, modifiers));

    private static Func<int> NextId()
    {
        var id = 5000;
        return () => id++;
    }

    [Fact]
    public void ControllerReadingsMapOntoTheStandardLayout()
    {
        using var app = PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Headless, EnableGamepadDevices = false });
        var pads = new CocoaGamepads(app, NextId());
        var pad = pads.Connect(1234, "Test Controller");
        Assert.Equal(5000, pad.Id);
        var state = app.Input.GetGamepad(pad.Id)!;
        Assert.Equal("Test Controller", state.Name);
        Assert.True(state.IsMapped);

        var reading = default(PadReading);
        reading.Set(0, true);   // buttonA is the south face button
        reading.Set(3, true);   // buttonY is the north face button
        reading.Set(10, true);  // d-pad up
        reading.Set(7, true);   // menu is Start
        reading.LeftX = 1f;
        reading.RightY = 1f;    // the framework's up is positive; the standard layout's down is
        reading.RightTrigger = 1f;
        pad.Update(reading);

        Assert.True(state.IsDown(GamepadButton.South));
        Assert.True(state.IsDown(GamepadButton.North));
        Assert.False(state.IsDown(GamepadButton.East));
        Assert.True(state.IsDown(GamepadButton.DPadUp));
        Assert.True(state.IsDown(GamepadButton.Start));
        Assert.True(state.GetAxis(GamepadAxis.LeftX) > 0.9f);
        Assert.True(state.GetAxis(GamepadAxis.RightY) < -0.9f, "up on the stick must read as negative Y");
        Assert.True(state.GetAxis(GamepadAxis.RightTrigger) > 0.9f);
        Assert.Equal(0f, state.GetAxis(GamepadAxis.LeftTrigger));

        var kinds = new List<PlatformEventKind>();
        while (app.TryGetEvent(out var e))
        {
            kinds.Add(e.Kind);
        }

        Assert.Contains(PlatformEventKind.GamepadConnected, kinds);
        Assert.Equal(4, kinds.Count(k => k == PlatformEventKind.GamepadButtonDown));

        // Releasing everything, then a stick inside the dead zone, changes nothing but the releases.
        pad.Update(new PadReading { LeftX = 0.05f });
        Assert.False(state.IsDown(GamepadButton.South));
        Assert.Equal(0f, state.GetAxis(GamepadAxis.LeftX));
        pads.Disconnect(1234);
        var last = default(PlatformEvent);
        while (app.TryGetEvent(out var e))
        {
            last = e;
        }

        Assert.Equal(PlatformEventKind.GamepadDisconnected, last.Kind);
    }
}

/// <summary>
/// The Cocoa backend on a Mac. Input is delivered the way the system delivers it: real <c>NSEvent</c> objects handed to the window,
/// and the text-input methods the input method calls, invoked through the Objective-C runtime on the real view.
/// </summary>
[Collection("cocoa")]
public class CocoaTests
{
    private static PlatformApp Start()
    {
        return PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Cocoa, EnableGamepadDevices = false });
    }

    private static List<PlatformEvent> Drain(PlatformApp app, int pumps = 3)
    {
        for (var i = 0; i < pumps; i++)
        {
            app.PumpEvents();
        }

        var list = new List<PlatformEvent>();
        while (app.TryGetEvent(out var e))
        {
            list.Add(e);
        }

        return list;
    }

    private static CocoaWindow Show(PlatformApp app, int width = 640, int height = 360) =>
        (CocoaWindow)app.CreateWindow(new WindowOptions { Title = "cocoa test", Width = width, Height = height });

    [Fact]
    public void AWindowReportsItsGeometryAndHandsOutAMetalLayer() => MainThread.Run(() =>
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "needs macOS");
        using var app = Start();
        Assert.Equal(PlatformBackendKind.Cocoa, app.Backend);
        var w = Show(app);
        Drain(app);
        Assert.Equal((640, 360), (w.Width, w.Height));
        Assert.True(w.Scale >= 1f);
        var mainScale = (float)Msg.Dbl(ObjC.Send(ObjC.Class("NSScreen"), "mainScreen"), ObjC.Sel("backingScaleFactor"));
        Assert.Equal(mainScale, w.Scale);
        var h = w.NativeHandle;
        Assert.Equal(NativeWindowKind.Cocoa, h.Kind);
        Assert.Equal(w.WindowObject, h.Display);
        Assert.NotEqual(0, h.Window);
        Assert.Equal("CAMetalLayer", System.Runtime.InteropServices.Marshal.PtrToStringUTF8(ObjC.object_getClassName(h.Window)));
        Assert.Equal("cocoa test", w.Title);
        w.SetTitle("renamed");
        Assert.Equal("renamed", ObjC.ToManaged(ObjC.Send(w.WindowObject, "title")));
    });

    [Fact]
    public void ResizingReportsTheNewPixelSize() => MainThread.Run(() =>
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "needs macOS");
        using var app = Start();
        var w = Show(app);
        Drain(app);
        w.RequestSize(480, 300);
        var events = Drain(app);
        Assert.Equal((480, 300), (w.Width, w.Height));
        var resized = Assert.Single(events, e => e.Kind == PlatformEventKind.WindowResized);
        Assert.Equal((w.Id, 480, 300), (resized.WindowId, resized.Width, resized.Height));
    });

    [Fact]
    public void ClosingTheWindowFromTheTitleBarIsOnlyARequest() => MainThread.Run(() =>
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "needs macOS");
        using var app = Start();
        var w = Show(app);
        Drain(app);

        // performClose: is what the close button calls; it asks the delegate first.
        Msg.Void(w.WindowObject, ObjC.Sel("performClose:"), (nint)0);
        var events = Drain(app);
        Assert.Contains(events, e => e.Kind == PlatformEventKind.WindowCloseRequested && e.WindowId == w.Id);
        Assert.False(w.IsClosed);
        Assert.Contains(w, app.Windows);

        w.Close();
        Assert.True(w.IsClosed);
        Assert.DoesNotContain(w, app.Windows);
    });

    [Fact]
    public void TheQuitCommandBecomesAQuitRequest() => MainThread.Run(() =>
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "needs macOS");
        using var app = Start();
        Show(app);
        Drain(app);

        // The application delegate answers the system's terminate request (Command+Q, the Dock's Quit) without ending the process.
        var reply = Msg.UL(CocoaBackend.Current!.DelegateObject, ObjC.Sel("applicationShouldTerminate:"), CocoaBackend.Current.NSApp);
        Assert.Equal(0UL, (ulong)reply);
        Assert.Contains(Drain(app), e => e.Kind == PlatformEventKind.QuitRequested);
        Assert.True(app.QuitRequested);
    });

    [Fact]
    public void FullscreenCanBeEnteredAndLeft() => MainThread.Run(() =>
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "needs macOS");
        using var app = Start();
        var w = Show(app);
        Drain(app);

        // Fullscreen and key focus need the app to be frontmost, which the system refuses on a locked screen or for a process that
        // nobody launched through the Finder or `open`.
        PumpUntil(app, () => Msg.Flag(CocoaBackend.Current!.NSApp, ObjC.Sel("isActive")) != 0, 1500);
        Assert.SkipUnless(Msg.Flag(CocoaBackend.Current!.NSApp, ObjC.Sel("isActive")) != 0, "the app could not become active (locked screen, or not started through LaunchServices)");
        w.SetFullscreen(true);
        PumpUntil(app, () => w.IsFullscreen, 8000);
        Assert.True(w.IsFullscreen, $"the window never reported entering fullscreen (focused {w.IsFocused}, visible {Msg.Flag(w.WindowObject, ObjC.Sel("isVisible"))}, app active {Msg.Flag(CocoaBackend.Current!.NSApp, ObjC.Sel("isActive"))}, style {Msg.UL(w.WindowObject, ObjC.Sel("styleMask")):x})");
        Assert.True(w.Width >= 640);
        w.SetFullscreen(false);
        PumpUntil(app, () => !w.IsFullscreen, 8000);
        Assert.False(w.IsFullscreen);
    });

    [Fact]
    public void KeysArePhysicalAndTextFollowsTheLayout() => MainThread.Run(() =>
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "needs macOS");
        using var app = Start();
        var w = Show(app);
        Drain(app);

        MacEvents.Key(w, down: true, code: 0x0D, characters: "w");
        MacEvents.Key(w, down: false, code: 0x0D, characters: "w");
        MacEvents.Key(w, down: true, code: 0x00, characters: "A", flags: 1UL << 17);
        MacEvents.Key(w, down: true, code: 0x24, characters: "\r");
        MacEvents.Key(w, down: true, code: 0x7E, characters: "\uF700");
        var e = Drain(app);

        var kinds = e.Select(x => (x.Kind, x.Key, x.Text)).ToArray();
        Assert.Equal(
            [
                (PlatformEventKind.KeyDown, Key.W, (string?)null),
                (PlatformEventKind.TextInput, Key.Unknown, "w"),
                (PlatformEventKind.KeyUp, Key.W, null),
                (PlatformEventKind.KeyDown, Key.A, null),
                (PlatformEventKind.TextInput, Key.Unknown, "A"),
                (PlatformEventKind.KeyDown, Key.Enter, null),
                (PlatformEventKind.KeyDown, Key.ArrowUp, null),
            ],
            kinds);
        Assert.Equal(KeyModifiers.Shift, e[3].Modifiers);
    });

    [Fact]
    public void ACommandChordIsATapWithNoText() => MainThread.Run(() =>
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "needs macOS");
        using var app = Start();
        var w = Show(app);
        Drain(app);
        MacEvents.Key(w, down: true, code: 0x09, characters: "v", flags: 1UL << 20);
        var kinds = Drain(app).Select(x => (x.Kind, x.Key)).ToArray();

        // AppKit sends no key-up while Command is held, so the backend closes the chord itself.
        Assert.Equal([(PlatformEventKind.KeyDown, Key.V), (PlatformEventKind.KeyUp, Key.V)], kinds);
        Assert.False(app.Input.IsKeyDown(Key.V));
    });

    [Fact]
    public void TypingWithAnActiveTextFieldGoesThroughTheTextSystem() => MainThread.Run(() =>
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "needs macOS");
        using var app = Start();
        var w = Show(app);
        Drain(app);
        w.SetTextInput(true, 10, 20, 2, 18);
        MacEvents.Key(w, down: true, code: 0x04, characters: "h");
        MacEvents.Key(w, down: true, code: 0x33, characters: "\u007f");
        var e = Drain(app);
        Assert.Contains(e, x => x.Kind == PlatformEventKind.TextInput && x.Text == "h");
        Assert.Contains(e, x => x.Kind == PlatformEventKind.KeyDown && x.Key == Key.Backspace);
        Assert.DoesNotContain(e, x => x.Kind == PlatformEventKind.TextInput && x.Text != "h");
    });

    [Fact]
    public void AnInputMethodComposesAndCommitsThroughTheTextInputClient() => MainThread.Run(() =>
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "needs macOS");
        using var app = Start();
        var w = Show(app);
        Drain(app);
        w.SetTextInput(true, 100, 50, 2, 20);
        var view = w.ViewObject;

        // Marked text is the pre-edit string: "か" is being composed with the cursor after it.
        MacEvents.SetMarkedText(view, "か", 1, 0);
        Assert.True(Msg.Flag(view, ObjC.Sel("hasMarkedText")) != 0);
        var marked = Msg.Ret<NSRange>(view, ObjC.Sel("markedRange"));
        Assert.Equal((0UL, 1UL), ((ulong)marked.Location, (ulong)marked.Length));
        MacEvents.SetMarkedText(view, "かな", 2, 0);
        MacEvents.InsertText(view, "仮名");
        Assert.False(Msg.Flag(view, ObjC.Sel("hasMarkedText")) != 0);

        var e = Drain(app);
        var comps = e.Where(x => x.Kind == PlatformEventKind.TextComposition).ToArray();
        Assert.Equal(["か", "かな", ""], comps.Select(c => c.Text).ToArray());
        Assert.Equal((1, 1), (comps[0].CursorStart, comps[0].CursorEnd));
        Assert.Equal((2, 2), (comps[1].CursorStart, comps[1].CursorEnd));
        var typed = Assert.Single(e, x => x.Kind == PlatformEventKind.TextInput);
        Assert.Equal("仮名", typed.Text);

        // The composition ends before the finished text arrives.
        Assert.True(e.IndexOf(comps[2]) < e.IndexOf(typed));

        // The candidate window is placed from the caret rectangle the game gave, in screen coordinates.
        var rect = Msg.Ret<CGRect, nuint, nuint, nint>(view, ObjC.Sel("firstRectForCharacterRange:actualRange:"), 0, 0, 0);
        Assert.True(rect.Size.Width > 0 && rect.Size.Height > 0);
        var windowFrame = Msg.Ret<CGRect>(w.WindowObject, ObjC.Sel("frame"));
        Assert.True(rect.Origin.X >= windowFrame.Origin.X && rect.Origin.X <= windowFrame.Origin.X + windowFrame.Size.Width);

        // Turning text input off while composing cancels the pre-edit.
        MacEvents.SetMarkedText(view, "x", 1, 0);
        Drain(app);
        w.SetTextInput(false);
        Assert.Contains(Drain(app), x => x.Kind == PlatformEventKind.TextComposition && x.Text == "");
        Assert.False(Msg.Flag(view, ObjC.Sel("hasMarkedText")) != 0);
    });

    [Fact]
    public void MouseButtonsMoveAndWheelUseTopLeftPixels() => MainThread.Run(() =>
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "needs macOS");
        using var app = Start();
        var w = Show(app);
        Drain(app);
        var s = w.Scale;

        // AppKit measures window points from the bottom left; the platform layer reports pixels from the top left.
        var heightPoints = w.Height / s;
        MacEvents.Mouse(w, 5 /* mouseMoved */, 100 / s, heightPoints - (40 / s), deliverToView: true);
        MacEvents.Mouse(w, 1 /* leftMouseDown */, 100 / s, heightPoints - (40 / s), flags: 1UL << 17);
        MacEvents.Mouse(w, 2 /* leftMouseUp */, 100 / s, heightPoints - (40 / s));
        MacEvents.Mouse(w, 3 /* rightMouseDown */, 10 / s, heightPoints - (10 / s));
        var e = Drain(app);

        var move = e.First(x => x.Kind == PlatformEventKind.PointerMoved);
        Assert.Equal((100f, 40f), (move.X, move.Y), new PointComparer());
        var down = e.First(x => x.Kind == PlatformEventKind.PointerButtonDown);
        Assert.Equal(MouseButton.Left, down.Button);
        Assert.Equal((100f, 40f), (down.X, down.Y), new PointComparer());
        Assert.Equal(KeyModifiers.Shift, down.Modifiers);
        Assert.Contains(e, x => x.Kind == PlatformEventKind.PointerButtonUp && x.Button == MouseButton.Left);
        var right = e.First(x => x.Kind == PlatformEventKind.PointerButtonDown && x.Button == MouseButton.Right);
        Assert.Equal((10f, 10f), (right.X, right.Y), new PointComparer());

        // A mouse wheel reports lines; positive Y is away from the user.
        MacEvents.Scroll(w, dx: 0, dy: 3, precise: false);
        MacEvents.Scroll(w, dx: 0, dy: -40, precise: true);
        var wheel = Drain(app).Where(x => x.Kind == PlatformEventKind.PointerWheel).ToArray();
        Assert.Equal(2, wheel.Length);
        Assert.Equal(3f, wheel[0].DeltaY);
        Assert.Equal(-2f, wheel[1].DeltaY);
    });

    [Fact]
    public void ALockedCursorReportsRelativeMotion() => MainThread.Run(() =>
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "needs macOS");
        using var app = Start();
        var w = Show(app);
        Drain(app);
        w.SetCursorHidden(true);
        Assert.True(w.CursorHidden);
        w.SetCursorLocked(true);
        try
        {
            Assert.True(w.CursorLocked);
            MacEvents.RelativeMove(w, 4, -2);
            var motion = Assert.Single(Drain(app), x => x.Kind == PlatformEventKind.PointerRelativeMotion);
            Assert.Equal((4f * w.Scale, -2f * w.Scale), (motion.DeltaX, motion.DeltaY));
        }
        finally
        {
            w.SetCursorLocked(false);
            w.SetCursorHidden(false);
        }

        Assert.False(w.CursorLocked);
    });

    [Fact]
    public void TheClipboardRoundTripsThroughThePasteboard() => MainThread.Run(() =>
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "needs macOS");
        using var app = Start();
        Assert.True(app.Clipboard.IsAvailable);
        Assert.False(app.Clipboard.Supports(ClipboardSelection.Primary));
        var text = "onegame clipboard: héllo 世界 😀\nsecond line";
        Assert.True(app.Clipboard.SetText(text));
        Assert.Equal(text, app.Clipboard.GetText());

        // Another program sees the same text.
        var read = Run("/usr/bin/pbpaste", null);
        Assert.Equal(text, read);

        Run("/usr/bin/pbcopy", "from another program");
        Assert.Equal("from another program", app.Clipboard.GetText());
        Assert.False(app.Clipboard.SetText(new string('x', PlatformClipboard.MaxTextBytes + 1)));
    });

    [Fact]
    public void PollingControllersWithNoneConnectedIsQuiet() => MainThread.Run(() =>
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "needs macOS");
        using var app = PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Cocoa });
        Show(app);
        var events = Drain(app, 20);

        // A Mac with a controller attached would add GamepadConnected here; the framework itself must load and answer either way.
        Assert.All(events.Where(e => e.Kind is PlatformEventKind.GamepadConnected or PlatformEventKind.GamepadDisconnected), e => Assert.True(e.GamepadId >= 1000));
        Assert.True(ObjC.LoadGameController());
        using var pool = new Pool();
        var list = ObjC.Send(ObjC.Class("GCController"), "controllers");
        Assert.NotEqual(0, list);
    });

    private static void PumpUntil(PlatformApp app, Func<bool> done, int timeoutMs)
    {
        var end = Environment.TickCount64 + timeoutMs;
        while (!done() && Environment.TickCount64 < end)
        {
            app.PumpEvents(20);
            while (app.TryGetEvent(out _))
            {
            }
        }
    }

    private static string Run(string exe, string? stdin)
    {
        using var p = Process.Start(new ProcessStartInfo(exe) { RedirectStandardInput = stdin is not null, RedirectStandardOutput = true, UseShellExecute = false })!;
        if (stdin is not null)
        {
            p.StandardInput.Write(stdin);
            p.StandardInput.Close();
        }

        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return output;
    }

    private sealed class PointComparer : IEqualityComparer<(float X, float Y)>
    {
        public bool Equals((float X, float Y) a, (float X, float Y) b) => Math.Abs(a.X - b.X) < 0.51f && Math.Abs(a.Y - b.Y) < 0.51f;

        public int GetHashCode((float X, float Y) p) => 0;
    }
}

/// <summary>Builds the <c>NSEvent</c>s and text-input calls the system would send, and delivers them the way it does.</summary>
internal static class MacEvents
{
    public static void Key(CocoaWindow w, bool down, int code, string characters, ulong flags = 0)
    {
        using var pool = new Pool();
        var ev = Msg.Obj(
            ObjC.Class("NSEvent"),
            ObjC.Sel("keyEventWithType:location:modifierFlags:timestamp:windowNumber:context:characters:charactersIgnoringModifiers:isARepeat:keyCode:"),
            (nuint)(down ? 10 : 11),
            default(CGPoint),
            (nuint)flags,
            0.0,
            Msg.Obj(w.WindowObject, ObjC.Sel("windowNumber")),
            (nint)0,
            ObjC.NSString(characters),
            ObjC.NSString(characters.ToLowerInvariant()),
            (byte)0,
            (ushort)code);
        Msg.Void(w.WindowObject, ObjC.Sel("sendEvent:"), ev);
    }

    /// <summary>A mouse event at a point in window coordinates (points, origin bottom left). Types: 1 left down, 2 left up, 3 right down, 4 right up, 5 moved.</summary>
    public static void Mouse(CocoaWindow w, int type, double x, double y, ulong flags = 0, bool deliverToView = false)
    {
        using var pool = new Pool();
        var ev = Msg.Obj(
            ObjC.Class("NSEvent"),
            ObjC.Sel("mouseEventWithType:location:modifierFlags:timestamp:windowNumber:context:eventNumber:clickCount:pressure:"),
            (nuint)type,
            new CGPoint { X = x, Y = y },
            (nuint)flags,
            0.0,
            Msg.Obj(w.WindowObject, ObjC.Sel("windowNumber")),
            (nint)0,
            (nint)1,
            (nint)1,
            1f);
        if (deliverToView)
        {
            // Mouse-moved events reach the view through the window's tracking machinery, which needs a real pointer over the window.
            Msg.Void(w.ViewObject, ObjC.Sel("mouseMoved:"), ev);
            return;
        }

        Msg.Void(w.WindowObject, ObjC.Sel("sendEvent:"), ev);
    }

    /// <summary>A mouse-moved event that carries relative motion, which the NSEvent factory cannot set; it comes from a Quartz event.</summary>
    public static void RelativeMove(CocoaWindow w, int deltaX, int deltaY)
    {
        using var pool = new Pool();
        var cg = CgEvents.MouseMoved(deltaX, deltaY);
        var ev = Msg.Obj(ObjC.Class("NSEvent"), ObjC.Sel("eventWithCGEvent:"), cg);
        Msg.Void(w.ViewObject, ObjC.Sel("mouseMoved:"), ev);
        CgEvents.Release(cg);
    }

    public static void Scroll(CocoaWindow w, double dx, double dy, bool precise)
    {
        using var pool = new Pool();
        var cg = CgEvents.Scroll((int)dy, (int)dx, precise);
        var ev = Msg.Obj(ObjC.Class("NSEvent"), ObjC.Sel("eventWithCGEvent:"), cg);
        Msg.Void(w.ViewObject, ObjC.Sel("scrollWheel:"), ev);
        CgEvents.Release(cg);
    }

    public static void SetMarkedText(nint view, string text, int selectionStart, int selectionLength)
    {
        using var pool = new Pool();
        Msg.Void(view, ObjC.Sel("setMarkedText:selectedRange:replacementRange:"), ObjC.NSString(text), (nuint)selectionStart, (nuint)selectionLength, NSRange.NotFound, (nuint)0);
    }

    public static void InsertText(nint view, string text)
    {
        using var pool = new Pool();
        Msg.Void(view, ObjC.Sel("insertText:replacementRange:"), ObjC.NSString(text), NSRange.NotFound, (nuint)0);
    }
}

/// <summary>Quartz events, the lower layer of NSEvent, used where the NSEvent factory cannot set a field.</summary>
internal static unsafe partial class CgEvents
{
    private const string Lib = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    [LibraryImport(Lib)]
    private static partial nint CGEventCreateScrollWheelEvent2(nint source, uint units, uint wheelCount, int wheel1, int wheel2, int wheel3);

    [LibraryImport(Lib)]
    private static partial void CGEventSetIntegerValueField(nint ev, uint field, long value);

    [LibraryImport(Lib)]
    private static partial void CFRelease(nint obj);

    public static nint Scroll(int dy, int dx, bool precise)
    {
        // kCGScrollEventUnitPixel is 0 and kCGScrollEventUnitLine is 1; field 88 is kCGScrollWheelEventIsContinuous.
        var ev = CGEventCreateScrollWheelEvent2(0, precise ? 0u : 1u, 2, dy, dx, 0);
        CGEventSetIntegerValueField(ev, 88, precise ? 1 : 0);
        return ev;
    }

    public static nint MouseMoved(int dx, int dy)
    {
        // kCGEventMouseMoved is 5; the delta fields are kCGMouseEventDeltaX (4) and kCGMouseEventDeltaY (5).
        var create = (delegate* unmanaged<nint, uint, CGPoint, uint, nint>)NativeLibrary.GetExport(NativeLibrary.Load(Lib), "CGEventCreateMouseEvent");
        var ev = create(0, 5, default, 0);
        CGEventSetIntegerValueField(ev, 4, dx);
        CGEventSetIntegerValueField(ev, 5, dy);
        return ev;
    }

    public static void Release(nint ev) => CFRelease(ev);
}
