[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# SurfaceKind enum

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public enum SurfaceKind : int
```

Native window system a [`SurfaceSource`](SurfaceSource.md) belongs to.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-x11"></a>`X11` | `0` | X11: a Display pointer and a Window id. |
| <a id="m-wayland"></a>`Wayland` | `1` | Wayland: a wl\_display pointer and a wl\_surface pointer. |
| <a id="m-win32"></a>`Win32` | `2` | Win32: an HINSTANCE and an HWND. |
| <a id="m-canvas"></a>`Canvas` | `3` | A browser canvas element, named by its id. |
| <a id="m-android"></a>`Android` | `4` | Android: an ANativeWindow pointer. |
| <a id="m-metal"></a>`Metal` | `5` | Metal: a CAMetalLayer pointer (macOS and iOS). |
