[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# CommandOutcome struct

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`CommandOutcome`](CommandOutcome.md)&gt;

```csharp
public readonly struct CommandOutcome : IEquatable<CommandOutcome>
```

What happened to a command when the authority ran it.

## Constructors

| Member | Description |
| --- | --- |
| [`CommandOutcome(long, EntityId, uint, CommandStatus)`](#m-ctor-long-entityid-uint-commandstatus) | What happened to a command when the authority ran it. |

### <a id="m-ctor-long-entityid-uint-commandstatus"></a>CommandOutcome(long, EntityId, uint, CommandStatus)

```csharp
public CommandOutcome(long Tick, EntityId Entity, uint CommandId, CommandStatus Status);
```

What happened to a command when the authority ran it.

**Parameters**

- `Tick` (`long`): The tick it ran in.
- `Entity` ([`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)): The target.
- `CommandId` (`uint`): The command id.
- `Status` ([`CommandStatus`](../../OneGame.Core/OneGame.Core/CommandStatus.md)): The outcome: executed, rejected by the handler, unknown entity or command, or malformed.

## Properties

| Member | Description |
| --- | --- |
| [`CommandId`](#m-commandid) | The command id. |
| [`Entity`](#m-entity) | The target. |
| [`Status`](#m-status) | The outcome: executed, rejected by the handler, unknown entity or command, or malformed. |
| [`Tick`](#m-tick) | The tick it ran in. |

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

The target.

**Type:** [`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)

### <a id="m-status"></a>Status

```csharp
public CommandStatus Status { get; init; }
```

The outcome: executed, rejected by the handler, unknown entity or command, or malformed.

**Type:** [`CommandStatus`](../../OneGame.Core/OneGame.Core/CommandStatus.md)

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The tick it ran in.

**Type:** `long`
