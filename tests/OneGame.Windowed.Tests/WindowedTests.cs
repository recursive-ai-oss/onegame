using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using OneGame.Control;
using OneGame.Host;
using OneGame.Live;
using OneGame.Platform;
using OneGame.Platform.Tests;

namespace OneGame.Windowed.Tests;

/// <summary>A windowed game on a real display server, driven through the control layer like an agent would.</summary>
internal sealed class Session : IDisposable
{
    public Session(PlatformBackendKind backend, string display, OneGame.Gpu.PresentMode presentMode = OneGame.Gpu.PresentMode.Vsync)
    {
        // The software rasterizer is deterministic and needs no display hardware; the validation layer reports anything the game does wrong.
        if (backend != PlatformBackendKind.Cocoa)
        {
            Environment.SetEnvironmentVariable("ONEGAME_GPU_ADAPTER", "software");
        }

        Environment.SetEnvironmentVariable("ONEGAME_GPU_VALIDATION", "1");
        OutputDirectory = Path.Combine(Path.GetTempPath(), "onegame-windowed-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(OutputDirectory);
        Game = GameInstance.Create(
            new WindowedGame(),
            new GameOptions { Width = 640, Height = 360, Gpu = GpuMode.Required, Backend = backend, DisplayName = display, PresentMode = presentMode });
        Control = new ControlSurface(Game, OutputDirectory);
        StepUntil(() => Game.PresentedFrames >= 3 && Game.Window.IsFocused || Game.PresentedFrames >= 30);
    }

    public GameInstance Game { get; }

    public ControlSurface Control { get; }

    public string OutputDirectory { get; }

    public JsonElement Call(string op, string? args = null) => JsonDocument.Parse(Control.Call(op, args)).RootElement;

    public void StepUntil(Func<bool> done, int maxFrames = 300)
    {
        for (var i = 0; i < maxFrames && !done(); i++)
        {
            Game.Step();
            Thread.Sleep(2);
        }
    }

    public OneGame.UI.Kit.TextInputView Field => (OneGame.UI.Kit.TextInputView)Game.Ui.Find("name")!;

    public string Text(string id)
    {
        var matches = Call("ui_query", $$"""{"selector":"#{{id}}"}""");
        return matches[0].GetProperty("text").GetString() ?? "";
    }

    public (double X, double Y) Center(string id)
    {
        var r = Game.Ui.Find(id)!.AbsoluteBounds;
        return (r.X + (r.Width / 2), r.Y + (r.Height / 2));
    }

    public void Click(string id)
    {
        var (x, y) = Center(id);
        Call("input_pointer", string.Create(System.Globalization.CultureInfo.InvariantCulture, $$"""{"action":"click","x":{{x}},"y":{{y}}}"""));
    }

    public string[] Validation() => Game.TakeGpuValidationMessages();

    public static void Save(string name, CapturedFrame frame)
    {
        if (Environment.GetEnvironmentVariable("ONEGAME_TEST_SAVE") is { Length: > 0 } dir)
        {
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), Png.Encode(frame));
        }
    }

    public void Dispose()
    {
        Game.Dispose();
        try
        {
            Directory.Delete(OutputDirectory, true);
        }
        catch (IOException)
        {
        }
    }
}

/// <summary>Speaks newline-delimited JSON-RPC to the game's live-control socket while the test keeps the game's thread turning.</summary>
internal sealed class McpClient : IDisposable
{
    private readonly Socket _socket;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private int _id;

    public McpClient(string path)
    {
        _socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        _socket.Connect(new UnixDomainSocketEndPoint(path));
        var stream = new NetworkStream(_socket);
        _reader = new StreamReader(stream, Encoding.UTF8);
        _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
    }

    public Task<JsonElement> Send(string method, string paramsJson)
    {
        var id = ++_id;
        _writer.WriteLine($$"""{"jsonrpc":"2.0","id":{{id}},"method":"{{method}}","params":{{paramsJson}}}""");
        return Task.Run(() => JsonDocument.Parse(_reader.ReadLine()!).RootElement);
    }

