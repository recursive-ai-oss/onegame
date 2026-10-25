# platform: windows, events, input

Owner: `src/OneGame.Platform` (namespace `OneGame.Platform`). One assembly; the per-OS backends are internal. Consumers: UI (pointer, keyboard, text, focus), harness (injection, window control), GPU layer (native handles).

## Shape

```csharp
using var app = PlatformApp.Create(new PlatformOptions());       // Auto: Cocoa on macOS, Wayland, X11, Win32, else Headless
var window = app.CreateWindow(new WindowOptions { Title = "game", Width = 1280, Height = 720 });

while (!app.QuitRequested)
{
    app.PumpEvents();                                             // or PumpEvents(timeoutMs) to sleep until input
    while (app.TryGetEvent(out var e)) { /* PlatformEvent */ }
    // app.Input.IsKeyDown(Key.W), app.Input.GetGamepad(id)?.GetAxis(GamepadAxis.LeftX) ...
}
```

Single-threaded: use an app from the thread that created it. All real and synthetic input goes through one queue (`PlatformApp.Post`), so game code cannot tell them apart. The queue holds 16384 events; `DroppedEvents` counts overflow.

## Events (`PlatformEvent`, a record struct; only the fields listed for a kind are meaningful)

| Kind | Fields |
|---|---|
| `QuitRequested` | none (`app.QuitRequested` stays true) |
| `WindowCloseRequested` | `WindowId`. The window stays open until the game calls `Close()` |
| `WindowResized` | `WindowId`, `Width`, `Height` (pixels) |
| `WindowScaleChanged` | `WindowId`, `Scale` |
| `WindowFocusChanged` | `WindowId`, `Focused`. Losing focus releases every held key and button in `app.Input` |
| `KeyDown` / `KeyUp` | `WindowId`, `Key` (physical, US-position names), `Modifiers`, `IsRepeat` |
| `TextInput` | `WindowId`, `Text` (whole Unicode characters in the active layout), `Modifiers`. Not sent for Ctrl/Alt/Meta chords or control characters |
| `TextComposition` | `WindowId`, `Text` (the whole pre-edit string; empty when the composition ended), `CursorStart`/`CursorEnd` (UTF-16 indexes into `Text`, -1 for no cursor). The finished text follows as `TextInput` |
| `PointerMoved` | `WindowId`, `X`, `Y` |
| `PointerButtonDown/Up` | `WindowId`, `Button`, `X`, `Y`, `Modifiers` |
| `PointerWheel` | `WindowId`, `DeltaX`, `DeltaY` in notches, positive Y away from the user |
| `PointerRelativeMotion` | `WindowId`, `DeltaX`, `DeltaY` in pixels; only while the cursor is locked |
| `PointerInside` | `WindowId`, `Focused` = entered/left |
| `GamepadConnected/Disconnected` | `GamepadId` |
| `GamepadButtonDown/Up` | `GamepadId`, `GamepadButton` |
| `GamepadAxisMoved` | `GamepadId`, `GamepadAxis`, `Value` |
| `TouchDown` / `TouchMove` / `TouchUp` / `TouchCancel` | `WindowId`, `TouchId`, `X`, `Y`, `Pressure` (see "Mobile") |
| `AppLifecycle` | `Lifecycle`: `Resumed`, `Paused`, `LowMemory` |
| `SafeAreaChanged` | `WindowId`, `Insets` (`EdgeInsets` Left/Top/Right/Bottom, pixels) |
| `SurfaceLost` / `SurfaceCreated` | `WindowId`. The drawing surface went away / is back (see "Mobile") |
| `BackRequested` | `WindowId`. The system's back button or gesture |

**Units.** Everything spatial is physical pixels: `Window.Width/Height` is the framebuffer size to give a swapchain, pointer positions are in the same space. `Window.Scale` (1.0 at 96 dpi, fractional where the OS says so) converts to logical units: `logical = pixels / Scale`. UI should lay out in logical units and multiply by `Scale` for the atlas/font size.

**Text vs keys.** Use `Key` for bindings (it names the key by position, so WASD works on AZERTY) and `TextInput` for typed characters. A text field turns input-method support on with `window.SetTextInput(true, x, y, w, h)` (the caret rectangle in window pixels; call again when it moves, `SetTextInput(false)` when focus leaves) and then gets `TextComposition` events while the user composes and `TextInput` for the result. Where the system has no input-method protocol (X11 without XIM, Win32 for now) `SetTextInput` only records the state and plain `TextInput` keeps working; dead keys come out of the layout as ordinary text.

