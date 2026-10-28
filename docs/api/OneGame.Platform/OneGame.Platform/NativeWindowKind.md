[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# NativeWindowKind enum

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public enum NativeWindowKind : byte
```

The kind of native window system handles in a [`NativeWindowHandle`](NativeWindowHandle.md).

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-none"></a>`None` | `0` | No native window (headless). |
| <a id="m-wayland"></a>`Wayland` | `1` | Wayland: [`NativeWindowHandle.Display`](NativeWindowHandle.md#m-display) is the wl\_display\*, [`NativeWindowHandle.Window`](NativeWindowHandle.md#m-window) the wl\_surface\*. |
| <a id="m-x11"></a>`X11` | `2` | X11: [`NativeWindowHandle.Display`](NativeWindowHandle.md#m-display) is the Display\*, [`NativeWindowHandle.Window`](NativeWindowHandle.md#m-window) the X Window id. |
| <a id="m-win32"></a>`Win32` | `3` | Win32: [`NativeWindowHandle.Display`](NativeWindowHandle.md#m-display) is the HINSTANCE, [`NativeWindowHandle.Window`](NativeWindowHandle.md#m-window) the HWND. |
| <a id="m-browser"></a>`Browser` | `4` | A canvas element of a web page; the swapchain finds it by id. |
| <a id="m-cocoa"></a>`Cocoa` | `5` | Cocoa: [`NativeWindowHandle.Display`](NativeWindowHandle.md#m-display) is the NSWindow\*, [`NativeWindowHandle.Window`](NativeWindowHandle.md#m-window) the CAMetalLayer\* of its content view. |
| <a id="m-android"></a>`Android` | `6` | Android: [`NativeWindowHandle.Display`](NativeWindowHandle.md#m-display) is 0, [`NativeWindowHandle.Window`](NativeWindowHandle.md#m-window) the ANativeWindow\*. The pointer is valid until a [`PlatformEventKind.SurfaceLost`](PlatformEventKind.md#m-surfacelost) event has been handled. |
| <a id="m-metallayer"></a>`MetalLayer` | `7` | Metal (macOS and iOS): [`NativeWindowHandle.Display`](NativeWindowHandle.md#m-display) is 0, [`NativeWindowHandle.Window`](NativeWindowHandle.md#m-window) the `CAMetalLayer*` of the window's view. It lives as long as the window. |
