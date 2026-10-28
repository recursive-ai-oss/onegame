[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam.Input

# SteamTriggerAction record

**Namespace:** `OneGame.Steam.Input`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SteamTriggerAction`](SteamTriggerAction.md)&gt;

```csharp
public sealed record SteamTriggerAction : IEquatable<SteamTriggerAction>
```

One analog action with a single axis, such as a trigger.

## Constructors

| Member | Description |
| --- | --- |
| [`SteamTriggerAction(string, string, GamepadAxis?)`](#m-ctor-string-string-gamepadaxis) | One analog action with a single axis, such as a trigger. |

### <a id="m-ctor-string-string-gamepadaxis"></a>SteamTriggerAction(string, string, GamepadAxis?)

```csharp
public SteamTriggerAction(string Name, string Title, GamepadAxis? Axis);
```

One analog action with a single axis, such as a trigger.

**Parameters**

- `Name` (`string`): The action's name; unique across all sets.
- `Title` (`string`): The text shown in Steam's controller configurator.
- `Axis` ([`GamepadAxis`](../../OneGame.Platform/OneGame.Platform/GamepadAxis.md)?): The trigger axis of the virtual gamepad the action also drives, or null for none.

## Properties

| Member | Description |
| --- | --- |
| [`Axis`](#m-axis) | The trigger axis of the virtual gamepad the action also drives, or null for none. |
| [`Name`](#m-name) | The action's name; unique across all sets. |
| [`Title`](#m-title) | The text shown in Steam's controller configurator. |

### <a id="m-axis"></a>Axis

```csharp
public GamepadAxis? Axis { get; init; }
```

The trigger axis of the virtual gamepad the action also drives, or null for none.

**Type:** [`GamepadAxis`](../../OneGame.Platform/OneGame.Platform/GamepadAxis.md)?

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
