using System.Collections.Concurrent;
using System.Text;

namespace OneGame.Platform.Tests;

public class ClipboardTextTests
{
    [Fact]
    public void Utf8IsDecodedWithReplacementAndStopsAtNul()
    {
        Assert.Equal("héllo ✓", ClipboardText.FromUtf8(Encoding.UTF8.GetBytes("héllo ✓\0trailing")));
        Assert.Equal("a\uFFFDb", ClipboardText.FromUtf8([(byte)'a', 0xFF, (byte)'b']));
        Assert.Equal("", ClipboardText.FromUtf8([]));
    }

    [Fact]
    public void Latin1RoundTripsAndReplacesWhatItCannotHold()
    {
        Assert.Equal("café", ClipboardText.FromLatin1([(byte)'c', (byte)'a', (byte)'f', 0xE9]));
        Assert.Equal([(byte)'c', 0xE9, (byte)'?'], ClipboardText.ToLatin1("cé✓"));
    }

    [Fact]
    public void LineEndsConvertBothWays()
    {
        Assert.Equal("a\r\nb\r\nc\r\n", ClipboardText.ToCrLf("a\nb\r\nc\r"));
        Assert.Equal("a\nb\nc\n", ClipboardText.FromCrLf("a\r\nb\nc\r"));
        Assert.Equal("plain", ClipboardText.ToCrLf("plain"));
    }

    [Fact]
    public void InputMethodByteOffsetsBecomeUtf16Indexes()
    {
        // "a" is 1 byte, "你" 3 bytes, the emoji 4 bytes (two UTF-16 units).
        const string text = "a你😀b";
        Assert.Equal(0, ImeOffsets.Utf8ToUtf16(text, 0));
        Assert.Equal(1, ImeOffsets.Utf8ToUtf16(text, 1));
        Assert.Equal(2, ImeOffsets.Utf8ToUtf16(text, 4));
        Assert.Equal(4, ImeOffsets.Utf8ToUtf16(text, 8));
        Assert.Equal(5, ImeOffsets.Utf8ToUtf16(text, 9));
        Assert.Equal(1, ImeOffsets.Utf8ToUtf16(text, 2)); // inside a character: the boundary before it
        Assert.Equal(5, ImeOffsets.Utf8ToUtf16(text, 999));
        Assert.Equal(-1, ImeOffsets.Utf8ToUtf16(text, -1));
    }
}

public class HeadlessClipboardAndImeTests
{
    [Fact]
    public void HeadlessAppHasAPrivateClipboard()
    {
        using var app = PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Headless, EnableGamepadDevices = false });
        Assert.True(app.Clipboard.IsAvailable);
        Assert.Null(app.Clipboard.GetText());
        Assert.True(app.Clipboard.SetText("copied"));
        Assert.Equal("copied", app.Clipboard.GetText());
        Assert.Null(app.Clipboard.GetText(ClipboardSelection.Primary));
        Assert.False(app.Clipboard.SetText(new string('x', PlatformClipboard.MaxTextBytes + 1)));
    }

    [Fact]
    public void InjectedCompositionArrivesAsAnEventAndCommitAsText()
    {
        using var app = PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Headless, EnableGamepadDevices = false });
        var id = app.CreateWindow().Id;
        app.Injector.Composition(id, "ni", 0, 2);
        app.Injector.Composition(id, "");
        app.Injector.Text(id, "你");
        var events = new List<PlatformEvent>();
        while (app.TryGetEvent(out var e))
        {
            events.Add(e);
        }

        Assert.Equal(PlatformEventKind.TextComposition, events[0].Kind);
        Assert.Equal(("ni", 0, 2), (events[0].Text, events[0].CursorStart, events[0].CursorEnd));
        Assert.Equal(("", 0, 0), (events[1].Text, events[1].CursorStart, events[1].CursorEnd));
        Assert.Equal((PlatformEventKind.TextInput, "你"), (events[2].Kind, events[2].Text));
    }

    [Fact]
    public void TextInputStateIsRecordedOnTheWindow()
    {
        using var app = PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Headless, EnableGamepadDevices = false });
        var w = app.CreateWindow();
        Assert.False(w.TextInputActive);
        w.SetTextInput(true, 10, 20, 30, 40);
        Assert.True(w.TextInputActive);
        Assert.Equal((10, 20, 30, 40), w.TextInputArea);
        w.SetTextInput(false);
        Assert.False(w.TextInputActive);
    }
}

