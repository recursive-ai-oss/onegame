[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam.Input

# SteamDigitalAction record

**Namespace:** `OneGame.Steam.Input`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SteamDigitalAction`](SteamDigitalAction.md)&gt;

```csharp
public sealed record SteamDigitalAction : IEquatable<SteamDigitalAction>
```

One digital action: a button that is down or up.

## Constructors

| Member | Description |
| --- | --- |
| [`SteamDigitalAction(string, string, GamepadButton?)`](#m-ctor-string-string-gamepadbutton) | One digital action: a button that is down or up. |

### <a id="m-ctor-string-string-gamepadbutton"></a>SteamDigitalAction(string, string, GamepadButton?)

```csharp
public SteamDigitalAction(string Name, string Title, GamepadButton? Button);
```

One digital action: a button that is down or up.

**Parameters**

- `Name` (`string`): The action's name; unique across all sets.
- `Title` (`string`): The text shown in Steam's controller configurator.
- `Button` ([`GamepadButton`](../../OneGame.Platform/OneGame.Platform/GamepadButton.md)?): The gamepad button the action also drives in the engine's input path, or null for none.

## Properties

| Member | Description |
| --- | --- |
| [`Button`](#m-button) | The gamepad button the action also drives in the engine's input path, or null for none. |
| [`Name`](#m-name) | The action's name; unique across all sets. |
| [`Title`](#m-title) | The text shown in Steam's controller configurator. |

### <a id="m-button"></a>Button

```csharp
public GamepadButton? Button { get; init; }
```

The gamepad button the action also drives in the engine's input path, or null for none.

**Type:** [`GamepadButton`](../../OneGame.Platform/OneGame.Platform/GamepadButton.md)?

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The action's name; unique across all sets.

**Type:** `string`

### <a id="m-title"></a>Title

```csharp
public string Title { get; init; }
```

The text shown in Steam's controller configurator.

**Type:** `string`
