# Entity model and generated sync surface

What the one-node sync stack (snapshots, command log, replay, drift hashes) and the harness (state query/patch, JSON dumps) build on. Source: `src/OneGame.Core` (runtime) and `src/OneGame.Generators` (generator and analyzers).

## Declaring things

```csharp
[SyncedStruct] public partial struct ItemSlot { public int ItemId { get; set; } public int Count { get; set; } }

[Entity(Schema = 2)]                                   // Schema: version of the synced layout (default 1)
public partial class Shop : Entity                     // partial, top level, non-generic, derives Entity directly
{
    [Synced] public partial string Name { get; set; }  // synced scalar: partial, get + set (any accessibility)
    [Synced] public partial Fixed Gold { get; private set; }
    [Synced] public SyncList<ItemSlot> Items { get; } = new();   // synced list: get-only with initializer

    [Command] public bool Buy(int itemId, int count) { ... }     // sim handler; void or bool (false = rejected)
    [Migrate(1)] static void FromV1(ref SyncReader old, Shop shop) { ... }  // reads layout of schema 1
    [View] public string Label => $"{Name}: {Items.Count}";      // view member: reads only
    protected override void OnTick() { ... }                     // per-tick sim logic
}

[HotData(Name = "bots")] public struct BotRow { public Fixed X { get; set; } ... }   // generates BotRowTable
```

Synced types: `bool`, `byte`/`sbyte`, `short`/`ushort`, `int`/`uint`, `long`/`ulong`, `string`, enums, `Fixed`, `FVec2`, `EntityId`, `Timed<T>` (`T` = `Fixed` or `FVec2`), `[SyncedStruct]` structs, and `SyncList<T>` of any of those. Anything else is OG0009; floats are OG0001. At most 64 synced properties per entity. A null string is stored as empty.

## Sim code and view code (analyzers)

- **Sim code** = every member of a type marked `[Entity]` or `[Sim]` or implementing `ISimSystem`, plus any member marked `[Sim]`, except members (or types) marked `[View]`. Rules OG0002–OG0007 check the code written in those members; a call into some other method is not followed.
- **View code** = members marked `[View]` and every member of a `[View]` type. Rule OG0008 flags assigning a synced property, calling a `[Command]` handler or `[MutatesState]` method (SyncList mutators, `Rng` draws, `World.Add/Remove/Step/ReadSnapshot`...), and calling any method in the same compilation that does either, at any depth. Views send commands through `entity.Send.X(...)`.
- Rules: OG0001 float in synced state/command args · OG0002 float arithmetic in sim · OG0003 Dictionary/HashSet (or other unordered) iteration · OG0004 clock · OG0005 threads/tasks/async/lock · OG0006 I/O (File, Console, Environment, System.Net, Process) · OG0007 random source other than `world.Rng["name"]` · OG0008 view writes state · OG0009 no codec · OG0010 entity shape · OG0011 more than 64 properties · OG0012 synced member shape · OG0013 command shape · OG0014 migration shape.

## What the generator adds to an entity `E`

| Member | Meaning |
|---|---|
| `TypeId`, `TypeName`, `SchemaVersion`, `PropertyCount`, `PropertyName(i)` | metadata. `TypeId` = FNV-1a 32 of the full name unless `[Entity(Id = n)]`. Property index = declaration order (bit position in masks). |
| `DirtyMask`, `ClearDirty()` | bit i set when property i changed since the last clear (lists via `SyncList.IsDirty`). |
| `WriteState(SyncWriter)` / `ReadState(ref SyncReader)` | full state, current schema. Read decodes everything first, so an error leaves the entity unchanged. |
| `ReadState(uint schemaVersion, ref SyncReader)` | current version → `ReadState`; older with a `[Migrate(v)]` method → runs it; else `UnsupportedVersion`. A failed migration may leave the entity partly filled: discard it. |
| `WriteDirtyDelta(w)`, `WriteDeltaFrom(baseline, w)`, `ApplyDelta(ref r)` | delta = varint mask + the changed values in order. `ApplyDelta` rejects unknown bits, decodes all values before storing any, and marks applied properties dirty (so a relay can forward them). |
| `WriteJson(JsonOut)`, `TrySetFromText(name, text)` | inspection and the state-patch harness (scalars, enums by number, `Fixed`; not lists/structs). |
| `Send.X(args)` | typed command stub: encodes args, queues the command in the entity's world. Returns `CommandStatus` (`Queued` on an authority, `Forwarded` on a replica). Calling `X(args)` directly runs the handler immediately — sim code only. |
| `AsReadOnly` | `ReadOnlyView` struct: getters only. |
| `E.Commands`, `E.ReadOnlyView` | nested types. |

