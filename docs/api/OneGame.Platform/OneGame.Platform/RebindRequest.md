[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# RebindRequest record

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`RebindRequest`](RebindRequest.md)&gt;

```csharp
public sealed record RebindRequest : IEquatable<RebindRequest>
```

A rebinding waiting for the player's next input.

## Constructors

| Member | Description |
| --- | --- |
| [`RebindRequest(string, int, int)`](#m-ctor-string-int-int) | A rebinding waiting for the player's next input. |

### <a id="m-ctor-string-int-int"></a>RebindRequest(string, int, int)

```csharp
public RebindRequest(string Action, int Slot, int Part);
```

A rebinding waiting for the player's next input.

**Parameters**

- `Action` (`string`): The action name.
- `Slot` (`int`): The slot being replaced (the binding count to add one).
- `Part` (`int`): Which source of a composite binding is being replaced.

## Properties

| Member | Description |
| --- | --- |
| [`Action`](#m-action) | The action name. |
| [`Part`](#m-part) | Which source of a composite binding is being replaced. |
| [`Slot`](#m-slot) | The slot being replaced (the binding count to add one). |

### <a id="m-action"></a>Action

```csharp
public string Action { get; init; }
```

The action name.

**Type:** `string`

### <a id="m-part"></a>Part

```csharp
public int Part { get; init; }
```

Which source of a composite binding is being replaced.

**Type:** `int`

### <a id="m-slot"></a>Slot

```csharp
public int Slot { get; init; }
```

The slot being replaced (the binding count to add one).

**Type:** `int`
