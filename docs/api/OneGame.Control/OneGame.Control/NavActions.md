[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# NavActions static class

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)

```csharp
public static class NavActions
```

The names the `input_nav` operation (and the scenario `nav` step) accepts for a virtual gamepad button: the menu actions (`activate`, `cancel`...) and every [`GamepadButton`](../../OneGame.Platform/OneGame.Platform/GamepadButton.md) by name. Case, `_`, `-` and spaces are ignored.

## Properties

| Member | Description |
| --- | --- |
| [`Names`](#m-names) | The names listed in the operation's schema, in a stable order. |

### <a id="m-names"></a>Names

```csharp
public static IReadOnlyList<string> Names { get; }
```

The names listed in the operation's schema, in a stable order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

## Methods

| Member | Description |
| --- | --- |
| [`TryParse(string, out GamepadButton)`](#m-tryparse-string-gamepadbutton) | Finds the button a name stands for. |

### <a id="m-tryparse-string-gamepadbutton"></a>TryParse(string, out GamepadButton)

```csharp
public static bool TryParse(string name, out GamepadButton button);
```

Finds the button a name stands for.

**Parameters**

- `name` (`string`): An action or button name.
- `button` ([`GamepadButton`](../../OneGame.Platform/OneGame.Platform/GamepadButton.md)): The button.

**Returns:** `bool` — False when the name is unknown.
