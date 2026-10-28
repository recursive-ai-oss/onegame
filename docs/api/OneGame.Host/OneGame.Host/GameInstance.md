[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# GameInstance class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class GameInstance : IDisposable
```

One running game: the world, the UI tree, the window and event queue, and the render stage, advanced one frame at a time. Each frame handles input, runs one fixed simulation tick and brings the UI up to date. A windowed instance also draws and presents. Everything the harness does to a game goes through this type.

[`GameApp.Run()`](GameApp.md#m-run-gamedefinition-string) makes one for you. Make your own with [`GameInstance.Create()`](GameInstance.md#m-create-gamedefinition-gameoptions) in tests or tools: a headless instance has no window, takes injected input, and renders captures offscreen.

**Example**

Drive a game headlessly, as a unit test would:

```csharp
using var game = GameInstance.Create(new MyGame(), new GameOptions { Headless = true, Width = 800, Height = 600 });
game.Step(10);                                   // ten frames, one simulation tick each
var ship = (Ship)game.World.Entities[0];
ship.Send.Steer(1, 0, false);                    // commands are how a view or test changes the world
game.Step(5);
var frame = game.Capture(800, 600);              // CapturedFrame.Rgba holds the pixels
```

## Fields

| Member | Description |
| --- | --- |
| [`TickRate`](#m-tickrate) | Simulation ticks per second of a realtime run. |

### <a id="m-tickrate"></a>TickRate

```csharp
public const int TickRate = 60;
```

Simulation ticks per second of a realtime run.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`Actions`](#m-actions) | The game's input actions, declared in [`GameDefinition.DefineActions()`](GameDefinition.md#m-defineactions-actionmap). |
| [`AdapterName`](#m-adaptername) | The name of the graphics adapter in use, or null while none has been created. |
| [`Art`](#m-art) | The UI's SVG and image atlas. |
| [`Assets`](#m-assets) | The game's asset service: load textures, data, sounds and models by path. |
| [`Clipboard`](#m-clipboard) | The system clipboard (a private in-process one when headless). |
| [`Cluster`](#m-cluster) | The single-player cluster: one authority node holding [`GameInstance.World`](GameInstance.md#m-world) and one local replica of it. |
| [`CurrentScreen`](#m-currentscreen) | The name of the screen that is open. |
| [`Definition`](#m-definition) | The game's definition. |
| [`DriftReports`](#m-driftreports) | Drift reports filed so far by the local replica; empty while replica and authority agree. |
| [`FixtureNames`](#m-fixturenames) | Names of the declared fixtures. |
| [`Fonts`](#m-fonts) | The game's fonts, as [`GameDefinition.DefineFonts()`](GameDefinition.md#m-definefonts-fontlibrary) filled them. |
| [`Frame`](#m-frame) | The number of frames run so far, counting from 0 at creation. |
| [`FrameAlpha`](#m-framealpha) | How far a realtime run is between the last tick and the next, from 0 to just under 1. |
| [`GpuError`](#m-gpuerror) | Why no graphics device exists, when [`GameInstance.Options`](GameInstance.md#m-options) allowed one; null otherwise. |
| [`HostPaused`](#m-hostpaused) | True while a harness holds a hosted page still: frames still draw, but the simulation does not advance. |
| [`HotShaders`](#m-hotshaders) | The shaders the game's render stage registered for hot reload. |
| [`InputFilter`](#m-inputfilter) | Sees every platform event before the game does; returning true consumes it. |
| [`Options`](#m-options) | The options the instance was created with. |
| [`Pages`](#m-pages) | OneRender pages shown as screens (builder trees or HTML and CSS), drawn over the 2D pass and the `OneGame.UI` tree. |
| [`Platform`](#m-platform) | The platform app (window, events, injection). |
| [`PresentedFrames`](#m-presentedframes) | Frames drawn to the window and handed to the display so far. |
| [`QuitRequested`](#m-quitrequested) | True once the window was closed or a quit was requested. |
| [`Remote`](#m-remote) | The connection to the server when the game was started with [`GameOptions.Connect`](GameOptions.md#m-connect); null for a game that runs its own world. |
| [`Replica`](#m-replica) | The local replica of the world, kept in step by the cluster. |
| [`Replicas`](#m-replicas) | Every scope this game follows: [`GameInstance.Replica`](GameInstance.md#m-replica) first, then the ones named in [`ConnectOptions.AlsoScopes`](ConnectOptions.md#m-alsoscopes). |
| [`Scope`](#m-scope) | The authority scope that owns [`GameInstance.World`](GameInstance.md#m-world). |
| [`ScreenNames`](#m-screennames) | Names of the declared screens. |
| [`Seed`](#m-seed) | The seed of the current run. |
| [`Stage`](#m-stage) | The render stage. |
| [`Storage`](#m-storage) | Save slots, settings and files for this game. |
| [`Suspended`](#m-suspended) | True while the app is in the background (the platform reported [`AppLifecycleState.Paused`](../../OneGame.Platform/OneGame.Platform/AppLifecycleState.md#m-paused)). |
| [`Tweens`](#m-tweens) | The tweens of this game's UI: style transitions and the ones game code starts. |
| [`Ui`](#m-ui) | The retained UI tree of the current screen. |
| [`UiScale`](#m-uiscale) | The current UI scale: [`GameInstance.UiScaleOverride`](GameInstance.md#m-uiscaleoverride) when set, otherwise the window's scale. |
| [`UiScaleOverride`](#m-uiscaleoverride) | A UI scale chosen by the player (a settings screen's "UI size") that replaces the window's. |
| [`Window`](#m-window) | The game window: its size in pixels, UI scale and safe area. |
| [`WindowScale`](#m-windowscale) | The scale the window system (or [`GameOptions.Scale`](GameOptions.md#m-scale) for a headless run) asks for, before any [`GameInstance.UiScaleOverride`](GameInstance.md#m-uiscaleoverride). |
| [`World`](#m-world) | The authority world: the state the simulation runs on. |

### <a id="m-actions"></a>Actions

```csharp
public ActionMap Actions { get; }
```

The game's input actions, declared in [`GameDefinition.DefineActions()`](GameDefinition.md#m-defineactions-actionmap). Updated once per frame before [`GameDefinition.OnEachFrame()`](GameDefinition.md#m-oneachframe-gameinstance); read them from view code and send commands. Actions never write synced state.

**Type:** [`ActionMap`](../../OneGame.Platform/OneGame.Platform/ActionMap.md)

### <a id="m-adaptername"></a>AdapterName

```csharp
public string? AdapterName { get; }
```

The name of the graphics adapter in use, or null while none has been created.

**Type:** `string?`

### <a id="m-art"></a>Art

```csharp
public ArtAtlas Art { get; }
```

The UI's SVG and image atlas. Call [`ArtAtlas.Preload()`](../../OneGame.Atlas/OneGame.Atlas/ArtAtlas.md#m-preload-ienumerable-string-float) before a screen opens so its art is ready on the first draw.

**Type:** [`ArtAtlas`](../../OneGame.Atlas/OneGame.Atlas/ArtAtlas.md)

### <a id="m-assets"></a>Assets

```csharp
public Assets Assets { get; }
```

The game's asset service: load textures, data, sounds and models by path. Textures are uploaded through the 2D stage's renderer. See `docs/interfaces/assets.md`.

**Type:** [`Assets`](Assets.md)

### <a id="m-clipboard"></a>Clipboard

```csharp
public PlatformClipboard Clipboard { get; }
```

The system clipboard (a private in-process one when headless). Text fields use it for copy and paste.

**Type:** [`PlatformClipboard`](../../OneGame.Platform/OneGame.Platform/PlatformClipboard.md)

### <a id="m-cluster"></a>Cluster

```csharp
public Cluster Cluster { get; }
```

The single-player cluster: one authority node holding [`GameInstance.World`](GameInstance.md#m-world) and one local replica of it. Rebuilt whenever the world is replaced.

**Type:** [`Cluster`](../../OneGame.Sync/OneGame.Sync/Cluster.md)

**Exceptions**

- [`ReplicaWriteException`](../../OneGame.Core/OneGame.Core/ReplicaWriteException.md): The game is connected to a server and has no cluster of its own.

### <a id="m-currentscreen"></a>CurrentScreen

```csharp
public string? CurrentScreen { get; }
```

The name of the screen that is open.

**Type:** `string?`

### <a id="m-definition"></a>Definition

```csharp
public GameDefinition Definition { get; }
```

The game's definition.

**Type:** [`GameDefinition`](GameDefinition.md)

### <a id="m-driftreports"></a>DriftReports

```csharp
public IReadOnlyList<DriftReport> DriftReports { get; }
```

Drift reports filed so far by the local replica; empty while replica and authority agree.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`DriftReport`](../../OneGame.Sync/OneGame.Sync/DriftReport.md)&gt;

### <a id="m-fixturenames"></a>FixtureNames

```csharp
public IReadOnlyList<string> FixtureNames { get; }
```

Names of the declared fixtures.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

### <a id="m-fonts"></a>Fonts

```csharp
public FontLibrary Fonts { get; }
```

The game's fonts, as [`GameDefinition.DefineFonts()`](GameDefinition.md#m-definefonts-fontlibrary) filled them.

**Type:** [`FontLibrary`](../../OneGame.Text/OneGame.Text/FontLibrary.md)

### <a id="m-frame"></a>Frame

```csharp
public long Frame { get; }
```

The number of frames run so far, counting from 0 at creation. Frames and simulation ticks advance together while the game runs on its own world.

**Type:** `long`

### <a id="m-framealpha"></a>FrameAlpha

```csharp
public Fixed FrameAlpha { get; }
```

How far a realtime run is between the last tick and the next, from 0 to just under 1. Stages draw between ticks with it; stepped runs and captures show whole ticks (0).

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-gpuerror"></a>GpuError

```csharp
public string? GpuError { get; }
```

Why no graphics device exists, when [`GameInstance.Options`](GameInstance.md#m-options) allowed one; null otherwise.

**Type:** `string?`

### <a id="m-hostpaused"></a>HostPaused

```csharp
public bool HostPaused { get; set; }
```

True while a harness holds a hosted page still: frames still draw, but the simulation does not advance.

**Type:** `bool`

### <a id="m-hotshaders"></a>HotShaders

```csharp
public HotShaders HotShaders { get; }
```

The shaders the game's render stage registered for hot reload. A development build recompiles them when their files change.

**Type:** [`HotShaders`](HotShaders.md)

### <a id="m-inputfilter"></a>InputFilter

```csharp
public PlatformEventFilter? InputFilter { get; set; }
```

Sees every platform event before the game does; returning true consumes it. Development tools use it for their own keys, so a toggle never reaches the game. Null in a release build.

**Type:** [`PlatformEventFilter`](PlatformEventFilter.md)?

### <a id="m-options"></a>Options

```csharp
public GameOptions Options { get; }
```

The options the instance was created with.

**Type:** [`GameOptions`](GameOptions.md)

### <a id="m-pages"></a>Pages

```csharp
public PageSet Pages { get; }
```

OneRender pages shown as screens (builder trees or HTML and CSS), drawn over the 2D pass and the `OneGame.UI` tree. They run on the view clock the tweens use and see input before the UI does. Pages are view-only: they read nothing from the simulation and change no hash.

**Type:** [`PageSet`](../../OneGame.Pages/OneGame.Pages/PageSet.md)

### <a id="m-platform"></a>Platform

```csharp
public PlatformApp Platform { get; }
```

The platform app (window, events, injection).

**Type:** [`PlatformApp`](../../OneGame.Platform/OneGame.Platform/PlatformApp.md)

### <a id="m-presentedframes"></a>PresentedFrames

```csharp
public long PresentedFrames { get; }
```

Frames drawn to the window and handed to the display so far. Stays 0 in a headless instance, with no graphics device, and while the window is minimized.

**Type:** `long`

### <a id="m-quitrequested"></a>QuitRequested

```csharp
public bool QuitRequested { get; }
```

True once the window was closed or a quit was requested.

**Type:** `bool`

### <a id="m-remote"></a>Remote

```csharp
public RemoteSession? Remote { get; }
```

The connection to the server when the game was started with [`GameOptions.Connect`](GameOptions.md#m-connect); null for a game that runs its own world.

**Type:** [`RemoteSession`](RemoteSession.md)?

### <a id="m-replica"></a>Replica

```csharp
public ReplicaScope Replica { get; }
```

The local replica of the world, kept in step by the cluster. Its hash must equal the authority's at every tick; drift is reported by [`GameInstance.DriftReports`](GameInstance.md#m-driftreports).

**Type:** [`ReplicaScope`](../../OneGame.Sync/OneGame.Sync/ReplicaScope.md)

### <a id="m-replicas"></a>Replicas

```csharp
public IReadOnlyList<ReplicaScope> Replicas { get; }
```

Every scope this game follows: [`GameInstance.Replica`](GameInstance.md#m-replica) first, then the ones named in [`ConnectOptions.AlsoScopes`](ConnectOptions.md#m-alsoscopes).

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ReplicaScope`](../../OneGame.Sync/OneGame.Sync/ReplicaScope.md)&gt;

