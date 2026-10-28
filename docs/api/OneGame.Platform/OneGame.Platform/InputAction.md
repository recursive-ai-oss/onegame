[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# InputAction class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public sealed class InputAction
```

One declared action and its state for the current frame.

## Properties

| Member | Description |
| --- | --- |
| [`Bindings`](#m-bindings) | The current bindings, in slot order. |
| [`Context`](#m-context) | The name of the context that declares the action. |
| [`DeadZone`](#m-deadzone) | Stick and axis values closer to the centre than this read as zero; the rest is rescaled to start from zero. |
| [`Defaults`](#m-defaults) | The bindings the game declared. |
| [`Held`](#m-held) | Down this frame: a button is down, or an analog value is past [`InputAction.Threshold`](InputAction.md#m-threshold). |
| [`IsActive`](#m-isactive) | Whether the action's context is active this frame. |
| [`IsDefault`](#m-isdefault) | True when the bindings are the declared ones. |
| [`Kind`](#m-kind) | Button, axis or vector. |
| [`Name`](#m-name) | The action's name, unique in the map. |
| [`Pressed`](#m-pressed) | Went down since the last frame (a press shorter than a frame still counts). |
| [`Released`](#m-released) | Went up since the last frame. |
| [`Threshold`](#m-threshold) | How far an analog source has to go for [`InputAction.Held`](InputAction.md#m-held) (default 0.5). |
| [`Title`](#m-title) | The text shown in a rebinding list; the name until [`ActionContext.Tune()`](ActionContext.md#m-tune-string-float-float-string) changes it. |
| [`Value`](#m-value) | A button's strength 0..1, an axis's value, or a vector's x. |
| [`Vector`](#m-vector) | The vector value. |
| [`X`](#m-x) | The value, or a vector's x. |
| [`Y`](#m-y) | A vector's y (down is positive); 0 for other kinds. |

### <a id="m-bindings"></a>Bindings

```csharp
public IReadOnlyList<ActionBinding> Bindings { get; }
```

The current bindings, in slot order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ActionBinding`](ActionBinding.md)&gt;

### <a id="m-context"></a>Context

```csharp
public string Context { get; }
```

The name of the context that declares the action.

**Type:** `string`

### <a id="m-deadzone"></a>DeadZone

```csharp
public float DeadZone { get; }
```

Stick and axis values closer to the centre than this read as zero; the rest is rescaled to start from zero. Default 0.15; buttons ignore it.

**Type:** `float`

### <a id="m-defaults"></a>Defaults

```csharp
public IReadOnlyList<ActionBinding> Defaults { get; }
```

The bindings the game declared.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ActionBinding`](ActionBinding.md)&gt;

### <a id="m-held"></a>Held

```csharp
public bool Held { get; }
```

Down this frame: a button is down, or an analog value is past [`InputAction.Threshold`](InputAction.md#m-threshold).

**Type:** `bool`

### <a id="m-isactive"></a>IsActive

```csharp
public bool IsActive { get; }
```

Whether the action's context is active this frame.

**Type:** `bool`

### <a id="m-isdefault"></a>IsDefault

```csharp
public bool IsDefault { get; }
```

True when the bindings are the declared ones.

**Type:** `bool`

### <a id="m-kind"></a>Kind

```csharp
public ActionKind Kind { get; }
```

Button, axis or vector.

**Type:** [`ActionKind`](ActionKind.md)

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The action's name, unique in the map.

**Type:** `string`

### <a id="m-pressed"></a>Pressed

```csharp
public bool Pressed { get; }
```

Went down since the last frame (a press shorter than a frame still counts).

**Type:** `bool`

### <a id="m-released"></a>Released

```csharp
public bool Released { get; }
```

Went up since the last frame.

**Type:** `bool`

### <a id="m-threshold"></a>Threshold

```csharp
public float Threshold { get; }
```

How far an analog source has to go for [`InputAction.Held`](InputAction.md#m-held) (default 0.5).

**Type:** `float`

### <a id="m-title"></a>Title

```csharp
public string Title { get; }
```

The text shown in a rebinding list; the name until [`ActionContext.Tune()`](ActionContext.md#m-tune-string-float-float-string) changes it.

**Type:** `string`

### <a id="m-value"></a>Value

```csharp
public float Value { get; }
```

A button's strength 0..1, an axis's value, or a vector's x.

**Type:** `float`

### <a id="m-vector"></a>Vector

```csharp
public ActionVector Vector { get; }
```

The vector value.

**Type:** [`ActionVector`](ActionVector.md)

### <a id="m-x"></a>X

```csharp
public float X { get; }
```

The value, or a vector's x.

**Type:** `float`

### <a id="m-y"></a>Y

```csharp
public float Y { get; }
```

A vector's y (down is positive); 0 for other kinds.

**Type:** `float`
