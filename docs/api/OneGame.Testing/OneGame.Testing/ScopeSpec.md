[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# ScopeSpec record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ScopeSpec`](ScopeSpec.md)&gt;

```csharp
public sealed record ScopeSpec : IEquatable<ScopeSpec>
```

A scope a server node hosts.

## Constructors

| Member | Description |
| --- | --- |
| [`ScopeSpec(uint, ulong)`](#m-ctor-uint-ulong) | A scope a server node hosts. |

### <a id="m-ctor-uint-ulong"></a>ScopeSpec(uint, ulong)

```csharp
public ScopeSpec(uint Id, ulong Seed);
```

A scope a server node hosts.

**Parameters**

- `Id` (`uint`): The scope id.
- `Seed` (`ulong`): The seed of its random streams.

## Properties

| Member | Description |
| --- | --- |
| [`Id`](#m-id) | The scope id. |
| [`Seed`](#m-seed) | The seed of its random streams. |

### <a id="m-id"></a>Id

```csharp
public uint Id { get; init; }
```

The scope id.

**Type:** `uint`

### <a id="m-seed"></a>Seed

```csharp
public ulong Seed { get; init; }
```

The seed of its random streams.

**Type:** `ulong`
