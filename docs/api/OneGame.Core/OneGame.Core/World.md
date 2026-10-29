[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# World class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public sealed class World
```

The simulation: entities in a deterministic order, hot tables, systems, the named random streams and the tick counter. One [`World.Step()`](World.md#m-step) runs queued commands, then each entity's [`Entity.OnTick()`](Entity.md#m-ontick) in id order, then each system in the order added. Nothing here reads a clock, a thread or a hash-ordered collection, so the same seed and the same commands give the same state, tick for tick, on every platform.

## Constructors

| Member | Description |
| --- | --- |
| [`World(ulong, EntityRegistry, WorldRole)`](#m-ctor-ulong-entityregistry-worldrole) | Creates an empty world. |

### <a id="m-ctor-ulong-entityregistry-worldrole"></a>World(ulong, EntityRegistry, WorldRole)

```csharp
public World(ulong seed, EntityRegistry registry, WorldRole role = 0);
```

Creates an empty world. Build the same way (registry, systems, tables) on every node, then add entities or load a snapshot.

**Parameters**

- `seed` (`ulong`): Seeds every random stream.
- `registry` ([`EntityRegistry`](EntityRegistry.md)): The entity types snapshots may contain.
- `role` ([`WorldRole`](WorldRole.md)): Authority, or replica of another node's world.

## Properties

| Member | Description |
| --- | --- |
| [`Entities`](#m-entities) | The live entities in id order. |
| [`Observer`](#m-observer) | Called for every command this world executes, queues-then-runs on an authority or forwards on a replica. |
| [`PendingCommandCount`](#m-pendingcommandcount) | Commands accepted and waiting for the next tick. |
| [`Rng`](#m-rng) | The named random streams. |
| [`Role`](#m-role) | Whether this world is the authority or a replica. |
| [`Seed`](#m-seed) | The seed the random streams derive from. |
| [`Tables`](#m-tables) | The hot tables, in the order added. |
| [`Tick`](#m-tick) | The number of completed ticks. |

### <a id="m-entities"></a>Entities

```csharp
public EntityList Entities { get; }
```

The live entities in id order. An entity removed during a tick is no longer listed, even though it is only detached when the tick ends, so a loop that removes an entity never sees it again in the same tick.

**Type:** [`EntityList`](EntityList.md)

### <a id="m-observer"></a>Observer

```csharp
public CommandObserver? Observer { get; set; }
```

Called for every command this world executes, queues-then-runs on an authority or forwards on a replica. The command log hooks in here.

**Type:** [`CommandObserver`](CommandObserver.md)?

### <a id="m-pendingcommandcount"></a>PendingCommandCount

```csharp
public int PendingCommandCount { get; }
```

Commands accepted and waiting for the next tick. Snapshots do not hold them, so a recording must not start while any wait.

**Type:** `int`

### <a id="m-rng"></a>Rng

```csharp
public RngStreams Rng { get; }
```

The named random streams.

**Type:** [`RngStreams`](RngStreams.md)

### <a id="m-role"></a>Role

```csharp
public WorldRole Role { get; }
```

Whether this world is the authority or a replica.

**Type:** [`WorldRole`](WorldRole.md)

### <a id="m-seed"></a>Seed

```csharp
public ulong Seed { get; }
```

The seed the random streams derive from.

**Type:** `ulong`

### <a id="m-tables"></a>Tables

```csharp
public IReadOnlyList<IHotTable> Tables { get; }
```

The hot tables, in the order added.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`IHotTable`](IHotTable.md)&gt;

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; }
```

The number of completed ticks. Replicas take it from the snapshots they apply.

**Type:** `long`

## Methods

| Member | Description |
| --- | --- |
| [`Add<T>(T)`](#m-add-t-t) | Adds an entity under a fresh id (one above the highest used so far) and returns it, so `var ship = world.Add(new Ship());` works. |
| [`AddSystem(ISimSystem)`](#m-addsystem-isimsystem) | Adds a system. |
| [`AddTable<T>(T)`](#m-addtable-t-t) | Adds a hot table. |
| [`AddWithId(Entity, EntityId)`](#m-addwithid-entity-entityid) | Adds an entity under a given id; replicas use it to mirror the authority's ids. |
| [`ComputeAudienceHash(in Audience, bool)`](#m-computeaudiencehash-audience-bool) | The hash of the view an audience gets from this world: tick, the random streams when `includeRandom`, the tables that are not hidden, and each entity the audience may see with only the properties it may see. |
| [`ComputeHash()`](#m-computehash) | A 64-bit hash of everything that defines the simulation: tick, random streams, tables and every entity's state. |
| [`ComputeViewHash()`](#m-computeviewhash) | The hash of what this world holds as a subscriber: equal to [`World.ComputeHash()`](World.md#m-computehash) on an authority and on a replica that receives everything, and the hash of the filtered view on a replica that does not. |
| [`DumpJson()`](#m-dumpjson) | The world as a JSON document, for inspection: tick, seed, every entity and every table. |
| [`EncodeCommand(SyncWriter, EntityId, uint, ReadOnlySpan<byte>)`](#m-encodecommand-syncwriter-entityid-uint-readonlyspan-byte) | The wire form of a command, the inverse of [`World.SubmitEncoded()`](World.md#m-submitencoded-readonlyspan-byte). |
| [`Find(EntityId)`](#m-find-entityid) | Finds an entity by id. |
| [`ReadSnapshot(ReadOnlySpan<byte>)`](#m-readsnapshot-readonlyspan-byte) | Replaces this world's contents with a snapshot from untrusted bytes. |
| [`Remove(EntityId)`](#m-remove-entityid) | Removes an entity. |
| [`Step()`](#m-step) | Advances the simulation by one tick. |
| [`Submit(EntityId, uint, ReadOnlySpan<byte>)`](#m-submit-entityid-uint-readonlyspan-byte) | Sends a command to an entity. |
| [`SubmitEncoded(ReadOnlySpan<byte>)`](#m-submitencoded-readonlyspan-byte) | Submits a command from its wire form (entity id, command id, arguments), as read from a log or a connection. |
| [`TryGet<T>(EntityId, out T)`](#m-tryget-t-entityid-t) | Finds an entity by id and type. |
| [`WriteSnapshot(SyncWriter)`](#m-writesnapshot-syncwriter) | Writes the whole world: tick, id counter, random streams, entities (with their schema versions) and tables. |
| [`WriteViewSnapshot(SyncWriter, in Audience, bool)`](#m-writeviewsnapshot-syncwriter-audience-bool) | Writes the snapshot an audience is allowed to hold: only the entities it may see, each with only the properties it may see, without hidden tables, and without the random streams unless `includeRandom`. |

### <a id="m-add-t-t"></a>Add&lt;T&gt;(T)

```csharp
public T Add<T>(T entity) where T : Entity;
```

Adds an entity under a fresh id (one above the highest used so far) and returns it, so `var ship = world.Add(new Ship());` works. Call it from setup code, from sim code and from tests; views never add entities.

**Type parameters**

- `T`: The entity type.

**Parameters**

- `entity` (`T`): A new entity that is not in any world.

**Returns:** `T`

**Exceptions**

- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): The entity is already in a world.

### <a id="m-addsystem-isimsystem"></a>AddSystem(ISimSystem)

```csharp
public void AddSystem(ISimSystem system);
```

Adds a system. Systems tick in the order added, so add them in the same order on every node.

**Parameters**

- `system` ([`ISimSystem`](ISimSystem.md)): The system. It is sim code and is checked by the sim analyzers.

### <a id="m-addtable-t-t"></a>AddTable&lt;T&gt;(T)

```csharp
public T AddTable<T>(T table) where T : IHotTable;
```

Adds a hot table. Tables are snapshotted and hashed in the order added.

**Type parameters**

- `T`: The generated hot table type.

**Parameters**

- `table` (`T`): The table to add.

**Returns:** `T` — The table, for chaining.

**Exceptions**

- [`DuplicateTypeException`](DuplicateTypeException.md): A table with this name exists.

### <a id="m-addwithid-entity-entityid"></a>AddWithId(Entity, EntityId)

```csharp
public void AddWithId(Entity entity, EntityId id);
```

Adds an entity under a given id; replicas use it to mirror the authority's ids.

**Parameters**

- `entity` ([`Entity`](Entity.md)): A new entity that is not in any world.
- `id` ([`EntityId`](EntityId.md)): The id to give it; not zero and not used by another entity.

**Exceptions**

- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): The entity is already in a world, or the id is zero or taken.

### <a id="m-computeaudiencehash-audience-bool"></a>ComputeAudienceHash(in Audience, bool)

```csharp
public ulong ComputeAudienceHash(in Audience audience, bool includeRandom = true);
```

The hash of the view an audience gets from this world: tick, the random streams when `includeRandom`, the tables that are not hidden, and each entity the audience may see with only the properties it may see.

**Parameters**

- `audience` ([`Audience`](Audience.md)): The subscriber's player and team.
- `includeRandom` (`bool`): Whether the subscriber receives the random streams.

**Returns:** `ulong`

### <a id="m-computehash"></a>ComputeHash()

```csharp
public ulong ComputeHash();
```

A 64-bit hash of everything that defines the simulation: tick, random streams, tables and every entity's state. Two nodes with equal hashes at the same tick have equal state. It allocates nothing once the internal buffers have grown.

**Returns:** `ulong`

### <a id="m-computeviewhash"></a>ComputeViewHash()

```csharp
public ulong ComputeViewHash();
```

The hash of what this world holds as a subscriber: equal to [`World.ComputeHash()`](World.md#m-computehash) on an authority and on a replica that receives everything, and the hash of the filtered view on a replica that does not. The authority computes the same number for the same subscriber with [`World.ComputeAudienceHash()`](World.md#m-computeaudiencehash-audience-bool), which is how drift is checked when subscribers see different things.

**Returns:** `ulong`

### <a id="m-dumpjson"></a>DumpJson()

```csharp
public string DumpJson();
```

The world as a JSON document, for inspection: tick, seed, every entity and every table.

**Returns:** `string`

### <a id="m-encodecommand-syncwriter-entityid-uint-readonlyspan-byte"></a>EncodeCommand(SyncWriter, EntityId, uint, ReadOnlySpan&lt;byte&gt;)

```csharp
public static void EncodeCommand(SyncWriter writer, EntityId entity, uint commandId, ReadOnlySpan<byte> payload);
```

The wire form of a command, the inverse of [`World.SubmitEncoded()`](World.md#m-submitencoded-readonlyspan-byte).

**Parameters**

- `writer` ([`SyncWriter`](SyncWriter.md)): Receives the bytes.
- `entity` ([`EntityId`](EntityId.md)): The entity the command is for.
- `commandId` (`uint`): The command's id.
- `payload` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): The encoded arguments.

### <a id="m-find-entityid"></a>Find(EntityId)

```csharp
public Entity? Find(EntityId id);
```

Finds an entity by id. Returns null for an id that was never used, was removed, or whose entity this replica has not received (or may not see).

**Parameters**

- `id` ([`EntityId`](EntityId.md)): The entity's id.

**Returns:** [`Entity`](Entity.md)?

### <a id="m-readsnapshot-readonlyspan-byte"></a>ReadSnapshot(ReadOnlySpan&lt;byte&gt;)

```csharp
public SyncError ReadSnapshot(ReadOnlySpan<byte> bytes);
```

Replaces this world's contents with a snapshot from untrusted bytes. Entities of older schema versions go through their migrations. Restore into a freshly built world with the same tables and systems: on an error the world is in an unspecified state and should be discarded.

**Parameters**

- `bytes` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): A snapshot from [`World.WriteSnapshot()`](World.md#m-writesnapshot-syncwriter), a save file or a server. Untrusted input is fine: it is bounds-checked and never throws.

**Returns:** [`SyncError`](SyncError.md) — [`SyncError.None`](SyncError.md#m-none) on success.

### <a id="m-remove-entityid"></a>Remove(EntityId)

```csharp
public bool Remove(EntityId id);
```

Removes an entity. During a tick the removal takes effect when the tick ends.

**Parameters**

- `id` ([`EntityId`](EntityId.md)): The entity to remove.

**Returns:** `bool` — False if no such entity exists.

### <a id="m-step"></a>Step()

```csharp
public void Step();
```

Advances the simulation by one tick.

**Exceptions**

- [`ReplicaWriteException`](ReplicaWriteException.md): This world is a replica.

### <a id="m-submit-entityid-uint-readonlyspan-byte"></a>Submit(EntityId, uint, ReadOnlySpan&lt;byte&gt;)

```csharp
public CommandStatus Submit(EntityId entity, uint commandId, ReadOnlySpan<byte> payload);
```

Sends a command to an entity. An authority queues it for the next tick; a replica passes it on through [`World.Observer`](World.md#m-observer) for the networking layer to deliver.

**Parameters**

- `entity` ([`EntityId`](EntityId.md)): The entity to command.
- `commandId` (`uint`): The command's id (generated; entities expose it through `Send`).
- `payload` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): The encoded arguments.

**Returns:** [`CommandStatus`](CommandStatus.md) — [`CommandStatus.Queued`](CommandStatus.md#m-queued) on an authority, [`CommandStatus.Forwarded`](CommandStatus.md#m-forwarded) on a replica, or why it was refused.

### <a id="m-submitencoded-readonlyspan-byte"></a>SubmitEncoded(ReadOnlySpan&lt;byte&gt;)

```csharp
public CommandStatus SubmitEncoded(ReadOnlySpan<byte> wire);
```

Submits a command from its wire form (entity id, command id, arguments), as read from a log or a connection. Untrusted bytes are fine: bad input returns [`CommandStatus.Malformed`](CommandStatus.md#m-malformed).

**Parameters**

- `wire` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): The bytes written by [`World.EncodeCommand()`](World.md#m-encodecommand-syncwriter-entityid-uint-readonlyspan-byte).

**Returns:** [`CommandStatus`](CommandStatus.md)

### <a id="m-tryget-t-entityid-t"></a>TryGet&lt;T&gt;(EntityId, out T)

```csharp
public bool TryGet<T>(EntityId id, out T entity) where T : Entity;
```

Finds an entity by id and type. Returns false when there is no such entity or it is of another type.

**Type parameters**

- `T`: The entity type to look for.

**Parameters**

- `id` ([`EntityId`](EntityId.md)): The entity's id.
- `entity` (`T`): The entity when found; otherwise null.

**Returns:** `bool`

### <a id="m-writesnapshot-syncwriter"></a>WriteSnapshot(SyncWriter)

```csharp
public void WriteSnapshot(SyncWriter w);
```

Writes the whole world: tick, id counter, random streams, entities (with their schema versions) and tables. This is a save: [`World.ReadSnapshot()`](World.md#m-readsnapshot-readonlyspan-byte) restores it, through the entities' migrations if their schema grew since.

**Parameters**

- `w` ([`SyncWriter`](SyncWriter.md)): Receives the bytes.

### <a id="m-writeviewsnapshot-syncwriter-audience-bool"></a>WriteViewSnapshot(SyncWriter, in Audience, bool)

```csharp
public void WriteViewSnapshot(SyncWriter w, in Audience audience, bool includeRandom);
```

Writes the snapshot an audience is allowed to hold: only the entities it may see, each with only the properties it may see, without hidden tables, and without the random streams unless `includeRandom`. [`World.ReadSnapshot()`](World.md#m-readsnapshot-readonlyspan-byte) reads it.

**Parameters**

- `w` ([`SyncWriter`](SyncWriter.md))
- `audience` ([`Audience`](Audience.md))
- `includeRandom` (`bool`)
