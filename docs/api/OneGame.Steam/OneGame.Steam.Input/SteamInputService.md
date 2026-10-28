[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam.Input

# SteamInputService class

**Namespace:** `OneGame.Steam.Input`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class SteamInputService : IDisposable
```

Steam Input for a game: the declared action sets, the active set per screen, digital and analog action state, controller connect and disconnect, and the glyphs of the buttons the player actually has. Everything is polled from [`SteamInputService.Update()`](SteamInputService.md#m-update), on the game thread, like the rest of the store binding. When Steam is not running the service is disabled and does nothing, so game code never branches on it; the platform gamepad path is untouched.

## Properties

| Member | Description |
| --- | --- |
| [`Actions`](#m-actions) | The declarations the service was started with. |
| [`Active`](#m-active) | The service most recently started in this process, which the harness reads. |
| [`ActiveSet`](#m-activeset) | The set the game last activated (all controllers share it), or null before the first activation. |
| [`Controllers`](#m-controllers) | The connected controllers, in index order. |
| [`Driver`](#m-driver) | The driver for the harness's stand-in, or null when real Steam is behind the service. |
| [`IsEnabled`](#m-isenabled) | True when Steam Input is running. |
| [`IsHandheld`](#m-ishandheld) | True when the game runs on a Steam Deck or another handheld, where the on-screen keyboard is the player's only keyboard. |
| [`IsKeyboardVisible`](#m-iskeyboardvisible) | True while the keyboard has been shown and not dismissed by the game. |
| [`UnavailableReason`](#m-unavailablereason) | Why Steam Input is not running, or null. |

### <a id="m-actions"></a>Actions

```csharp
public SteamInputActions Actions { get; }
```

The declarations the service was started with.

**Type:** [`SteamInputActions`](SteamInputActions.md)

### <a id="m-active"></a>Active

```csharp
public static SteamInputService? Active { get; }
```

The service most recently started in this process, which the harness reads. Set by `Start`; cleared when that service is disposed.

**Type:** [`SteamInputService`](SteamInputService.md)?

### <a id="m-activeset"></a>ActiveSet

```csharp
public string? ActiveSet { get; }
```

The set the game last activated (all controllers share it), or null before the first activation.

**Type:** `string?`

### <a id="m-controllers"></a>Controllers

```csharp
public IReadOnlyList<SteamController> Controllers { get; }
```

The connected controllers, in index order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`SteamController`](SteamController.md)&gt;

### <a id="m-driver"></a>Driver

```csharp
public ISteamInputDriver? Driver { get; }
```

The driver for the harness's stand-in, or null when real Steam is behind the service.

**Type:** [`ISteamInputDriver`](ISteamInputDriver.md)?

### <a id="m-isenabled"></a>IsEnabled

```csharp
public bool IsEnabled { get; }
```

True when Steam Input is running.

**Type:** `bool`

### <a id="m-ishandheld"></a>IsHandheld

```csharp
public bool IsHandheld { get; }
```

True when the game runs on a Steam Deck or another handheld, where the on-screen keyboard is the player's only keyboard.

**Type:** `bool`

### <a id="m-iskeyboardvisible"></a>IsKeyboardVisible

```csharp
public bool IsKeyboardVisible { get; }
```

True while the keyboard has been shown and not dismissed by the game.

**Type:** `bool`

### <a id="m-unavailablereason"></a>UnavailableReason

```csharp
public string? UnavailableReason { get; }
```

Why Steam Input is not running, or null.

**Type:** `string?`

## Events

| Member | Description |
| --- | --- |
| [`ActionChanged`](#m-actionchanged) | Raised from [`SteamInputService.Update()`](SteamInputService.md#m-update) when an action's value changes (a button goes down or up, an axis moves). |
| [`ControllerConnected`](#m-controllerconnected) | Raised from [`SteamInputService.Update()`](SteamInputService.md#m-update) when a controller appears. |
| [`ControllerDisconnected`](#m-controllerdisconnected) | Raised from [`SteamInputService.Update()`](SteamInputService.md#m-update) when a controller goes away. |

### <a id="m-actionchanged"></a>ActionChanged

```csharp
public event Action<SteamActionChange>? ActionChanged;
```

Raised from [`SteamInputService.Update()`](SteamInputService.md#m-update) when an action's value changes (a button goes down or up, an axis moves).

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`SteamActionChange`](SteamActionChange.md)&gt;?

### <a id="m-controllerconnected"></a>ControllerConnected

```csharp
public event Action<SteamController>? ControllerConnected;
```

Raised from [`SteamInputService.Update()`](SteamInputService.md#m-update) when a controller appears.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`SteamController`](SteamController.md)&gt;?

### <a id="m-controllerdisconnected"></a>ControllerDisconnected

```csharp
public event Action<SteamController>? ControllerDisconnected;
```

Raised from [`SteamInputService.Update()`](SteamInputService.md#m-update) when a controller goes away.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`SteamController`](SteamController.md)&gt;?

## Methods

| Member | Description |
| --- | --- |
| [`ActivateForScreen(string?)`](#m-activateforscreen-string) | Activates the set declared for a screen ([`SteamInputSet.OnScreens()`](SteamInputSet.md#m-onscreens-string)), or the default set. |
| [`ActivateSet(string)`](#m-activateset-string) | Makes `name` the active set on every controller (and on controllers that connect later). |
| [`BindGlyphs(InputGlyphs)`](#m-bindglyphs-inputglyphs) | Keeps an [`InputGlyphs`](../../OneGame.UI.Kit/OneGame.UI.Kit/InputGlyphs.md) up to date: each declared action's gamepad glyph follows the buttons the player's controller and bindings really use, so `UiKit.InputGlyph` hints show real buttons. |
| [`Describe()`](#m-describe) | Everything the harness shows about Steam Input. |
| [`DismissKeyboard()`](#m-dismisskeyboard) | Closes the on-screen keyboard. |
| [`Dispose()`](#m-dispose) | Stops Steam Input. |
| [`GetAnalog(string, out float, out float, int)`](#m-getanalog-string-float-float-int) | The value of an analog action: the controller with the largest deflection when none is named. |
| [`GetGlyphName(string, int)`](#m-getglyphname-string-int) | The kit's gamepad glyph name (`a`, `lb`, `dpad-up`...) for the first button an action is bound to, whatever pad the player has: the origin is translated to the Xbox One layout first. |
| [`GetGlyphPath(string, SteamGlyphSize, int)`](#m-getglyphpath-string-steamglyphsize-int) | The path of Steam's own PNG for the first button the action is bound to, in the player's controller style, the way `GetGlyphForActionOrigin` hands one out. |
| [`GetOrigins(string, int)`](#m-getorigins-string-int) | Steam's origin numbers for the buttons an action is bound to on a controller, in the action's own set. |
| [`IsDown(string, int)`](#m-isdown-string-int) | Whether a digital action is down now on any controller, or on the numbered one. |
| [`ShowKeyboard(int, int, int, int)`](#m-showkeyboard-int-int-int-int) | Opens Steam's on-screen keyboard over the game, positioned so it does not cover a text field. |
| [`Start(IStorePlatform, SteamInputActions, string?)`](#m-start-istoreplatform-steaminputactions-string) | Starts Steam Input on a connected Steam store. |
| [`StartFake(SteamInputActions)`](#m-startfake-steaminputactions) | Starts the harness stand-in, which has no Steam behind it and is driven through [`SteamInputService.Driver`](SteamInputService.md#m-driver). |
| [`Update()`](#m-update) | Reads Steam's latest data and raises connect, disconnect and change events. |
| [`WasPressed(string, int)`](#m-waspressed-string-int) | Whether a digital action went down during the latest [`SteamInputService.Update()`](SteamInputService.md#m-update). |

### <a id="m-activateforscreen-string"></a>ActivateForScreen(string?)

```csharp
public StoreResult ActivateForScreen(string? screen);
```

Activates the set declared for a screen ([`SteamInputSet.OnScreens()`](SteamInputSet.md#m-onscreens-string)), or the default set.

**Parameters**

- `screen` (`string?`): The open screen's name, or null.

**Returns:** [`StoreResult`](../OneGame.Stores/StoreResult.md) — As [`SteamInputService.ActivateSet()`](SteamInputService.md#m-activateset-string).

### <a id="m-activateset-string"></a>ActivateSet(string)

```csharp
public StoreResult ActivateSet(string name);
```

Makes `name` the active set on every controller (and on controllers that connect later). Cheap; safe to call every frame.

**Parameters**

- `name` (`string`): A declared set's name.

**Returns:** [`StoreResult`](../OneGame.Stores/StoreResult.md) — [`StoreResult.NotFound`](../OneGame.Stores/StoreResult.md#m-notfound) for an undeclared set, [`StoreResult.Unavailable`](../OneGame.Stores/StoreResult.md#m-unavailable) when Steam Input is not running, else Ok.

### <a id="m-bindglyphs-inputglyphs"></a>BindGlyphs(InputGlyphs)

```csharp
public void BindGlyphs(InputGlyphs glyphs);
```

Keeps an [`InputGlyphs`](../../OneGame.UI.Kit/OneGame.UI.Kit/InputGlyphs.md) up to date: each declared action's gamepad glyph follows the buttons the player's controller and bindings really use, so `UiKit.InputGlyph` hints show real buttons. Keyboard and mouse bindings are untouched.

**Parameters**

- `glyphs` ([`InputGlyphs`](../../OneGame.UI.Kit/OneGame.UI.Kit/InputGlyphs.md)): The game's glyph table.

### <a id="m-describe"></a>Describe()

```csharp
public SteamInputReport Describe();
```

Everything the harness shows about Steam Input.

**Returns:** [`SteamInputReport`](SteamInputReport.md) — A snapshot.

### <a id="m-dismisskeyboard"></a>DismissKeyboard()

```csharp
public void DismissKeyboard();
```

Closes the on-screen keyboard.

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Stops Steam Input.

### <a id="m-getanalog-string-float-float-int"></a>GetAnalog(string, out float, out float, int)

```csharp
public void GetAnalog(string action, out float x, out float y, int controller = 0);
```

The value of an analog action: the controller with the largest deflection when none is named.

**Parameters**

- `action` (`string`): The action's name.
- `x` (`float`): The first axis (positive right).
- `y` (`float`): The second axis (positive up); 0 for a one-axis action.
- `controller` (`int`): A controller index, or 0 for any.

### <a id="m-getglyphname-string-int"></a>GetGlyphName(string, int)

```csharp
public string? GetGlyphName(string action, int controller = 0);
```

The kit's gamepad glyph name (`a`, `lb`, `dpad-up`...) for the first button an action is bound to, whatever pad the player has: the origin is translated to the Xbox One layout first.

**Parameters**

- `action` (`string`): The action's name.
- `controller` (`int`): A controller index, or 0 for the first.

**Returns:** `string?` — The glyph name, or null when unbound or when no kit glyph stands for it.

### <a id="m-getglyphpath-string-steamglyphsize-int"></a>GetGlyphPath(string, SteamGlyphSize, int)

```csharp
public string? GetGlyphPath(string action, SteamGlyphSize size = 1, int controller = 0);
```

The path of Steam's own PNG for the first button the action is bound to, in the player's controller style, the way `GetGlyphForActionOrigin` hands one out.

**Parameters**

- `action` (`string`): The action's name.
- `size` ([`SteamGlyphSize`](SteamGlyphSize.md)): The picture size.
- `controller` (`int`): A controller index, or 0 for the first.

**Returns:** `string?` — A local file path, or null when the action has no button.

### <a id="m-getorigins-string-int"></a>GetOrigins(string, int)

```csharp
public IReadOnlyList<int> GetOrigins(string action, int controller = 0);
```

Steam's origin numbers for the buttons an action is bound to on a controller, in the action's own set.

**Parameters**

- `action` (`string`): The action's name.
- `controller` (`int`): A controller index, or 0 for the first.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<int>` — Origin numbers (empty when unbound, unknown or Steam Input is off).

### <a id="m-isdown-string-int"></a>IsDown(string, int)

```csharp
public bool IsDown(string action, int controller = 0);
```

Whether a digital action is down now on any controller, or on the numbered one.

**Parameters**

- `action` (`string`): The action's name.
- `controller` (`int`): A controller index, or 0 for any.

**Returns:** `bool` — True while down.

### <a id="m-showkeyboard-int-int-int-int"></a>ShowKeyboard(int, int, int, int)

```csharp
public StoreResult ShowKeyboard(int x, int y, int width, int height);
```

Opens Steam's on-screen keyboard over the game, positioned so it does not cover a text field. Steam sends the typed keys to the game as ordinary keyboard input, so they arrive through the normal text-input path.

**Parameters**

- `x` (`int`): Left of the field in window pixels.
- `y` (`int`): Top of the field.
- `width` (`int`): Width of the field.
- `height` (`int`): Height of the field.

**Returns:** [`StoreResult`](../OneGame.Stores/StoreResult.md) — Ok when shown; Unavailable without Steam Input; Failed when Steam refused (no Big Picture or Deck keyboard available).

### <a id="m-start-istoreplatform-steaminputactions-string"></a>Start(IStorePlatform, SteamInputActions, string?)

```csharp
public static SteamInputService Start(IStorePlatform store, SteamInputActions actions, string? manifestPath = null);
```

Starts Steam Input on a connected Steam store. Never throws for a missing Steam: the result is a disabled service with [`SteamInputService.UnavailableReason`](SteamInputService.md#m-unavailablereason).

**Parameters**

- `store` ([`IStorePlatform`](../OneGame.Stores/IStorePlatform.md)): The platform from [`SteamPlatform.Start()`](../OneGame.Steam/SteamPlatform.md#m-start-steamoptions).
- `actions` ([`SteamInputActions`](SteamInputActions.md)): The game's action sets.
- `manifestPath` (`string?`): Absolute path to an action manifest file shipped with the game, for games that bundle Steam Input configurations; or null to use the configuration uploaded to Steam.

**Returns:** [`SteamInputService`](SteamInputService.md) — The service; always non-null.

### <a id="m-startfake-steaminputactions"></a>StartFake(SteamInputActions)

```csharp
public static SteamInputService StartFake(SteamInputActions actions);
```

Starts the harness stand-in, which has no Steam behind it and is driven through [`SteamInputService.Driver`](SteamInputService.md#m-driver).

**Parameters**

- `actions` ([`SteamInputActions`](SteamInputActions.md)): The game's action sets.

**Returns:** [`SteamInputService`](SteamInputService.md) — The service.

### <a id="m-update"></a>Update()

```csharp
public void Update();
```

Reads Steam's latest data and raises connect, disconnect and change events. Call once per frame after `IStorePlatform.Pump`.

### <a id="m-waspressed-string-int"></a>WasPressed(string, int)

```csharp
public bool WasPressed(string action, int controller = 0);
```

Whether a digital action went down during the latest [`SteamInputService.Update()`](SteamInputService.md#m-update).

**Parameters**

- `action` (`string`): The action's name.
- `controller` (`int`): A controller index, or 0 for any.

**Returns:** `bool` — True on the frame it went down.