    public void Dispose()
    {
        _socket.Dispose();
    }
}

public abstract class WindowedTests
{
    private protected abstract Session Start();

    private protected abstract bool IsX11 { get; }

    // The name the backend reports in `info`, and whether the window system gives the window exactly the size asked for.
    private protected virtual string BackendName => IsX11 ? "x11" : "wayland";

    private protected virtual bool HonorsRequestedSize => IsX11;

    [Fact]
    public void FramesArePresentedAndTheCapturedFrameShowsTheUi() => MainThread.Run(() =>
    {
        using var s = Start();
        s.StepUntil(() => s.Game.PresentedFrames >= 10);
        Assert.True(s.Game.PresentedFrames >= 10, $"only {s.Game.PresentedFrames} frames were presented");

        var info = s.Call("info");
        Assert.Equal(BackendName, info.GetProperty("backend").GetString());
        Assert.False(info.GetProperty("headless").GetBoolean());
        Assert.Equal("render2d", info.GetProperty("stage").GetString());
        Assert.True(info.GetProperty("presented").GetInt64() >= 10);

        var frame = s.Game.CapturePresented();
        Assert.NotNull(frame);
        Assert.Equal((s.Game.Window.Width, s.Game.Window.Height), (frame.Width, frame.Height));
        var colours = new HashSet<uint>();
        for (var i = 0; i + 3 < frame.Rgba.Length; i += 4)
        {
            colours.Add(BitConverter.ToUInt32(frame.Rgba, i));
        }

        // Panels, borders and anti-aliased text give many colours; a cleared or black frame gives one.
        Assert.True(colours.Count > 12, $"only {colours.Count} distinct colours");
        Session.Save(BackendName + "-presented", frame);
        Assert.Empty(s.Validation());
    });

    [Fact]
    public void ResizingTheWindowRebuildsTheSwapchainWithoutValidationErrors() => MainThread.Run(() =>
    {
        using var s = Start();
        var before = s.Game.CapturePresented()!;
        s.Game.Window.RequestSize(480, 300);
        var started = s.Game.PresentedFrames;
        s.StepUntil(() => s.Game.PresentedFrames >= started + 5);
        var after = s.Game.CapturePresented()!;

        // Under a compositor that fixes window sizes (the headless Weston's kiosk shell) the size may not change; the frame must still
        // match the window it is shown in.
        Assert.Equal((s.Game.Window.Width, s.Game.Window.Height), (after.Width, after.Height));
        if (HonorsRequestedSize)
        {
            Assert.Equal((480, 300), (after.Width, after.Height));
            Assert.NotEqual((before.Width, before.Height), (after.Width, after.Height));
        }

        // The UI was laid out for the new size.
        Assert.Equal(s.Game.Window.Width, (int)s.Game.Ui.Root.AbsoluteBounds.Width);
        Session.Save(BackendName + "-resized", after);

        s.Game.Window.RequestSize(700, 420);
        s.StepUntil(() => s.Game.PresentedFrames >= started + 12);
        Assert.Equal((s.Game.Window.Width, s.Game.Window.Height), (s.Game.CapturePresented()!.Width, s.Game.CapturePresented()!.Height));
        Assert.Empty(s.Validation());
    });