/// <summary>X11 selections between two connections, following the ICCCM conventions real programs use.</summary>
public sealed unsafe class X11ClipboardTests : IClassFixture<XvfbFixture>
{
    private readonly XvfbFixture _xvfb;

    public X11ClipboardTests(XvfbFixture xvfb) => _xvfb = xvfb;

    private PlatformOptions Options()
    {
        Assert.SkipUnless(_xvfb.Available, "Xvfb is not installed");
        return new PlatformOptions { Backend = PlatformBackendKind.X11, DisplayName = _xvfb.Display, EnableGamepadDevices = false };
    }

    [Fact]
    public void TextMovesBetweenTwoClientsBothWays()
    {
        var options = Options();
        using var owner = new AppThread(options);
        using var other = new AppThread(options);
        Assert.Null(other.Run(a => a.Clipboard.GetText()));

        const string text = "héllo wörld ✓ 你好 😀\nsecond line";
        Assert.True(owner.Run(a => a.Clipboard.SetText(text)));
        Assert.Equal(text, other.Run(a => a.Clipboard.GetText()));

        // Taking the clipboard over tells the old owner, which then reads the new text from the new one.
        Assert.True(other.Run(a => a.Clipboard.SetText("taken over")));
        Assert.Equal("taken over", owner.Run(a => a.Clipboard.GetText()));
        Assert.Equal("taken over", other.Run(a => a.Clipboard.GetText()));
    }

    [Fact]
    public void ThePrimarySelectionIsSeparateFromTheClipboard()
    {
        var options = Options();
        using var owner = new AppThread(options);
        using var other = new AppThread(options);
        Assert.True(other.Run(a => a.Clipboard.Supports(ClipboardSelection.Primary)));
        Assert.True(owner.Run(a => a.Clipboard.SetText("highlighted", ClipboardSelection.Primary)));
        Assert.True(owner.Run(a => a.Clipboard.SetText("copied")));
        Assert.Equal("highlighted", other.Run(a => a.Clipboard.GetText(ClipboardSelection.Primary)));
        Assert.Equal("copied", other.Run(a => a.Clipboard.GetText()));
    }

    [Fact]
    public void ThreeMegabytesTransferAndMoreThanTheLimitIsRefused()
    {
        var options = Options();
        using var owner = new AppThread(options);
        using var other = new AppThread(options);
        var big = new string('x', 3 * 1024 * 1024) + "end";
        Assert.True(owner.Run(a => a.Clipboard.SetText(big)));
        Assert.Equal(big, other.Run(a => a.Clipboard.GetText()));
        Assert.False(other.Run(a => a.Clipboard.SetText(new string('y', PlatformClipboard.MaxTextBytes + 1))));
    }

    [Fact]
    public void AnOwnerThatDoesNotAnswerTimesOutInsteadOfHanging()
    {
        var options = Options();
        // The owner never pumps events, so it never answers the request.
        using var silent = PlatformApp.Create(options);
        using var me = PlatformApp.Create(options);
        Assert.True(silent.Clipboard.SetText("never delivered"));
        var started = Environment.TickCount64;
        Assert.Null(me.Clipboard.GetText());
        Assert.InRange(Environment.TickCount64 - started, 500, 4000);
    }

