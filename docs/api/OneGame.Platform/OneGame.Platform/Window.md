[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# Window class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public abstract class Window : IDisposable
```

An application window. Sizes are physical pixels (the size of the framebuffer a GPU swapchain should have); [`Window.Scale`](Window.md#m-scale) converts to logical units: logical = pixels / scale.

## Properties

| Member | Description |
| --- | --- |
| [`App`](#m-app) | The owning application. |
| [`CursorHidden`](#m-cursorhidden) | Whether the cursor is hidden while over the window. |
| [`CursorLocked`](#m-cursorlocked) | Whether the cursor is locked: it stays put and [`PlatformEventKind.PointerRelativeMotion`](PlatformEventKind.md#m-pointerrelativemotion) events report movement. |
| [`Height`](#m-height) | Client height in pixels. |
| [`Id`](#m-id) | The id carried by this window's events. |
| [`IsClosed`](#m-isclosed) | Whether the window has been closed. |
| [`IsFocused`](#m-isfocused) | Whether the window has input focus. |
| [`IsFullscreen`](#m-isfullscreen) | Whether the window is fullscreen. |
| [`IsMinimized`](#m-isminimized) | Whether the window is minimized. |
| [`NativeHandle`](#m-nativehandle) | Handles for creating a graphics surface. |
| [`SafeArea`](#m-safearea) | The distances in pixels from each edge that content must keep clear of: display cut-outs, rounded corners, system bars and the gesture area. |
| [`Scale`](#m-scale) | Content scale: 1.0 at 96 dpi, 2.0 on a typical HiDPI screen, fractional where the system offers it. |
| [`TextInputActive`](#m-textinputactive) | Whether a text field has focus and the game asked for input-method (IME) support; see [`Window.SetTextInput()`](Window.md#m-settextinput-bool-int-int-int-int). |
| [`TextInputArea`](#m-textinputarea) | Where the caret is, in window pixels, as last given to [`Window.SetTextInput()`](Window.md#m-settextinput-bool-int-int-int-int). |
| [`Title`](#m-title) | The window title. |
| [`Width`](#m-width) | Client width in pixels. |

### <a id="m-app"></a>App

```csharp
public PlatformApp App { get; }
```

The owning application.

**Type:** [`PlatformApp`](PlatformApp.md)

### <a id="m-cursorhidden"></a>CursorHidden

```csharp
public bool CursorHidden { get; }
```

Whether the cursor is hidden while over the window.

**Type:** `bool`

### <a id="m-cursorlocked"></a>CursorLocked

```csharp
public bool CursorLocked { get; }
```

Whether the cursor is locked: it stays put and [`PlatformEventKind.PointerRelativeMotion`](PlatformEventKind.md#m-pointerrelativemotion) events report movement.

**Type:** `bool`

### <a id="m-height"></a>Height

```csharp
public int Height { get; }
```

Client height in pixels.

**Type:** `int`

### <a id="m-id"></a>Id

```csharp
public int Id { get; }
```

The id carried by this window's events. Ids start at 1 and are not reused.

**Type:** `int`

### <a id="m-isclosed"></a>IsClosed

```csharp
public bool IsClosed { get; }
```

Whether the window has been closed.

**Type:** `bool`

### <a id="m-isfocused"></a>IsFocused

```csharp
public bool IsFocused { get; }
```

Whether the window has input focus.

**Type:** `bool`

### <a id="m-isfullscreen"></a>IsFullscreen

```csharp
public bool IsFullscreen { get; }
```

Whether the window is fullscreen.

**Type:** `bool`

### <a id="m-isminimized"></a>IsMinimized

```csharp
public bool IsMinimized { get; }
```

Whether the window is minimized.

**Type:** `bool`

### <a id="m-nativehandle"></a>NativeHandle

```csharp
public abstract NativeWindowHandle NativeHandle { get; }
```

Handles for creating a graphics surface.

**Type:** [`NativeWindowHandle`](NativeWindowHandle.md)

### <a id="m-safearea"></a>SafeArea

```csharp
public EdgeInsets SafeArea { get; }
```

The distances in pixels from each edge that content must keep clear of: display cut-outs, rounded corners, system bars and the gesture area. Zero on desktop. Changes arrive as [`PlatformEventKind.SafeAreaChanged`](PlatformEventKind.md#m-safeareachanged).

**Type:** [`EdgeInsets`](EdgeInsets.md)

### <a id="m-scale"></a>Scale

```csharp
public float Scale { get; }
```

Content scale: 1.0 at 96 dpi, 2.0 on a typical HiDPI screen, fractional where the system offers it.

**Type:** `float`

### <a id="m-textinputactive"></a>TextInputActive

```csharp
public bool TextInputActive { get; }
```

Whether a text field has focus and the game asked for input-method (IME) support; see [`Window.SetTextInput()`](Window.md#m-settextinput-bool-int-int-int-int).

**Type:** `bool`

### <a id="m-textinputarea"></a>TextInputArea

```csharp
public (int X, int Y, int Width, int Height) TextInputArea { get; }
```

Where the caret is, in window pixels, as last given to [`Window.SetTextInput()`](Window.md#m-settextinput-bool-int-int-int-int).

**Type:** `(int X, int Y, int Width, int Height)`

### <a id="m-title"></a>Title

```csharp
public string Title { get; }
```

The window title.

**Type:** `string`

### <a id="m-width"></a>Width

```csharp
public int Width { get; }
```

Client width in pixels.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Close()`](#m-close) | Closes the window. |
| [`Dispose()`](#m-dispose) |  |
| [`FillSolid(uint)`](#m-fillsolid-uint) | Fills a window made with [`WindowOptions.SoftwareSurface`](WindowOptions.md#m-softwaresurface) with one colour (0xAARRGGBB). |
| [`Minimize()`](#m-minimize) | Minimizes the window. |
| [`RequestSize(int, int)`](#m-requestsize-int-int) | Asks for a new client size in pixels. |
| [`SetCursorHidden(bool)`](#m-setcursorhidden-bool) | Shows or hides the cursor over the window. |
| [`SetCursorLocked(bool)`](#m-setcursorlocked-bool) | Locks or unlocks the cursor for relative motion (mouse look). |
| [`SetFullscreen(bool)`](#m-setfullscreen-bool) | Enters or leaves fullscreen. |
| [`SetTextInput(bool, int, int, int, int)`](#m-settextinput-bool-int-int-int-int) | Tells the system that a text field has (or no longer has) focus, and where its caret is, so an input method can show its candidate window beside it and send [`PlatformEventKind.TextComposition`](PlatformEventKind.md#m-textcomposition) events. |
| [`SetTitle(string)`](#m-settitle-string) | Sets the title. |

### <a id="m-close"></a>Close()

```csharp
public void Close();
```

Closes the window. Further calls do nothing.

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

### <a id="m-fillsolid-uint"></a>FillSolid(uint)

```csharp
public virtual void FillSolid(uint argb);
```

Fills a window made with [`WindowOptions.SoftwareSurface`](WindowOptions.md#m-softwaresurface) with one colour (0xAARRGGBB). Backends without a CPU surface ignore the call.

**Parameters**

- `argb` (`uint`)

### <a id="m-minimize"></a>Minimize()

```csharp
public abstract void Minimize();
```

Minimizes the window.

### <a id="m-requestsize-int-int"></a>RequestSize(int, int)

```csharp
public abstract void RequestSize(int width, int height);
```

Asks for a new client size in pixels. The size takes effect when the system confirms it, which is reported as a [`PlatformEventKind.WindowResized`](PlatformEventKind.md#m-windowresized) event.

**Parameters**

- `width` (`int`)
- `height` (`int`)

### <a id="m-setcursorhidden-bool"></a>SetCursorHidden(bool)

```csharp
public abstract void SetCursorHidden(bool hidden);
```

Shows or hides the cursor over the window.

**Parameters**

- `hidden` (`bool`)

### <a id="m-setcursorlocked-bool"></a>SetCursorLocked(bool)

```csharp
public abstract void SetCursorLocked(bool locked);
```

Locks or unlocks the cursor for relative motion (mouse look). Locking also hides the cursor.

**Parameters**

- `locked` (`bool`)

### <a id="m-setfullscreen-bool"></a>SetFullscreen(bool)

```csharp
public abstract void SetFullscreen(bool fullscreen);
```

Enters or leaves fullscreen.

**Parameters**

- `fullscreen` (`bool`)

### <a id="m-settextinput-bool-int-int-int-int"></a>SetTextInput(bool, int, int, int, int)

```csharp
public void SetTextInput(bool active, int x = 0, int y = 0, int width = 0, int height = 0);
```

Tells the system that a text field has (or no longer has) focus, and where its caret is, so an input method can show its candidate window beside it and send [`PlatformEventKind.TextComposition`](PlatformEventKind.md#m-textcomposition) events. Call it again when the caret moves. Where the system has no input-method protocol this only records the state, and plain [`PlatformEventKind.TextInput`](PlatformEventKind.md#m-textinput) keeps working.

**Parameters**

- `active` (`bool`)
- `x` (`int`)
- `y` (`int`)
- `width` (`int`)
- `height` (`int`)

### <a id="m-settitle-string"></a>SetTitle(string)

```csharp
public abstract void SetTitle(string title);
```

Sets the title.

**Parameters**

- `title` (`string`)
