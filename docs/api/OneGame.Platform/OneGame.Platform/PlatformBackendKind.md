[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# PlatformBackendKind enum

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public enum PlatformBackendKind : byte
```

Which windowing system a [`PlatformApp`](PlatformApp.md) talks to.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-auto"></a>`Auto` | `0` | Pick the best one available: Android on a phone, otherwise Wayland, then X11, then headless. |
| <a id="m-headless"></a>`Headless` | `1` | Windows are plain in-memory objects with no operating-system window. Events come only from injection. |
| <a id="m-wayland"></a>`Wayland` | `2` | Wayland through libwayland-client. |
| <a id="m-x11"></a>`X11` | `3` | X11 through Xlib. |
| <a id="m-win32"></a>`Win32` | `4` | Win32. |
| <a id="m-browser"></a>`Browser` | `5` | A web page: the window is a canvas element and the platform is the browser. |
| <a id="m-cocoa"></a>`Cocoa` | `6` | Cocoa (AppKit) on macOS. |
| <a id="m-android"></a>`Android` | `7` | Android through the NDK: a native activity with touch, lifecycle and a Vulkan surface. |
| <a id="m-ios"></a>`Ios` | `8` | iOS through UIKit: a scene with touch, lifecycle, a soft keyboard and a Metal layer. |
