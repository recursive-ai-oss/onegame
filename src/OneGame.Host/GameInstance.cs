using System.Diagnostics;
using OneGame.Core;
using OneGame.Gpu;
using OneGame.Atlas;
using OneGame.Gpu.Vulkan;
using OneGame.Render2D;
using OneGame.Text;
using OneGame.Platform;
using OneGame.Sync;
using OneGame.UI;

namespace OneGame.Host;

/// <summary>How the host picks a graphics device.</summary>
public enum GpuMode
{
    /// <summary>Use a device if one can be created; otherwise render captures on the CPU.</summary>
    Auto,

    /// <summary>Never create a device. Captures are rendered on the CPU.</summary>
    Off,

    /// <summary>Fail if no device can be created.</summary>
    Required,
}

/// <summary>Options of a <see cref="GameInstance"/>.</summary>
public sealed record GameOptions
{
    /// <summary>No window: events come only from injection and nothing is presented.</summary>
    public bool Headless { get; init; }

    /// <summary>Window (or virtual window) width in pixels.</summary>
    public int Width { get; init; } = 1280;

    /// <summary>Window (or virtual window) height in pixels.</summary>
    public int Height { get; init; } = 720;

    /// <summary>UI scale for a headless instance. A real window uses the scale the OS reports.</summary>
    public float Scale { get; init; } = 1f;

    /// <summary>Replaces the definition's seed when set.</summary>
    public ulong? Seed { get; init; }

    /// <summary>Graphics device policy.</summary>
    public GpuMode Gpu { get; init; } = GpuMode.Auto;

    /// <summary>Where the 2D renderer gets its compiled shaders. When null, the folder named by <c>ONEGAME_SHADER_DIR</c> is used if set.</summary>
    public IShaderSource? Shaders { get; init; }
}

/// <summary>A captured frame: RGBA8 pixels, top row first.</summary>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
/// <param name="Rgba">Pixels, <c>Width × Height × 4</c> bytes.</param>
/// <param name="Renderer">Where the pixels were made: the adapter name, or <c>cpu</c>.</param>
public sealed record CapturedFrame(int Width, int Height, byte[] Rgba, string Renderer);

/// <summary>
/// One running game: the world, the UI tree, the window and event queue, and the render stage, advanced one frame at a time.
/// Each frame handles input, runs one fixed simulation tick and brings the UI up to date. A windowed instance also draws and
/// presents. Everything the harness does to a game goes through this type.
/// </summary>
public sealed class GameInstance : IDisposable
{
    /// <summary>Simulation ticks per second of a realtime run.</summary>
    public const int TickRate = 60;

    private readonly ScreenSet _screenSet = new();
    private readonly FixtureSet _fixtureSet = new();
    private readonly EntityRegistry _registry = new();
    private readonly IRenderStage _stage;
    private GpuDevice? _device;
    private string? _gpuError;
    private bool _gpuTried;
    private GpuSwapchain? _swapchain;
    private bool _disposed;
    private Cluster _cluster = null!;
    private AuthorityScope _scope = null!;
    private ReplicaScope _replica = null!;

    private GameInstance(GameDefinition definition, GameOptions options)
    {
        Definition = definition;
        Options = options;
        definition.RegisterEntities(_registry);
        definition.DefineScreens(_screenSet);
        definition.DefineFixtures(_fixtureSet);
        var fonts = new FontLibrary();
        definition.DefineFonts(fonts);
        var haveFonts = fonts.FaceCount > 0;
        var measurer = haveFonts ? new FontTextMeasurer(fonts) : null;
        var art = new ArtAtlas();
        definition.DefineArt(art);
        var shaders = options.Shaders ?? (Environment.GetEnvironmentVariable("ONEGAME_SHADER_DIR") is { Length: > 0 } dir ? new DirectoryShaders(dir) : null);
        _stage = definition.CreateRenderStage(new HostResources(haveFonts ? fonts : null, measurer, art, shaders));
        Platform = PlatformApp.Create(new PlatformOptions
        {
            Backend = options.Headless ? PlatformBackendKind.Headless : PlatformBackendKind.Auto,
            EnableGamepadDevices = !options.Headless,
        });
        Window = Platform.CreateWindow(new WindowOptions { Title = definition.Name, Width = options.Width, Height = options.Height });
        Ui = new UiTree(measurer);
        Seed = options.Seed ?? definition.Seed;
        Attach(NewWorld(null));
        definition.Style(Ui);
        SyncViewport();
        OpenScreen(_screenSet.Names.Any(n => n == "main") ? "main" : _screenSet.Names.Count > 0 ? _screenSet.Names[0] : throw new InvalidOperationException($"{definition.Name} declares no screens. Add one named \"main\" in DefineScreens."));
    }

