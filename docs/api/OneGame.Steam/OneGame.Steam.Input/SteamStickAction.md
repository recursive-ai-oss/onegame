[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam.Input

# SteamStickAction record

**Namespace:** `OneGame.Steam.Input`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SteamStickAction`](SteamStickAction.md)&gt;

```csharp
public sealed record SteamStickAction : IEquatable<SteamStickAction>
```

One analog action with two axes.

## Constructors

| Member | Description |
| --- | --- |
| [`SteamStickAction(string, string, SteamStickMode, SteamStick?)`](#m-ctor-string-string-steamstickmode-steamstick) | One analog action with two axes. |

### <a id="m-ctor-string-string-steamstickmode-steamstick"></a>SteamStickAction(string, string, SteamStickMode, SteamStick?)

```csharp
public SteamStickAction(string Name, string Title, SteamStickMode Mode, SteamStick? Stick);
```

One analog action with two axes.

**Parameters**

- `Name` (`string`): The action's name; unique across all sets.
- `Title` (`string`): The text shown in Steam's controller configurator.
- `Mode` ([`SteamStickMode`](SteamStickMode.md)): How Steam interprets the axes.
- `Stick` ([`SteamStick`](SteamStick.md)?): The stick of the virtual gamepad the action also drives, or null for none.

## Properties

| Member | Description |
| --- | --- |
| [`Mode`](#m-mode) | How Steam interprets the axes. |
| [`Name`](#m-name) | The action's name; unique across all sets. |
| [`Stick`](#m-stick) | The stick of the virtual gamepad the action also drives, or null for none. |
| [`Title`](#m-title) | The text shown in Steam's controller configurator. |

### <a id="m-mode"></a>Mode

```csharp
public SteamStickMode Mode { get; init; }
```

How Steam interprets the axes.

**Type:** [`SteamStickMode`](SteamStickMode.md)

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The action's name; unique across all sets.

**Type:** `string`

### <a id="m-stick"></a>Stick

```csharp
public SteamStick? Stick { get; init; }
```

The stick of the virtual gamepad the action also drives, or null for none.

**Type:** [`SteamStick`](SteamStick.md)?

### <a id="m-title"></a>Title

```csharp
public string Title { get; init; }
```

The text shown in Steam's controller configurator.

**Type:** `string`