Per assembly: `OneGame.Generated.<AssemblyName with . → _>.EntityTypes.Register(EntityRegistry)` registers every entity so a world can build them while restoring. `[SyncedStruct]` gets `Write/Read/WriteJson/Equals/==` (implements `ISyncValue<T>`). `[HotData]` struct `S` gets `STable : IHotTable` with one `Span<T>` column per field, `Add/Get/Set/RemoveAtSwap/Clear/Reserve`.

## Wire format (all integers little-endian where fixed; otherwise LEB128)

- unsigned = LEB128 (max 10 bytes, the 10th ≤ 1); signed = zigzag then LEB128; bool = 1 byte (0/1); `Fixed` = zigzag raw; string = length + UTF-8 (≤ 64 KiB, valid UTF-8 only); list = count + elements (count ≤ 65 536 and ≤ bytes left); enum = its underlying integer.
- **Snapshot** (`World.WriteSnapshot`/`ReadSnapshot`): `"OGSN"` (fixed32), format (1), tick, next entity id, RNG streams (count; name, 4×fixed64 each, in name order), entity count, per entity `{typeId fixed32, id, schema, length-prefixed state}` in id order, table count, per table `{name, length-prefixed rows}`. Decoding checks: ids strictly increasing and below next id, every entity body fully consumed, table set equal to the world's tables, no trailing bytes.
- **Command wire** (`World.EncodeCommand`/`SubmitEncoded`): entity id, command id (= FNV-1a 32 of the method name), argument bytes (≤ 64 KiB). Handlers consume all bytes or the command is `Malformed`.

All decoders report problems as `SyncError` on the reader/return value; none throws on bad bytes.

## World

`new World(seed, registry, role)`; `Add/AddWithId/Remove/Find`, `AddSystem(ISimSystem)`, `AddTable`, `Step()`, `Tick`, `Rng["name"]`, `Submit/SubmitEncoded`, `Observer`, `ComputeHash()`, `WriteSnapshot/ReadSnapshot`, `DumpJson()`.

- **Tick order**: queued commands (arrival order) → each entity's `OnTick` in id order → each system in the order added → `Tick++`. Entities added during a tick first run next tick; removals take effect when the tick ends. Ids are never reused.
- **Roles**: an `Authority` world simulates. A `Replica` world only applies snapshots/deltas: entity setters and `Step()` throw `ReplicaWriteException`, and `Submit` forwards through `Observer` (status `Forwarded`) for the transport to deliver. `AddWithId` lets a replica mirror the authority's ids.
- **Observer** (`CommandObserver`) is called for every executed command (tick, entity, command id, payload, status) — the hook for the command log. Replay = resubmit logged commands with `SubmitEncoded` before the tick they ran in, then `Step`.
- **Hash**: `ComputeHash()` covers tick, RNG streams, tables and every entity (type id, id, full state bytes). Equal hashes at equal ticks mean equal state. Seed and next-id are not hashed. It allocates nothing once buffers are warm.
- **Restore** into a world that already has the same tables and systems; on error the world is unspecified — discard it.
- Systems keep no state of their own: put it in entities or tables so snapshots and hashes see it.

## Numbers

`Fixed` is a signed fixed-point number with 32 fractional bits in a `long` (arithmetic wraps; `Sqrt/Sin/Cos/Atan2/Atan` are integer-only; `FromRatio`, `FromInt`, `Round/Floor/Ceiling/Frac`, `Lerp`, `ToString`/`TryParse` exist; `ToDouble/ToSingle` are for views). `Rng` is xoshiro256** seeded from `world seed ^ hash(name)`. `Timed<T>.ValueAt(tick)` interpolates linearly between `StartTick` and `EndTick`.
