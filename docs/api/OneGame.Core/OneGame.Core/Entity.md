[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# Entity class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public abstract class Entity
```

Base class of every entity. The generator fills in everything abstract here for a class marked [`EntityAttribute`](EntityAttribute.md); authors override [`Entity.OnTick()`](Entity.md#m-ontick) for per-tick sim logic.

## Constructors

| Member | Description |
| --- | --- |
| [`Entity()`](#m-ctor) |  |

### <a id="m-ctor"></a>Entity()

```csharp
protected Entity();
```

## Properties

| Member | Description |
| --- | --- |
| [`CommandSignatures`](#m-commandsignatures) | Signatures of the commands that [`Entity.SendFromText()`](Entity.md#m-sendfromtext-string-string) accepts, such as `Buy(int itemId, int count)`, for the harness. |
| [`DirtyMask`](#m-dirtymask) | Bit i is set when synced property i changed since [`Entity.ClearDirty()`](Entity.md#m-cleardirty). |
| [`FullMask`](#m-fullmask) | The mask with a bit for every synced property. |
| [`Id`](#m-id) | The entity's id, or [`EntityId.None`](EntityId.md#m-none) before it is added to a world. |
| [`IsRemoved`](#m-isremoved) | True once the entity has been removed from its world. |
| [`OwnerKey`](#m-ownerkey) | The value of the [`OwnerKeyAttribute`](OwnerKeyAttribute.md) property, or 0. |
| [`PropertyCount`](#m-propertycount) | The number of synced properties. |
| [`SchemaVersion`](#m-schemaversion) | The schema version of the state this build writes. |
| [`TeamKey`](#m-teamkey) | The value of the [`TeamKeyAttribute`](TeamKeyAttribute.md) property, or 0. |
| [`TypeId`](#m-typeid) | The stable id of this entity type, used in snapshots. |
| [`TypeName`](#m-typename) | The type's name. |
| [`World`](#m-world) | The world this entity belongs to, or null. |

### <a id="m-commandsignatures"></a>CommandSignatures

```csharp
public virtual string[] CommandSignatures { get; }
```

Signatures of the commands that [`Entity.SendFromText()`](Entity.md#m-sendfromtext-string-string) accepts, such as `Buy(int itemId, int count)`, for the harness.

**Type:** `string[]`

### <a id="m-dirtymask"></a>DirtyMask

```csharp
public abstract ulong DirtyMask { get; }
```

Bit i is set when synced property i changed since [`Entity.ClearDirty()`](Entity.md#m-cleardirty).

**Type:** `ulong`

### <a id="m-fullmask"></a>FullMask

```csharp
public ulong FullMask { get; }
```

The mask with a bit for every synced property.

**Type:** `ulong`

### <a id="m-id"></a>Id

```csharp
public EntityId Id { get; }
```

The entity's id, or [`EntityId.None`](EntityId.md#m-none) before it is added to a world.

**Type:** [`EntityId`](EntityId.md)

### <a id="m-isremoved"></a>IsRemoved

```csharp
public bool IsRemoved { get; }
```

True once the entity has been removed from its world.

**Type:** `bool`

### <a id="m-ownerkey"></a>OwnerKey

```csharp
public virtual ulong OwnerKey { get; }
```

The value of the [`OwnerKeyAttribute`](OwnerKeyAttribute.md) property, or 0.

**Type:** `ulong`

### <a id="m-propertycount"></a>PropertyCount

```csharp
public abstract int PropertyCount { get; }
```

The number of synced properties.

**Type:** `int`

### <a id="m-schemaversion"></a>SchemaVersion

```csharp
public abstract uint SchemaVersion { get; }
```

The schema version of the state this build writes.

**Type:** `uint`

### <a id="m-teamkey"></a>TeamKey

```csharp
public virtual ulong TeamKey { get; }
```

The value of the [`TeamKeyAttribute`](TeamKeyAttribute.md) property, or 0.

**Type:** `ulong`

### <a id="m-typeid"></a>TypeId

```csharp
public abstract uint TypeId { get; }
```

The stable id of this entity type, used in snapshots.

**Type:** `uint`

### <a id="m-typename"></a>TypeName

```csharp
public abstract string TypeName { get; }
```

The type's name.

**Type:** `string`

### <a id="m-world"></a>World

```csharp
public World? World { get; }
```

The world this entity belongs to, or null.

**Type:** [`World`](World.md)?

## Methods

| Member | Description |
| --- | --- |
| [`ApplyDelta(ref SyncReader)`](#m-applydelta-syncreader) | Applies a delta. |
| [`AssertWritable()`](#m-assertwritable) | Throws if this entity belongs to a replica world. |
| [`BeginCommand()`](#m-begincommand) | Starts a command message: returns the world's scratch writer for the arguments. |
| [`BeginRestore()`](#m-beginrestore) | Lets generated migration code assign properties on a replica. |
| [`ClearDirty()`](#m-cleardirty) | Forgets which properties changed; the sync layer calls it after sending a delta. |
| [`ComputeHash()`](#m-computehash) | Hash of this entity's type, id and full state, for drift detection. |
| [`ComputeViewHash()`](#m-computeviewhash) | The hash of what this copy holds: [`Entity.ComputeHash()`](Entity.md#m-computehash) for a complete entity, the filtered form for a replica of a filtered one. |
| [`ComputeViewHash(ulong)`](#m-computeviewhash-ulong) | Hash of what an audience sees of this entity: with every property visible it equals [`Entity.ComputeHash()`](Entity.md#m-computehash), otherwise it covers the type, the id and only the visible properties. |
| [`CopyStateCore(Entity)`](#m-copystatecore-entity) | Makes this entity's synced state equal to `source`'s (same type). |
| [`EndRestore()`](#m-endrestore) | Ends [`Entity.BeginRestore()`](Entity.md#m-beginrestore). |
| [`ExecuteCommand(uint, ref SyncReader)`](#m-executecommand-uint-syncreader) | Runs the handler for a command. |
| [`IsOwnerOnlyCommand(uint)`](#m-isowneronlycommand-uint) | True when the command is marked `OwnerOnly`. |
| [`IsPredictedCommand(uint)`](#m-ispredictedcommand-uint) | True when the command is marked `Predicted`. |
| [`OnTick()`](#m-ontick) | Per-tick sim logic. |
| [`PredictedCommandUsesRandom(uint)`](#m-predictedcommandusesrandom-uint) | True when the command is marked `Predicted` with `UsesRandom`. |
| [`PropertyName(int)`](#m-propertyname-int) | The name of synced property `index` (declaration order). |
| [`ReadState(ref SyncReader)`](#m-readstate-syncreader) | Replaces all synced state from the current schema's layout. |
| [`ReadState(uint, ref SyncReader)`](#m-readstate-uint-syncreader) | Replaces all synced state from a given schema version, running the author's migration for older versions. |
| [`SendCommand(uint, SyncWriter)`](#m-sendcommand-uint-syncwriter) | Sends the command whose arguments are in the writer from [`Entity.BeginCommand()`](Entity.md#m-begincommand). |
| [`SendFromText(string, string[])`](#m-sendfromtext-string-string) | Encodes and sends a command given its name and text arguments (the state-patch harness uses this). |
| [`SetRejectReason(string?)`](#m-setrejectreason-string) | Records why the command handler running now rejected the command; the authority sends the text back to the submitter. |
| [`TryGetVisibleMask(in Audience, out ulong)`](#m-trygetvisiblemask-audience-ulong) | Which of this entity's properties (bit i = property i) an audience may receive. |
| [`TrySetFromText(string, string)`](#m-trysetfromtext-string-string) | Sets a scalar synced property from text, for the state-patch harness. |
| [`WriteDeltaFrom(Entity, SyncWriter)`](#m-writedeltafrom-entity-syncwriter) | Writes a delta that turns `baseline` into this entity's state. |
| [`WriteDirtyDelta(SyncWriter)`](#m-writedirtydelta-syncwriter) | Writes a delta holding the properties changed since [`Entity.ClearDirty()`](Entity.md#m-cleardirty). |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the entity as a JSON object. |
| [`WriteMaskedDelta(SyncWriter, ulong)`](#m-writemaskeddelta-syncwriter-ulong) | Writes a delta holding exactly the properties in `mask`, whatever their dirty state. |
| [`WriteState(SyncWriter)`](#m-writestate-syncwriter) | Writes all synced state in the current schema. |

### <a id="m-applydelta-syncreader"></a>ApplyDelta(ref SyncReader)

```csharp
public abstract void ApplyDelta(ref SyncReader reader);
```

Applies a delta. All values are decoded before any is stored, so an error leaves the entity unchanged. Applied properties become dirty, so a relay can forward them.

**Parameters**

- `reader` ([`SyncReader`](SyncReader.md))

### <a id="m-assertwritable"></a>AssertWritable()

```csharp
protected void AssertWritable();
```

Throws if this entity belongs to a replica world. Generated setters call it.

**Exceptions**

- [`ReplicaWriteException`](ReplicaWriteException.md): The entity is a replica; only the authority changes state.

### <a id="m-begincommand"></a>BeginCommand()

```csharp
protected SyncWriter BeginCommand();
```

Starts a command message: returns the world's scratch writer for the arguments.

**Returns:** [`SyncWriter`](SyncWriter.md)

**Exceptions**

- [`NotInWorldException`](NotInWorldException.md): The entity is not in a world.

### <a id="m-beginrestore"></a>BeginRestore()

```csharp
protected void BeginRestore();
```

Lets generated migration code assign properties on a replica.

### <a id="m-cleardirty"></a>ClearDirty()

```csharp
public abstract void ClearDirty();
```

Forgets which properties changed; the sync layer calls it after sending a delta.

### <a id="m-computehash"></a>ComputeHash()

```csharp
public ulong ComputeHash();
```

Hash of this entity's type, id and full state, for drift detection.

**Returns:** `ulong`

### <a id="m-computeviewhash"></a>ComputeViewHash()

```csharp
public ulong ComputeViewHash();
```

The hash of what this copy holds: [`Entity.ComputeHash()`](Entity.md#m-computehash) for a complete entity, the filtered form for a replica of a filtered one.

**Returns:** `ulong`

### <a id="m-computeviewhash-ulong"></a>ComputeViewHash(ulong)

```csharp
public ulong ComputeViewHash(ulong visibleMask);
```

Hash of what an audience sees of this entity: with every property visible it equals [`Entity.ComputeHash()`](Entity.md#m-computehash), otherwise it covers the type, the id and only the visible properties. A replica computes the same value with [`Entity.ComputeViewHash()`](Entity.md#m-computeviewhash).

**Parameters**

- `visibleMask` (`ulong`)

**Returns:** `ulong`

### <a id="m-copystatecore-entity"></a>CopyStateCore(Entity)

```csharp
protected virtual void CopyStateCore(Entity source);
```

Makes this entity's synced state equal to `source`'s (same type). The generator overrides it to assign fields directly; the default goes through the encoded form.

**Parameters**

- `source` ([`Entity`](Entity.md))

### <a id="m-endrestore"></a>EndRestore()

```csharp
protected void EndRestore();
```

Ends [`Entity.BeginRestore()`](Entity.md#m-beginrestore).

### <a id="m-executecommand-uint-syncreader"></a>ExecuteCommand(uint, ref SyncReader)

```csharp
protected abstract CommandStatus ExecuteCommand(uint commandId, ref SyncReader arguments);
```

Runs the handler for a command. Returns [`CommandStatus.Executed`](CommandStatus.md#m-executed), [`CommandStatus.Rejected`](CommandStatus.md#m-rejected), [`CommandStatus.UnknownCommand`](CommandStatus.md#m-unknowncommand) or [`CommandStatus.Malformed`](CommandStatus.md#m-malformed).

**Parameters**

- `commandId` (`uint`)
- `arguments` ([`SyncReader`](SyncReader.md))

**Returns:** [`CommandStatus`](CommandStatus.md)

### <a id="m-isowneronlycommand-uint"></a>IsOwnerOnlyCommand(uint)

```csharp
public virtual bool IsOwnerOnlyCommand(uint commandId);
```

True when the command is marked `OwnerOnly`.

**Parameters**

- `commandId` (`uint`)

**Returns:** `bool`

### <a id="m-ispredictedcommand-uint"></a>IsPredictedCommand(uint)

```csharp
public virtual bool IsPredictedCommand(uint commandId);
```

True when the command is marked `Predicted`.

**Parameters**

- `commandId` (`uint`)

**Returns:** `bool`

### <a id="m-ontick"></a>OnTick()

```csharp
protected virtual void OnTick();
```

Per-tick sim logic. Runs on the authority, in entity id order, after queued commands.

### <a id="m-predictedcommandusesrandom-uint"></a>PredictedCommandUsesRandom(uint)

```csharp
public virtual bool PredictedCommandUsesRandom(uint commandId);
```

True when the command is marked `Predicted` with `UsesRandom`.

**Parameters**

- `commandId` (`uint`)

**Returns:** `bool`

### <a id="m-propertyname-int"></a>PropertyName(int)

```csharp
public abstract string PropertyName(int index);
```

The name of synced property `index` (declaration order).

**Parameters**

- `index` (`int`)

**Returns:** `string`

### <a id="m-readstate-syncreader"></a>ReadState(ref SyncReader)

```csharp
public abstract void ReadState(ref SyncReader reader);
```

Replaces all synced state from the current schema's layout. Check `reader` afterwards; on error the entity is unchanged.

**Parameters**

- `reader` ([`SyncReader`](SyncReader.md))

### <a id="m-readstate-uint-syncreader"></a>ReadState(uint, ref SyncReader)

```csharp
public abstract void ReadState(uint schemaVersion, ref SyncReader reader);
```

Replaces all synced state from a given schema version, running the author's migration for older versions. On error the reader holds the reason.

**Parameters**

- `schemaVersion` (`uint`)
- `reader` ([`SyncReader`](SyncReader.md))

### <a id="m-sendcommand-uint-syncwriter"></a>SendCommand(uint, SyncWriter)

```csharp
protected CommandStatus SendCommand(uint commandId, SyncWriter arguments);
```

Sends the command whose arguments are in the writer from [`Entity.BeginCommand()`](Entity.md#m-begincommand).

**Parameters**

- `commandId` (`uint`)
- `arguments` ([`SyncWriter`](SyncWriter.md))

**Returns:** [`CommandStatus`](CommandStatus.md)

### <a id="m-sendfromtext-string-string"></a>SendFromText(string, string\[\])

```csharp
public virtual CommandStatus SendFromText(string commandName, string[] arguments);
```

Encodes and sends a command given its name and text arguments (the state-patch harness uses this). Returns [`CommandStatus.UnknownCommand`](CommandStatus.md#m-unknowncommand) for an unknown name and [`CommandStatus.Malformed`](CommandStatus.md#m-malformed) when the argument count or a value is wrong.

**Parameters**

- `commandName` (`string`)
- `arguments` (`string[]`)

**Returns:** [`CommandStatus`](CommandStatus.md)

### <a id="m-setrejectreason-string"></a>SetRejectReason(string?)

```csharp
protected void SetRejectReason(string? reason);
```

Records why the command handler running now rejected the command; the authority sends the text back to the submitter.

**Parameters**

- `reason` (`string?`)

### <a id="m-trygetvisiblemask-audience-ulong"></a>TryGetVisibleMask(in Audience, out ulong)

```csharp
public virtual bool TryGetVisibleMask(in Audience audience, out ulong mask);
```

Which of this entity's properties (bit i = property i) an audience may receive. Returns false when the entity as a whole is hidden from it. Entities without visibility rules show everything to everyone.

**Parameters**

- `audience` ([`Audience`](Audience.md))
- `mask` (`ulong`)

**Returns:** `bool`

### <a id="m-trysetfromtext-string-string"></a>TrySetFromText(string, string)

```csharp
public abstract bool TrySetFromText(string propertyName, string text);
```

Sets a scalar synced property from text, for the state-patch harness. Returns false if the name is unknown, the type is not a scalar, or the text does not parse.

**Parameters**

- `propertyName` (`string`)
- `text` (`string`)

**Returns:** `bool`

### <a id="m-writedeltafrom-entity-syncwriter"></a>WriteDeltaFrom(Entity, SyncWriter)

```csharp
public abstract bool WriteDeltaFrom(Entity baseline, SyncWriter writer);
```

Writes a delta that turns `baseline` into this entity's state.

**Parameters**

- `baseline` ([`Entity`](Entity.md))
- `writer` ([`SyncWriter`](SyncWriter.md))

**Returns:** `bool` — False if `baseline` is not the same entity type; nothing is written then.

### <a id="m-writedirtydelta-syncwriter"></a>WriteDirtyDelta(SyncWriter)

```csharp
public abstract void WriteDirtyDelta(SyncWriter writer);
```

Writes a delta holding the properties changed since [`Entity.ClearDirty()`](Entity.md#m-cleardirty).

**Parameters**

- `writer` ([`SyncWriter`](SyncWriter.md))

### <a id="m-writejson-jsonout"></a>WriteJson(JsonOut)

```csharp
public abstract void WriteJson(JsonOut json);
```

Writes the entity as a JSON object.

**Parameters**

- `json` ([`JsonOut`](JsonOut.md))

### <a id="m-writemaskeddelta-syncwriter-ulong"></a>WriteMaskedDelta(SyncWriter, ulong)

```csharp
public abstract void WriteMaskedDelta(SyncWriter writer, ulong mask);
```

Writes a delta holding exactly the properties in `mask`, whatever their dirty state. With the visible mask of an audience it is that audience's full view of the entity.

**Parameters**

- `writer` ([`SyncWriter`](SyncWriter.md))
- `mask` (`ulong`)

### <a id="m-writestate-syncwriter"></a>WriteState(SyncWriter)

```csharp
public abstract void WriteState(SyncWriter writer);
```

Writes all synced state in the current schema.

**Parameters**

- `writer` ([`SyncWriter`](SyncWriter.md))
