[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# CommandResult struct

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`CommandResult`](CommandResult.md)&gt;

```csharp
public readonly struct CommandResult : IEquatable<CommandResult>
```

The answer to one command a replica sent, matched to the send by [`CommandResult.Sequence`](CommandResult.md#m-sequence).

## Constructors

| Member | Description |
| --- | --- |
| [`CommandResult(ScopeId, uint, EntityId, uint, CommandVerdict, RefusalReason, string?)`](#m-ctor-scopeid-uint-entityid-uint-commandverdict-refusalreason-string) | The answer to one command a replica sent, matched to the send by [`CommandResult.Sequence`](CommandResult.md#m-sequence). |

### <a id="m-ctor-scopeid-uint-entityid-uint-commandverdict-refusalreason-string"></a>CommandResult(ScopeId, uint, EntityId, uint, CommandVerdict, RefusalReason, string?)

```csharp
public CommandResult(ScopeId Scope, uint Sequence, EntityId Entity, uint CommandId, CommandVerdict Verdict, RefusalReason Refusal, string? Reason);
```

The answer to one command a replica sent, matched to the send by [`CommandResult.Sequence`](CommandResult.md#m-sequence).

**Parameters**

- `Scope` ([`ScopeId`](ScopeId.md)): The scope.
- `Sequence` (`uint`): The number [`ReplicaScope.LastSequence`](ReplicaScope.md#m-lastsequence) returned right after the send.
- `Entity` ([`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)): The target entity.
- `CommandId` (`uint`): The command id.
- `Verdict` ([`CommandVerdict`](CommandVerdict.md)): How it ended.
- `Refusal` ([`RefusalReason`](RefusalReason.md)): Why it was refused, when `Verdict` is [`CommandVerdict.Refused`](CommandVerdict.md#m-refused).
- `Reason` (`string?`): The handler's reason for rejecting, or null.

## Properties

| Member | Description |
| --- | --- |
| [`CommandId`](#m-commandid) | The command id. |
| [`Entity`](#m-entity) | The target entity. |
| [`Reason`](#m-reason) | The handler's reason for rejecting, or null. |
| [`Refusal`](#m-refusal) | Why it was refused, when `Verdict` is [`CommandVerdict.Refused`](CommandVerdict.md#m-refused). |
| [`Scope`](#m-scope) | The scope. |
| [`Sequence`](#m-sequence) | The number [`ReplicaScope.LastSequence`](ReplicaScope.md#m-lastsequence) returned right after the send. |
| [`Verdict`](#m-verdict) | How it ended. |

### <a id="m-commandid"></a>CommandId

```csharp
public uint CommandId { get; init; }
```

The command id.

**Type:** `uint`

### <a id="m-entity"></a>Entity

```csharp
public EntityId Entity { get; init; }
```

The target entity.

**Type:** [`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)

### <a id="m-reason"></a>Reason

```csharp
public string? Reason { get; init; }
```

The handler's reason for rejecting, or null.

**Type:** `string?`

### <a id="m-refusal"></a>Refusal

```csharp
public RefusalReason Refusal { get; init; }
```

Why it was refused, when `Verdict` is [`CommandVerdict.Refused`](CommandVerdict.md#m-refused).

**Type:** [`RefusalReason`](RefusalReason.md)

### <a id="m-scope"></a>Scope

```csharp
public ScopeId Scope { get; init; }
```

The scope.

**Type:** [`ScopeId`](ScopeId.md)

### <a id="m-sequence"></a>Sequence

```csharp
public uint Sequence { get; init; }
```

The number [`ReplicaScope.LastSequence`](ReplicaScope.md#m-lastsequence) returned right after the send.

**Type:** `uint`

### <a id="m-verdict"></a>Verdict

```csharp
public CommandVerdict Verdict { get; init; }
```

How it ended.

**Type:** [`CommandVerdict`](CommandVerdict.md)
