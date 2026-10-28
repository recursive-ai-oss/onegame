[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# ScopeId struct

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ScopeId`](ScopeId.md)&gt;

```csharp
public readonly struct ScopeId : IEquatable<ScopeId>
```

Names a scope: a unit of state with exactly one authority node.

## Constructors

| Member | Description |
| --- | --- |
| [`ScopeId(uint)`](#m-ctor-uint) | Names a scope: a unit of state with exactly one authority node. |

### <a id="m-ctor-uint"></a>ScopeId(uint)

```csharp
public ScopeId(uint Value);
```

Names a scope: a unit of state with exactly one authority node.

**Parameters**

- `Value` (`uint`): The id.

## Properties

| Member | Description |
| --- | --- |
| [`Value`](#m-value) | The id. |

### <a id="m-value"></a>Value

```csharp
public uint Value { get; init; }
```

The id.

**Type:** `uint`

## Methods

| Member | Description |
| --- | --- |
| [`ToString()`](#m-tostring) |  |

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

Overrides [`ValueType.ToString()`](https://learn.microsoft.com/dotnet/api/system.valuetype.tostring).

**Returns:** `string`
