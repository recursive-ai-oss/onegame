[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# GameOptions record

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`GameOptions`](GameOptions.md)&gt;

```csharp
public sealed record GameOptions : IEquatable<GameOptions>
```

Options of a [`GameInstance`](GameInstance.md).

## Constructors

| Member | Description |
| --- | --- |
| [`GameOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>GameOptions()

```csharp
public GameOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`Args`](#m-args) | The command line the game was started with, for [`GameInstance.Args`](GameInstance.md#m-args). |
| [`Backend`](#m-backend) | Which windowing system a windowed instance uses. |
| [`CanvasId`](#m-canvasid) | The id of the canvas element a web page draws to. |
| [`Connect`](#m-connect) | Connect to a server instead of running a world of its own. |
| [`DataDirectory`](#m-datadirectory) | A folder for the game's saves and settings, overriding the default choice (the `--data` argument). |
| [`Device`](#m-device) | A graphics device the host already made. |
| [`DisplayName`](#m-displayname) | The display to open the window on: an X display name (`:1`) or a Wayland socket name. |
| [`Gpu`](#m-gpu) | Graphics device policy: [`GpuMode.Auto`](GpuMode.md#m-auto) uses a device when one can be made, [`GpuMode.Off`](GpuMode.md#m-off) renders captures on the CPU, [`GpuMode.Required`](GpuMode.md#m-required) fails without a device. |
| [`Headless`](#m-headless) | No window: events come only from injection and nothing is presented. |
| [`Height`](#m-height) | Window (or virtual window) height in pixels. |
| [`Host`](#m-host) | Where the window, the graphics device and the swapchain live. |
| [`PersistData`](#m-persistdata) | Keep saves and settings in the platform's persistent store even for a headless or unpresented run (the `--persist-data` argument). |
| [`PresentFrames`](#m-presentframes) | False runs frames (simulation, input, layout) without drawing them to the window; captures still render offscreen. |
| [`PresentMode`](#m-presentmode) | How the swapchain paces presentation. |
| [`Scale`](#m-scale) | UI scale for a headless instance. |
| [`Seed`](#m-seed) | Replaces the definition's seed when set. |
| [`Shaders`](#m-shaders) | Where the 2D renderer gets its compiled shaders. |
| [`Storage`](#m-storage) | Where the game keeps its saves and settings. |
| [`Width`](#m-width) | Window (or virtual window) width in pixels. |

### <a id="m-args"></a>Args

```csharp
public GameArgs Args { get; init; }
```

The command line the game was started with, for [`GameInstance.Args`](GameInstance.md#m-args). [`GameApp.Run()`](GameApp.md#m-run-gamedefinition-string) sets it; a test sets it to try a game's own options.

**Type:** [`GameArgs`](GameArgs.md)

### <a id="m-backend"></a>Backend

```csharp
public PlatformBackendKind Backend { get; init; }
```

Which windowing system a windowed instance uses. Auto picks Wayland, then X11. Ignored when [`GameOptions.Headless`](GameOptions.md#m-headless).

**Type:** [`PlatformBackendKind`](../../OneGame.Platform/OneGame.Platform/PlatformBackendKind.md)

### <a id="m-canvasid"></a>CanvasId

```csharp
public string? CanvasId { get; init; }
```

The id of the canvas element a web page draws to. Only used in a browser.

**Type:** `string?`

### <a id="m-connect"></a>Connect

```csharp
public ConnectOptions? Connect { get; init; }
```

Connect to a server instead of running a world of its own. The game's world is then a replica of the server's scope: the server runs the simulation, commands are forwarded to it, and state cannot be written locally.

**Type:** [`ConnectOptions`](ConnectOptions.md)?

### <a id="m-datadirectory"></a>DataDirectory

```csharp
public string? DataDirectory { get; init; }
```

A folder for the game's saves and settings, overriding the default choice (the `--data` argument).

**Type:** `string?`

### <a id="m-device"></a>Device

```csharp
public GpuDevice? Device { get; init; }
```

A graphics device the host already made. A browser creates its device asynchronously, before the game starts, and hands it in here; the instance then uses it and leaves its disposal to the host.

**Type:** `GpuDevice?`

### <a id="m-displayname"></a>DisplayName

```csharp
public string? DisplayName { get; init; }
```

The display to open the window on: an X display name (`:1`) or a Wayland socket name. The environment decides when null.

**Type:** `string?`

### <a id="m-gpu"></a>Gpu

```csharp
public GpuMode Gpu { get; init; }
```

Graphics device policy: [`GpuMode.Auto`](GpuMode.md#m-auto) uses a device when one can be made, [`GpuMode.Off`](GpuMode.md#m-off) renders captures on the CPU, [`GpuMode.Required`](GpuMode.md#m-required) fails without a device.

**Type:** [`GpuMode`](GpuMode.md)

### <a id="m-headless"></a>Headless

```csharp
public bool Headless { get; init; }
```

No window: events come only from injection and nothing is presented.

**Type:** `bool`

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Window (or virtual window) height in pixels.

**Type:** `int`

### <a id="m-host"></a>Host

```csharp
public GameHost? Host { get; init; }
```

Where the window, the graphics device and the swapchain live. When set, a windowed instance borrows them from the host and gives them back when it is disposed, so the next instance on the same host starts drawing without building them again. Null (the default for [`GameInstance.Create()`](GameInstance.md#m-create-gamedefinition-gameoptions)) gives the instance its own, closed with it. [`GameApp.Run()`](GameApp.md#m-run-gamedefinition-string) passes [`GameHost.Shared`](GameHost.md#m-shared). Ignored by a headless instance and by one given a [`GameOptions.Device`](GameOptions.md#m-device).

**Type:** [`GameHost`](GameHost.md)?

### <a id="m-persistdata"></a>PersistData

```csharp
public bool PersistData { get; init; }
```

Keep saves and settings in the platform's persistent store even for a headless or unpresented run (the `--persist-data` argument). Tests that must see a save survive a restart or a page reload use it.

**Type:** `bool`

### <a id="m-presentframes"></a>PresentFrames

```csharp
public bool PresentFrames { get; init; }
```

False runs frames (simulation, input, layout) without drawing them to the window; captures still render offscreen. A web page whose browser cannot present WebGPU canvases (headless Chrome on a software adapter) runs this way under test.

**Type:** `bool`

### <a id="m-presentmode"></a>PresentMode

```csharp
public PresentMode PresentMode { get; init; }
```

How the swapchain paces presentation. `PresentMode.Vsync` (the default) is always available; the other two fall back to it where the system lacks them.

**Type:** `PresentMode`

### <a id="m-scale"></a>Scale

```csharp
public float Scale { get; init; }
```

UI scale for a headless instance. A real window uses the scale the OS reports.

**Type:** `float`

### <a id="m-seed"></a>Seed

```csharp
public ulong? Seed { get; init; }
```

Replaces the definition's seed when set.

**Type:** `ulong?`

### <a id="m-shaders"></a>Shaders

```csharp
public IShaderSource? Shaders { get; init; }
```

Where the 2D renderer gets its compiled shaders. When null, the renderer's own shaders, compiled during the build, are used.

**Type:** [`IShaderSource`](../../OneGame.Render2D/OneGame.Render2D/IShaderSource.md)?

### <a id="m-storage"></a>Storage

```csharp
public IByteStore? Storage { get; init; }
```

Where the game keeps its saves and settings. When null the host picks: the `--data` folder or `ONEGAME_DATA_DIR` if given, otherwise memory for a headless or unpresented run (a test never touches a player's files), IndexedDB in a browser, and a folder under the user's local application data elsewhere. See [`GameStorage`](GameStorage.md).

**Type:** [`IByteStore`](../../OneGame.Core/OneGame.Storage/IByteStore.md)?

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Window (or virtual window) width in pixels.

**Type:** `int`
