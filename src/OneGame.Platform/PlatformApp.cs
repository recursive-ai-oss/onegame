namespace OneGame.Platform;

/// <summary>
/// The entry point of the platform layer: owns the connection to the windowing system, the windows, the gamepads and the single
/// event queue. Use it from one thread (normally the one that runs the game loop).
/// </summary>
public sealed class PlatformApp : IDisposable
{
    private const int MaxQueuedEvents = 16384;

    private readonly Queue<PlatformEvent> _queue = new();
    private readonly List<Window> _windows = [];
    private readonly PlatformBackend _backend;
    private int _nextWindowId = 1;

    private PlatformApp(PlatformOptions options, Func<PlatformApp, PlatformBackend> backendFactory)
    {
        Options = options;
        Input = new InputState();
        Injector = new InputInjector(this);
        _backend = backendFactory(this);
        Gamepads = new GamepadManager(this, options.EnableGamepadDevices);
    }

    /// <summary>The options this application was created with.</summary>
    public PlatformOptions Options { get; }

    internal PlatformBackend NativeBackend => _backend;

    /// <summary>Which backend was chosen.</summary>
    public PlatformBackendKind Backend => _backend.Kind;

    /// <summary>What is currently held.</summary>
    public InputState Input { get; }

    /// <summary>Synthetic input. Events it makes are queued exactly like real ones.</summary>
    public InputInjector Injector { get; }

    /// <summary>Gamepad devices and rumble.</summary>
    public GamepadManager Gamepads { get; }

    /// <summary>The open windows.</summary>
    public IReadOnlyList<Window> Windows => _windows;

    /// <summary>True once a quit was requested.</summary>
    public bool QuitRequested { get; private set; }

    /// <summary>How many events were dropped because the queue was full (the game stopped polling).</summary>
    public int DroppedEvents { get; private set; }

    /// <summary>Connects to the windowing system.</summary>
    /// <exception cref="PlatformException">The requested backend is not available here.</exception>
    public static PlatformApp Create(PlatformOptions? options = null)
    {
        options ??= new PlatformOptions();
        var kind = options.Backend;
        if (kind == PlatformBackendKind.Auto)
        {
            kind = PlatformBackendFactory.Detect();
        }

        return new PlatformApp(options, app => PlatformBackendFactory.Open(kind, app));
    }

    /// <summary>Creates a window.</summary>
    /// <exception cref="PlatformException">The window system refused.</exception>
    public Window CreateWindow(WindowOptions? options = null)
    {
        options ??= new WindowOptions();
        if (options.Width <= 0 || options.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Window size must be positive.");
        }

        var window = _backend.CreateWindow(options, _nextWindowId++);
        _windows.Add(window);
        return window;
    }

    /// <summary>
    /// Reads everything the operating system and the gamepads have to say and queues it. Call it once per frame.
    /// </summary>
    /// <param name="timeoutMilliseconds">0 returns at once; a positive value waits up to that long for the first event.</param>
    public void PumpEvents(int timeoutMilliseconds = 0)
    {
        _backend.Pump(timeoutMilliseconds);
        Gamepads.Poll();
    }

    /// <summary>Takes the next queued event, oldest first.</summary>
    public bool TryGetEvent(out PlatformEvent platformEvent) => _queue.TryDequeue(out platformEvent);

    /// <summary>Asks the application to quit; the game sees <see cref="PlatformEventKind.QuitRequested"/>.</summary>
    public void RequestQuit() => Post(new PlatformEvent { Kind = PlatformEventKind.QuitRequested });

    /// <summary>Closes every window and disconnects.</summary>
    public void Dispose()
    {
        foreach (var w in _windows.ToArray())
        {
            w.Close();
        }

        Gamepads.Dispose();
        _backend.Dispose();
    }

    // Every event, whatever its source, goes through here.
    internal void Post(in PlatformEvent e)
    {
        if (e.Kind == PlatformEventKind.QuitRequested)
        {
            QuitRequested = true;
        }

        Input.Apply(e);
        if (_queue.Count >= MaxQueuedEvents)
        {
            DroppedEvents++;
            return;
        }

        _queue.Enqueue(e);
    }

    internal void OnWindowClosed(Window window) => _windows.Remove(window);

    internal Window? FindWindow(int id)
    {
        foreach (var w in _windows)
        {
            if (w.Id == id)
            {
                return w;
            }
        }

        return null;
    }
}

/// <summary>Thrown when the platform layer cannot do what was asked.</summary>
public sealed class PlatformException : Exception
{
    /// <summary>Creates the exception.</summary>
    public PlatformException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception.</summary>
    public PlatformException(string message, Exception inner)
        : base(message, inner)
    {
    }

    /// <summary>Creates the exception.</summary>
    public PlatformException()
    {
    }
}

/// <summary>The per-OS half of <see cref="PlatformApp"/>.</summary>
internal abstract class PlatformBackend : IDisposable
{
    protected PlatformBackend(PlatformApp app) => App = app;

    protected PlatformApp App { get; }

    public abstract PlatformBackendKind Kind { get; }

    public abstract Window CreateWindow(WindowOptions options, int id);

    public abstract void Pump(int timeoutMilliseconds);

    public virtual void Dispose()
    {
    }
}

internal static class PlatformBackendFactory
{
    public static PlatformBackendKind Detect()
    {
        if (OperatingSystem.IsWindows())
        {
            return PlatformBackendKind.Win32;
        }

        if (OperatingSystem.IsLinux())
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"))
                || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_SOCKET")))
            {
                return PlatformBackendKind.Wayland;
            }

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")))
            {
                return PlatformBackendKind.X11;
            }
        }

        return PlatformBackendKind.Headless;
    }

    public static PlatformBackend Open(PlatformBackendKind kind, PlatformApp app) => kind switch
    {
        PlatformBackendKind.Headless => new HeadlessBackend(app),
        PlatformBackendKind.X11 when OperatingSystem.IsLinux() => new X11Backend(app),
        PlatformBackendKind.Wayland when OperatingSystem.IsLinux() => new WaylandBackend(app),
        PlatformBackendKind.Win32 when OperatingSystem.IsWindows() => new Win32Backend(app),
        _ => throw new PlatformException($"The {kind} backend is not available on this operating system."),
    };
}
