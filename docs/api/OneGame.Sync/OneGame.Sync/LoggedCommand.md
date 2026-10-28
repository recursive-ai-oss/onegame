[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# LoggedCommand record

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`LoggedCommand`](LoggedCommand.md)&gt;

```csharp
public sealed record LoggedCommand : IEquatable<LoggedCommand>
```

A command that entered an authority from outside its tick. Authority transfers are logged here too, as operations on entity 0.

## Constructors

| Member | Description |
| --- | --- |
| [`LoggedCommand(long, ulong, uint, byte[])`](#m-ctor-long-ulong-uint-byte) | A command that entered an authority from outside its tick. |

### <a id="m-ctor-long-ulong-uint-byte"></a>LoggedCommand(long, ulong, uint, byte\[\])

```csharp
public LoggedCommand(long Tick, ulong Entity, uint CommandId, byte[] Payload);
```

A command that entered an authority from outside its tick. Authority transfers are logged here too, as operations on entity 0.

**Parameters**

- `Tick` (`long`): The tick it ran in (the world's completed tick count when it was submitted).
- `Entity` (`ulong`): The target entity id.
- `CommandId` (`uint`): The command id.
- `Payload` (`byte[]`): The encoded arguments.

## Properties

| Member | Description |
| --- | --- |
| [`CommandId`](#m-commandid) | The command id. |
| [`Entity`](#m-entity) | The target entity id. |
| [`Payload`](#m-payload) | The encoded arguments. |
| [`Tick`](#m-tick) | The tick it ran in (the world's completed tick count when it was submitted). |

### <a id="m-commandid"></a>CommandId

```csharp
public uint CommandId { get; init; }
```

The command id.

**Type:** `uint`

### <a id="m-entity"></a>Entity

```csharp
public ulong Entity { get; init; }
```

The target entity id.

**Type:** `ulong`

### <a id="m-payload"></a>Payload

```csharp
public byte[] Payload { get; init; }
```

The encoded arguments.

**Type:** `byte[]`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The tick it ran in (the world's completed tick count when it was submitted).

**Type:** `long`
