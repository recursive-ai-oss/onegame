[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# CommandRefusal struct

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`CommandRefusal`](CommandRefusal.md)&gt;

```csharp
public readonly struct CommandRefusal : IEquatable<CommandRefusal>
```

A command or subscription that an authority refused, or that this node was told was refused.

## Constructors

| Member | Description |
| --- | --- |
| [`CommandRefusal(ScopeId, int, EntityId, uint, RefusalReason)`](#m-ctor-scopeid-int-entityid-uint-refusalreason) | A command or subscription that an authority refused, or that this node was told was refused. |

### <a id="m-ctor-scopeid-int-entityid-uint-refusalreason"></a>CommandRefusal(ScopeId, int, EntityId, uint, RefusalReason)

```csharp
public CommandRefusal(ScopeId Scope, int Connection, EntityId Entity, uint CommandId, RefusalReason Reason);
```

A command or subscription that an authority refused, or that this node was told was refused.

**Parameters**

- `Scope` ([`ScopeId`](ScopeId.md)): The scope concerned.
- `Connection` (`int`): Id of the connection it came from (on the authority) or went over (on the submitter).
- `Entity` ([`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)): The target entity (zero for subscriptions).
- `CommandId` (`uint`): The command id (zero for subscriptions).
- `Reason` ([`RefusalReason`](RefusalReason.md)): Why.

## Properties

| Member | Description |
| --- | --- |
| [`CommandId`](#m-commandid) | The command id (zero for subscriptions). |
| [`Connection`](#m-connection) | Id of the connection it came from (on the authority) or went over (on the submitter). |
| [`Entity`](#m-entity) | The target entity (zero for subscriptions). |
| [`Reason`](#m-reason) | Why. |
| [`Scope`](#m-scope) | The scope concerned. |

### <a id="m-commandid"></a>CommandId

```csharp
public uint CommandId { get; init; }
```

The command id (zero for subscriptions).

**Type:** `uint`

### <a id="m-connection"></a>Connection

```csharp
public int Connection { get; init; }
```

Id of the connection it came from (on the authority) or went over (on the submitter).

**Type:** `int`

### <a id="m-entity"></a>Entity

```csharp
public EntityId Entity { get; init; }
```

The target entity (zero for subscriptions).

**Type:** [`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)

### <a id="m-reason"></a>Reason

```csharp
public RefusalReason Reason { get; init; }
```

Why.

**Type:** [`RefusalReason`](RefusalReason.md)

### <a id="m-scope"></a>Scope

```csharp
public ScopeId Scope { get; init; }
```

The scope concerned.

**Type:** [`ScopeId`](ScopeId.md)
