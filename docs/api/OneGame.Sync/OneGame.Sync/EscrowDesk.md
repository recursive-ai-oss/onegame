[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# EscrowDesk class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Inherits:** [`Entity`](../../OneGame.Core/OneGame.Core/Entity.md)

```csharp
[Entity]
public class EscrowDesk : Entity
```

The command surface of an escrow in a scope. Participants answer the coordinator's steps through it; the coordinator's desk also takes trades ([`EscrowDesk.Open()`](EscrowDesk.md#m-open-ulong-uint-uint-uint-uint-long-uint-uint-uint-uint-long)) and the participants' answers ([`EscrowDesk.Report()`](EscrowDesk.md#m-report-ulong-int-int-bool)). Every command is `OwnerOnly` with the coordinator identity ([`Escrow.CoordinatorPlayer`](Escrow.md#m-coordinatorplayer)) as the owner, so an ordinary player cannot call any of them. Created by [`Escrow.Install()`](Escrow.md#m-install-world-iescrowhandler).

## Nested types

| Type | Description |
| --- | --- |
| [`EscrowDesk.Commands`](EscrowDesk.Commands.md) | Sends commands to this entity. |
| [`EscrowDesk.ReadOnlyView`](EscrowDesk.ReadOnlyView.md) | A read-only window onto the synced state, for views. |

## Constructors

| Member | Description |
| --- | --- |
| [`EscrowDesk()`](#m-ctor) |  |

### <a id="m-ctor"></a>EscrowDesk()

```csharp
public EscrowDesk();
```

## Properties

| Member | Description |
| --- | --- |
| [`AsReadOnly`](#m-asreadonly) | The read-only view of this entity's synced state. |
| [`CommandSignatures`](#m-commandsignatures) | Signatures of the commands that [`Entity.SendFromText()`](../../OneGame.Core/OneGame.Core/Entity.md#m-sendfromtext-string-string) accepts, such as `Buy(int itemId, int count)`, for the harness. |
| [`Coordinator`](#m-coordinator) | The identity that owns the desk: [`Escrow.CoordinatorPlayer`](Escrow.md#m-coordinatorplayer). |
| [`DirtyMask`](#m-dirtymask) | Bit i is set when synced property i changed since [`Entity.ClearDirty()`](../../OneGame.Core/OneGame.Core/Entity.md#m-cleardirty). |
| [`OwnerKey`](#m-ownerkey) | The value of the [`OwnerKeyAttribute`](../../OneGame.Core/OneGame.Core/OwnerKeyAttribute.md) property, or 0. |
| [`PropertyCount`](#m-propertycount) | The number of [`SyncedAttribute`](../../OneGame.Core/OneGame.Core/SyncedAttribute.md) properties, which is also the width of the bit masks used by [`Entity.DirtyMask`](../../OneGame.Core/OneGame.Core/Entity.md#m-dirtymask) and visibility. |
| [`SchemaVersion`](#m-schemaversion) | The schema version of the state this build writes. |
| [`Send`](#m-send) | Sends commands to this entity; views and tests use this instead of calling the handlers. |
| [`TypeId`](#m-typeid) | The stable id of this entity type, used in snapshots. |
| [`TypeName`](#m-typename) | The entity type's name as written in code, used in harness output, scenarios and snapshots' JSON dump. |

### <a id="m-asreadonly"></a>AsReadOnly

```csharp
public EscrowDesk.ReadOnlyView AsReadOnly { get; }
```

The read-only view of this entity's synced state.

**Type:** `EscrowDesk.`[`ReadOnlyView`](EscrowDesk.ReadOnlyView.md)

### <a id="m-commandsignatures"></a>CommandSignatures

```csharp
public override string[] CommandSignatures { get; }
```

Signatures of the commands that [`Entity.SendFromText()`](../../OneGame.Core/OneGame.Core/Entity.md#m-sendfromtext-string-string) accepts, such as `Buy(int itemId, int count)`, for the harness.

**Type:** `string[]`

### <a id="m-coordinator"></a>Coordinator

```csharp
public uint Coordinator { get; set; }
```

The identity that owns the desk: [`Escrow.CoordinatorPlayer`](Escrow.md#m-coordinatorplayer).

**Type:** `uint`

### <a id="m-dirtymask"></a>DirtyMask

```csharp
public override ulong DirtyMask { get; }
```

Bit i is set when synced property i changed since [`Entity.ClearDirty()`](../../OneGame.Core/OneGame.Core/Entity.md#m-cleardirty).

**Type:** `ulong`

### <a id="m-ownerkey"></a>OwnerKey

```csharp
public override ulong OwnerKey { get; }
```

The value of the [`OwnerKeyAttribute`](../../OneGame.Core/OneGame.Core/OwnerKeyAttribute.md) property, or 0.

**Type:** `ulong`

### <a id="m-propertycount"></a>PropertyCount

```csharp
public override int PropertyCount { get; }
```

The number of [`SyncedAttribute`](../../OneGame.Core/OneGame.Core/SyncedAttribute.md) properties, which is also the width of the bit masks used by [`Entity.DirtyMask`](../../OneGame.Core/OneGame.Core/Entity.md#m-dirtymask) and visibility.

**Type:** `int`

### <a id="m-schemaversion"></a>SchemaVersion

```csharp
public override uint SchemaVersion { get; }
```

The schema version of the state this build writes.

**Type:** `uint`

### <a id="m-send"></a>Send

```csharp
public EscrowDesk.Commands Send { get; }
```

Sends commands to this entity; views and tests use this instead of calling the handlers.

**Type:** `EscrowDesk.`[`Commands`](EscrowDesk.Commands.md)

### <a id="m-typeid"></a>TypeId

```csharp
public override uint TypeId { get; }
```

The stable id of this entity type, used in snapshots.

**Type:** `uint`

### <a id="m-typename"></a>TypeName

```csharp
public override string TypeName { get; }
```

The entity type's name as written in code, used in harness output, scenarios and snapshots' JSON dump.

**Type:** `string`

## Methods

| Member | Description |
| --- | --- |
| [`Abort(ulong, int)`](#m-abort-ulong-int) | Makes sure a withdraw that has not happened never will. |
| [`ApplyDelta(ref SyncReader)`](#m-applydelta-syncreader) | Applies a delta. |
| [`ClearDirty()`](#m-cleardirty) | Forgets which properties changed; the sync layer calls it after sending a delta. |
| [`CopyStateCore(Entity)`](#m-copystatecore-entity) | Makes this entity's synced state equal to `source`'s (same type). |
| [`Credit(ulong, int, uint, uint, long)`](#m-credit-ulong-int-uint-uint-long) | Credits a receiver, once per transaction and leg. |
| [`ExecuteCommand(uint, ref SyncReader)`](#m-executecommand-uint-syncreader) | Runs the handler for a command. |
| [`IsOwnerOnlyCommand(uint)`](#m-isowneronlycommand-uint) | True when the command is marked `OwnerOnly`. |
| [`OnTick()`](#m-ontick) | Per-tick sim logic: override it to move, age or react. |
| [`Open(ulong, uint, uint, uint, uint, long, uint, uint, uint, uint, long)`](#m-open-ulong-uint-uint-uint-uint-long-uint-uint-uint-uint-long) | Coordinator: opens a trade between two scopes. |
| [`PropertyName(int)`](#m-propertyname-int) | The name of synced property `index` (declaration order). |
| [`ReadState(ref SyncReader)`](#m-readstate-syncreader) | Replaces all synced state from the current schema's layout. |
| [`ReadState(uint, ref SyncReader)`](#m-readstate-uint-syncreader) | Replaces all synced state from a given schema version, running the author's migration for older versions. |
| [`Report(ulong, int, int, bool)`](#m-report-ulong-int-int-bool) | Coordinator: records a participant's answer. |
| [`SendFromText(string, string[])`](#m-sendfromtext-string-string) | Encodes and sends a command given its name and text arguments (the state-patch harness uses this). |
| [`TrySetFromText(string, string)`](#m-trysetfromtext-string-string) | Sets a scalar synced property from text, for the state-patch harness. |
| [`Withdraw(ulong, int, uint, uint, long)`](#m-withdraw-ulong-int-uint-uint-long) | Takes a giver's value into escrow. |
| [`WriteDeltaFrom(Entity, SyncWriter)`](#m-writedeltafrom-entity-syncwriter) | Writes a delta that turns `baseline` into this entity's state. |
| [`WriteDirtyDelta(SyncWriter)`](#m-writedirtydelta-syncwriter) | Writes a delta holding the properties changed since [`Entity.ClearDirty()`](../../OneGame.Core/OneGame.Core/Entity.md#m-cleardirty). |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the entity as a JSON object. |
| [`WriteMaskedDelta(SyncWriter, ulong)`](#m-writemaskeddelta-syncwriter-ulong) | Writes a delta holding exactly the properties in `mask`, whatever their dirty state. |
| [`WriteState(SyncWriter)`](#m-writestate-syncwriter) | Writes all synced state in the current schema. |

### <a id="m-abort-ulong-int"></a>Abort(ulong, int)

```csharp
public CommandReply Abort(ulong tx, int leg);
```

Makes sure a withdraw that has not happened never will. Declined with "held" when the value was already taken.

**Parameters**

- `tx` (`ulong`)
- `leg` (`int`)

**Returns:** [`CommandReply`](../../OneGame.Core/OneGame.Core/CommandReply.md)

### <a id="m-applydelta-syncreader"></a>ApplyDelta(ref SyncReader)

```csharp
public override void ApplyDelta(ref SyncReader r);
```

Applies a delta. All values are decoded before any is stored, so an error leaves the entity unchanged. Applied properties become dirty, so a relay can forward them.

**Parameters**

- `r` ([`SyncReader`](../../OneGame.Core/OneGame.Core/SyncReader.md))

### <a id="m-cleardirty"></a>ClearDirty()

```csharp
public override void ClearDirty();
```

Forgets which properties changed; the sync layer calls it after sending a delta.

### <a id="m-copystatecore-entity"></a>CopyStateCore(Entity)

```csharp
protected override void CopyStateCore(Entity source);
```

Makes this entity's synced state equal to `source`'s (same type). The generator overrides it to assign fields directly; the default goes through the encoded form.

**Parameters**

- `source` ([`Entity`](../../OneGame.Core/OneGame.Core/Entity.md))

### <a id="m-credit-ulong-int-uint-uint-long"></a>Credit(ulong, int, uint, uint, long)

```csharp
public CommandReply Credit(ulong tx, int leg, uint owner, uint kind, long amount);
```

Credits a receiver, once per transaction and leg.

**Parameters**

- `tx` (`ulong`)
- `leg` (`int`)
- `owner` (`uint`)
- `kind` (`uint`)
- `amount` (`long`)

**Returns:** [`CommandReply`](../../OneGame.Core/OneGame.Core/CommandReply.md)

### <a id="m-executecommand-uint-syncreader"></a>ExecuteCommand(uint, ref SyncReader)

```csharp
protected override CommandStatus ExecuteCommand(uint commandId, ref SyncReader r);
```

Runs the handler for a command. Returns [`CommandStatus.Executed`](../../OneGame.Core/OneGame.Core/CommandStatus.md#m-executed), [`CommandStatus.Rejected`](../../OneGame.Core/OneGame.Core/CommandStatus.md#m-rejected), [`CommandStatus.UnknownCommand`](../../OneGame.Core/OneGame.Core/CommandStatus.md#m-unknowncommand) or [`CommandStatus.Malformed`](../../OneGame.Core/OneGame.Core/CommandStatus.md#m-malformed).

**Parameters**

- `commandId` (`uint`): The command's generated id.
- `r` ([`SyncReader`](../../OneGame.Core/OneGame.Core/SyncReader.md))

**Returns:** [`CommandStatus`](../../OneGame.Core/OneGame.Core/CommandStatus.md)

### <a id="m-isowneronlycommand-uint"></a>IsOwnerOnlyCommand(uint)

```csharp
public override bool IsOwnerOnlyCommand(uint commandId);
```

True when the command is marked `OwnerOnly`.

**Parameters**

- `commandId` (`uint`): The command's generated id.

**Returns:** `bool`

### <a id="m-ontick"></a>OnTick()

```csharp
protected override void OnTick();
```

Per-tick sim logic: override it to move, age or react. It runs once per fixed simulation tick on the authority (and on a predicting replica's predicted copy), in entity id order, after the commands queued for that tick, and never on a plain replica. It is sim code: fixed-point numbers, named random streams ([`World.Rng`](../../OneGame.Core/OneGame.Core/World.md#m-rng)), no clock, threads or I/O. An entity removed during a tick is gone when the tick ends.

### <a id="m-open-ulong-uint-uint-uint-uint-long-uint-uint-uint-uint-long"></a>Open(ulong, uint, uint, uint, uint, long, uint, uint, uint, uint, long)

```csharp
public CommandReply Open(ulong tx, uint scopeA, uint giverA, uint receiverA, uint kindA, long amountA, uint scopeB, uint giverB, uint receiverB, uint kindB, long amountB);
```

Coordinator: opens a trade between two scopes. A repeat of an open transaction id is accepted and ignored.

**Parameters**

- `tx` (`ulong`)
- `scopeA` (`uint`)
- `giverA` (`uint`)
- `receiverA` (`uint`)
- `kindA` (`uint`)
- `amountA` (`long`)
- `scopeB` (`uint`)
- `giverB` (`uint`)
- `receiverB` (`uint`)
- `kindB` (`uint`)
- `amountB` (`long`)

**Returns:** [`CommandReply`](../../OneGame.Core/OneGame.Core/CommandReply.md)

### <a id="m-propertyname-int"></a>PropertyName(int)

```csharp
public override string PropertyName(int index);
```

The name of synced property `index` (declaration order).

**Parameters**

- `index` (`int`)

**Returns:** `string`

### <a id="m-readstate-syncreader"></a>ReadState(ref SyncReader)

```csharp
public override void ReadState(ref SyncReader r);
```

Replaces all synced state from the current schema's layout. Check `reader` afterwards; on error the entity is unchanged.

**Parameters**

- `r` ([`SyncReader`](../../OneGame.Core/OneGame.Core/SyncReader.md))

### <a id="m-readstate-uint-syncreader"></a>ReadState(uint, ref SyncReader)

```csharp
public override void ReadState(uint schemaVersion, ref SyncReader r);
```

Replaces all synced state from a given schema version, running the author's migration for older versions. On error the reader holds the reason.

**Parameters**

- `schemaVersion` (`uint`): The schema version the bytes were written with.
- `r` ([`SyncReader`](../../OneGame.Core/OneGame.Core/SyncReader.md))

### <a id="m-report-ulong-int-int-bool"></a>Report(ulong, int, int, bool)

```csharp
public CommandReply Report(ulong tx, int leg, int step, bool ok);
```

Coordinator: records a participant's answer. `step` is an [`EscrowStep`](EscrowStep.md); `ok` is the participant's verdict on it.

**Parameters**

- `tx` (`ulong`)
- `leg` (`int`)
- `step` (`int`)
- `ok` (`bool`)

**Returns:** [`CommandReply`](../../OneGame.Core/OneGame.Core/CommandReply.md)

### <a id="m-sendfromtext-string-string"></a>SendFromText(string, string\[\])

```csharp
public override CommandStatus SendFromText(string commandName, string[] arguments);
```

Encodes and sends a command given its name and text arguments (the state-patch harness uses this). Returns [`CommandStatus.UnknownCommand`](../../OneGame.Core/OneGame.Core/CommandStatus.md#m-unknowncommand) for an unknown name and [`CommandStatus.Malformed`](../../OneGame.Core/OneGame.Core/CommandStatus.md#m-malformed) when the argument count or a value is wrong.

**Parameters**

- `commandName` (`string`): The command method's name.
- `arguments` (`string[]`): One text per parameter, in declaration order.

**Returns:** [`CommandStatus`](../../OneGame.Core/OneGame.Core/CommandStatus.md)

### <a id="m-trysetfromtext-string-string"></a>TrySetFromText(string, string)

```csharp
public override bool TrySetFromText(string propertyName, string text);
```

Sets a scalar synced property from text, for the state-patch harness. Returns false if the name is unknown, the type is not a scalar, or the text does not parse.

**Parameters**

- `propertyName` (`string`): The property's name.
- `text` (`string`): The new value in invariant text form (for example `12.5` for a [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)).

**Returns:** `bool`

### <a id="m-withdraw-ulong-int-uint-uint-long"></a>Withdraw(ulong, int, uint, uint, long)

```csharp
public CommandReply Withdraw(ulong tx, int leg, uint owner, uint kind, long amount);
```

Takes a giver's value into escrow. Declined when the giver has too little, or when the withdraw was aborted.

**Parameters**

- `tx` (`ulong`)
- `leg` (`int`)
- `owner` (`uint`)
- `kind` (`uint`)
- `amount` (`long`)

**Returns:** [`CommandReply`](../../OneGame.Core/OneGame.Core/CommandReply.md)

### <a id="m-writedeltafrom-entity-syncwriter"></a>WriteDeltaFrom(Entity, SyncWriter)

```csharp
public override bool WriteDeltaFrom(Entity baseline, SyncWriter w);
```

Writes a delta that turns `baseline` into this entity's state.

**Parameters**

- `baseline` ([`Entity`](../../OneGame.Core/OneGame.Core/Entity.md))
- `w` ([`SyncWriter`](../../OneGame.Core/OneGame.Core/SyncWriter.md))

**Returns:** `bool` — False if `baseline` is not the same entity type; nothing is written then.

### <a id="m-writedirtydelta-syncwriter"></a>WriteDirtyDelta(SyncWriter)

```csharp
public override void WriteDirtyDelta(SyncWriter w);
```

Writes a delta holding the properties changed since [`Entity.ClearDirty()`](../../OneGame.Core/OneGame.Core/Entity.md#m-cleardirty).

**Parameters**

- `w` ([`SyncWriter`](../../OneGame.Core/OneGame.Core/SyncWriter.md))

### <a id="m-writejson-jsonout"></a>WriteJson(JsonOut)

```csharp
public override void WriteJson(JsonOut j);
```

Writes the entity as a JSON object.

**Parameters**

- `j` ([`JsonOut`](../../OneGame.Core/OneGame.Core/JsonOut.md))

### <a id="m-writemaskeddelta-syncwriter-ulong"></a>WriteMaskedDelta(SyncWriter, ulong)

```csharp
public override void WriteMaskedDelta(SyncWriter w, ulong mask);
```

Writes a delta holding exactly the properties in `mask`, whatever their dirty state. With the visible mask of an audience it is that audience's full view of the entity.

**Parameters**

- `w` ([`SyncWriter`](../../OneGame.Core/OneGame.Core/SyncWriter.md))
- `mask` (`ulong`)

### <a id="m-writestate-syncwriter"></a>WriteState(SyncWriter)

```csharp
public override void WriteState(SyncWriter w);
```

Writes all synced state in the current schema.

**Parameters**

- `w` ([`SyncWriter`](../../OneGame.Core/OneGame.Core/SyncWriter.md))
