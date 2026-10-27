using System.Threading.Tasks;
using System.Diagnostics;
using OneGame.Core;
using OneGame.Gpu;
using OneGame.Gpu.Graph;
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

    /// <summary>Which windowing system a windowed instance uses. Auto picks Wayland, then X11. Ignored when <see cref="Headless"/>.</summary>
    public PlatformBackendKind Backend { get; init; } = PlatformBackendKind.Auto;

    /// <summary>The display to open the window on: an X display name (<c>:1</c>) or a Wayland socket name. The environment decides when null.</summary>
    public string? DisplayName { get; init; }

    /// <summary>How the swapchain paces presentation. <see cref="PresentMode.Vsync"/> (the default) is always available; the other two fall back to it where the system lacks them.</summary>
    public PresentMode PresentMode { get; init; } = PresentMode.Vsync;

    /// <summary>
    /// False runs frames (simulation, input, layout) without drawing them to the window; captures still render offscreen. A web page whose
    /// browser cannot present WebGPU canvases (headless Chrome on a software adapter) runs this way under test.
    /// </summary>
    public bool PresentFrames { get; init; } = true;

    /// <summary>
    /// Connect to a server instead of running a world of its own. The game's world is then a replica of the server's scope: the server
    /// runs the simulation, commands are forwarded to it, and state cannot be written locally.
    /// </summary>
    public ConnectOptions? Connect { get; init; }

    /// <summary>
    /// A graphics device the host already made. A browser creates its device asynchronously, before the game starts, and hands it in here;
    /// the instance then uses it and leaves its disposal to the host.
    /// </summary>
    public GpuDevice? Device { get; init; }

    /// <summary>The id of the canvas element a web page draws to. Only used in a browser.</summary>
    public string? CanvasId { get; init; }

    /// <summary>Where the 2D renderer gets its compiled shaders. When null, the renderer's own shaders, compiled during the build, are used.</summary>
    public IShaderSource? Shaders { get; init; }

    /// <summary>
    /// Where the game keeps its saves and settings. When null the host picks: the <c>--data</c> folder or <c>ONEGAME_DATA_DIR</c> if given,
    /// otherwise memory for a headless or unpresented run (a test never touches a player's files), IndexedDB in a browser, and a folder
    /// under the user's local application data elsewhere. See <see cref="GameStorage"/>.
    /// </summary>
    public OneGame.Storage.IByteStore? Storage { get; init; }

    /// <summary>A folder for the game's saves and settings, overriding the default choice (the <c>--data</c> argument).</summary>
    public string? DataDirectory { get; init; }

    /// <summary>
    /// Keep saves and settings in the platform's persistent store even for a headless or unpresented run (the <c>--persist-data</c>
    /// argument). Tests that must see a save survive a restart or a page reload use it.
    /// </summary>
    public bool PersistData { get; init; }
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

    private const int MinFrameMilliseconds = 4;

    private readonly ScreenSet _screenSet = new();
    private readonly FixtureSet _fixtureSet = new();
    private readonly EntityRegistry _registry = new();
    private readonly IRenderStage _stage;
    private GpuDevice? _device;
    private string? _gpuError;
    private bool _gpuTried;
    private GpuSwapchain? _swapchain;
    private readonly StickNavigator _stick = new();
    private bool _grabNext;
    private Task<CapturedFrame>? _grab;
    private bool _disposed;
    private Cluster? _cluster;
    private AuthorityScope? _scope;
    private ReplicaScope _replica = null!;
    private RemoteSession? _remote;
    private long _remoteTick = -1;

    private GameInstance(GameDefinition definition, GameOptions options)
    {
        Definition = definition;
        Options = options;
        definition.RegisterEntities(_registry);
        definition.DefineScreens(_screenSet);
        definition.DefineActions(Actions);
        definition.DefineFixtures(_fixtureSet);
        var fonts = new FontLibrary();
        definition.DefineFonts(fonts);
        var haveFonts = fonts.FaceCount > 0;
        var measurer = haveFonts ? new FontTextMeasurer(fonts) : null;
        var art = new ArtAtlas();
        definition.DefineArt(art);
        var shaders = options.Shaders ?? GeneratedShaders.Instance;
        HotShaders = new HotShaders();
        Assets = new Assets(definition.CreateAssetSource());
        _stage = definition.CreateRenderStage(new HostResources(haveFonts ? fonts : null, measurer, art, shaders) { HotShaders = HotShaders, Assets = Assets });
        Platform = PlatformApp.Create(new PlatformOptions
        {
            Backend = options.Headless ? PlatformBackendKind.Headless : options.Backend,
            DisplayName = options.Headless ? null : options.CanvasId ?? options.DisplayName,
            // Android hands gamepads over as input events; there are no device files to scan.
            EnableGamepadDevices = !options.Headless && !OneGame.Platform.RunningOn.Android,
        });
        Window = Platform.CreateWindow(new WindowOptions { Title = definition.Name, Width = options.Width, Height = options.Height });
        Ui = new UiTree(measurer) { ArtSizes = new OneGame.Text.ArtAtlasSizes(art) };
        if (!options.Headless && Platform.Clipboard.IsAvailable)
        {
            // The kit's text fields copy and paste through the window system's clipboard.
            OneGame.UI.Kit.UiKit.DefaultClipboard = new SystemClipboard(Platform.Clipboard);
        }

        Seed = options.Seed ?? definition.Seed;
        if (options.Connect is { } connect)
        {
            AttachRemote(connect);
        }
        else
        {
            Attach(NewWorld(null));
        }

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

    /// <summary>Raised after <see cref="EditWorld"/> changed the world outside the simulation. Session recorders start a new segment here.</summary>
    public event Action<World>? WorldEdited;

    /// <summary>Raised after every simulation tick with the world that ran it. Recorders use it to log state hashes; keep handlers cheap.</summary>
    public event Action<World>? TickCompleted;

    /// <summary>The game's definition.</summary>
    public GameDefinition Definition { get; }

    /// <summary>The options the instance was created with.</summary>
    public GameOptions Options { get; }

    /// <summary>The authority world: the state the simulation runs on. Read it through this property rather than keeping the reference: it changes when a snapshot or fixture is loaded.</summary>
    public World World { get; private set; } = null!;

    /// <summary>The game's input actions, declared in <see cref="GameDefinition.DefineActions"/>. Updated once per frame before <see cref="GameDefinition.OnEachFrame"/>; read them from view code and send commands. Actions never write synced state.</summary>
    public ActionMap Actions { get; } = new();

    /// <summary>The single-player cluster: one authority node holding <see cref="World"/> and one local replica of it. Rebuilt whenever the world is replaced.</summary>
    /// <exception cref="ReplicaWriteException">The game is connected to a server and has no cluster of its own.</exception>
    public Cluster Cluster => _cluster ?? throw RemoteOnly("has no local cluster");

    /// <summary>The authority scope that owns <see cref="World"/>. Recordings start here.</summary>
    /// <exception cref="ReplicaWriteException">The game is connected to a server and has no authority scope.</exception>
    public AuthorityScope Scope => _scope ?? throw RemoteOnly("has no authority scope");

    /// <summary>The connection to the server when the game was started with <see cref="GameOptions.Connect"/>; null for a game that runs its own world.</summary>
    public RemoteSession? Remote => _remote;

    /// <summary>The local replica of the world, kept in step by the cluster. Its hash must equal the authority's at every tick; drift is reported by <see cref="DriftReports"/>.</summary>
    public ReplicaScope Replica => _replica;

    /// <summary>Every scope this game follows: <see cref="Replica"/> first, then the ones named in <see cref="ConnectOptions.AlsoScopes"/>.</summary>
    public IReadOnlyList<ReplicaScope> Replicas => _remote is { } r ? r.Replicas : [_replica];

    /// <summary>Drift reports filed so far by the local replica; empty while replica and authority agree.</summary>
    public IReadOnlyList<DriftReport> DriftReports => _remote is { } r ? r.DriftReports : _cluster!.DriftReports;

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

    /// <summary>The shaders the game's render stage registered for hot reload. A development build recompiles them when their files change.</summary>
    public HotShaders HotShaders { get; }

    /// <summary>Save slots, settings and files for this game. See <see cref="GameStorage"/> and <c>docs/saves.md</c>.</summary>
    public GameStorage Storage => _storage ??= new GameStorage(this);

    private GameStorage? _storage;

    /// <summary>The game's asset service: load textures, data, sounds and models by path. Textures are uploaded through the 2D stage's renderer. See <c>docs/interfaces/assets.md</c>.</summary>
    public Assets Assets { get; }

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

    /// <summary>Frames drawn to the window and handed to the display so far. Stays 0 in a headless instance, with no graphics device, and while the window is minimized.</summary>
    public long PresentedFrames { get; private set; }

    /// <summary>The system clipboard (a private in-process one when headless). Text fields use it for copy and paste.</summary>
    public PlatformClipboard Clipboard => Platform.Clipboard;

    private float? _uiScaleOverride;

    /// <summary>The scale the window system (or <see cref="GameOptions.Scale"/> for a headless run) asks for, before any <see cref="UiScaleOverride"/>.</summary>
    public float WindowScale => Options.Headless ? Options.Scale : Window.Scale;

    /// <summary>
    /// The current UI scale: <see cref="UiScaleOverride"/> when set, otherwise the window's scale. Layout, hit-testing, drawing and
    /// captures that do not name a scale all use it.
    /// </summary>
    public float UiScale => _uiScaleOverride ?? WindowScale;

    /// <summary>
    /// A UI scale chosen by the player (a settings screen's "UI size") that replaces the window's. Null follows the window, including
    /// later changes of the window's scale. The UI is laid out again at once. Not part of the simulation, so it is not recorded.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not between 0 (exclusive) and 16.</exception>
    public float? UiScaleOverride
    {
        get => _uiScaleOverride;
        set
        {
            if (value is { } v && !(v > 0 && v <= MaxUiScale))
            {
                throw new ArgumentOutOfRangeException(nameof(value), v, "The UI scale override must be above 0 and at most 16, or null to follow the window.");
            }

            if (_uiScaleOverride == value)
            {
                return;
            }

            _uiScaleOverride = value;
            SyncViewport();
            Ui.Update();
        }
    }

    private const float MaxUiScale = 16f;

    private World NewWorld(Action<World>? populate, ulong? seed = null)
    {
        var world = new World(seed ?? Seed, _registry);
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
    public World CreateWorld() => _remote is null ? NewWorld(null) : throw RemoteOnly("cannot build a starting world");

    /// <summary>Builds an empty world with the game's registry and systems, the starting point for a replay or a snapshot load.</summary>
    public World CreateBlankWorld() => NewWorld(static _ => { });

    private static ReplicaWriteException RemoteOnly(string what) =>
        new($"This game is connected to a server: it {what}. The server owns the world; send commands instead, or start the game without --connect.");

    private void AttachRemote(ConnectOptions connect)
    {
        var view = new World(Seed, _registry, WorldRole.Replica);
        Definition.RegisterSystems(view);
        var extra = new List<World>();
        foreach (var _ in connect.AlsoScopes)
        {
            var w = new World(Seed, _registry, WorldRole.Replica);
            Definition.RegisterSystems(w);
            extra.Add(w);
        }

        _remote = new RemoteSession(connect, view, extra);
        _replica = _remote.Replica;
        World = view;
        _remote.Pump();
    }

    private void Attach(World world)
    {
        World = world;
        var cluster = new Cluster(new ClusterOptions());
        _cluster = cluster;
        var node = cluster.AddNode("game");
        _scope = cluster.AddScope(node, new ScopeId(1), world);
        var view = new World(Seed, _registry, WorldRole.Replica);
        Definition.RegisterSystems(view);
        _replica = cluster.AddLocalReplica(_scope, view);
        cluster.Settle();
    }

    private void Replace(World world)
    {
        Attach(world);
        WorldReplaced?.Invoke(world);
    }

    /// <summary>Starts recording the run from the current state. Stop the recorder to get the <see cref="Recording"/>; replay it with <see cref="Replay(Recording, Action{World})"/>.</summary>
    /// <exception cref="InvalidOperationException">Commands are queued; step once first.</exception>
    public Recorder StartRecording(RecordingOptions? options = null) => Scope.StartRecording(options ?? new RecordingOptions { CheckpointInterval = 1 });

    /// <summary>
    /// Starts recording the session from the current state and keeps going across world replacements (resets, fixtures, loaded saves),
    /// so the result replays a run that starts new games. Stop the recorder to get the <see cref="SessionRecording"/>; replay it with <see cref="Replay(SessionRecording)"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Commands are queued; step once first.</exception>
    public SessionRecorder StartSessionRecording(RecordingOptions? options = null) => new(this, options ?? new RecordingOptions { CheckpointInterval = 1 });

    /// <summary>Replays every segment of a session, each on a fresh world, and stops at the first divergence. The running game is left alone.</summary>
    public SessionReplayResult Replay(SessionRecording session)
    {
        ArgumentNullException.ThrowIfNull(session);
        long ticks = 0;
        var verified = 0;
        ulong final = 0;
        for (var i = 0; i < session.Segments.Count; i++)
        {
            var r = Replay(session.Segments[i]);
            ticks += r.TicksRun;
            verified += r.CheckpointsVerified;
            final = r.FinalHash;
            if (!r.Ok)
            {
                return new SessionReplayResult(false, ticks, verified, final, i, r.Divergence);
            }
        }

        return new SessionReplayResult(true, ticks, verified, final, -1, null);
    }

    /// <summary>Replays a recording on a fresh world built like the game's and compares the hashes. The running game is left alone.</summary>
    public ReplayResult Replay(Recording recording, Action<World>? onTick = null)
    {
        ArgumentNullException.ThrowIfNull(recording);
        // The world's seed decides random streams that did not exist yet when the snapshot was taken, so replay builds the world with the recorded seed.
        return Replayer.Replay(recording, NewWorld(static _ => { }, recording.Seed), new ReplayOptions { OnTick = onTick });
    }

    /// <summary>Starts a new game: a fresh world filled by <see cref="GameDefinition.Setup"/>, optionally with a different seed.</summary>
    public void Reset(ulong? seed = null)
    {
        if (_remote is not null)
        {
            throw RemoteOnly("cannot be reset");
        }

        if (seed is { } s)
        {
            Seed = s;
        }

        Replace(NewWorld(null));
    }

    /// <summary>
    /// Runs a change to the world that does not go through commands or ticks (the harness's state patch), and tells session recorders
    /// about it: the recording continues from the edited state, so replay reproduces it.
    /// </summary>
    /// <param name="edit">Changes the world; returns false when it changed nothing.</param>
    /// <returns>What <paramref name="edit"/> returned.</returns>
    /// <exception cref="InvalidOperationException">A session is being recorded and commands are queued for the next tick: step first, because a recording cannot restart from a state that holds queued commands.</exception>
    public bool EditWorld(Func<bool> edit)
    {
        ArgumentNullException.ThrowIfNull(edit);
        if (WorldEdited is not null && World.PendingCommandCount > 0)
        {
            throw new InvalidOperationException("Commands are queued for the next tick and a recording is running. Step once before editing state, so the recording can continue from the edited state.");
        }

        if (!edit())
        {
            return false;
        }

        WorldEdited?.Invoke(World);
        return true;
    }

    /// <summary>Replaces the world with one filled by a named fixture.</summary>
    /// <exception cref="KeyNotFoundException">No fixture has that name.</exception>
    public void LoadFixture(string name)
    {
        if (_remote is not null)
        {
            throw RemoteOnly("cannot load fixtures");
        }

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
        var result = TryLoadSnapshot(snapshot);
        if (result.Status != SaveStatus.Ok)
        {
            throw new InvalidDataException($"The save could not be loaded: {result.Status} ({result.Detail}).");
        }
    }

    /// <summary>
    /// Like <see cref="LoadSnapshot"/> but reports a refused save instead of throwing. The world is built fresh and swapped in only when
    /// the load worked, so a refused save leaves the running game untouched.
    /// </summary>
    public LoadResult TryLoadSnapshot(ReadOnlySpan<byte> snapshot)
    {
        if (_remote is not null)
        {
            throw RemoteOnly("cannot load snapshots");
        }

        var world = NewWorld(static _ => { });
        var result = SaveFile.Load(snapshot, world);
        if (result.Status == SaveStatus.Ok)
        {
            Seed = world.Seed;
            Replace(world);
        }

        return result;
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
        _remote?.Pump();
        Platform.PumpEvents();
        while (Platform.TryGetEvent(out var e))
        {
            Handle(e);
        }

        Ui.Update();
        SyncTextInput();
    }

    // Tells the platform when a text field gains or loses focus and where its caret is, so an input method can follow it.
    private void SyncTextInput()
    {
        if (Ui.TextInputArea is { } r)
        {
            var area = ((int)MathF.Round(r.X), (int)MathF.Round(r.Y), (int)MathF.Round(r.Width), (int)MathF.Round(r.Height));
            if (!Window.TextInputActive || Window.TextInputArea != area)
            {
                Window.SetTextInput(true, area.Item1, area.Item2, area.Item3, area.Item4);
            }
        }
        else if (Window.TextInputActive)
        {
            Window.SetTextInput(false);
        }
    }

    /// <summary>How far a realtime run is between the last tick and the next, from 0 to just under 1. Stages draw between ticks with it; stepped runs and captures show whole ticks (0).</summary>
    public Fixed FrameAlpha { get; private set; }

    /// <summary>
    /// True while the app is in the background (the platform reported <see cref="AppLifecycleState.Paused"/>). A realtime run stops its clock
    /// then, so the simulation does not move while nobody can see it; stepping through live control still works.
    /// </summary>
    public bool Suspended { get; private set; }

    private void DropSwapchain()
    {
        _stage.ReleaseTargets(null);
        _swapchain?.Dispose();
        _swapchain = null;
    }

    private void RunFrame(int ticks)
    {
        DrainInput();
        if (Actions.FollowsScreens)
        {
            Actions.ActivateForScreen(CurrentScreen);
        }

        Actions.KeyboardCaptured = Ui.TextInputArea is not null;
        Actions.Update(Platform.Input, Window.Width, Window.Height);
        Definition.OnEachFrame(this);
        if (_stick.Tick(out var repeat))
        {
            Ui.Dispatch(repeat);
        }

        if (_remote is { } remote)
        {
            // The server runs the ticks; a frame takes in whatever arrived.
            remote.Pump();
            Definition.OnFrame(this);
            if (World.Tick != _remoteTick)
            {
                _remoteTick = World.Tick;
                TickCompleted?.Invoke(World);
            }
        }
        else
        {
            for (var t = 0; t < ticks; t++)
            {
                _cluster!.Step(1);
                TickCompleted?.Invoke(World);
            }
        }

        Frame++;
        Assets.Update();
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
        // Actions see every event first so a press shorter than a frame is not lost; a pending rebinding swallows input.
        Actions.KeyboardCaptured = Ui.TextInputArea is not null;
        if (Actions.Observe(e))
        {
            return;
        }

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
            case PlatformEventKind.SurfaceLost:
                DropSwapchain();
                return;
            case PlatformEventKind.SurfaceCreated:
                // The next draw makes a swapchain on the new surface.
                _stage.ReleaseTargets(null);
                return;
            case PlatformEventKind.AppLifecycle:
                Suspended = e.Lifecycle == AppLifecycleState.Paused;
                if (e.Lifecycle is AppLifecycleState.Paused or AppLifecycleState.Resumed)
                {
                    // Leaving the foreground releases the audio hardware; coming back starts it again where the sound left off.
                    OneGame.Audio.AudioService.Active?.SetSuspended(Suspended);
                }

                if (e.Lifecycle == AppLifecycleState.LowMemory)
                {
                    _stage.ReleaseTargets(null);
                }

                Definition.OnInput(this, e);
                return;
            case PlatformEventKind.BackRequested:
                // Back closes the topmost thing; with nothing left to close it leaves the app.
                if (!Ui.Dispatch(UiEvent.Cancel()))
                {
                    Platform.RequestQuit();
                }

                return;
        }

        if (e.Kind == PlatformEventKind.GamepadAxisMoved && _stick.OnAxis(e, out var stickNav))
        {
            Ui.Dispatch(stickNav);
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
            if (Suspended)
            {
                // In the background: keep taking in events, but neither tick nor draw, and owe nothing when the app returns.
                owed = 0;
                DrainInput();
                Platform.PumpEvents(50);
                continue;
            }

            var ticks = 0;
            while (owed >= step)
            {
                owed -= step;
                ticks++;
            }

            var presented = PresentedFrames;
            var frameStart = now;
            FrameAlpha = Fixed.FromRatio(owed, step);
            RunFrame(ticks);
            if (Options.Headless)
            {
                Thread.Sleep(1);
            }
            else if (PresentedFrames == presented)
            {
                // Nothing was drawn (no device, or minimized), so nothing paced the frame: wait for the next tick, waking for input.
                var waitMs = (int)((step - owed) * 1000 / Stopwatch.Frequency);
                if (waitMs > 0)
                {
                    Platform.PumpEvents(Math.Min(waitMs, 16));
                }
            }
            else if (Options.PresentMode != PresentMode.Immediate)
            {
                // Vsync paces a real display, but a software presenter returns at once; a frame never takes less than a few
                // milliseconds so an unthrottled one cannot spin a core at hundreds of frames per second.
                var spentMs = (int)((clock.ElapsedTicks - frameStart) * 1000 / Stopwatch.Frequency);
                if (spentMs < MinFrameMilliseconds)
                {
                    Platform.PumpEvents(MinFrameMilliseconds - spentMs);
                }
            }
        }
    }

    private Stopwatch? _hostClock;
    private long _hostOwed;
    private long _hostLast;

    /// <summary>True while a harness holds a hosted page still: frames still draw, but the simulation does not advance.</summary>
    public bool HostPaused { get; set; }

    /// <summary>
    /// Runs one frame of a realtime run for a host that owns the loop (a browser's animation-frame callback): the ticks the clock owes since
    /// the last call, then drawing. Passing <paramref name="paused"/> draws without advancing the simulation, which is how a harness holds a page still.
    /// </summary>
    public void RunHostedFrame(bool paused = false)
    {
        var step = Stopwatch.Frequency / TickRate;
        _hostClock ??= Stopwatch.StartNew();
        var now = _hostClock.ElapsedTicks;
        _hostOwed = Math.Min(_hostOwed + (now - _hostLast), step * 5);
        _hostLast = now;
        var ticks = 0;
        while (_hostOwed >= step)
        {
            _hostOwed -= step;
            ticks++;
        }

        if (paused)
        {
            ticks = 0;
            _hostOwed = 0;
        }

        FrameAlpha = Fixed.FromRatio(_hostOwed, step);
        RunFrame(ticks);
    }

    private void EnsureDevice()
    {
        if (_gpuTried || Options.Gpu == GpuMode.Off)
        {
            return;
        }

        _gpuTried = true;
        if (Options.Device is { } given)
        {
            _device = given;
            return;
        }
        try
        {
            if (!GpuBackends.IsAvailable)
            {
                throw new GpuUnsupportedException(GpuBackends.UnavailableReason);
            }

            _device = GpuBackends.CreateDevice(new GpuDeviceOptions
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
        if (!Options.PresentFrames || _device is null || Window.NativeHandle.Kind == NativeWindowKind.None || Window.IsMinimized)
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
                NativeWindowKind.Browser => SurfaceSource.Canvas(Options.CanvasId ?? "onegame-canvas"),
                NativeWindowKind.Cocoa => SurfaceSource.MetalLayer(h.Window),
                NativeWindowKind.Android => SurfaceSource.Android(h.Window),
                NativeWindowKind.MetalLayer => SurfaceSource.MetalLayer(h.Window),
                _ => SurfaceSource.Win32(h.Display, h.Window),
            };
            _swapchain = _device.CreateSwapchain(source, new SwapchainDesc((uint)Window.Width, (uint)Window.Height) { Srgb = _stage.RequiresSrgb, PresentMode = Options.PresentMode });
        }

        try
        {
            if (_swapchain.TryAcquire(out var backbuffer) == AcquireStatus.Ready)
            {
                _stage.Render(_device, backbuffer, new FrameInfo(Window.Width, Window.Height, UiScale, World.Tick, list, World, FrameAlpha));
                if (_grabNext)
                {
                    // The read starts now, while the image is still ours; a browser finishes it later.
                    _grabNext = false;
                    _grab = ReadBackbufferAsync(backbuffer).AsTask();
                }

                _swapchain.Present();
                PresentedFrames++;
            }
        }
        catch (GpuSurfaceLostException)
        {
            // The system took the surface before the platform's event reached us; the event follows and a new surface gets a new swapchain.
            DropSwapchain();
        }
    }

    private async ValueTask<CapturedFrame> ReadBackbufferAsync(GpuTexture backbuffer)
    {
        var w = (int)backbuffer.Width;
        var h = (int)backbuffer.Height;
        var pixels = new byte[checked(w * h * 4)];
        await _device!.ReadTextureAsync(backbuffer, 0, 0, pixels);
        if (backbuffer.Format is GpuFormat.Bgra8Unorm or GpuFormat.Bgra8UnormSrgb)
        {
            for (var i = 0; i + 3 < pixels.Length; i += 4)
            {
                (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);
            }
        }

        return new CapturedFrame(w, h, pixels, _device.Adapter.Name);
    }

    /// <summary>
    /// Draws the current state to the window once more and returns what was presented: the swapchain image itself, at the window's size and
    /// scale. Returns null for a headless instance, without a graphics device, or while the window is minimized.
    /// </summary>
    public CapturedFrame? CapturePresented() => CapturePresentedAsync().AsTask().GetAwaiter().GetResult();

    /// <summary>Like <see cref="CapturePresented"/>, for platforms that cannot wait for the graphics device (a browser page).</summary>
    public async ValueTask<CapturedFrame?> CapturePresentedAsync()
    {
        if (Options.Headless)
        {
            return null;
        }

        DrainInput();
        _grab = null;
        _grabNext = true;
        Present();
        _grabNext = false;
        return _grab is null ? null : await _grab;
    }

    /// <summary>
    /// Renders the current state at a given size and UI scale into pixels. The UI is laid out for the capture size and put back
    /// afterwards. Uses the graphics device when there is one and the stage's CPU path otherwise.
    /// </summary>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="scale">UI scale.</param>
    /// <param name="shown">The world the render stage draws; the game's own when null. A test passes a replica's world to check what that node would show.</param>
    /// <exception cref="ArgumentOutOfRangeException">The size is not positive or larger than 16384.</exception>
    public CapturedFrame Capture(int width, int height, float scale = 1f, World? shown = null) =>
        CaptureAsync(width, height, scale, shown).AsTask().GetAwaiter().GetResult();

    /// <summary>Like <see cref="Capture"/>, for platforms that cannot wait for the graphics device (a browser page).</summary>
    public async ValueTask<CapturedFrame> CaptureAsync(int width, int height, float scale = 1f, World? shown = null)
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
            var list = SettleUi();
            var info = new FrameInfo(width, height, scale, (shown ?? World).Tick, list, shown ?? World);
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
                await _device.ReadTextureAsync(target, 0, 0, pixels);
                _stage.ReleaseTargets(target);
                _stage.PaintOver(pixels, info);
                renderer = _device.Adapter.Name;
            }

            return new CapturedFrame(width, height, pixels, renderer);
        }
        finally
        {
            SyncViewport();
            SettleUi();
        }
    }

    // Bindings run before layout, so one that reads another element's bounds sees the previous layout. After a viewport
    // change, frame again until the display list stops changing (bounded, in case a binding never settles).
    private DisplayList SettleUi()
    {
        var list = Ui.Frame();
        for (var i = 0; i < 8; i++)
        {
            var version = list.Version;
            list = Ui.Frame();
            if (list.Version == version)
            {
                break;
            }
        }

        return list;
    }

    /// <summary>
    /// Describes the render graph of the last frame the render stage drew (passes, resources, GPU time). When nothing has been drawn yet, draws
    /// one frame offscreen at the window's size first. Returns null without a graphics device or when the stage does not use a render graph.
    /// </summary>
    public RenderGraphReport? DescribeRenderGraph()
    {
        if (_stage.Graph is not { Frame: > 0 })
        {
            Capture(Math.Max(Window.Width, 1), Math.Max(Window.Height, 1), UiScale);
        }

        return _stage.Graph?.Describe();
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
        _remote?.Close();
        _device?.WaitIdle();
        _swapchain?.Dispose();
        (_stage as IDisposable)?.Dispose();
        Assets.Dispose();
        if (Options.Device is null)
        {
            _device?.Dispose();
        }

        Platform.Dispose();
    }
}