    /// <summary>Creates an instance. Disposing it closes the window and the device.</summary>
    public static GameInstance Create(GameDefinition definition, GameOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new GameInstance(definition, options ?? new GameOptions { Headless = true });
    }

    /// <summary>Raised after the world is replaced by a reset, a fixture or a snapshot load. Recorders reattach here.</summary>
    public event Action<World>? WorldReplaced;

    /// <summary>Raised after every simulation tick with the world that ran it. Recorders use it to log state hashes; keep handlers cheap.</summary>
    public event Action<World>? TickCompleted;

    /// <summary>The game's definition.</summary>
    public GameDefinition Definition { get; }

    /// <summary>The options the instance was created with.</summary>
    public GameOptions Options { get; }

    /// <summary>The authority world: the state the simulation runs on. Read it through this property rather than keeping the reference: it changes when a snapshot or fixture is loaded.</summary>
    public World World { get; private set; } = null!;

    /// <summary>The single-player cluster: one authority node holding <see cref="World"/> and one local replica of it. Rebuilt whenever the world is replaced.</summary>
    public Cluster Cluster => _cluster;

    /// <summary>The authority scope that owns <see cref="World"/>. Recordings start here.</summary>
    public AuthorityScope Scope => _scope;

    /// <summary>The local replica of the world, kept in step by the cluster. Its hash must equal the authority's at every tick; drift is reported by <see cref="DriftReports"/>.</summary>
    public ReplicaScope Replica => _replica;

    /// <summary>Drift reports filed so far by the local replica; empty while replica and authority agree.</summary>
    public IReadOnlyList<DriftReport> DriftReports => _cluster.DriftReports;

    /// <summary>The seed of the current run.</summary>
    public ulong Seed { get; private set; }

    /// <summary>The UI tree.</summary>
    public UiTree Ui { get; }

    /// <summary>The platform app (window, events, injection).</summary>
    public PlatformApp Platform { get; }

    /// <summary>The game window.</summary>
    public Window Window { get; }

    /// <summary>Frames run so far.</summary>
    public long Frame { get; private set; }

    /// <summary>True once the window was closed or a quit was requested.</summary>
    public bool QuitRequested => Platform.QuitRequested || Window.IsClosed;

    /// <summary>The name of the screen that is open.</summary>
    public string? CurrentScreen { get; private set; }

    /// <summary>Names of the declared screens.</summary>
    public IReadOnlyList<string> ScreenNames => _screenSet.Names;

    /// <summary>Names of the declared fixtures.</summary>
    public IReadOnlyList<string> FixtureNames => _fixtureSet.Names;

    /// <summary>The render stage.</summary>
    public IRenderStage Stage => _stage;

    /// <summary>Why no graphics device exists, when <see cref="Options"/> allowed one; null otherwise.</summary>
    public string? GpuError => _gpuError;

    /// <summary>The name of the graphics adapter in use, or null while none has been created.</summary>
    public string? AdapterName => _device?.Adapter.Name;

    /// <summary>The current UI scale.</summary>
    public float UiScale => Options.Headless ? Options.Scale : Window.Scale;

    private World NewWorld(Action<World>? populate)
    {
        var world = new World(Seed, _registry);
        Definition.RegisterSystems(world);
        if (populate is null)
        {
            Definition.Setup(world);
        }
        else
        {
            populate(world);
        }

        return world;
    }

    /// <summary>Builds a world the way the game does (registry, tables, systems) and fills it with the game's starting state. Replays run on such a world.</summary>
    public World CreateWorld() => NewWorld(null);

    /// <summary>Builds an empty world with the game's registry and systems, the starting point for a replay or a snapshot load.</summary>
    public World CreateBlankWorld() => NewWorld(static _ => { });

    private void Attach(World world)
    {
        World = world;
        _cluster = new Cluster(new ClusterOptions());
        var node = _cluster.AddNode("game");
        _scope = _cluster.AddScope(node, new ScopeId(1), world);
        var view = new World(Seed, _registry, WorldRole.Replica);
        Definition.RegisterSystems(view);
        _replica = _cluster.AddLocalReplica(_scope, view);
        _cluster.Settle();
    }

    private void Replace(World world)
    {
        Attach(world);
        WorldReplaced?.Invoke(world);
    }

    /// <summary>Starts recording the run from the current state. Stop the recorder to get the <see cref="Recording"/>; replay it with <see cref="Replay"/>.</summary>
    /// <exception cref="InvalidOperationException">Commands are queued; step once first.</exception>
    public Recorder StartRecording(RecordingOptions? options = null) => _scope.StartRecording(options ?? new RecordingOptions { CheckpointInterval = 1 });

