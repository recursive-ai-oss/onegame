[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# ActionContext class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public sealed class ActionContext
```

Declares the actions of one context. Get one from [`ActionMap.Context()`](ActionMap.md#m-context-string-action-actioncontext).

## Properties

| Member | Description |
| --- | --- |
| [`Name`](#m-name) | The context's name. |
| [`Screens`](#m-screens) | The screens that switch this context on (see [`ActionMap.ActivateForScreen()`](ActionMap.md#m-activateforscreen-string)); empty means it is on whenever screens are followed. |

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The context's name.

**Type:** `string`

### <a id="m-screens"></a>Screens

```csharp
public IReadOnlyList<string> Screens { get; }
```

The screens that switch this context on (see [`ActionMap.ActivateForScreen()`](ActionMap.md#m-activateforscreen-string)); empty means it is on whenever screens are followed.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

## Methods

| Member | Description |
| --- | --- |
| [`Axis(string, ActionBinding[])`](#m-axis-string-actionbinding) | Declares a 1D axis from single sources (a pad axis, the wheel) or pairs (`Bind.Pair(Key.A, Key.D)`). |
| [`Button(string, ActionBinding[])`](#m-button-string-actionbinding) | Declares a button. |
| [`OnScreens(string[])`](#m-onscreens-string) | Makes this context the active one while the named game screens are open. |
| [`Tune(string, float?, float?, string?)`](#m-tune-string-float-float-string) | Changes an action's tuning or its list title. |
| [`Vector(string, ActionBinding[])`](#m-vector-string-actionbinding) | Declares a 2D vector from quads ([`Bind.Wasd`](Bind.md#m-wasd)) or sticks ([`Bind.LeftStick`](Bind.md#m-leftstick)). |

### <a id="m-axis-string-actionbinding"></a>Axis(string, ActionBinding\[\])

```csharp
public ActionContext Axis(string name, params ActionBinding[] bindings);
```

Declares a 1D axis from single sources (a pad axis, the wheel) or pairs (`Bind.Pair(Key.A, Key.D)`).

**Parameters**

- `name` (`string`): The action's name.
- `bindings` ([`ActionBinding`](ActionBinding.md)\[\]): The bindings.

**Returns:** [`ActionContext`](ActionContext.md) — This context.

### <a id="m-button-string-actionbinding"></a>Button(string, ActionBinding\[\])

```csharp
public ActionContext Button(string name, params ActionBinding[] bindings);
```

Declares a button. Several bindings can drive it; any of them presses it.

**Parameters**

- `name` (`string`): Letters, digits and underscores, starting with a letter (Steam Input uses the same names).
- `bindings` ([`ActionBinding`](ActionBinding.md)\[\]): Single sources: `Bind.Key(Key.Space)`, `Bind.Pad(GamepadButton.South)`, `Bind.Mouse(MouseButton.Left)`…

**Returns:** [`ActionContext`](ActionContext.md) — This context.

### <a id="m-onscreens-string"></a>OnScreens(string\[\])

```csharp
public ActionContext OnScreens(params string[] screens);
```

Makes this context the active one while the named game screens are open.

**Parameters**

- `screens` (`string[]`): Screen names as `DefineScreens` registers them.

**Returns:** [`ActionContext`](ActionContext.md) — This context.

### <a id="m-tune-string-float-float-string"></a>Tune(string, float?, float?, string?)

```csharp
public ActionContext Tune(string name, float? deadZone = default, float? threshold = default, string? title = null);
```

Changes an action's tuning or its list title.

**Parameters**

- `name` (`string`): An action declared in this context.
- `deadZone` (`float?`): New dead zone 0..0.95, or null to keep.
- `threshold` (`float?`): New hold threshold 0.05..1, or null to keep.
- `title` (`string?`): The text a rebinding list shows, or null to keep.

**Returns:** [`ActionContext`](ActionContext.md) — This context.

### <a id="m-vector-string-actionbinding"></a>Vector(string, ActionBinding\[\])

```csharp
public ActionContext Vector(string name, params ActionBinding[] bindings);
```

Declares a 2D vector from quads ([`Bind.Wasd`](Bind.md#m-wasd)) or sticks ([`Bind.LeftStick`](Bind.md#m-leftstick)).

**Parameters**

- `name` (`string`): The action's name.
- `bindings` ([`ActionBinding`](ActionBinding.md)\[\]): The bindings.

**Returns:** [`ActionContext`](ActionContext.md) — This context.
