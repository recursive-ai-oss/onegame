# platform: windows, events, input

Owner: `src/OneGame.Platform` (namespace `OneGame.Platform`). One assembly; the per-OS backends are internal. Consumers: UI (pointer, keyboard, text, focus), harness (injection, window control), GPU layer (native handles).

## Shape

```csharp
using var app = PlatformApp.Create(new PlatformOptions());       // Auto: Wayland, X11, Win32, else Headless
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
| `PointerMoved` | `WindowId`, `X`, `Y` |
| `PointerButtonDown/Up` | `WindowId`, `Button`, `X`, `Y`, `Modifiers` |
| `PointerWheel` | `WindowId`, `DeltaX`, `DeltaY` in notches, positive Y away from the user |
| `PointerRelativeMotion` | `WindowId`, `DeltaX`, `DeltaY` in pixels; only while the cursor is locked |
| `PointerInside` | `WindowId`, `Focused` = entered/left |
| `GamepadConnected/Disconnected` | `GamepadId` |
| `GamepadButtonDown/Up` | `GamepadId`, `GamepadButton` |
| `GamepadAxisMoved` | `GamepadId`, `GamepadAxis`, `Value` |

**Units.** Everything spatial is physical pixels: `Window.Width/Height` is the framebuffer size to give a swapchain, pointer positions are in the same space. `Window.Scale` (1.0 at 96 dpi, fractional where the OS says so) converts to logical units: `logical = pixels / Scale`. UI should lay out in logical units and multiply by `Scale` for the atlas/font size.

**Text vs keys.** Use `Key` for bindings (it names the key by position, so WASD works on AZERTY) and `TextInput` for typed characters. Dead keys and compositions (IME) are not handled yet; only what the layout produces directly arrives.

## Windows (`Window`)

`Width Height Scale IsFocused IsFullscreen IsMinimized IsClosed CursorHidden CursorLocked Title NativeHandle`; `SetTitle`, `RequestSize(w,h)` (takes effect through a `WindowResized` event; on Wayland the client decides, so it applies immediately), `SetFullscreen`, `Minimize`, `SetCursorHidden`, `SetCursorLocked` (hides the cursor and reports `PointerRelativeMotion`; refused, `CursorLocked` stays false, when the OS has no way to do it), `FillSolid(argb)` for windows made with `WindowOptions.SoftwareSurface` (tests and tools with no GPU surface: Wayland shows nothing until a buffer is attached), `Close`.

`NativeHandle` (`NativeWindowHandle(Kind, Display, Window)`) is what the GPU layer needs to build a surface; neither project references the other:

| Kind | `Display` | `Window` |
|---|---|---|
| `Wayland` | `wl_display*` | `wl_surface*` |
| `X11` | `Display*` | X `Window` id |
| `Win32` | `HINSTANCE` | `HWND` |
| `None` | 0 | 0 (headless) |

A GPU swapchain that presents to a Wayland window must not be combined with `SoftwareSurface`.

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

Not yet: IME composition and dead keys, clipboard, touch, multi-monitor selection, custom cursors (Wayland draws the cursor only through cursor-shape, so a compositor without it keeps whatever cursor it had), HID gamepads on Windows, gyro/touchpad.
