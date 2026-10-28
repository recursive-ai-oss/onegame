[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# ScopeConfig record

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ScopeConfig`](ScopeConfig.md)&gt;

```csharp
public sealed record ScopeConfig : IEquatable<ScopeConfig>
```

One scope the server hosts.

## Constructors

| Member | Description |
| --- | --- |
| [`ScopeConfig(uint, ulong)`](#m-ctor-uint-ulong) | One scope the server hosts. |

### <a id="m-ctor-uint-ulong"></a>ScopeConfig(uint, ulong)

```csharp
public ScopeConfig(uint Id, ulong Seed);
```

One scope the server hosts.

**Parameters**

- `Id` (`uint`): The scope id.
- `Seed` (`ulong`): Seeds the scope's random streams.

## Properties

| Member | Description |
| --- | --- |
| [`Id`](#m-id) | The scope id. |
| [`Seed`](#m-seed) | Seeds the scope's random streams. |

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

Seeds the scope's random streams.

**Type:** `ulong`
