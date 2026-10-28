[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# PlatformEventKind enum

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public enum PlatformEventKind : byte
```

What happened.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-none"></a>`None` | `0` | No event. |
| <a id="m-quitrequested"></a>`QuitRequested` | `1` | The application was asked to quit (session end, [`PlatformApp.RequestQuit()`](PlatformApp.md#m-requestquit)). |
| <a id="m-windowcloserequested"></a>`WindowCloseRequested` | `2` | The user asked to close a window. The window stays open until the game closes it. |
| <a id="m-windowresized"></a>`WindowResized` | `3` | A window's size in pixels changed. |
| <a id="m-windowscalechanged"></a>`WindowScaleChanged` | `4` | A window's content scale changed. |
| <a id="m-windowfocuschanged"></a>`WindowFocusChanged` | `5` | A window gained or lost input focus. |
| <a id="m-keydown"></a>`KeyDown` | `6` | A key went down (or auto-repeated, see [`PlatformEvent.IsRepeat`](PlatformEvent.md#m-isrepeat)). |
| <a id="m-keyup"></a>`KeyUp` | `7` | A key went up. |
| <a id="m-textinput"></a>`TextInput` | `8` | Text was typed. [`PlatformEvent.Text`](PlatformEvent.md#m-text) holds it. |
| <a id="m-textcomposition"></a>`TextComposition` | `9` | An input method is composing text that is not final yet. [`PlatformEvent.Text`](PlatformEvent.md#m-text) is the whole pre-edit string (empty when the composition ended or was cancelled) and [`PlatformEvent.CursorStart`](PlatformEvent.md#m-cursorstart)/[`PlatformEvent.CursorEnd`](PlatformEvent.md#m-cursorend) mark the highlighted part. The finished text arrives as [`PlatformEventKind.TextInput`](PlatformEventKind.md#m-textinput). |
| <a id="m-pointermoved"></a>`PointerMoved` | `10` | The pointer moved to [`PlatformEvent.X`](PlatformEvent.md#m-x), [`PlatformEvent.Y`](PlatformEvent.md#m-y). |
| <a id="m-pointerbuttondown"></a>`PointerButtonDown` | `11` | A pointer button went down. |
| <a id="m-pointerbuttonup"></a>`PointerButtonUp` | `12` | A pointer button went up. |
| <a id="m-pointerwheel"></a>`PointerWheel` | `13` | The wheel turned; [`PlatformEvent.DeltaX`](PlatformEvent.md#m-deltax) and [`PlatformEvent.DeltaY`](PlatformEvent.md#m-deltay) are in notches, positive Y away from the user. |
| <a id="m-pointerrelativemotion"></a>`PointerRelativeMotion` | `14` | Raw pointer motion in pixels, sent while the cursor is locked. |
| <a id="m-pointerinside"></a>`PointerInside` | `15` | The pointer entered or left a window; see [`PlatformEvent.Focused`](PlatformEvent.md#m-focused). |
| <a id="m-gamepadconnected"></a>`GamepadConnected` | `16` | A gamepad was connected. |
| <a id="m-gamepaddisconnected"></a>`GamepadDisconnected` | `17` | A gamepad was removed. |
| <a id="m-gamepadbuttondown"></a>`GamepadButtonDown` | `18` | A gamepad button went down. |
| <a id="m-gamepadbuttonup"></a>`GamepadButtonUp` | `19` | A gamepad button went up. |
| <a id="m-gamepadaxismoved"></a>`GamepadAxisMoved` | `20` | A gamepad axis changed. |
| <a id="m-touchdown"></a>`TouchDown` | `21` | A finger touched the window. [`PlatformEvent.TouchId`](PlatformEvent.md#m-touchid) names the finger until it lifts; [`PlatformEvent.X`](PlatformEvent.md#m-x)/[`PlatformEvent.Y`](PlatformEvent.md#m-y) are in window pixels. |
| <a id="m-touchmove"></a>`TouchMove` | `22` | A finger that is down moved. |
| <a id="m-touchup"></a>`TouchUp` | `23` | A finger lifted. |
| <a id="m-touchcancel"></a>`TouchCancel` | `24` | The system took a finger away (a gesture of its own, or the window lost the surface). Treat it as a lift that must not activate anything. |
| <a id="m-applifecycle"></a>`AppLifecycle` | `25` | The app moved between foreground and background, or the system is short of memory; see [`PlatformEvent.Lifecycle`](PlatformEvent.md#m-lifecycle). |
| <a id="m-safeareachanged"></a>`SafeAreaChanged` | `26` | The area of the window that is free of cut-outs and system bars changed; see [`PlatformEvent.Insets`](PlatformEvent.md#m-insets). |
| <a id="m-surfacelost"></a>`SurfaceLost` | `27` | The window's drawing surface is gone (the app went to the background, or the system is rebuilding the window). Release every swapchain made on it before the next frame. |
| <a id="m-surfacecreated"></a>`SurfaceCreated` | `28` | The window has a drawing surface again; [`Window.NativeHandle`](Window.md#m-nativehandle) names the new one. Make a new swapchain. |
| <a id="m-backrequested"></a>`BackRequested` | `29` | The user pressed the system's back gesture or button. If the game leaves it unhandled it should ask to quit. |