    /// <summary>Replays a recording on a fresh world built like the game's and compares the hashes. The running game is left alone.</summary>
    public ReplayResult Replay(Recording recording, Action<World>? onTick = null)
    {
        ArgumentNullException.ThrowIfNull(recording);
        return Replayer.Replay(recording, CreateBlankWorld(), new ReplayOptions { OnTick = onTick });
    }

    /// <summary>Starts a new game: a fresh world filled by <see cref="GameDefinition.Setup"/>, optionally with a different seed.</summary>
    public void Reset(ulong? seed = null)
    {
        if (seed is { } s)
        {
            Seed = s;
        }

        Replace(NewWorld(null));
    }

    /// <summary>Replaces the world with one filled by a named fixture.</summary>
    /// <exception cref="KeyNotFoundException">No fixture has that name.</exception>
    public void LoadFixture(string name)
    {
        if (!_fixtureSet.TryGet(name, out var populate))
        {
            throw new KeyNotFoundException($"No fixture named '{name}'. Declared: {string.Join(", ", _fixtureSet.Names)}.");
        }

        Replace(NewWorld(populate));
    }

    /// <summary>Replaces the world with the state in a save (see <see cref="SaveFile"/>).</summary>
    /// <exception cref="InvalidDataException">The save is malformed, damaged or from an unsupported version.</exception>
    public void LoadSnapshot(ReadOnlySpan<byte> snapshot)
    {
        var world = NewWorld(static _ => { });
        var result = SaveFile.Load(snapshot, world);
        if (result.Status != SaveStatus.Ok)
        {
            throw new InvalidDataException($"The save could not be loaded: {result.Status} ({result.Detail}).");
        }

        Seed = world.Seed;
        Replace(world);
    }

    /// <summary>Writes the current world as a save file (header, hashes, snapshot).</summary>
    public byte[] SaveSnapshot() => SaveFile.Write(World);

    /// <summary>Replaces the UI with a declared screen.</summary>
    /// <exception cref="KeyNotFoundException">No screen has that name.</exception>
    public void OpenScreen(string name)
    {
        if (!_screenSet.TryGet(name, out var build))
        {
            throw new KeyNotFoundException($"No screen named '{name}'. Declared: {string.Join(", ", _screenSet.Names)}.");
        }

        Ui.Root.Clear();
        Ui.Root.Add(build(this));
        CurrentScreen = name;
    }

    /// <summary>Resizes the window (or the virtual window of a headless run) and lets the UI lay out at the new size.</summary>
    public void Resize(int width, int height)
    {
        Window.RequestSize(width, height);
        DrainInput();
    }

    private void SyncViewport() => Ui.SetViewport(Window.Width, Window.Height, UiScale);

    /// <summary>Runs <paramref name="frames"/> frames: input, one tick, UI update (and drawing when windowed).</summary>
    public void Step(int frames = 1)
    {
        for (var i = 0; i < frames && !QuitRequested; i++)
        {
            RunFrame(1);
        }
    }

    /// <summary>Handles all pending input without running a tick. Control operations call this after injecting input so its effect is visible at once.</summary>
    public void DrainInput()
    {
        Platform.PumpEvents();
        while (Platform.TryGetEvent(out var e))
        {
            Handle(e);
        }

        Ui.Update();
    }

    private void RunFrame(int ticks)
    {
        DrainInput();
        for (var t = 0; t < ticks; t++)
        {
            _cluster.Step(1);
            TickCompleted?.Invoke(World);
        }

        Frame++;
        if (Options.Headless)
        {
            Ui.Update();
        }
        else
        {
            Present();
        }
    }

    private void Handle(in PlatformEvent e)
    {
        switch (e.Kind)
        {
            case PlatformEventKind.WindowResized:
                SyncViewport();
                _stage.ReleaseTargets(null);
                _swapchain?.Resize((uint)Math.Max(1, e.Width), (uint)Math.Max(1, e.Height));
                return;
            case PlatformEventKind.WindowScaleChanged:
                SyncViewport();
                return;
            case PlatformEventKind.WindowCloseRequested:
                Window.Close();
                return;
        }

        if (InputRouter.TryTranslate(e, out var ui) && Ui.Dispatch(ui))
        {
            return;
        }

        Definition.OnInput(this, e);
    }