    [Fact]
    public void InjectedPointerAndTextReachTheUiTheSameWayRealInputDoes() => MainThread.Run(() =>
    {
        using var s = Start();

        s.Click("click");
        s.Call("step", """{"frames":2}""");
        Assert.Equal("1", s.Call("state_query", """{"entity":"Clicker","property":"Clicks"}""").ToString().Trim('"'));
        Assert.Equal("Clicks: 1", s.Text("clicks"));

        // Clicking the field focuses it; typed text and Backspace then edit it.
        s.Click("name");
        Assert.Contains("name", s.Call("input_text", """{"text":"hello"}""").GetProperty("focused").GetString(), StringComparison.Ordinal);
        Assert.Equal("hello", s.Field.FieldText);
        s.Call("input_key", """{"key":"Backspace"}""");
        Assert.Equal("hell", s.Field.FieldText);
        Assert.True(s.Game.Window.TextInputActive, "a focused text field must switch the platform's text input on");

        // Gamepad focus navigation reaches the button and activates it.
        s.Call("input_nav", """{"action":"up"}""");
        s.Call("input_nav", """{"action":"activate"}""");
        s.Call("step", """{"frames":2}""");
        Assert.Equal("Clicks: 2", s.Text("clicks"));
        Assert.False(s.Game.Window.TextInputActive, "leaving the field must switch it off");
        Assert.Empty(s.Validation());
        Assert.Equal(0, s.Call("info").GetProperty("drift").GetInt32());
    });

    [Fact]
    public void CompositionShowsInTheFieldAndTheCommitReplacesIt() => MainThread.Run(() =>
    {
        using var s = Start();
        s.Click("name");
        s.Call("input_composition", """{"text":"ka","cursorStart":2,"cursorEnd":2}""");
        Assert.Equal("ka", s.Field.Preedit);
        s.Call("step", """{"frames":2}""");
        Assert.Equal("ka", s.Game.Ui.Find("name/text")!.Text);
        Assert.True(s.Game.Window.TextInputActive);
        Session.Save(BackendName + "-preedit", s.Game.CapturePresented()!);
        s.Call("input_composition", """{"text":""}""");
        s.Call("input_text", """{"text":"か"}""");
        Assert.Equal("", s.Field.Preedit);
        Assert.Equal("か", s.Field.FieldText);
    });

    [Fact]
    public void CopyAndPasteGoThroughTheSystemClipboard() => MainThread.Run(() =>
    {
        using var s = Start();
        Assert.SkipUnless(s.Game.Clipboard.IsAvailable, "this compositor has no seat, so it has no clipboard");
        s.Click("name");
        s.Call("clipboard_set", """{"text":"from outside"}""");
        s.Call("input_key", """{"key":"v","modifiers":["Control"]}""");
        Assert.Equal("from outside", s.Field.FieldText);

        s.Call("input_text", """{"text":"!"}""");
        s.Call("input_key", """{"key":"a","modifiers":["Control"]}""");
        s.Call("input_key", """{"key":"c","modifiers":["Control"]}""");
        Assert.Equal("from outside!", s.Call("clipboard_get").GetProperty("text").GetString());
    });

    [Fact]
    public void LiveControlOverTheSocketWorksWhileTheWindowPresents() => MainThread.Run(() =>
    {
        using var s = Start();
        using var server = new LiveServer(s.Control);
        var path = Path.Combine(s.OutputDirectory, "live.sock");
        server.Start(path);
        using var client = new McpClient(path);

        JsonElement Ask(string method, string args)
        {
            var reply = client.Send(method, args);
            var end = Environment.TickCount64 + 15_000;
            while (!reply.IsCompleted && Environment.TickCount64 < end)
            {
                server.Pump();
                s.Game.Step();
                Thread.Sleep(2);
            }

            return reply.IsCompleted ? reply.Result : throw new TimeoutException(method);
        }

        Ask("initialize", """{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"test","version":"1"}}""");
        var tools = Ask("tools/list", "{}").GetProperty("result").GetProperty("tools");
        Assert.Contains(tools.EnumerateArray(), t => t.GetProperty("name").GetString() == "input_composition");

        var before = s.Game.PresentedFrames;
        var click = Center(s, "click");
        Ask("tools/call", $$$"""{"name":"input_pointer","arguments":{"action":"click","x":{{{click.X}}},"y":{{{click.Y}}}}}""");
        Ask("tools/call", """{"name":"step","arguments":{"frames":3}}""");
        var state = Ask("tools/call", """{"name":"state_query","arguments":{"entity":"Clicker","property":"Clicks"}}""");
        Assert.Contains("1", state.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString(), StringComparison.Ordinal);

        var capture = Ask("tools/call", """{"name":"capture_frame","arguments":{"presented":true}}""");
        var content = capture.GetProperty("result").GetProperty("content");
        Assert.Contains(content.EnumerateArray(), c => c.GetProperty("type").GetString() == "image");
        Assert.Contains("\"renderer\"", content[0].GetProperty("text").GetString(), StringComparison.Ordinal);
        Assert.True(s.Game.PresentedFrames > before, "the window kept presenting while requests were served");
        Assert.Empty(s.Validation());
    });

