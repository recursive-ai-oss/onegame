[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# SurfaceSource struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SurfaceSource`](SurfaceSource.md)&gt;

```csharp
public readonly struct SurfaceSource : IEquatable<SurfaceSource>
```

Where a swapchain draws: the native handles of a window the platform layer created.

## Properties

| Member | Description |
| --- | --- |
| [`CanvasId`](#m-canvasid) | The id of the canvas element for [`SurfaceKind.Canvas`](SurfaceKind.md#m-canvas). |
| [`Display`](#m-display) | First handle: Display\*, wl\_display\*, or HINSTANCE. |
| [`Kind`](#m-kind) | Window system. |
| [`Window`](#m-window) | Second handle: the X11 Window id, wl\_surface\*, or HWND. |

### <a id="m-canvasid"></a>CanvasId

```csharp
public string? CanvasId { get; init; }
```

The id of the canvas element for [`SurfaceKind.Canvas`](SurfaceKind.md#m-canvas).

**Type:** `string?`

### <a id="m-display"></a>Display

```csharp
public nint Display { get; init; }
```

First handle: Display\*, wl\_display\*, or HINSTANCE.

**Type:** `nint`

### <a id="m-kind"></a>Kind

```csharp
public SurfaceKind Kind { get; init; }
```

Window system.

**Type:** [`SurfaceKind`](SurfaceKind.md)

### <a id="m-window"></a>Window

```csharp
public nint Window { get; init; }
```

Second handle: the X11 Window id, wl\_surface\*, or HWND.

**Type:** `nint`

## Methods

| Member | Description |
| --- | --- |
| [`Android(nint)`](#m-android-nint) | An Android window (an `ANativeWindow*`). |
| [`Canvas(string)`](#m-canvas-string) | A canvas element of a web page. |
| [`MetalLayer(nint)`](#m-metallayer-nint) | A Metal layer (`CAMetalLayer*`) the window system created for a view. |
| [`Wayland(nint, nint)`](#m-wayland-nint-nint) | A Wayland surface. |
| [`Win32(nint, nint)`](#m-win32-nint-nint) | A Win32 window. |
| [`X11(nint, nint)`](#m-x11-nint-nint) | An X11 window. |

### <a id="m-android-nint"></a>Android(nint)

```csharp
public static SurfaceSource Android(nint nativeWindow);
```

An Android window (an `ANativeWindow*`). The window must outlive the swapchain: dispose the swapchain when the platform reports the surface lost.

**Parameters**

- `nativeWindow` (`nint`)

**Returns:** [`SurfaceSource`](SurfaceSource.md)

### <a id="m-canvas-string"></a>Canvas(string)

```csharp
public static SurfaceSource Canvas(string canvasId);
```

A canvas element of a web page.

**Parameters**

- `canvasId` (`string`)

**Returns:** [`SurfaceSource`](SurfaceSource.md)

### <a id="m-metallayer-nint"></a>MetalLayer(nint)

```csharp
public static SurfaceSource MetalLayer(nint layer);
```

A Metal layer (`CAMetalLayer*`) the window system created for a view.

**Parameters**

- `layer` (`nint`)

**Returns:** [`SurfaceSource`](SurfaceSource.md)

### <a id="m-wayland-nint-nint"></a>Wayland(nint, nint)

```csharp
public static SurfaceSource Wayland(nint display, nint surface);
```

A Wayland surface.

**Parameters**

- `display` (`nint`)
- `surface` (`nint`)

**Returns:** [`SurfaceSource`](SurfaceSource.md)

### <a id="m-win32-nint-nint"></a>Win32(nint, nint)

```csharp
public static SurfaceSource Win32(nint hinstance, nint hwnd);
```

A Win32 window.

**Parameters**

- `hinstance` (`nint`)
- `hwnd` (`nint`)

**Returns:** [`SurfaceSource`](SurfaceSource.md)

### <a id="m-x11-nint-nint"></a>X11(nint, nint)

```csharp
public static SurfaceSource X11(nint display, nint window);
```

An X11 window.

**Parameters**

- `display` (`nint`)
- `window` (`nint`)

**Returns:** [`SurfaceSource`](SurfaceSource.md)
