[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# PatchResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PatchResult`](PatchResult.md)&gt;

```csharp
public sealed record PatchResult : IEquatable<PatchResult>
```

Result of `state_patch`: a synced property written directly, bypassing commands (for building a situation in a test).

## Constructors

| Member | Description |
| --- | --- |
| [`PatchResult(string, string, string, string)`](#m-ctor-string-string-string-string) | Result of `state_patch`: a synced property written directly, bypassing commands (for building a situation in a test). |

### <a id="m-ctor-string-string-string-string"></a>PatchResult(string, string, string, string)

```csharp
public PatchResult(string Entity, string Property, string Value, string Hash);
```

Result of `state_patch`: a synced property written directly, bypassing commands (for building a situation in a test).

**Parameters**

- `Entity` (`string`): The entity patched, as `Type#id` (or `Table[row]` for a hot table).
- `Property` (`string`): The property.
- `Value` (`string`): The value now stored, as text.
- `Hash` (`string`): The state hash after the patch.

## Properties

| Member | Description |
| --- | --- |
| [`Entity`](#m-entity) | The entity patched, as `Type#id` (or `Table[row]` for a hot table). |
| [`Hash`](#m-hash) | The state hash after the patch. |
| [`Property`](#m-property) | The property. |
| [`Value`](#m-value) | The value now stored, as text. |

### <a id="m-entity"></a>Entity

```csharp
public string Entity { get; init; }
```

The entity patched, as `Type#id` (or `Table[row]` for a hot table).

**Type:** `string`

### <a id="m-hash"></a>Hash

```csharp
public string Hash { get; init; }
```

The state hash after the patch.

**Type:** `string`

### <a id="m-property"></a>Property

```csharp
public string Property { get; init; }
```

The property.

**Type:** `string`

### <a id="m-value"></a>Value

```csharp
public string Value { get; init; }
```

The value now stored, as text.

**Type:** `string`
