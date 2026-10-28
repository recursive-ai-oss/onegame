[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# ScopeInfo record

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ScopeInfo`](ScopeInfo.md)&gt;

```csharp
public sealed record ScopeInfo : IEquatable<ScopeInfo>
```

A snapshot of one scope.

## Constructors

| Member | Description |
| --- | --- |
| [`ScopeInfo(uint, long, string, int, int)`](#m-ctor-uint-long-string-int-int) | A snapshot of one scope. |

### <a id="m-ctor-uint-long-string-int-int"></a>ScopeInfo(uint, long, string, int, int)

```csharp
public ScopeInfo(uint Id, long Tick, string Hash, int Entities, int Subscribers);
```

A snapshot of one scope.

**Parameters**

- `Id` (`uint`): The scope id.
- `Tick` (`long`): The scope's world tick.
- `Hash` (`string`): The world hash, as 16 hex digits.
- `Entities` (`int`): The entity count.
- `Subscribers` (`int`): How many connections are subscribed.

## Properties

| Member | Description |
| --- | --- |
| [`Entities`](#m-entities) | The entity count. |
| [`Hash`](#m-hash) | The world hash, as 16 hex digits. |
| [`Id`](#m-id) | The scope id. |
| [`Subscribers`](#m-subscribers) | How many connections are subscribed. |
| [`Tick`](#m-tick) | The scope's world tick. |

### <a id="m-entities"></a>Entities

```csharp
public int Entities { get; init; }
```

The entity count.

**Type:** `int`

### <a id="m-hash"></a>Hash

```csharp
public string Hash { get; init; }
```

The world hash, as 16 hex digits.

**Type:** `string`

### <a id="m-id"></a>Id

```csharp
public uint Id { get; init; }
```

The scope id.

**Type:** `uint`

### <a id="m-subscribers"></a>Subscribers

```csharp
public int Subscribers { get; init; }
```

How many connections are subscribed.

**Type:** `int`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The scope's world tick.

**Type:** `long`
