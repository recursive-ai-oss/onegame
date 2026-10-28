[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# TransferOffer record

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`TransferOffer`](TransferOffer.md)&gt;

```csharp
public sealed record TransferOffer : IEquatable<TransferOffer>
```

An offer another node made to hand entities to a scope on this node; [`AuthorityScope.TransferGate`](AuthorityScope.md#m-transfergate) may veto it.

## Constructors

| Member | Description |
| --- | --- |
| [`TransferOffer(ulong, Connection, IReadOnlyList<Entity>)`](#m-ctor-ulong-connection-ireadonlylist-entity) | An offer another node made to hand entities to a scope on this node; [`AuthorityScope.TransferGate`](AuthorityScope.md#m-transfergate) may veto it. |

### <a id="m-ctor-ulong-connection-ireadonlylist-entity"></a>TransferOffer(ulong, Connection, IReadOnlyList&lt;Entity&gt;)

```csharp
public TransferOffer(ulong Id, Connection From, IReadOnlyList<Entity> Entities);
```

An offer another node made to hand entities to a scope on this node; [`AuthorityScope.TransferGate`](AuthorityScope.md#m-transfergate) may veto it.

**Parameters**

- `Id` (`ulong`): The transfer id.
- `From` ([`Connection`](Connection.md)): The connection the offer came on.
- `Entities` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`Entity`](../../OneGame.Core/OneGame.Core/Entity.md)&gt;): The entities, decoded and not yet in the world (read their state, do not keep them).

## Properties

| Member | Description |
| --- | --- |
| [`Entities`](#m-entities) | The entities, decoded and not yet in the world (read their state, do not keep them). |
| [`From`](#m-from) | The connection the offer came on. |
| [`Id`](#m-id) | The transfer id. |

### <a id="m-entities"></a>Entities

```csharp
public IReadOnlyList<Entity> Entities { get; init; }
```

The entities, decoded and not yet in the world (read their state, do not keep them).

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`Entity`](../../OneGame.Core/OneGame.Core/Entity.md)&gt;

### <a id="m-from"></a>From

```csharp
public Connection From { get; init; }
```

The connection the offer came on.

**Type:** [`Connection`](Connection.md)

### <a id="m-id"></a>Id

```csharp
public ulong Id { get; init; }
```

The transfer id.

**Type:** `ulong`
