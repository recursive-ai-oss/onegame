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
| [`Backend`](#m-backend) | Which windowing system a windowed instance uses. |
| [`CanvasId`](#m-canvasid) | The id of the canvas element a web page draws to. |
| [`Connect`](#m-connect) | Connect to a server instead of running a world of its own. |
| [`DataDirectory`](#m-datadirectory) | A folder for the game's saves and settings, overriding the default choice (the `--data` argument). |
| [`Device`](#m-device) | A graphics device the host already made. |
| [`DisplayName`](#m-displayname) | The display to open the window on: an X display name (`:1`) or a Wayland socket name. |
| [`Gpu`](#m-gpu) | Graphics device policy: [`GpuMode.Auto`](GpuMode.md#m-auto) uses a device when one can be made, [`GpuMode.Off`](GpuMode.md#m-off) renders captures on the CPU, [`GpuMode.Required`](GpuMode.md#m-required) fails without a device. |
| [`Headless`](#m-headless) | No window: events come only from injection and nothing is presented. |
| [`Height`](#m-height) | Window (or virtual window) height in pixels. |
| [`PersistData`](#m-persistdata) | Keep saves and settings in the platform's persistent store even for a headless or unpresented run (the `--persist-data` argument). |
| [`PresentFrames`](#m-presentframes) | False runs frames (simulation, input, layout) without drawing them to the window; captures still render offscreen. |
| [`PresentMode`](#m-presentmode) | How the swapchain paces presentation. |
| [`Scale`](#m-scale) | UI scale for a headless instance. |
| [`Seed`](#m-seed) | Replaces the definition's seed when set. |
| [`Shaders`](#m-shaders) | Where the 2D renderer gets its compiled shaders. |
| [`Storage`](#m-storage) | Where the game keeps its saves and settings. |
| [`Width`](#m-width) | Window (or virtual window) width in pixels. |

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

**Type:** [`GpuDevice`](../../OneGame.Gpu/OneGame.Gpu/GpuDevice.md)?

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

How the swapchain paces presentation. [`PresentMode.Vsync`](../../OneGame.Gpu/OneGame.Gpu/PresentMode.md#m-vsync) (the default) is always available; the other two fall back to it where the system lacks them.

**Type:** [`PresentMode`](../../OneGame.Gpu/OneGame.Gpu/PresentMode.md)

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