## Windows (`Window`)

`Width Height Scale SafeArea IsFocused IsFullscreen IsMinimized IsClosed CursorHidden CursorLocked Title NativeHandle`; `SetTitle`, `RequestSize(w,h)` (takes effect through a `WindowResized` event; on Wayland the client decides, so it applies immediately), `SetFullscreen`, `Minimize`, `SetCursorHidden`, `SetCursorLocked` (hides the cursor and reports `PointerRelativeMotion`; refused, `CursorLocked` stays false, when the OS has no way to do it), `TextInputActive`/`TextInputArea`/`SetTextInput` (above), `FillSolid(argb)` for windows made with `WindowOptions.SoftwareSurface` (tests and tools with no GPU surface: Wayland shows nothing until a buffer is attached), `Close`.

`NativeHandle` (`NativeWindowHandle(Kind, Display, Window)`) is what the GPU layer needs to build a surface; neither project references the other:

| Kind | `Display` | `Window` |
|---|---|---|
| `Wayland` | `wl_display*` | `wl_surface*` |
| `X11` | `Display*` | X `Window` id |
| `Win32` | `HINSTANCE` | `HWND` |
| `Cocoa` | `NSWindow*` | `CAMetalLayer*` of the content view (give it to the Metal backend as `SurfaceSource.MetalLayer`) |
| `Android` | 0 | `ANativeWindow*` |
| `None` | 0 | 0 (headless) |

A GPU swapchain that presents to a Wayland window must not be combined with `SoftwareSurface`.

## Clipboard

`app.Clipboard` (`PlatformClipboard`): `GetText(selection)`, `SetText(text, selection)`, `IsAvailable`, `Supports(selection)`. UTF-8 text only, up to `MaxTextBytes` (4 MB); longer content reads as absent and `SetText` returns false. Calls are synchronous on the app's thread.

| Backend | How | Notes |
|---|---|---|
| X11 | ICCCM selections on a hidden helper window: `CLIPBOARD` and `PRIMARY` (`ClipboardSelection.Primary`); targets `UTF8_STRING`, `text/plain;charset=utf-8`, `TEXT`, `STRING` | Reading asks the owner and waits up to 1 s while still handling other events; the INCR protocol is read, not written, so `SetText` is limited to what one request holds (megabytes). While the app owns the selection it answers other programs from `PumpEvents`, so keep pumping. |
| Wayland | `wl_data_device` on the seat; offers `text/plain;charset=utf-8`, `UTF8_STRING`, `text/plain`, `TEXT`, `STRING` | Needs a seat: `IsAvailable` is false on a compositor without one (headless Weston). Writes to the reader happen on a pool thread, so a slow reader cannot stall the game. `Primary` is not supported (`Supports` false). |
| Win32 | `CF_UNICODETEXT`, CR LF converted both ways | Compiles; not run on Windows yet. |
| Cocoa | `NSPasteboard` general pasteboard, `public.utf8-plain-text` | Size is checked (`lengthOfBytesUsingEncoding:`) before the text is copied out. No `Primary` selection. |
| Headless | a private in-process store | So scenarios and tests can copy and paste. |

A reader that gets no answer in time returns null instead of hanging. Text from other programs is decoded with replacement characters, cut at the first NUL, and read under the size limit before anything is allocated.

## Mobile: touch, lifecycle, safe area, keyboard

These are the events a phone or tablet adds. They are backend-neutral: Android posts them today, and iOS (UIKit) is meant to post the same ones, so a game written against this section runs on both. Desktop backends never post the lifecycle, surface or safe-area events and post touch only from injection.

**Touch.** A finger is a `TouchId` from `TouchDown` until `TouchUp` or `TouchCancel` (cancel means the system took the gesture; do not activate anything). Ids are small integers (0 to 31) reused after a lift. `app.Input.TouchCount` and `TryGetTouch(id, out x, out y)` give the fingers that are down. Positions are window pixels, like the pointer's.

