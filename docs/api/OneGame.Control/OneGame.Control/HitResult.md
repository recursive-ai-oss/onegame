[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# HitResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`HitResult`](HitResult.md)&gt;

```csharp
public sealed record HitResult : IEquatable<HitResult>
```

Result of `ui_hit`.

## Constructors

| Member | Description |
| --- | --- |
| [`HitResult(string?, string?)`](#m-ctor-string-string) | Result of `ui_hit`. |

### <a id="m-ctor-string-string"></a>HitResult(string?, string?)

```csharp
public HitResult(string? Element, string? Id);
```

Result of `ui_hit`.

**Parameters**

- `Element` (`string?`)
- `Id` (`string?`)

## Properties

| Member | Description |
| --- | --- |
| [`Element`](#m-element) |  |
| [`Id`](#m-id) |  |

### <a id="m-element"></a>Element

```csharp
public string? Element { get; init; }
```

**Type:** `string?`

### <a id="m-id"></a>Id

```csharp
public string? Id { get; init; }
```

**Type:** `string?`