    private static (double X, double Y) Center(Session s, string id) => s.Center(id);
}

/// <summary>The windowed suite on Windows: a Win32 window with a Vulkan swapchain, driven through the control layer.</summary>
public sealed class Win32WindowedTests : WindowedTests
{
    private protected override bool IsX11 => false;

    private protected override string BackendName => "win32";

    private protected override bool HonorsRequestedSize => true;

    private protected override Session Start()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "needs Windows");
        return new Session(PlatformBackendKind.Win32, "");
    }
}

public sealed class X11WindowedTests : WindowedTests, IClassFixture<XvfbFixture>
{
    private readonly XvfbFixture _xvfb;

    public X11WindowedTests(XvfbFixture xvfb) => _xvfb = xvfb;

    private protected override bool IsX11 => true;

    private protected override Session Start()
    {
        Assert.SkipUnless(_xvfb.Available, "Xvfb is not installed");
        return new Session(PlatformBackendKind.X11, _xvfb.Display!);
    }

    [Fact]
    public unsafe void MinimizingAndFocusLossAreSurvivedAndPresentingResumes()
    {
        using var s = Start();
        var name = Encoding.ASCII.GetBytes(_xvfb.Display + "\0");
        nint other;
        fixed (byte* p = name)
        {
            other = Xlib.OpenDisplay(p);
        }

        var window = (nuint)s.Game.Window.NativeHandle.Window;
        s.Game.Platform.Injector.Key(s.Game.Window.Id, Key.W, true);
        Assert.True(s.Game.Platform.Input.IsKeyDown(Key.W));

        // Another window takes the keyboard focus: the game notices, releases held keys and carries on.
        var thief = Xlib.CreateSimpleWindow(other, Xlib.RootWindow(other, 0), 0, 0, 50, 50, 0, 0, 0);
        Xlib.MapWindow(other, thief);
        Xlib.SetInputFocus(other, thief, 2, 0);
        Xlib.Flush(other);
        s.StepUntil(() => !s.Game.Window.IsFocused);
        Assert.False(s.Game.Window.IsFocused);
        Assert.False(s.Game.Platform.Input.IsKeyDown(Key.W));
        var tick = s.Game.World.Tick;
        s.Game.Step(3);
        Assert.Equal(tick + 3, s.Game.World.Tick);

        // Hiding the window (how a minimize looks without a window manager) stops presenting but not the simulation.
        Xlib.UnmapWindow(other, window);
        Xlib.Flush(other);
        s.StepUntil(() => s.Game.Window.IsMinimized);
        Assert.True(s.Game.Window.IsMinimized);
        var presented = s.Game.PresentedFrames;
        var tickBefore = s.Game.World.Tick;
        s.Game.Step(10);
        Assert.Equal(presented, s.Game.PresentedFrames);
        Assert.Equal(tickBefore + 10, s.Game.World.Tick);

        Xlib.MapWindow(other, window);
        Xlib.Flush(other);
        s.StepUntil(() => s.Game.PresentedFrames > presented + 2);
        Assert.True(s.Game.PresentedFrames > presented + 2, "presenting did not resume after the window was shown again");
        Assert.Empty(s.Validation());
        Xlib.CloseDisplay(other);
    }