    [Fact]
    public void ADesktopScaleChangeReachesTheWindowAsAnEvent()
    {
        var options = Options();
        using var app = PlatformApp.Create(options);
        var window = app.CreateWindow(new WindowOptions { Width = 200, Height = 100 });
        Assert.Equal(1f, window.Scale);

        // The desktop's scale lives in the root window's RESOURCE_MANAGER property (Xft.dpi, 96 = 1.0).
        var name = Encoding.ASCII.GetBytes(_xvfb.Display + "\0");
        nint other;
        fixed (byte* p = name)
        {
            other = Xlib.OpenDisplay(p);
        }

        var root = Xlib.RootWindow(other, Xlib.DefaultScreen(other));
        nuint atom;
        fixed (byte* n = "RESOURCE_MANAGER\0"u8)
        {
            atom = Xlib.InternAtom(other, n, 0);
        }

        var value = Encoding.ASCII.GetBytes("Xft.dpi:\t192\n");
        fixed (byte* v = value)
        {
            Xlib.ChangeProperty(other, root, atom, 31, 8, Xlib.PropModeReplace, v, value.Length);
        }

        Xlib.Flush(other);
        var events = Pump.Until(app, ev => ev.Any(e => e.Kind == PlatformEventKind.WindowScaleChanged));
        Xlib.CloseDisplay(other);
        Assert.Contains(events, e => e is { Kind: PlatformEventKind.WindowScaleChanged, Scale: 2f });
        Assert.Equal(2f, window.Scale);
    }
}

/// <summary>The Wayland clipboard and text-input-v3 against a scripted compositor that follows the protocol's rules.</summary>
public sealed class WaylandClipboardAndImeTests : IDisposable
{
    private readonly FakeCompositor? _server;

