[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam.Input

# SteamInputBridge class

**Namespace:** `OneGame.Steam.Input`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class SteamInputBridge : IDisposable
```

Joins [`SteamInputService`](SteamInputService.md) to the engine's input path so game code reads one path whether Steam is on or off. Each Steam controller becomes a virtual gamepad on the platform layer: an action declared with a gamepad button target presses that button, a stick action moves that stick, a trigger action moves that trigger, and all of it arrives as ordinary gamepad events (menu navigation, `GameDefinition.OnInput`, the harness's input state). The bridge also switches action sets with the open screen and opens Steam's on-screen keyboard while a text input has focus and the player is on a controller. Call [`SteamInputBridge.Update()`](SteamInputBridge.md#m-update-string) once per frame, for example from `GameDefinition.OnFrame`.

## Constructors

| Member | Description |
| --- | --- |
| [`SteamInputBridge(SteamInputService, PlatformApp, UiTree, Func<float>?, InputGlyphs?)`](#m-ctor-steaminputservice-platformapp-uitree-func-float-inputglyphs) | Creates the bridge. |

### <a id="m-ctor-steaminputservice-platformapp-uitree-func-float-inputglyphs"></a>SteamInputBridge(SteamInputService, PlatformApp, UiTree, Func&lt;float&gt;?, InputGlyphs?)

```csharp
public SteamInputBridge(SteamInputService input, PlatformApp platform, UiTree ui, Func<float>? uiScale = null, InputGlyphs? glyphs = null);
```

Creates the bridge.

**Parameters**

- `input` ([`SteamInputService`](SteamInputService.md)): The Steam Input service.
- `platform` ([`PlatformApp`](../../OneGame.Platform/OneGame.Platform/PlatformApp.md)): The platform layer whose input queue receives the virtual gamepad's events.
- `ui` ([`UiTree`](../../OneGame.UI/OneGame.UI/UiTree.md)): The UI tree, watched for a focused text input.
- `uiScale` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<float>?`): The current UI scale, to turn a text input's box into window pixels; null for 1.
- `glyphs` ([`InputGlyphs`](../../OneGame.UI.Kit/OneGame.UI.Kit/InputGlyphs.md)?): The game's glyph table; when given the service keeps its gamepad glyphs on the player's real buttons and the keyboard also opens when the last used device was the gamepad.

## Methods

| Member | Description |
| --- | --- |
| [`Dispose()`](#m-dispose) | Detaches from the service and removes the virtual gamepads. |
| [`PadFor(int)`](#m-padfor-int) | The platform gamepad id that stands for a Steam controller, or 0 when it has none. |
| [`Update(string?)`](#m-update-string) | Runs one frame: reads Steam Input, switches the action set for the open screen and shows or hides the on-screen keyboard. |

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Detaches from the service and removes the virtual gamepads.

### <a id="m-padfor-int"></a>PadFor(int)

```csharp
public int PadFor(int controllerIndex);
```

The platform gamepad id that stands for a Steam controller, or 0 when it has none.

**Parameters**

- `controllerIndex` (`int`): The controller's index.

**Returns:** `int` — The gamepad id.

### <a id="m-update-string"></a>Update(string?)

```csharp
public void Update(string? screen);
```

Runs one frame: reads Steam Input, switches the action set for the open screen and shows or hides the on-screen keyboard.

**Parameters**

- `screen` (`string?`): The open screen's name (`GameInstance.CurrentScreen`).