    [Fact]
    public unsafe void ADesktopScaleChangeRelaysTheUiAtTheNewScale()
    {
        using var s = Start();
        var button = s.Game.Ui.Find("click")!;
        var narrow = button.AbsoluteBounds.Width;
        Assert.Equal(80f, narrow);

        var name = Encoding.ASCII.GetBytes(_xvfb.Display + "\0");
        nint other;
        fixed (byte* p = name)
        {
            other = Xlib.OpenDisplay(p);
        }

        nuint atom;
        fixed (byte* n = "RESOURCE_MANAGER\0"u8)
        {
            atom = Xlib.InternAtom(other, n, 0);
        }

        var value = Encoding.ASCII.GetBytes("Xft.dpi:\t192\n");
        fixed (byte* v = value)
        {
            Xlib.ChangeProperty(other, Xlib.RootWindow(other, 0), atom, 31, 8, Xlib.PropModeReplace, v, value.Length);
        }

        Xlib.Flush(other);
        s.StepUntil(() => s.Game.UiScale > 1.5f);
        Xlib.CloseDisplay(other);
        Assert.Equal(2f, s.Game.UiScale);
        Assert.Equal(160f, s.Game.Ui.Find("click")!.AbsoluteBounds.Width);
        var frame = s.Game.CapturePresented()!;
        Session.Save("x11-scale2", frame);
        Assert.Empty(s.Validation());
    }

    [Fact]
    public void TextCopiedInAnotherProgramPastesIntoTheField()
    {
        using var s = Start();
        var options = new PlatformOptions { Backend = PlatformBackendKind.X11, DisplayName = _xvfb.Display, EnableGamepadDevices = false };
        using var other = new AppThread(options);
        Assert.True(other.Run(a => a.Clipboard.SetText("pasted from another program")));

        // The game reads the other program's clipboard through the X selection protocol while it keeps presenting.
        s.Click("name");
        s.Call("input_key", """{"key":"v","modifiers":["Control"]}""");
        Assert.Equal("pasted from another program", s.Field.FieldText);
    }
}

public sealed class WestonWindowedTests : WindowedTests, IClassFixture<WestonFixture>
{
    private readonly WestonFixture _weston;

    public WestonWindowedTests(WestonFixture weston) => _weston = weston;

    private protected override bool IsX11 => false;

    private protected override Session Start()
    {
        Assert.SkipUnless(_weston.Available, "weston is not installed");
        return new Session(PlatformBackendKind.Wayland, _weston.SocketPath!);
    }

    /// <summary>
    /// A compositor that stops releasing buffers leaves every swapchain image in its hands. Acquiring used to wait for one forever, which
    /// froze the whole frame loop (and the control layer with it). Now the frames are skipped and the game keeps answering.
    /// </summary>
    [Fact]
    public void ACompositorThatStopsAnsweringDoesNotHangTheFrameLoop()
    {
        Assert.SkipUnless(_weston.Available, "weston is not installed");

        // Low-latency presentation does not wait for frame callbacks, so a stalled compositor shows up where the swapchain waits for a free image.
        using var s = new Session(PlatformBackendKind.Wayland, _weston.SocketPath!, OneGame.Gpu.PresentMode.LowLatency);
        s.StepUntil(() => s.Game.PresentedFrames >= 5);
        var thawed = false;
        _weston.Freeze(true);
        using var watchdog = new Timer(_ =>
        {
            thawed = true;
            _weston.Freeze(false);
        }, null, TimeSpan.FromSeconds(45), Timeout.InfiniteTimeSpan);
        try
        {

            // Each skipped frame waits out the acquire timeout, so a few steps are enough to see the loop keep turning.
            for (var i = 0; i < 6; i++)
            {
                s.Game.Step();
            }
        }
        finally
        {
            _weston.Freeze(false);
        }

        Assert.False(thawed, "the frame loop was blocked until the watchdog thawed the compositor");
        Assert.Equal("wayland", s.Call("info").GetProperty("backend").GetString());
    }
}
