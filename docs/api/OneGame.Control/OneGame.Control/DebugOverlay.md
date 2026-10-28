[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# DebugOverlay class

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IFrameOverlay`](../../OneGame.Host/OneGame.Host/IFrameOverlay.md)

```csharp
public sealed class DebugOverlay : IFrameOverlay
```

The on-screen debug panel of development builds: frame time, entity count, network figures when the game is connected, and the audio device's counters. It reads what the control layer already reports, draws over the finished frame (captures included) and never touches game state, so the state hash is the same with it shown or hidden. A release build does not reference this assembly.

Toggle it with [`DebugOverlay.ToggleKey`](DebugOverlay.md#m-togglekey) (F3) or the [`DebugOverlay.ToggleChord`](DebugOverlay.md#m-togglechord) on a gamepad (Back and Start together). Both can be changed with the environment variables `ONEGAME_OVERLAY_KEY` and `ONEGAME_OVERLAY_CHORD` (`none` turns one off), and `ONEGAME_OVERLAY=on` shows it from the first frame. The `overlay` control operation and the scenario step `overlay:` do the same.

## Properties

| Member | Description |
| --- | --- |
| [`AudioSource`](#m-audiosource) | The audio service whose device is reported; the newest one when null. |
| [`IsMasked`](#m-ismasked) | Whether the figures are masked now. |
| [`Lines`](#m-lines) | The lines of the panel as last built. |
| [`Masked`](#m-masked) | Shows `--` for every figure that depends on the machine or the moment (frame times, network rates, audio counters), so a capture is the same everywhere. |
| [`ToggleChord`](#m-togglechord) | The gamepad buttons that toggle the panel when all are held, or empty for none. |
| [`ToggleKey`](#m-togglekey) | The key that toggles the panel, or null for none. |
| [`Visible`](#m-visible) | Whether the panel is shown. |

### <a id="m-audiosource"></a>AudioSource

```csharp
public Func<AudioService?>? AudioSource { get; set; }
```

The audio service whose device is reported; the newest one when null.

**Type:** [`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)&lt;[`AudioService`](../../OneGame.Audio/OneGame.Audio/AudioService.md)?&gt;?

### <a id="m-ismasked"></a>IsMasked

```csharp
public bool IsMasked { get; }
```

Whether the figures are masked now.

**Type:** `bool`

### <a id="m-lines"></a>Lines

```csharp
public IReadOnlyList<string> Lines { get; }
```

The lines of the panel as last built.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

### <a id="m-masked"></a>Masked

```csharp
public bool? Masked { get; set; }
```

Shows `--` for every figure that depends on the machine or the moment (frame times, network rates, audio counters), so a capture is the same everywhere. Null, the default, masks in a headless run only.

**Type:** `bool?`

### <a id="m-togglechord"></a>ToggleChord

```csharp
public GamepadButton[] ToggleChord { get; set; }
```

The gamepad buttons that toggle the panel when all are held, or empty for none. Back and Start by default.

**Type:** [`GamepadButton`](../../OneGame.Platform/OneGame.Platform/GamepadButton.md)\[\]

### <a id="m-togglekey"></a>ToggleKey

```csharp
public Key? ToggleKey { get; set; }
```

The key that toggles the panel, or null for none. F3 by default.

**Type:** [`Key`](../../OneGame.Platform/OneGame.Platform/Key.md)?

### <a id="m-visible"></a>Visible

```csharp
public bool Visible { get; set; }
```

Whether the panel is shown.

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`Draw(OverlayCanvas)`](#m-draw-overlaycanvas) | Draws this frame's overlay. |
| [`For(GameInstance)`](#m-for-gameinstance) | Makes the overlay for a game, or returns the one it already has. |
| [`ParseChord(string)`](#m-parsechord-string) | Reads a button chord such as `Back+Start`; `none` gives an empty chord. |
| [`Refresh()`](#m-refresh) | Builds the lines now (the panel itself rebuilds four times a second) and returns them. |

### <a id="m-draw-overlaycanvas"></a>Draw(OverlayCanvas)

```csharp
public void Draw(OverlayCanvas canvas);
```

Draws this frame's overlay.

**Parameters**

- `canvas` ([`OverlayCanvas`](../../OneGame.Host/OneGame.Host/OverlayCanvas.md)): Where to draw; valid only during the call.

### <a id="m-for-gameinstance"></a>For(GameInstance)

```csharp
public static DebugOverlay For(GameInstance game);
```

Makes the overlay for a game, or returns the one it already has. It is hooked into the game's 2D stage and input.

**Parameters**

- `game` ([`GameInstance`](../../OneGame.Host/OneGame.Host/GameInstance.md)): The game.

**Returns:** [`DebugOverlay`](DebugOverlay.md) — The game's overlay.

### <a id="m-parsechord-string"></a>ParseChord(string)

```csharp
public static GamepadButton[] ParseChord(string text);
```

Reads a button chord such as `Back+Start`; `none` gives an empty chord.

**Parameters**

- `text` (`string`): Button names joined by `+`.

**Returns:** [`GamepadButton`](../../OneGame.Platform/OneGame.Platform/GamepadButton.md)\[\] — The buttons.

**Exceptions**

- [`FormatException`](https://learn.microsoft.com/dotnet/api/system.formatexception): A name is not a gamepad button.

### <a id="m-refresh"></a>Refresh()

```csharp
public IReadOnlyList<string> Refresh();
```

Builds the lines now (the panel itself rebuilds four times a second) and returns them.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>` — The panel's text.