    /// <summary>Runs in real time until the window closes or <paramref name="maxFrames"/> frames have run (0 = no limit). One call to <paramref name="beforeFrame"/> runs before each frame, for live control.</summary>
    public void RunRealtime(long maxFrames = 0, Action? beforeFrame = null)
    {
        var step = Stopwatch.Frequency / TickRate;
        var clock = Stopwatch.StartNew();
        long owed = 0;
        var last = clock.ElapsedTicks;
        var started = Frame;
        while (!QuitRequested && (maxFrames == 0 || Frame - started < maxFrames))
        {
            beforeFrame?.Invoke();
            var now = clock.ElapsedTicks;
            owed = Math.Min(owed + (now - last), step * 5);
            last = now;
            var ticks = 0;
            while (owed >= step)
            {
                owed -= step;
                ticks++;
            }

            RunFrame(ticks);
            if (Options.Headless)
            {
                Thread.Sleep(1);
            }
        }
    }

    private void EnsureDevice()
    {
        if (_gpuTried || Options.Gpu == GpuMode.Off)
        {
            return;
        }

        _gpuTried = true;
        try
        {
            if (!VulkanGpu.IsAvailable)
            {
                throw new GpuUnsupportedException("No Vulkan loader library was found.");
            }

            _device = VulkanGpu.CreateDevice(new GpuDeviceOptions
            {
                Validation = Environment.GetEnvironmentVariable("ONEGAME_GPU_VALIDATION") == "1",
                ApplicationName = Definition.Name,
            });
        }
        catch (GpuException ex)
        {
            _gpuError = ex.Message;
            if (Options.Gpu == GpuMode.Required)
            {
                throw;
            }
        }
    }

    /// <summary>Draws and presents the current state in a windowed instance. Does nothing in a headless one.</summary>
    public void Redraw()
    {
        if (!Options.Headless)
        {
            Present();
        }
    }

    private void Present()
    {
        EnsureDevice();
        var list = Ui.Frame();
        if (_device is null || Window.NativeHandle.Kind == NativeWindowKind.None)
        {
            return;
        }

        if (_swapchain is null)
        {
            var h = Window.NativeHandle;
            var source = h.Kind switch
            {
                NativeWindowKind.X11 => SurfaceSource.X11(h.Display, h.Window),
                NativeWindowKind.Wayland => SurfaceSource.Wayland(h.Display, h.Window),
                _ => SurfaceSource.Win32(h.Display, h.Window),
            };
            _swapchain = _device.CreateSwapchain(source, new SwapchainDesc((uint)Window.Width, (uint)Window.Height) { Srgb = _stage.RequiresSrgb });
        }

        if (_swapchain.TryAcquire(out var backbuffer) == AcquireStatus.Ready)
        {
            _stage.Render(_device, backbuffer, new FrameInfo(Window.Width, Window.Height, UiScale, World.Tick, list, World));
            _swapchain.Present();
        }
    }

    /// <summary>
    /// Renders the current state at a given size and UI scale into pixels. The UI is laid out for the capture size and put back
    /// afterwards. Uses the graphics device when there is one and the stage's CPU path otherwise.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The size is not positive or larger than 16384.</exception>
    public CapturedFrame Capture(int width, int height, float scale = 1f)
    {
        if (width is < 1 or > 16384 || height is < 1 or > 16384 || scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "A capture size must be between 1 and 16384 pixels and the scale positive.");
        }

        DrainInput();
        var pixels = new byte[checked(width * height * 4)];
        Ui.SetViewport(width, height, scale);
        try
        {
            var list = Ui.Frame();
            var info = new FrameInfo(width, height, scale, World.Tick, list, World);
            EnsureDevice();
            string renderer;
            if (_device is null)
            {
                _stage.RenderToPixels(pixels, info);
                renderer = "cpu";
            }
            else
            {
                using var target = _device.CreateTexture(new TextureDesc((uint)width, (uint)height, _stage.RequiresSrgb ? GpuFormat.Rgba8UnormSrgb : GpuFormat.Rgba8Unorm, TextureUsage.RenderTarget | TextureUsage.CopySource) { Label = "capture" });
                _stage.Render(_device, target, info);
                _device.ReadTexture(target, 0, 0, pixels);
                _stage.ReleaseTargets(target);
                _stage.PaintOver(pixels, info);
                renderer = _device.Adapter.Name;
            }

            return new CapturedFrame(width, height, pixels, renderer);
        }
        finally
        {
            SyncViewport();
            Ui.Update();
        }
    }

    /// <summary>Validation messages the graphics layer collected since the last call; empty when validation is off or nothing was reported.</summary>
    public string[] TakeGpuValidationMessages() => _device?.TakeValidationMessages() ?? [];

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _device?.WaitIdle();
        _swapchain?.Dispose();
        (_stage as IDisposable)?.Dispose();
        _device?.Dispose();
        Platform.Dispose();
    }
}
