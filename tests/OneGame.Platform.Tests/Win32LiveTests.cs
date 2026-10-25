using System.Runtime.InteropServices;

namespace OneGame.Platform.Tests;

/// <summary>
/// A real Win32 window on the machine's desktop. Messages are sent to the window the way the system sends them (SendMessage runs the window
/// procedure), so the whole path from the window procedure through the translator to the event queue is exercised. These tests only run on Windows.
/// </summary>
public sealed partial class Win32LiveTests : IDisposable
{
    private const uint WmKeyDown = 0x100, WmKeyUp = 0x101, WmChar = 0x102, WmMouseMove = 0x200, WmLButtonDown = 0x201, WmLButtonUp = 0x202, WmMouseWheel = 0x20A;

    private readonly PlatformApp? _app;
    private readonly Window? _window;

    public Win32LiveTests()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        _app = PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Win32, EnableGamepadDevices = false });
        _window = _app.CreateWindow(new WindowOptions { Title = "win32 test", Width = 320, Height = 200 });
        Pump.For(_app, 100);
    }

    public void Dispose() => _app?.Dispose();

    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    private static partial nint SendMessage(nint hwnd, uint message, nuint wParam, nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "IsWindowVisible")]
    private static partial int IsWindowVisible(nint hwnd);

    private PlatformApp App
    {
        get
        {
            Assert.SkipUnless(_app is not null, "needs Windows");
            return _app!;
        }
    }

    private static nint Xy(int x, int y) => (nint)((y << 16) | (x & 0xFFFF));

    private static nint KeyParam(int scan, bool extended = false, bool up = false) =>
        unchecked((nint)((up ? 0xC0000000u : 0u) | ((uint)scan << 16) | (extended ? 1u << 24 : 0u) | 1u));

    [Fact]
    public void TheWindowIsCreatedVisibleAndSizedInPixels()
    {
        var app = App;
        Assert.Equal(PlatformBackendKind.Win32, app.Backend);
        Assert.Equal(NativeWindowKind.Win32, _window!.NativeHandle.Kind);
        Assert.NotEqual(0, _window.NativeHandle.Window);
        Assert.NotEqual(0, IsWindowVisible(_window.NativeHandle.Window));
        Assert.Equal((320, 200), (_window.Width, _window.Height));
        Assert.Equal("win32 test", _window.Title);
        Assert.True(_window.Scale >= 1f);
    }

    [Fact]
    public void ResizeIsReportedWithTheClientSize()
    {
        var app = App;
        _window!.RequestSize(400, 300);
        var events = Pump.Until(app, ev => ev.Any(e => e.Kind == PlatformEventKind.WindowResized && e.Width == 400));
        var resized = events.Last(e => e.Kind == PlatformEventKind.WindowResized);
        Assert.Equal((400, 300), (resized.Width, resized.Height));
        Assert.Equal((400, 300), (_window.Width, _window.Height));
    }

    [Fact]
    public void KeyTextPointerAndWheelMessagesBecomeEvents()
    {
        var app = App;
        var hwnd = _window!.NativeHandle.Window;
        while (app.TryGetEvent(out _))
        {
        }

        SendMessage(hwnd, WmMouseMove, 0, Xy(50, 60));
        SendMessage(hwnd, WmLButtonDown, 1, Xy(50, 60));
        SendMessage(hwnd, WmLButtonUp, 0, Xy(50, 60));
        SendMessage(hwnd, WmMouseWheel, (nuint)(120u << 16), Xy(0, 0));
        SendMessage(hwnd, WmKeyDown, 0x41, KeyParam(0x1E));
        SendMessage(hwnd, WmChar, 'a', KeyParam(0x1E));
        SendMessage(hwnd, WmKeyUp, 0x41, KeyParam(0x1E, up: true));
        SendMessage(hwnd, WmKeyDown, 0x25, KeyParam(0x4B, extended: true));
        SendMessage(hwnd, WmKeyUp, 0x25, KeyParam(0x4B, extended: true, up: true));

        var events = Pump.Until(app, ev => ev.Count(e => e.Kind == PlatformEventKind.KeyUp) >= 2);
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.PointerMoved, X: 50, Y: 60 });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.PointerButtonDown, Button: MouseButton.Left, X: 50, Y: 60 });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.PointerButtonUp, Button: MouseButton.Left });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.PointerWheel, DeltaY: 1f });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.KeyDown, Key: Key.A });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.TextInput, Text: "a" });
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.KeyDown, Key: Key.ArrowLeft });
        Assert.False(app.Input.IsKeyDown(Key.A));
    }

    [Fact]
    public void TheClipboardRoundTripsUnicodeText()
    {
        var app = App;
        const string text = "clipboard \u00e9 \u3042 \U0001F600";
        Assert.True(app.Clipboard.SetText(text));
        Assert.Equal(text, app.Clipboard.GetText());
    }
}