**Touch drives the pointer.** With `PlatformOptions.TouchEmulatesPointer` (the default) the *first* finger down also posts `PointerMoved` and `PointerButtonDown(Left)`, its moves post `PointerMoved`, and its lift or cancel posts `PointerButtonUp`, right after the touch event itself. Later fingers while the first is down are touch events only; once the first lifts, the next finger to touch becomes the pointer. That is the whole mapping: the UI sees one pointer, so a kit built for a mouse (buttons, drag and drop of cards) works under a finger, and a game that wants gestures reads the touch events. A game that handles touch itself sets the option to false and gets no pointer events from fingers. A touch screen has no hover, so `:hover` is only true while a finger is down on the element.

**Lifecycle.** `AppLifecycle` with `Paused` means the app left the foreground (a call, the home button, another app): stop simulating and playing sound, keep state. `GameInstance` does this for realtime runs: `Suspended` is true, ticks stop, nothing is owed on return, and `Resumed` carries on from the same state hash. `LowMemory` asks for rebuildable caches to be dropped (the host releases the renderer's per-target caches). Nothing here saves the game; a game that wants a save on `Paused` writes it from `OnInput`.

**Surface.** A phone's window can lose its drawing surface while the app lives on (background, rotation on some devices, a display change). `SurfaceLost` arrives while the old surface is still valid; the game must destroy every swapchain made on it before it handles the next event or draws again, because the system waits for that before it takes the surface away. `Window.NativeHandle` is `None` until `SurfaceCreated`, whose handle is the new surface. `GameInstance` does this itself; a game with its own swapchain follows the same rule. A GPU call that finds the surface already gone throws `GpuSurfaceLostException`: drop the swapchain and wait for `SurfaceCreated`. Rotation normally arrives as `WindowResized` on the same surface (the app asks to handle configuration changes itself); the swapchain is rebuilt from the new size.

**Safe area.** `Window.SafeArea` and `SafeAreaChanged` give the distances in pixels the content should keep from each edge (status and navigation bars, a camera cut-out, the gesture area). Zero on desktop. Draw backgrounds to the edge and keep controls inside.

**Scale.** On Android one logical unit is one dp (160 per inch): `Scale = density / 160`, so a 420 dpi phone has 2.625 and a 40-unit button is about 6 mm wherever it is. The desktop rule (1.0 at 96 dpi) is a different unit; layouts for phones should size touch targets in logical units of at least 40.

**Back.** `BackRequested` is the system back button or gesture (on key up). `GameInstance` sends the UI a `Cancel`; if nothing handles it, the app quits to the launcher. A game that needs another meaning handles the event in `OnInput` first.

**Soft keyboard and text.** `window.SetTextInput(true, ...)` shows the system keyboard and `SetTextInput(false)` hides it (the kit's text fields already do this). On Android the keyboard's letters, digits and punctuation arrive as `KeyDown`/`KeyUp` plus a `TextInput` for the character the device's layout makes, which is also what `adb shell input text` and a hardware keyboard produce. **Composed text** (predictive text, swipe typing, CJK input methods) needs an input connection that only a Java view can offer; it is not delivered yet, so `TextComposition` never fires on Android, and keyboards that commit whole words instead of keys deliver nothing.

**Gamepads.** Android hands pads over as input events already in the standard layout (A south, B east, the d-pad as hat or keys, sticks and triggers as axes). Each device gets a `GamepadId` on its first event, with `GamepadConnected` and the usual button and axis events; the sticks use `StickDeadZone`.

**Injection.** `app.Injector.TouchDown/TouchMove/TouchUp/TouchCancel/Tap` and `Lifecycle(state)` post the same events; the control operations `input_touch`, `ui_tap` and `app_lifecycle` and the scenario steps `tap:` and `touch:` use them.

## Input state

`app.Input`: `IsKeyDown`, `IsButtonDown`, `PointerX/Y`, `Modifiers`, `GetGamepad(id)` (`GamepadState`: `IsDown`, `GetAxis`, `Name`, `IsMapped`, `CanRumble`, `IsConnected`).

## Gamepads

Always the standard layout: `GamepadButton` South/East/West/North (positional, so South is A on Xbox and Cross on PlayStation), shoulders, Back/Start/Guide, sticks, d-pad, Misc1, Touchpad, Paddles; `GamepadAxis` LeftX/LeftY/RightX/RightY in -1..1 with **down and right positive**, LeftTrigger/RightTrigger in 0..1. Sticks get a radial dead zone and rescale (`PlatformOptions.StickDeadZone`, default 0.1; `TriggerDeadZone` 0.03).

- Linux: evdev devices in `/dev/input` are scanned every 500 ms (hotplug). Buttons/axes/hats are numbered the way the community mapping database expects, then mapped through `app.Gamepads.Mappings` (a `GamepadMappingDatabase`, filled with `AddText(databaseText)`); a device with no entry gets the kernel's standard gamepad layout. The engine ships no mapping data yet: the database is test data (`tests/OneGame.Platform.Tests/testdata`, zlib licence notice alongside).
- Windows: XInput (four slots) mapped through the same mapper; no HID/DInput path yet, so non-XInput pads are not seen on Windows.
- `app.Gamepads.Rumble(id, low, high, ms)` returns false when the device cannot.

## Synthetic input (harness)

`app.Injector` produces the same events as devices: `Key`, `KeyPress`, `Text`, `PointerMove`, `PointerButton`, `Wheel`, `RelativeMotion`, `CloseRequest`, `GamepadConnect/Disconnect/Button/Axis`. Modifiers on injected key/pointer events come from the modifier keys currently held in `app.Input`. A real key press yields a `KeyDown` and, where the layout types something, a `TextInput`; inject both (`Key` then `Text`) to match. Injected gamepad values are final (no dead zone). `PlatformOptions.Backend = Headless` gives windows with no OS window at all, for runs with no display; `EnableGamepadDevices = false` stops device scanning.

## Backends and requirements

Wayland: libwayland-client, libxkbcommon; core, xdg-shell, plus fractional-scale/viewporter, xdg-decoration, relative-pointer, pointer-constraints and cursor-shape where the compositor has them. The protocol tables are generated by `tools/WaylandGen` from the XML in `tools/WaylandGen/protocols`. Key repeat is synthesized from `repeat_info`. X11: libX11, libxkbcommon(+x11). Win32: user32, xinput1_4. `PlatformOptions.DisplayName` picks the display (`:1`, a Wayland socket name or absolute socket path) instead of the environment.

X11 re-reads the desktop scale (`Xft.dpi` in the root window's `RESOURCE_MANAGER`) when it changes and reports `WindowScaleChanged`. Wayland input methods use `text-input-v3` when the compositor offers it (the preedit string, cursor and commit become `TextComposition` and `TextInput`; surrounding text and delete-surrounding are not used). Weston, the compositor the tests run, offers no text-input-v3 and no seat when headless; those paths are tested against a scripted compositor on the real protocol. Fractional scale (`wp_fractional_scale` plus `wp_viewport`) is driven here for `SoftwareSurface` windows; a Vulkan swapchain on a compositor that offers it is not verified (neither test compositor does), including whether the driver's own surface objects clash with ours.

Android (NDK, `libandroid`, `liblog`, JNI for the few Java calls): see "Mobile" and `android.md`. Not yet: XIM/IBus on X11 and IME on Windows (`WM_IME_*`), multi-monitor selection, custom cursors (Wayland draws the cursor only through cursor-shape, so a compositor without it keeps whatever cursor it had), HID gamepads on Windows, gyro/touchpad.

## macOS (Cocoa)

`PlatformBackendKind.Cocoa` (`--backend cocoa`), Apple silicon only: the Objective-C runtime is reached through typed `objc_msgSend` function pointers, and the content view and the delegate are classes made at run time whose methods point into this assembly. `NSApplication` and `NSWindow` are built without a nib.

- **Main thread.** AppKit only works on the process's main thread, so `PlatformApp.Create` throws `PlatformException` anywhere else. The game's frame loop is the main thread; `PumpEvents` drains `NSApplication`'s queue by hand and the delegate callbacks fill the event queue. Exceptions inside a callback are parked and rethrown from the next `PumpEvents`.
- **Window.** Sizes are pixels, as everywhere: `WindowOptions.Width/Height` are converted to points with the main screen's backing scale, and `Window.Scale` is `backingScaleFactor` (2.0 on a Retina screen). Resize, backing-scale changes (moving between screens), minimize, native fullscreen (`SetFullscreen` is animated: `IsFullscreen` follows when AppKit says it is done) and close work; the close button is a `WindowCloseRequested` event and the window stays until `Close()`. The menu bar has Hide, Enter Full Screen and Quit; Quit and the Dock's Quit become `QuitRequested`, never a terminate. Copy and Paste are not menu items, so Command+C and Command+V reach the game as keys. During a live resize drag AppKit runs its own loop, so the game does not step until the mouse is released.
- **Surface.** The content view is layer-backed and its layer is a `CAMetalLayer`; AppKit keeps its size and scale, the swapchain sets `drawableSize`.
- **Keys and text.** `Key` comes from the virtual key code (an ANSI-position table), modifiers from the event flags; left and right modifier keys are told apart by the device bits. While Command is held AppKit sends no key-up, so a Command chord is delivered as down followed by up. Text: with `SetTextInput(true, ...)` key events go through `interpretKeyEvents:` into the view's `NSTextInputClient` methods (`insertText:` becomes `TextInput`, `setMarkedText:` becomes `TextComposition` with the selection as `CursorStart/End`, `unmarkText` ends it; the caret rectangle for the candidate window comes from `TextInputArea`). A key pressed while an input method is composing is not sent as `KeyDown`. With text input off, typed text is the event's `characters`, so an input method never swallows game keys. Option is part of typing (Option+E gives dead-key text); Control and Command never produce text.
- **Pointer.** Positions are pixels from the top left. A wheel reports lines; a trackpad reports points, converted at 20 points per notch. `SetCursorLocked` detaches the pointer with `CGAssociateMouseAndMouseCursorPosition` and reports `PointerRelativeMotion` in pixels.
- **Gamepads.** The GameController framework, polled once per `PumpEvents`: controllers with an extended gamepad profile are mapped onto the standard layout (A/B/X/Y to South/East/West/North, Menu to Start, Options to Back, Home to Guide, thumbstick buttons, d-pad, triggers 0..1, sticks with down positive), through the same dead zones as every other pad. No rumble, paddles or touchpad yet. A pad that does not report extended gamepad input (Siri remotes, micro gamepads) is ignored.
- **Focus.** `WindowFocusChanged` follows key-window status. A process the system did not activate (started from an SSH session, or with a locked screen) never becomes frontmost: windows are created and take injected input, but there is no key focus and fullscreen does not start. `tools/mac-gui-run.sh` starts a program through `open` so a logged-in, unlocked session activates it.
- **Tests.** `CocoaTests` (Platform.Tests) hand real `NSEvent`s to the window and call the `NSTextInputClient` methods on the real view; `CocoaWindowedTests` runs the windowed suite on a Cocoa window with Metal API validation. Both test apps replace xUnit's generated entry point (`MainThread.Host`) so the main thread serves test bodies while the runner works on another thread.

## What a Windows run must prove

Win32 builds and its pure logic (key tables, text conversion, CR LF) is tested here; nothing has run on Windows. A run there checks: window creation, DPI awareness and `WindowScaleChanged` when dragged between monitors, resize and minimize with a swapchain on the `HWND`, key and `WM_CHAR` text, pointer and raw mouse, Alt+F4 as `WindowCloseRequested`, XInput pads, and the clipboard (`CF_UNICODETEXT` round trip with another program, CR LF both ways, a locked clipboard retried). IME (`WM_IME_COMPOSITION`) is not implemented, so `TextComposition` never fires there.

## Browser backend (`PlatformBackendKind.Browser`)

The window is the page's canvas (one window; the canvas id is `GameOptions.CanvasId`, default `onegame-canvas`). `js/onegame-platform.js` queues DOM events and hands them over as one JSON text per frame: pointer (mouse and pen) and touch (each finger a `TouchId` from 0 up, so the shared touch-to-pointer mapping applies), wheel, keys by DOM `code`, resize and pixel ratio, visibility and focus, text and input-method composition through a hidden text field (keys during a composition are not game keys), paste, and gamepads from the Gamepad API (standard mapping; `injectGamepads` replaces the source in tests). The browser owns the loop: `BrowserPage.StartFrames` calls back once per animation frame, and the game's `RunHostedFrame` runs the ticks the clock owes and draws. `GameApp.Run` returns at once in a browser; the page's `boot.mjs` registers the `onegame-platform` and `onegame-gpu` modules and starts the program. Missing in the browser: audio output (a game plays silent), the pointer-lock and fullscreen requests that need a user gesture first, and persistent saves (the file system is in memory).
`tools/web/platform-test.mjs` checks the script with synthetic events; `tools/web/input-test.mjs` drives a solitaire page with real injected input.
