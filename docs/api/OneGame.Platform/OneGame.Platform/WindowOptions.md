[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# WindowOptions record

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`WindowOptions`](WindowOptions.md)&gt;

```csharp
public sealed record WindowOptions : IEquatable<WindowOptions>
```

Options for [`PlatformApp.CreateWindow()`](PlatformApp.md#m-createwindow-windowoptions).

## Constructors

| Member | Description |
| --- | --- |
| [`WindowOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>WindowOptions()

```csharp
public WindowOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`Fullscreen`](#m-fullscreen) | Start in fullscreen. |
| [`Height`](#m-height) | Initial client height in pixels. |
| [`Resizable`](#m-resizable) | Whether the user can resize the window. |
| [`SoftwareSurface`](#m-softwaresurface) | Give the window a CPU-drawn surface (see [`Window.FillSolid()`](Window.md#m-fillsolid-uint)). |
| [`Title`](#m-title) | The window title. |
| [`Width`](#m-width) | Initial client width in pixels. |

### <a id="m-fullscreen"></a>Fullscreen

```csharp
public bool Fullscreen { get; init; }
```

Start in fullscreen.

**Type:** `bool`

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Initial client height in pixels.

**Type:** `int`

### <a id="m-resizable"></a>Resizable

```csharp
public bool Resizable { get; init; }
```

Whether the user can resize the window.

**Type:** `bool`

### <a id="m-softwaresurface"></a>SoftwareSurface

```csharp
public bool SoftwareSurface { get; init; }
```

Give the window a CPU-drawn surface (see [`Window.FillSolid()`](Window.md#m-fillsolid-uint)). Wayland only shows a window once it has content, so tests and tools with no GPU surface set this. Leave it off when a GPU swapchain presents to the window.

**Type:** `bool`

### <a id="m-title"></a>Title

```csharp
public string Title { get; init; }
```

The window title.

**Type:** `string`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Initial client width in pixels.

**Type:** `int`
