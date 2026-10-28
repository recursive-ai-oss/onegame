[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# InvokeResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`InvokeResult`](InvokeResult.md)&gt;

```csharp
public sealed record InvokeResult : IEquatable<InvokeResult>
```

Result of `ui_invoke`: the element's default action run without pointer events.

## Constructors

| Member | Description |
| --- | --- |
| [`InvokeResult(string, bool, long)`](#m-ctor-string-bool-long) | Result of `ui_invoke`: the element's default action run without pointer events. |

### <a id="m-ctor-string-bool-long"></a>InvokeResult(string, bool, long)

```csharp
public InvokeResult(string Target, bool Invoked, long Tick);
```

Result of `ui_invoke`: the element's default action run without pointer events.

**Parameters**

- `Target` (`string`): The element, as its id or description.
- `Invoked` (`bool`): True when the element had an action to run.
- `Tick` (`long`): The world's tick.

## Properties

| Member | Description |
| --- | --- |
| [`Invoked`](#m-invoked) | True when the element had an action to run. |
| [`Target`](#m-target) | The element, as its id or description. |
| [`Tick`](#m-tick) | The world's tick. |

### <a id="m-invoked"></a>Invoked

```csharp
public bool Invoked { get; init; }
```

True when the element had an action to run.

**Type:** `bool`

### <a id="m-target"></a>Target

```csharp
public string Target { get; init; }
```

The element, as its id or description.

**Type:** `string`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The world's tick.

**Type:** `long`
