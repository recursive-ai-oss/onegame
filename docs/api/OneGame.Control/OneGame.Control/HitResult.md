[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# HitResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`HitResult`](HitResult.md)&gt;

```csharp
public sealed record HitResult : IEquatable<HitResult>
```

Result of `ui_hit`: the element a pointer event at a position would reach.

## Constructors

| Member | Description |
| --- | --- |
| [`HitResult(string?, string?)`](#m-ctor-string-string) | Result of `ui_hit`: the element a pointer event at a position would reach. |

### <a id="m-ctor-string-string"></a>HitResult(string?, string?)

```csharp
public HitResult(string? Element, string? Id);
```

Result of `ui_hit`: the element a pointer event at a position would reach.

**Parameters**

- `Element` (`string?`): The element's description, or null when nothing is there.
- `Id` (`string?`): Its id, or null.

## Properties

| Member | Description |
| --- | --- |
| [`Element`](#m-element) | The element's description, or null when nothing is there. |
| [`Id`](#m-id) | Its id, or null. |

### <a id="m-element"></a>Element

```csharp
public string? Element { get; init; }
```

The element's description, or null when nothing is there.

**Type:** `string?`

### <a id="m-id"></a>Id

```csharp
public string? Id { get; init; }
```

Its id, or null.

**Type:** `string?`
