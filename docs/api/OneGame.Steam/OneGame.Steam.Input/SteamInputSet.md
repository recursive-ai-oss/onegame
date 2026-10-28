[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam.Input

# SteamInputSet class

**Namespace:** `OneGame.Steam.Input`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public sealed class SteamInputSet
```

An action set: the actions that are live together on one kind of screen (a map, a trade window, a menu).

## Properties

| Member | Description |
| --- | --- |
| [`Digital`](#m-digital) | The digital actions in declaration order. |
| [`Name`](#m-name) | The set's name, as Steam knows it. |
| [`Screens`](#m-screens) | The game screens this set is active on (see [`SteamInputActions.SetForScreen()`](SteamInputActions.md#m-setforscreen-string)). |
| [`Sticks`](#m-sticks) | The two-axis actions in declaration order. |
| [`Title`](#m-title) | The set's title in Steam's configurator. |
| [`Triggers`](#m-triggers) | The one-axis actions in declaration order. |

### <a id="m-digital"></a>Digital

```csharp
public IReadOnlyList<SteamDigitalAction> Digital { get; }
```

The digital actions in declaration order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`SteamDigitalAction`](SteamDigitalAction.md)&gt;

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The set's name, as Steam knows it.

**Type:** `string`

### <a id="m-screens"></a>Screens

```csharp
public IReadOnlyList<string> Screens { get; }
```

The game screens this set is active on (see [`SteamInputActions.SetForScreen()`](SteamInputActions.md#m-setforscreen-string)).

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

### <a id="m-sticks"></a>Sticks

```csharp
public IReadOnlyList<SteamStickAction> Sticks { get; }
```

The two-axis actions in declaration order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`SteamStickAction`](SteamStickAction.md)&gt;

### <a id="m-title"></a>Title

```csharp
public string Title { get; }
```

The set's title in Steam's configurator.

**Type:** `string`

### <a id="m-triggers"></a>Triggers

```csharp
public IReadOnlyList<SteamTriggerAction> Triggers { get; }
```

The one-axis actions in declaration order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`SteamTriggerAction`](SteamTriggerAction.md)&gt;

## Methods

| Member | Description |
| --- | --- |
| [`Button(string, string, GamepadButton?)`](#m-button-string-string-gamepadbutton) | Adds a button action. |
| [`OnScreens(string[])`](#m-onscreens-string) | Makes this set the one active while the named game screens are open. |
| [`Stick(string, string, SteamStickMode, SteamStick?)`](#m-stick-string-string-steamstickmode-steamstick) | Adds a two-axis action (a stick, pad or gyro). |
| [`Trigger(string, string, GamepadAxis?)`](#m-trigger-string-string-gamepadaxis) | Adds a one-axis action (an analog trigger). |

### <a id="m-button-string-string-gamepadbutton"></a>Button(string, string, GamepadButton?)

```csharp
public SteamInputSet Button(string name, string title, GamepadButton? button = default);
```

Adds a button action.

**Parameters**

- `name` (`string`): Letters, digits and underscores, starting with a letter.
- `title` (`string`): Text for the configurator.
- `button` ([`GamepadButton`](../../OneGame.Platform/OneGame.Platform/GamepadButton.md)?): The gamepad button it also presses in the engine's input path (menu navigation, `OnInput`), or null.

**Returns:** [`SteamInputSet`](SteamInputSet.md) — This set.

### <a id="m-onscreens-string"></a>OnScreens(string\[\])

```csharp
public SteamInputSet OnScreens(params string[] screens);
```

Makes this set the one active while the named game screens are open.

**Parameters**

- `screens` (`string[]`): Screen names as the game's `DefineScreens` registers them.

**Returns:** [`SteamInputSet`](SteamInputSet.md) — This set.

### <a id="m-stick-string-string-steamstickmode-steamstick"></a>Stick(string, string, SteamStickMode, SteamStick?)

```csharp
public SteamInputSet Stick(string name, string title, SteamStickMode mode = 0, SteamStick? stick = default);
```

Adds a two-axis action (a stick, pad or gyro).

**Parameters**

- `name` (`string`): Letters, digits and underscores, starting with a letter.
- `title` (`string`): Text for the configurator.
- `mode` ([`SteamStickMode`](SteamStickMode.md)): How Steam interprets it.
- `stick` ([`SteamStick`](SteamStick.md)?): The virtual gamepad stick it also drives, or null.

**Returns:** [`SteamInputSet`](SteamInputSet.md) — This set.

### <a id="m-trigger-string-string-gamepadaxis"></a>Trigger(string, string, GamepadAxis?)

```csharp
public SteamInputSet Trigger(string name, string title, GamepadAxis? axis = default);
```

Adds a one-axis action (an analog trigger). Valve suggests a button action when only a threshold matters.

**Parameters**

- `name` (`string`): Letters, digits and underscores, starting with a letter.
- `title` (`string`): Text for the configurator.
- `axis` ([`GamepadAxis`](../../OneGame.Platform/OneGame.Platform/GamepadAxis.md)?): The trigger axis it also drives ([`GamepadAxis.LeftTrigger`](../../OneGame.Platform/OneGame.Platform/GamepadAxis.md#m-lefttrigger) or [`GamepadAxis.RightTrigger`](../../OneGame.Platform/OneGame.Platform/GamepadAxis.md#m-righttrigger)), or null.

**Returns:** [`SteamInputSet`](SteamInputSet.md) — This set.
