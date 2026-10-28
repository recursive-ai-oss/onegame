[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# PatchResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PatchResult`](PatchResult.md)&gt;

```csharp
public sealed record PatchResult : IEquatable<PatchResult>
```

Result of `state_patch`.

## Constructors

| Member | Description |
| --- | --- |
| [`PatchResult(string, string, string, string)`](#m-ctor-string-string-string-string) | Result of `state_patch`. |

### <a id="m-ctor-string-string-string-string"></a>PatchResult(string, string, string, string)

```csharp
public PatchResult(string Entity, string Property, string Value, string Hash);
```

Result of `state_patch`.

**Parameters**

- `Entity` (`string`)
- `Property` (`string`)
- `Value` (`string`)
- `Hash` (`string`)

## Properties

| Member | Description |
| --- | --- |
| [`Entity`](#m-entity) |  |
| [`Hash`](#m-hash) |  |
| [`Property`](#m-property) |  |
| [`Value`](#m-value) |  |

### <a id="m-entity"></a>Entity

```csharp
public string Entity { get; init; }
```

**Type:** `string`

### <a id="m-hash"></a>Hash

```csharp
public string Hash { get; init; }
```

**Type:** `string`

### <a id="m-property"></a>Property

```csharp
public string Property { get; init; }
```

**Type:** `string`

### <a id="m-value"></a>Value

```csharp
public string Value { get; init; }
```

**Type:** `string`