### <a id="m-scope"></a>Scope

```csharp
public AuthorityScope Scope { get; }
```

The authority scope that owns [`GameInstance.World`](GameInstance.md#m-world). Recordings start here.

**Type:** [`AuthorityScope`](../../OneGame.Sync/OneGame.Sync/AuthorityScope.md)

**Exceptions**

- [`ReplicaWriteException`](../../OneGame.Core/OneGame.Core/ReplicaWriteException.md): The game is connected to a server and has no authority scope.

### <a id="m-screennames"></a>ScreenNames

```csharp
public IReadOnlyList<string> ScreenNames { get; }
```

Names of the declared screens.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

### <a id="m-seed"></a>Seed

```csharp
public ulong Seed { get; }
```

The seed of the current run.

**Type:** `ulong`

### <a id="m-stage"></a>Stage

```csharp
public IRenderStage Stage { get; }
```

The render stage.

**Type:** [`IRenderStage`](IRenderStage.md)

### <a id="m-storage"></a>Storage

```csharp
public GameStorage Storage { get; }
```

Save slots, settings and files for this game. See [`GameStorage`](GameStorage.md) and `docs/saves.md`.

**Type:** [`GameStorage`](GameStorage.md)

### <a id="m-suspended"></a>Suspended

```csharp
public bool Suspended { get; }
```

True while the app is in the background (the platform reported [`AppLifecycleState.Paused`](../../OneGame.Platform/OneGame.Platform/AppLifecycleState.md#m-paused)). A realtime run stops its clock then, so the simulation does not move while nobody can see it; stepping through live control still works.

**Type:** `bool`

### <a id="m-tweens"></a>Tweens

```csharp
public TweenSet Tweens { get; }
```

The tweens of this game's UI: style transitions and the ones game code starts. The host advances them once per frame by the view time that passed (the ticks shown plus [`GameInstance.FrameAlpha`](GameInstance.md#m-framealpha), at the simulation's rate), so a stepped run and a recorded one move them the same way and a paused or backgrounded game holds them still. Tweens are view-only: nothing here is simulation state, so hashes and replays do not depend on them.

**Type:** [`TweenSet`](../../OneGame.UI/OneGame.Tweens/TweenSet.md)

### <a id="m-ui"></a>Ui

```csharp
public UiTree Ui { get; }
```

The retained UI tree of the current screen. Screens built in [`GameDefinition.DefineScreens()`](GameDefinition.md#m-definescreens-screenset) are mounted here.

**Type:** [`UiTree`](../../OneGame.UI/OneGame.UI/UiTree.md)

### <a id="m-uiscale"></a>UiScale

```csharp
public float UiScale { get; }
```

The current UI scale: [`GameInstance.UiScaleOverride`](GameInstance.md#m-uiscaleoverride) when set, otherwise the window's scale. Layout, hit-testing, drawing and captures that do not name a scale all use it.

**Type:** `float`

### <a id="m-uiscaleoverride"></a>UiScaleOverride

```csharp
public float? UiScaleOverride { get; set; }
```

A UI scale chosen by the player (a settings screen's "UI size") that replaces the window's. Null follows the window, including later changes of the window's scale. The UI is laid out again at once. Not part of the simulation, so it is not recorded.

**Type:** `float?`

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The value is not between 0 (exclusive) and 16.

### <a id="m-window"></a>Window

```csharp
public Window Window { get; }
```

The game window: its size in pixels, UI scale and safe area. A headless instance has a virtual one.

**Type:** [`Window`](../../OneGame.Platform/OneGame.Platform/Window.md)

### <a id="m-windowscale"></a>WindowScale

```csharp
public float WindowScale { get; }
```

The scale the window system (or [`GameOptions.Scale`](GameOptions.md#m-scale) for a headless run) asks for, before any [`GameInstance.UiScaleOverride`](GameInstance.md#m-uiscaleoverride).

**Type:** `float`

### <a id="m-world"></a>World

```csharp
public World World { get; }
```

The authority world: the state the simulation runs on. Read it through this property rather than keeping the reference: it changes when a snapshot or fixture is loaded.

**Type:** [`World`](../../OneGame.Core/OneGame.Core/World.md)

## Events

| Member | Description |
| --- | --- |
| [`TickCompleted`](#m-tickcompleted) | Raised after every simulation tick with the world that ran it. |
| [`WorldEdited`](#m-worldedited) | Raised after [`GameInstance.EditWorld()`](GameInstance.md#m-editworld-func-bool) changed the world outside the simulation. |
| [`WorldReplaced`](#m-worldreplaced) | Raised after the world is replaced by a reset, a fixture or a snapshot load. |

### <a id="m-tickcompleted"></a>TickCompleted

```csharp
public event Action<World>? TickCompleted;
```

Raised after every simulation tick with the world that ran it. Recorders use it to log state hashes; keep handlers cheap.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`World`](../../OneGame.Core/OneGame.Core/World.md)&gt;?

### <a id="m-worldedited"></a>WorldEdited

```csharp
public event Action<World>? WorldEdited;
```

Raised after [`GameInstance.EditWorld()`](GameInstance.md#m-editworld-func-bool) changed the world outside the simulation. Session recorders start a new segment here.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`World`](../../OneGame.Core/OneGame.Core/World.md)&gt;?

### <a id="m-worldreplaced"></a>WorldReplaced

```csharp
public event Action<World>? WorldReplaced;
```

Raised after the world is replaced by a reset, a fixture or a snapshot load. Recorders reattach here.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`World`](../../OneGame.Core/OneGame.Core/World.md)&gt;?

## Methods

| Member | Description |
| --- | --- |
| [`AdvanceView(double)`](#m-advanceview-double) | Moves the tweens (style transitions included) forward by `seconds` of view time without running the simulation, then lays the UI out again. |
| [`Capture(int, int, float, World?)`](#m-capture-int-int-float-world) | Renders the current state at a given size and UI scale into pixels. |
| [`CaptureAsync(int, int, float, World?)`](#m-captureasync-int-int-float-world) | Like [`GameInstance.Capture()`](GameInstance.md#m-capture-int-int-float-world), for platforms that cannot wait for the graphics device (a browser page). |
| [`CapturePresented()`](#m-capturepresented) | Draws the current state to the window once more and returns what was presented: the swapchain image itself, at the window's size and scale. |
| [`CapturePresentedAsync()`](#m-capturepresentedasync) | Like [`GameInstance.CapturePresented()`](GameInstance.md#m-capturepresented), for platforms that cannot wait for the graphics device (a browser page). |
| [`Create(GameDefinition, GameOptions?)`](#m-create-gamedefinition-gameoptions) | Creates an instance. |
| [`CreateBlankWorld()`](#m-createblankworld) | Builds an empty world with the game's registry and systems, the starting point for a replay or a snapshot load. |
| [`CreateWorld()`](#m-createworld) | Builds a world the way the game does (registry, tables, systems) and fills it with the game's starting state. |
| [`DescribeRenderGraph()`](#m-describerendergraph) | Describes the render graph of the last frame the render stage drew (passes, resources, GPU time). |
| [`Dispose()`](#m-dispose) |  |
| [`DrainInput()`](#m-draininput) | Handles all pending input without running a tick. |
| [`EditWorld(Func<bool>)`](#m-editworld-func-bool) | Runs a change to the world that does not go through commands or ticks (the harness's state patch), and tells session recorders about it: the recording continues from the edited state, so replay reproduces it. |
| [`LoadFixture(string)`](#m-loadfixture-string) | Replaces the world with one filled by a named fixture. |
| [`LoadSnapshot(ReadOnlySpan<byte>)`](#m-loadsnapshot-readonlyspan-byte) | Replaces the world with the state in a save (see [`SaveFile`](../../OneGame.Sync/OneGame.Sync/SaveFile.md)). |
| [`OpenScreen(string)`](#m-openscreen-string) | Replaces the UI with a declared screen. |
| [`Redraw()`](#m-redraw) | Draws and presents the current state in a windowed instance. |
| [`RefreshScreens()`](#m-refreshscreens) | Declares the screens again from the game's current code and rebuilds the open screen, so an edit to [`GameDefinition.DefineScreens()`](GameDefinition.md#m-definescreens-screenset) shows on the next frame. |
| [`Replay(SessionRecording)`](#m-replay-sessionrecording) | Replays every segment of a session, each on a fresh world, and stops at the first divergence. |
| [`Replay(Recording, Action<World>?)`](#m-replay-recording-action-world) | Replays a recording on a fresh world built like the game's and compares the hashes. |
| [`Reset(ulong?)`](#m-reset-ulong) | Starts a new game: a fresh world filled by [`GameDefinition.Setup()`](GameDefinition.md#m-setup-world), optionally with a different seed. |
| [`Resize(int, int)`](#m-resize-int-int) | Resizes the window (or the virtual window of a headless run) and lets the UI lay out at the new size. |
| [`RunHostedFrame(bool)`](#m-runhostedframe-bool) | Runs one frame of a realtime run for a host that owns the loop (a browser's animation-frame callback): the ticks the clock owes since the last call, then drawing. |
| [`RunRealtime(long, Action?)`](#m-runrealtime-long-action) | Runs in real time until the window closes or `maxFrames` frames have run (0 = no limit). |
| [`SaveSnapshot()`](#m-savesnapshot) | Writes the current world as a save file (header, hashes, snapshot). |
| [`StartRecording(RecordingOptions?)`](#m-startrecording-recordingoptions) | Starts recording the run from the current state. |
| [`StartSessionRecording(RecordingOptions?)`](#m-startsessionrecording-recordingoptions) | Starts recording the session from the current state and keeps going across world replacements (resets, fixtures, loaded saves), so the result replays a run that starts new games. |
| [`Step(int)`](#m-step-int) | Runs `frames` frames: input, one tick, UI update (and drawing when windowed). |
| [`TakeGpuValidationMessages()`](#m-takegpuvalidationmessages) | Validation messages the graphics layer collected since the last call; empty when validation is off or nothing was reported. |
| [`TryLoadSnapshot(ReadOnlySpan<byte>)`](#m-tryloadsnapshot-readonlyspan-byte) | Like [`GameInstance.LoadSnapshot()`](GameInstance.md#m-loadsnapshot-readonlyspan-byte) but reports a refused save instead of throwing. |

### <a id="m-advanceview-double"></a>AdvanceView(double)

```csharp
public void AdvanceView(double seconds);
```

Moves the tweens (style transitions included) forward by `seconds` of view time without running the simulation, then lays the UI out again. The harness uses it to look at a transition at exact points; a game has no reason to call it.

**Parameters**

- `seconds` (`double`): View time to add; zero or more.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The value is negative or not finite.

### <a id="m-capture-int-int-float-world"></a>Capture(int, int, float, World?)

```csharp
public CapturedFrame Capture(int width, int height, float scale = 1f, World? shown = null);
```

Renders the current state at a given size and UI scale into pixels. The UI is laid out for the capture size and put back afterwards. Uses the graphics device when there is one and the stage's CPU path otherwise.

**Parameters**

- `width` (`int`): Width in pixels.
- `height` (`int`): Height in pixels.
- `scale` (`float`): UI scale.
- `shown` ([`World`](../../OneGame.Core/OneGame.Core/World.md)?): The world the render stage draws; the game's own when null. A test passes a replica's world to check what that node would show.

**Returns:** [`CapturedFrame`](CapturedFrame.md)

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The size is not positive or larger than 16384.

### <a id="m-captureasync-int-int-float-world"></a>CaptureAsync(int, int, float, World?)

```csharp
public ValueTask<CapturedFrame> CaptureAsync(int width, int height, float scale = 1f, World? shown = null);
```

Like [`GameInstance.Capture()`](GameInstance.md#m-capture-int-int-float-world), for platforms that cannot wait for the graphics device (a browser page).

**Parameters**

- `width` (`int`): Width in pixels.
- `height` (`int`): Height in pixels.
- `scale` (`float`): UI scale.
- `shown` ([`World`](../../OneGame.Core/OneGame.Core/World.md)?): The world to draw; the game's own when null.

**Returns:** [`ValueTask`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.valuetask-1)&lt;[`CapturedFrame`](CapturedFrame.md)&gt;

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The size is not positive or larger than 16384.

### <a id="m-capturepresented"></a>CapturePresented()

```csharp
public CapturedFrame? CapturePresented();
```

Draws the current state to the window once more and returns what was presented: the swapchain image itself, at the window's size and scale. Returns null for a headless instance, without a graphics device, or while the window is minimized.

**Returns:** [`CapturedFrame`](CapturedFrame.md)?

### <a id="m-capturepresentedasync"></a>CapturePresentedAsync()

```csharp
public ValueTask<CapturedFrame?> CapturePresentedAsync();
```

Like [`GameInstance.CapturePresented()`](GameInstance.md#m-capturepresented), for platforms that cannot wait for the graphics device (a browser page).

**Returns:** [`ValueTask`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.valuetask-1)&lt;[`CapturedFrame`](CapturedFrame.md)?&gt;

### <a id="m-create-gamedefinition-gameoptions"></a>Create(GameDefinition, GameOptions?)

```csharp
public static GameInstance Create(GameDefinition definition, GameOptions? options = null);
```

Creates an instance. Disposing it closes the window and the device.

**Parameters**

- `definition` ([`GameDefinition`](GameDefinition.md)): The game.
- `options` ([`GameOptions`](GameOptions.md)?): Window size, GPU policy, seed and so on; a headless instance when null.

**Returns:** [`GameInstance`](GameInstance.md)

### <a id="m-createblankworld"></a>CreateBlankWorld()

```csharp
public World CreateBlankWorld();
```

Builds an empty world with the game's registry and systems, the starting point for a replay or a snapshot load.

**Returns:** [`World`](../../OneGame.Core/OneGame.Core/World.md)

### <a id="m-createworld"></a>CreateWorld()

```csharp
public World CreateWorld();
```

Builds a world the way the game does (registry, tables, systems) and fills it with the game's starting state. Replays run on such a world.

**Returns:** [`World`](../../OneGame.Core/OneGame.Core/World.md)

### <a id="m-describerendergraph"></a>DescribeRenderGraph()

```csharp
public RenderGraphReport? DescribeRenderGraph();
```

Describes the render graph of the last frame the render stage drew (passes, resources, GPU time). When nothing has been drawn yet, draws one frame offscreen at the window's size first. Returns null without a graphics device or when the stage does not use a render graph.

**Returns:** [`RenderGraphReport`](../../OneGame.Gpu/OneGame.Gpu.Graph/RenderGraphReport.md)?

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Implements [`IDisposable.Dispose()`](https://learn.microsoft.com/dotnet/api/system.idisposable.dispose).

### <a id="m-draininput"></a>DrainInput()

```csharp
public void DrainInput();
```

Handles all pending input without running a tick. Control operations call this after injecting input so its effect is visible at once.

### <a id="m-editworld-func-bool"></a>EditWorld(Func&lt;bool&gt;)

```csharp
public bool EditWorld(Func<bool> edit);
```

Runs a change to the world that does not go through commands or ticks (the harness's state patch), and tells session recorders about it: the recording continues from the edited state, so replay reproduces it.

**Parameters**

- `edit` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<bool>`): Changes the world; returns false when it changed nothing.

**Returns:** `bool` — What `edit` returned.

**Exceptions**

- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): A session is being recorded and commands are queued for the next tick: step first, because a recording cannot restart from a state that holds queued commands.

### <a id="m-loadfixture-string"></a>LoadFixture(string)

```csharp
public void LoadFixture(string name);
```

Replaces the world with one filled by a named fixture.

**Parameters**

- `name` (`string`): A fixture declared in [`GameDefinition.DefineFixtures()`](GameDefinition.md#m-definefixtures-fixtureset).

**Exceptions**

- [`KeyNotFoundException`](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception): No fixture has that name.

### <a id="m-loadsnapshot-readonlyspan-byte"></a>LoadSnapshot(ReadOnlySpan&lt;byte&gt;)

```csharp
public void LoadSnapshot(ReadOnlySpan<byte> snapshot);
```

Replaces the world with the state in a save (see [`SaveFile`](../../OneGame.Sync/OneGame.Sync/SaveFile.md)).

**Parameters**

- `snapshot` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

**Exceptions**

- [`InvalidDataException`](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception): The save is malformed, damaged or from an unsupported version.

### <a id="m-openscreen-string"></a>OpenScreen(string)

```csharp
public void OpenScreen(string name);
```

Replaces the UI with a declared screen.

**Parameters**

- `name` (`string`): A screen declared in [`GameDefinition.DefineScreens()`](GameDefinition.md#m-definescreens-screenset).

**Exceptions**

- [`KeyNotFoundException`](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception): No screen has that name.

### <a id="m-redraw"></a>Redraw()

```csharp
public void Redraw();
```

Draws and presents the current state in a windowed instance. Does nothing in a headless one.

### <a id="m-refreshscreens"></a>RefreshScreens()

```csharp
public void RefreshScreens();
```

Declares the screens again from the game's current code and rebuilds the open screen, so an edit to [`GameDefinition.DefineScreens()`](GameDefinition.md#m-definescreens-screenset) shows on the next frame. Development hot reload calls it after it applies an edit; the world is not touched. The screen's own UI state (focus, scroll position, typed text) starts over.

### <a id="m-replay-sessionrecording"></a>Replay(SessionRecording)

```csharp
public SessionReplayResult Replay(SessionRecording session);
```

Replays every segment of a session, each on a fresh world, and stops at the first divergence. The running game is left alone.

**Parameters**

- `session` ([`SessionRecording`](SessionRecording.md))

**Returns:** [`SessionReplayResult`](SessionReplayResult.md)

### <a id="m-replay-recording-action-world"></a>Replay(Recording, Action&lt;World&gt;?)

```csharp
public ReplayResult Replay(Recording recording, Action<World>? onTick = null);
```

Replays a recording on a fresh world built like the game's and compares the hashes. The running game is left alone.

**Parameters**

- `recording` ([`Recording`](../../OneGame.Sync/OneGame.Sync/Recording.md))
- `onTick` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`World`](../../OneGame.Core/OneGame.Core/World.md)&gt;?)

**Returns:** [`ReplayResult`](../../OneGame.Sync/OneGame.Sync/ReplayResult.md)

### <a id="m-reset-ulong"></a>Reset(ulong?)

```csharp
public void Reset(ulong? seed = default);
```

Starts a new game: a fresh world filled by [`GameDefinition.Setup()`](GameDefinition.md#m-setup-world), optionally with a different seed.

**Parameters**

- `seed` (`ulong?`): The seed of the random streams; the game's own seed when null.

### <a id="m-resize-int-int"></a>Resize(int, int)

```csharp
public void Resize(int width, int height);
```

Resizes the window (or the virtual window of a headless run) and lets the UI lay out at the new size.

**Parameters**

- `width` (`int`): Width in pixels.
- `height` (`int`): Height in pixels.

### <a id="m-runhostedframe-bool"></a>RunHostedFrame(bool)

```csharp
public void RunHostedFrame(bool paused = false);
```

Runs one frame of a realtime run for a host that owns the loop (a browser's animation-frame callback): the ticks the clock owes since the last call, then drawing. Passing `paused` draws without advancing the simulation, which is how a harness holds a page still.

**Parameters**

- `paused` (`bool`)

### <a id="m-runrealtime-long-action"></a>RunRealtime(long, Action?)

```csharp
public void RunRealtime(long maxFrames = 0, Action? beforeFrame = null);
```

Runs in real time until the window closes or `maxFrames` frames have run (0 = no limit). One call to `beforeFrame` runs before each frame, for live control.

**Parameters**

- `maxFrames` (`long`)
- `beforeFrame` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action)?)

### <a id="m-savesnapshot"></a>SaveSnapshot()

```csharp
public byte[] SaveSnapshot();
```

Writes the current world as a save file (header, hashes, snapshot).

**Returns:** `byte[]`

### <a id="m-startrecording-recordingoptions"></a>StartRecording(RecordingOptions?)

```csharp
public Recorder StartRecording(RecordingOptions? options = null);
```

Starts recording the run from the current state. Stop the recorder to get the [`Recording`](../../OneGame.Sync/OneGame.Sync/Recording.md); replay it with [`GameInstance.Replay()`](GameInstance.md#m-replay-recording-action-world).

**Parameters**

- `options` ([`RecordingOptions`](../../OneGame.Sync/OneGame.Sync/RecordingOptions.md)?)

**Returns:** [`Recorder`](../../OneGame.Sync/OneGame.Sync/Recorder.md)

**Exceptions**

- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): Commands are queued; step once first.

### <a id="m-startsessionrecording-recordingoptions"></a>StartSessionRecording(RecordingOptions?)

```csharp
public SessionRecorder StartSessionRecording(RecordingOptions? options = null);
```

Starts recording the session from the current state and keeps going across world replacements (resets, fixtures, loaded saves), so the result replays a run that starts new games. Stop the recorder to get the [`SessionRecording`](SessionRecording.md); replay it with [`GameInstance.Replay()`](GameInstance.md#m-replay-sessionrecording).

**Parameters**

- `options` ([`RecordingOptions`](../../OneGame.Sync/OneGame.Sync/RecordingOptions.md)?)

**Returns:** [`SessionRecorder`](SessionRecorder.md)

**Exceptions**

- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): Commands are queued; step once first.

### <a id="m-step-int"></a>Step(int)

```csharp
public void Step(int frames = 1);
```

Runs `frames` frames: input, one tick, UI update (and drawing when windowed).

**Parameters**

- `frames` (`int`)

### <a id="m-takegpuvalidationmessages"></a>TakeGpuValidationMessages()

```csharp
public string[] TakeGpuValidationMessages();
```

Validation messages the graphics layer collected since the last call; empty when validation is off or nothing was reported.

**Returns:** `string[]`

### <a id="m-tryloadsnapshot-readonlyspan-byte"></a>TryLoadSnapshot(ReadOnlySpan&lt;byte&gt;)

```csharp
public LoadResult TryLoadSnapshot(ReadOnlySpan<byte> snapshot);
```

Like [`GameInstance.LoadSnapshot()`](GameInstance.md#m-loadsnapshot-readonlyspan-byte) but reports a refused save instead of throwing. The world is built fresh and swapped in only when the load worked, so a refused save leaves the running game untouched.

**Parameters**

- `snapshot` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

**Returns:** [`LoadResult`](../../OneGame.Sync/OneGame.Sync/LoadResult.md)
