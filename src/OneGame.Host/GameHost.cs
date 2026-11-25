using RecursiveAI.OneGpu;
using OneGame.Platform;

namespace OneGame.Host;

/// <summary>
/// The parts of a windowed game that are costly to build and need not die with one <see cref="GameInstance"/>: the connection to the
/// window system, the window, the graphics device and the swapchain. A process that runs several games one after another (a menu
/// loop, then a match, then the menu again) lets them share one host, so the later ones start drawing at once instead of paying for a new
/// window and device each time. <see cref="GameApp.Run(GameDefinition, string[])"/> uses <see cref="Shared"/> unless
/// <see cref="GameApp.ReuseWindow"/> is switched off.
/// </summary>
/// <remarks>
/// What survives a run: the window (position, size and focus as the player left them), the device and the swapchain. What does not: the
/// world, the session, the UI tree, the input actions, the render stage with its renderers and every texture or model the game uploaded.
/// Those belong to the instance and are rebuilt, so nothing one run did is visible to the next. A host serves one running instance at a
/// time and must be used from one thread. See <c>docs/interfaces/host-lifetime.md</c>.
/// </remarks>
/// <example>
/// <code>
/// using var host = new GameHost();
/// for (var i = 0; i &lt; 3; i++)
/// {
///     using var game = GameInstance.Create(new MyGame(), new GameOptions { Host = host });
///     game.RunRealtime(300);
/// }   // the window and device are still open here; disposing the host closes them
/// </code>
/// </example>
public sealed class GameHost : IDisposable
{
    private static readonly object SharedLock = new();
    private static GameHost? _shared;

    private PlatformApp? _platform;
    private Window? _window;
    private GpuDevice? _device;
    private GpuSwapchain? _swapchain;
    private bool _swapchainSrgb;
    private PresentMode _swapchainMode;
    private PlatformBackendKind _backend;
    private string? _display;
    private (int Width, int Height) _requested;
    private bool _inUse;
    private bool _disposed;

    /// <summary>The host <see cref="GameApp.Run(GameDefinition, string[])"/> uses. It is closed when the process exits, or earlier with <see cref="Release"/>.</summary>
    public static GameHost Shared
    {
        get
        {
            lock (SharedLock)
            {
                if (_shared is not { _disposed: false })
                {
                    var host = new GameHost();
                    AppDomain.CurrentDomain.ProcessExit += (_, _) => host.CloseAtExit();
                    _shared = host;
                }

                return _shared;
            }
        }
    }

    /// <summary>Closes the shared host's window and device now, if one is open and no game is running on it. The next run opens new ones.</summary>
    public static void Release()
    {
        GameHost? host;
        lock (SharedLock)
        {
            host = _shared;
            _shared = null;
        }

        host?.Dispose();
    }

    /// <summary>True while a window is open and waiting for the next run.</summary>
    public bool HasWindow => _window is { IsClosed: false };

    /// <summary>True while a graphics device is held for the next run.</summary>
    public bool HasDevice => _device is not null;

    /// <summary>How many instances have used this host.</summary>
    public int Runs { get; private set; }

    /// <summary>
    /// Keeps the held window answering the window system while the game does something else between runs (a long load, a sign-in). Without it a window
    /// manager may mark the window as not responding. Does nothing when there is no window or a game is running on it.
    /// </summary>
    public void Pump()
    {
        if (!_inUse && _platform is { } platform)
        {
            platform.PumpEvents();
            while (platform.TryGetEvent(out _))
            {
            }
        }
    }

    internal void ThrowIfBusy()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_inUse)
        {
            throw new InvalidOperationException("This GameHost already serves a running game. Dispose that GameInstance first, or give the second game its own host.");
        }
    }

    internal void Abort()
    {
        _inUse = false;
        Tear();
    }

    // What an instance gets when it starts. The window is reused when the options ask for the same window system and display;
    // a window the player closed, or a different target, is replaced.
    internal (PlatformApp Platform, Window Window) Acquire(GameOptions options, string title)
    {
        ThrowIfBusy();
        if (_platform is not null && (_window!.IsClosed || options.Backend != _backend || options.DisplayName != _display))
        {
            Tear();
        }

        if (_platform is null)
        {
            _platform = PlatformApp.Create(new PlatformOptions
            {
                Backend = options.Backend,
                DisplayName = options.CanvasId ?? options.DisplayName,
                EnableGamepadDevices = !OneGame.Platform.RunningOn.Android,
            });
            _window = _platform.CreateWindow(new WindowOptions { Title = title, Width = options.Width, Height = options.Height });
            _backend = options.Backend;
            _display = options.DisplayName;
            _requested = (options.Width, options.Height);
        }
        else
        {
            // Events that arrived between runs (a stale quit, keys held, a click) belong to nobody.
            _platform.ResetForNewRun();
            if (_window!.Title != title)
            {
                _window.SetTitle(title);
            }

            if (_requested != (options.Width, options.Height))
            {
                // A game that asks for another size than the last one gets it; one that asks for the same keeps whatever the player did to the window.
                _window.RequestSize(options.Width, options.Height);
                _requested = (options.Width, options.Height);
            }
        }

        _inUse = true;
        Runs++;
        return (_platform, _window!);
    }

    internal GpuDevice? Device => _device;

    internal void SetDevice(GpuDevice? device) => _device = device;

    internal GpuSwapchain? TakeSwapchain(out bool srgb, out PresentMode mode)
    {
        srgb = _swapchainSrgb;
        mode = _swapchainMode;
        var swapchain = _swapchain;
        _swapchain = null;
        return swapchain;
    }

    // The instance is done: it hands back its swapchain and the host becomes available. A window the player closed takes everything with it.
    internal void Return(GpuSwapchain? swapchain, bool srgb, PresentMode mode)
    {
        _swapchain = swapchain;
        _swapchainSrgb = srgb;
        _swapchainMode = mode;
        _inUse = false;
        if (_window is { IsClosed: true } || _disposed)
        {
            Tear();
        }
        else if (_window is not null)
        {
            _window.SetTextInput(false);
            _window.SetCursorHidden(false);
            _window.SetCursorLocked(false);
        }
    }

    // The window system connection closes while the graphics driver is still loaded: the driver hooks display close on the connection,
    // and destroying the device's instance can unload it. The surface goes with the swapchain, before the window.
    private void Tear()
    {
        var device = _device;
        var swapchain = _swapchain;
        var platform = _platform;
        _swapchain = null;
        _platform = null;
        _window = null;
        _device = null;
        device?.WaitIdle();
        swapchain?.Dispose();
        platform?.Dispose();
        device?.Dispose();
    }

    private void CloseAtExit()
    {
        // A process that exits from inside a running game leaves the teardown to the operating system, as it always did. On macOS
        // AppKit objects must be closed on the main thread and this handler may not run there, so a game that opted in calls Release itself.
        if (!_inUse && !OperatingSystem.IsMacOS())
        {
            Dispose();
        }
    }

    /// <summary>Closes the window and the device. Call it only when no game is running on the host.</summary>
    /// <exception cref="InvalidOperationException">A game is still running on the host.</exception>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_inUse)
        {
            throw new InvalidOperationException("A game is still running on this GameHost. Dispose its GameInstance first.");
        }

        _disposed = true;
        Tear();
    }
}