    public WaylandClipboardAndImeTests()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        try
        {
            _server = new FakeCompositor(Path.Combine(Path.GetTempPath(), $"onegame-fake-{Environment.ProcessId}-{Guid.NewGuid():N}"));
        }
        catch (DllNotFoundException)
        {
        }
    }

    public void Dispose() => _server?.Dispose();

    private PlatformOptions Options()
    {
        Assert.SkipUnless(_server is not null, "libwayland-server is not installed");
        return new PlatformOptions { Backend = PlatformBackendKind.Wayland, DisplayName = _server!.SocketPath, EnableGamepadDevices = false };
    }

    // The app thread pumps events between calls, so a changed clipboard shows up after a moment.
    private static string? WaitForText(AppThread app, string expected)
    {
        string? text = null;
        var end = Environment.TickCount64 + 3000;
        while (Environment.TickCount64 < end)
        {
            text = app.Run(a => a.Clipboard.GetText());
            if (text == expected)
            {
                break;
            }

            Thread.Sleep(10);
        }

        return text;
    }

    [Fact]
    public void TextMovesBetweenTwoClientsAndANewOwnerCancelsTheOld()
    {
        var options = Options();
        using var owner = new AppThread(options);
        using var other = new AppThread(options);
        Assert.True(other.Run(a => a.Clipboard.IsAvailable));

        const string text = "héllo wörld ✓ 你好 😀\nsecond line";
        Assert.True(owner.Run(a => a.Clipboard.SetText(text)));
        Assert.Equal(text, WaitForText(other, text));

        Assert.True(other.Run(a => a.Clipboard.SetText("taken over")));
        Assert.Equal("taken over", other.Run(a => a.Clipboard.GetText()));
        Assert.Equal("taken over", WaitForText(owner, "taken over"));
    }

    [Fact]
    public void MoreThanAPipeBufferOfTextTransfers()
    {
        var options = Options();
        using var owner = new AppThread(options);
        using var other = new AppThread(options);
        var big = string.Concat(Enumerable.Repeat("0123456789abcdef", 200_000)); // 3.2 MB, far past a pipe's 64 KB buffer
        Assert.True(owner.Run(a => a.Clipboard.SetText(big)));
        Assert.Equal(big, WaitForText(other, big));
    }

    [Fact]
    public void PrimarySelectionIsNotOfferedOnWayland()
    {
        var options = Options();
        using var me = PlatformApp.Create(options);
        Assert.False(me.Clipboard.Supports(ClipboardSelection.Primary));
        Assert.False(me.Clipboard.SetText("x", ClipboardSelection.Primary));
        Assert.Null(me.Clipboard.GetText(ClipboardSelection.Primary));
    }

    [Fact]
    public void InputMethodStateFollowsTheFocusedTextFieldAndComposesText()
    {
        var options = Options();
        var server = _server!;
        using var app = PlatformApp.Create(options);
        var window = app.CreateWindow(new WindowOptions { Width = 320, Height = 200 });
        Pump.For(app, 100);
        Assert.True(server.WaitForWindow(2000));

        // Nothing is sent until the compositor says the text input has the window's focus.
        window.SetTextInput(true, 10, 20, 30, 40);
        Pump.For(app, 100);
        Assert.DoesNotContain("enable", server.TextInputLog());

        server.Run(server.TextInputEnter);
        Pump.Until(app, _ => server.TextInputLog().Contains("commit"));
        Assert.Equal(["enable", "content-type", "rect 10 20 30 40", "commit"], server.TextInputLog());

        // Moving the caret updates the rectangle only.
        window.SetTextInput(true, 12, 22, 30, 40);
        Pump.For(app, 100);
        Assert.Equal(["rect 12 22 30 40", "commit"], server.TextInputLog()[4..]);

        // A pre-edit string, then its replacement by the finished text.
        server.Run(() => server.TextInputBatch("你好", 3, 6, null));
        var events = Pump.Until(app, ev => ev.Any(e => e.Kind == PlatformEventKind.TextComposition));
        var pre = events.Single(e => e.Kind == PlatformEventKind.TextComposition);
        Assert.Equal(("你好", 1, 2), (pre.Text, pre.CursorStart, pre.CursorEnd));

        server.Run(() => server.TextInputBatch(null, -1, -1, "你好吗"));
        events = Pump.Until(app, ev => ev.Any(e => e.Kind == PlatformEventKind.TextInput));
        var kinds = events.Where(e => e.Kind is PlatformEventKind.TextComposition or PlatformEventKind.TextInput).Select(e => (e.Kind, e.Text)).ToList();
        Assert.Equal([(PlatformEventKind.TextComposition, ""), (PlatformEventKind.TextInput, "你好吗")], kinds);

        // Losing focus of the text field ends the input method session.
        window.SetTextInput(false);
        Pump.Until(app, _ => server.TextInputLog().Contains("disable"));
        Assert.Contains("disable", server.TextInputLog());
    }
}

/// <summary>The clipboard against a real compositor (headless Weston).</summary>
public sealed class WaylandWestonClipboardTests : IClassFixture<WestonFixture>
{
    private readonly WestonFixture _weston;

    public WaylandWestonClipboardTests(WestonFixture weston) => _weston = weston;

    [Fact]
    public void TextMovesBetweenTwoClientsThroughWeston()
    {
        Assert.SkipUnless(_weston.Available, "weston is not installed");
        var options = new PlatformOptions { Backend = PlatformBackendKind.Wayland, DisplayName = _weston.SocketPath, EnableGamepadDevices = false };
        using var owner = new AppThread(options);
        using var other = new AppThread(options);
        Assert.SkipUnless(other.Run(a => a.Clipboard.IsAvailable), "this weston has no seat to hold a clipboard");
        Assert.True(owner.Run(a => a.Clipboard.SetText("through weston")));
        string? got = null;
        var end = Environment.TickCount64 + 2000;
        while (Environment.TickCount64 < end && got != "through weston")
        {
            got = other.Run(a => a.Clipboard.GetText());
            Thread.Sleep(10);
        }

        Assert.Equal("through weston", got);
    }
}
