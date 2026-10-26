# Entity model and generated sync surface

What the sync stack (snapshots, command log, replay, drift hashes, filtered views) and the harness (state query/patch, JSON dumps) build on. Source: `src/OneGame.Core` (runtime) and `src/OneGame.Generators` (generator and analyzers).

## Declaring things

```csharp
[SyncedStruct] public partial struct ItemSlot { public int ItemId { get; set; } public int Count { get; set; } }

[Entity(Schema = 2)]                                   // Schema: version of the synced layout (default 1)
public partial class Shop : Entity                     // partial, top level, non-generic, derives Entity directly
{
    [Synced] public partial string Name { get; set; }  // synced scalar: partial, get + set (any accessibility)
    [Synced] public partial Fixed Gold { get; private set; }
    [Synced] public SyncList<ItemSlot> Items { get; } = new();   // synced list: get-only with initializer

    [Command] public bool Buy(int itemId, int count) { ... }     // sim handler; void, bool (false = rejected) or CommandReply
    [Migrate(1)] static void FromV1(ref SyncReader old, Shop shop) { ... }  // reads layout of schema 1
    [View] public string Label => $"{Name}: {Items.Count}";      // view member: reads only
    protected override void OnTick() { ... }                     // per-tick sim logic
}

[HotData(Name = "bots")] public struct BotRow { public Fixed X { get; set; } ... }   // generates BotRowTable
```

### Who may see what

```csharp
[Entity]
public partial class Hero : Entity
{
    [Synced, OwnerKey] public partial uint Player { get; set; }      // which player owns this entity (0 = nobody); uint or int
    [Synced, TeamKey]  public partial uint Team { get; set; }        // which team it belongs to (0 = none)
    [Synced] public partial FVec2 Position { get; set; }             // default: everyone who may read the scope
    [Synced(Visibility = Visibility.Team)]   public partial int Stamina { get; set; }   // owner and team
    [Synced(Visibility = Visibility.Owner)]  public partial int Gold { get; set; }      // the owner only
    [Synced(Visibility = Visibility.Hidden)] public partial int Plot { get; set; }      // never leaves the authority

    [Command(OwnerOnly = true)] public void Rename(string name) { ... }   // needs Trust.Own and the owner's identity
    [Command] public CommandReply Spend(int amount) => amount > Gold ? CommandReply.Reject("not enough gold") : Pay(amount);
}
[Entity(Visibility = Visibility.Owner)] public partial class Diary : Entity { ... }   // the whole entity exists only for its owner
[HotData(Visibility = Visibility.Hidden)] public struct SecretRow { ... }              // a table that stays on the authority
```

- `Audience.Relay` (set through `Trust.AsRelay()` in the sync layer) marks a trusted relay node: it is treated as each entity's owner and team, so it receives every member that is not `Hidden` and can filter them again for its own subscribers. `Entity.ViewMask` limits what a replica passes on to what it holds.
- Levels: `Everyone` (default), `Owner`, `Team` (the owner and everyone whose team equals the entity's team key), `Hidden`. They sit on top of the scope's read trust. Entity-level and member-level visibility combine: a member is visible only if the entity is.
- A connection's `Audience` (`Trust.Who`: player, team; 0 = none, never matches) is matched against the entity's `[OwnerKey]` / `[TeamKey]` values at the moment a frame is built, so changing a key moves the data between subscribers on the next frame.
- A replica holds the type's default for every member it may not see, and the generated `ReadOnlyView` has no getter for `Hidden` members. At most one `[OwnerKey]` and one `[TeamKey]` per entity, each a `[Synced]` `uint` or `int`.
- `CommandReply`: a handler may return `CommandReply.Ok`, `CommandReply.Reject("why")`, or a plain `bool`; the reason goes to the sender only (not synced state).
- Compile-time checks: **OG0017** a key is not a synced `uint`/`int` or is declared twice · **OG0018** `Owner`/`Team` visibility or an `OwnerOnly` command with no `[OwnerKey]`/`[TeamKey]` to match, or `Owner`/`Team` on a hot table · **OG0019** view code reads a `Hidden` member (or a member of a `Hidden` entity), which no replica ever holds.

Synced types: `bool`, `byte`/`sbyte`, `short`/`ushort`, `int`/`uint`, `long`/`ulong`, `string`, enums, `Fixed`, `FVec2`, `EntityId`, `Timed<T>` (`T` = `Fixed` or `FVec2`), `[SyncedStruct]` structs, and `SyncList<T>` of any of those. Anything else is OG0009; floats are OG0001. At most 64 synced properties per entity. A null string is stored as empty.

## Sim code and view code (analyzers)

- **Sim code** = every member of a type marked `[Entity]` or `[Sim]` or implementing `ISimSystem`, plus any member marked `[Sim]`, except members (or types) marked `[View]`. Rules OG0002–OG0007 and OG0016 check the code written in those members **and follow every call** out of them (below).
- **View code** = members marked `[View]` and every member of a `[View]` type. Rule OG0008 flags assigning a synced property, calling a `[Command]` handler or `[MutatesState]` method (SyncList mutators, `Rng` draws, `World.Add/Remove/Step/ReadSnapshot`...), and calling any method in the same compilation that does either, at any depth. Views send commands through `entity.Send.X(...)`.
- **Calls are followed.** A call from sim code into a method, property, constructor or operator of the same compilation (generated code included) is analysed down through everything that method calls, at any depth. If any of it breaks OG0002–OG0007 or OG0016, the diagnostic is reported **at the call site in sim code** and names the chain: `... E.OnTick → PriceMath.Spread → DateTime.Now ...`. Lambdas written in sim code are sim code; lambdas inside a helper are part of the helper. Recursion ends at the first repeat. Calls the analysis cannot see into (interface or abstract members, delegates passed in as values) are not followed. Members of sim types are not re-reported at the callers; they are checked where they are written. Mark a helper `[SimSafe]` to stop the analysis from looking into it.
- **Calls out of the compilation (OG0015).** A call into another assembly is fine when (1) it is on the built-in framework list: integer math, `Fixed` and the rest of `OneGame.Core` (the assembly is `[SimSafe]`), collections (`List`, `SortedDictionary`, `Dictionary` lookups; iteration order is OG0003's job), `Span`/`Array`/`MemoryExtensions`, LINQ, `StringBuilder`, `string` and `Convert` (culture rules apply), enums, delegates, exceptions, date/time arithmetic, `BitOperations`, `BigInteger`; or (2) the symbol, its type or its assembly has `[SimSafe]`; or (3) it is trusted. Anything else is **warning OG0015** (an error under the repo's warnings-as-errors) saying which API. Hash codes (`object.GetHashCode`, `string.GetHashCode`, `HashCode`) are deliberately not on the list. To vouch for a library, either add `[assembly: SimTrust("Some.Namespace")]` (a namespace, a full type name or an assembly name; repeatable) or set `onegame_sim_trusted = A.B; C.D` in `.editorconfig` (global or per folder). Games should wrap a trusted library call in their own `[SimSafe]` helper when only one function is meant to be trusted.
- **Culture (OG0016).** Anything that formats or parses with the machine's current culture is an error in sim code: `int.ToString()`, `int.Parse(s)`, `string.Format(fmt, ...)` without a provider, `$"{n}"` and `"a" + n` with a number or date, `ToUpper()`/`ToLower()`, `StartsWith(string)`/`IndexOf(string)`/`Compare(a, b)` without a `StringComparison`, `StringComparison.CurrentCulture*`, `CultureInfo.CurrentCulture`, `Convert.ToInt32(string)`, `StringBuilder.Append(int)`. Pass `CultureInfo.InvariantCulture`, use `FormattableString.Invariant($"...")` or `string.Create(CultureInfo.InvariantCulture, $"...")`, or compare with `StringComparison.Ordinal`. Hex formats (`"x2"`) pass. Not covered: sorting strings with the default comparer (`List<string>.Sort()`, `OrderBy` on strings) uses the current culture; sort with `StringComparer.Ordinal`.
- **Predicted commands (OG0020–OG0022).** A command marked `[Command(Predicted = true)]` also runs on a replica's predicted world (`sync.md`: Prediction), where only what the audience receives exists. The analyzer follows calls into the same compilation, as the sim rules do, and reports: **OG0020** the handler reads a `Hidden` member, a member of a `Hidden` entity, or an `Owner`/`Team` member that is not the command's own entity's in an `OwnerOnly` command · **OG0021** the handler draws from a random stream (`World.Rng[...]`, any `Rng` draw) without `UsesRandom = true` (which accepts that the command is sent unpredicted when the scope hides its streams) · **OG0022** the handler writes a `Hidden` member (a replica never holds it, so the write is lost at the next correction). The sim (`OnTick`, systems) may read and write hidden members freely: it only affects the prediction.
- Rules: OG0001 float in synced state/command args · OG0002 float arithmetic in sim · OG0003 Dictionary/HashSet (or other unordered) iteration · OG0004 clock · OG0005 threads/tasks/async/lock · OG0006 I/O (File, Console, Environment, System.Net, Process) · OG0007 random source other than `world.Rng["name"]` · OG0008 view writes state · OG0009 no codec · OG0010 entity shape · OG0011 more than 64 properties · OG0012 synced member shape · OG0013 command shape · OG0014 migration shape · OG0015 outside API not known to be deterministic (warning) · OG0016 culture-sensitive text in sim. · OG0017 owner/team key declared wrongly · OG0018 visibility with nothing to match against · OG0019 view reads a member replicas never receive.

## What the generator adds to an entity `E`

| Member | Meaning |
|---|---|
| `TypeId`, `TypeName`, `SchemaVersion`, `PropertyCount`, `PropertyName(i)` | metadata. `TypeId` = FNV-1a 32 of the full name unless `[Entity(Id = n)]`. Property index = declaration order (bit position in masks). |
| `DirtyMask`, `ClearDirty()` | bit i set when property i changed since the last clear (lists via `SyncList.IsDirty`). |
| `WriteState(SyncWriter)` / `ReadState(ref SyncReader)` | full state, current schema. Read decodes everything first, so an error leaves the entity unchanged. |
| `ReadState(uint schemaVersion, ref SyncReader)` | current version → `ReadState`; older with a `[Migrate(v)]` method → runs it; else `UnsupportedVersion`. A failed migration may leave the entity partly filled: discard it. |
| `WriteMaskedDelta(w, mask)` | delta holding exactly the properties in `mask`: with the mask an audience may see, its whole view of the entity. |
| `TryGetVisibleMask(in Audience, out mask)`, `OwnerKey`, `TeamKey`, `IsOwnerOnlyCommand(id)`, `FullMask` | visibility metadata the sync layer reads. Only emitted for entities that restrict something. |
| `ComputeViewHash()`, `ComputeViewHash(mask)` | hash of what this copy holds / of the view a mask shows. Equals `ComputeHash()` when every property is visible. |
| `WriteDirtyDelta(w)`, `WriteDeltaFrom(baseline, w)`, `ApplyDelta(ref r)` | delta = varint mask + the changed values in order. `ApplyDelta` rejects unknown bits, decodes all values before storing any, and marks applied properties dirty (so a relay can forward them). |
| `WriteJson(JsonOut)`, `TrySetFromText(name, text)` | inspection and the state-patch harness (scalars, enums by number, `Fixed`; not lists/structs). |
| `Send.X(args)` | typed command stub: encodes args, queues the command in the entity's world. Returns `CommandStatus` (`Queued` on an authority, `Forwarded` on a replica). Calling `X(args)` directly runs the handler immediately — sim code only. |
| `AsReadOnly` | `ReadOnlyView` struct: getters only. |
| `E.Commands`, `E.ReadOnlyView` | nested types. |
| `IsPredictedCommand(id)`, `PredictedCommandUsesRandom(id)` | only emitted when a command is `Predicted` / `UsesRandom`. The replica asks the target entity before predicting. |
| `CopyStateCore(source)` (protected, called by the engine) | assigns every synced field directly from another entity of the same type (lists with `SyncList.CopyFrom`): how the predicted world is rebuilt from confirmed state without encoding or allocating. The base class falls back to the encoded form. |

Per assembly: `OneGame.Generated.<AssemblyName with . → _>.EntityTypes.Register(EntityRegistry)` registers every entity so a world can build them while restoring. `[SyncedStruct]` gets `Write/Read/WriteJson/Equals/==` (implements `ISyncValue<T>`). `[HotData]` struct `S` gets `STable : IHotTable` with one `Span<T>` column per field, `Add/Get/Set/RemoveAtSwap/Clear/Reserve`.

## Wire format (all integers little-endian where fixed; otherwise LEB128)

- unsigned = LEB128 (max 10 bytes, the 10th ≤ 1); signed = zigzag then LEB128; bool = 1 byte (0/1); `Fixed` = zigzag raw; string = length + UTF-8 (≤ 64 KiB, valid UTF-8 only); list = count + elements (count ≤ 65 536 and ≤ bytes left); enum = its underlying integer.
- **Snapshot** (`World.WriteSnapshot`/`ReadSnapshot`): `"OGSN"` (fixed32), format (1), tick, next entity id, RNG streams (count; name, 4×fixed64 each, in name order), entity count, per entity `{typeId fixed32, id, schema, length-prefixed state}` in id order, table count, per table `{name, length-prefixed rows}`. Decoding checks: ids strictly increasing and below next id, every entity body fully consumed, table set equal to the world's tables, no trailing bytes.
- **View snapshot** (`World.WriteViewSnapshot(w, audience, includeRandom)`, read by `ReadSnapshot`): the same header with format 2, then a flags varint (bit 0 = random streams included), [random streams], the visible entities each as `{typeId, id, schema, form, length-prefixed body}` (form 0 = whole state, 1 = a delta in the current layout holding mask + visible properties), and only the tables that are not hidden. Decoding adds: form ≤ 1, a masked body only at the entity's current schema, table count equal to the replica's non-hidden tables.
- **Command wire** (`World.EncodeCommand`/`SubmitEncoded`): entity id, command id (= FNV-1a 32 of the method name), argument bytes (≤ 64 KiB). Handlers consume all bytes or the command is `Malformed`.

All decoders report problems as `SyncError` on the reader/return value; none throws on bad bytes.

## World

- **Roles.** `Authority` simulates; `Replica` only applies what the authority sends and rejects writes and ticks; `Predicted` is a writable copy a replica simulates ahead (it ticks, runs handlers, forwards commands sent from outside its sim). Hot tables implement `CopyFrom(IHotTable)` (generated tables copy their columns in place, the default interface method goes through the encoded form) so a predicted world can be rebuilt without allocating.

`new World(seed, registry, role)`; `Add/AddWithId/Remove/Find`, `AddSystem(ISimSystem)`, `AddTable`, `Step()`, `Tick`, `Rng["name"]`, `Submit/SubmitEncoded`, `Observer`, `ComputeHash()`, `WriteSnapshot/ReadSnapshot`, `DumpJson()`.

- **Tick order**: queued commands (arrival order) → each entity's `OnTick` in id order → each system in the order added → `Tick++`. Entities added during a tick first run next tick; removals take effect when the tick ends. Ids are never reused.
- **Roles**: an `Authority` world simulates. A `Replica` world only applies snapshots/deltas: entity setters and `Step()` throw `ReplicaWriteException`, and `Submit` forwards through `Observer` (status `Forwarded`) for the transport to deliver. `AddWithId` lets a replica mirror the authority's ids.
- **Observer** (`CommandObserver`) is called for every executed command (tick, entity, command id, payload, status) — the hook for the command log. Replay = resubmit logged commands with `SubmitEncoded` before the tick they ran in, then `Step`.
- **Hash**: `ComputeHash()` covers tick, RNG streams, tables and every entity (type id, id, full state bytes). Equal hashes at equal ticks mean equal state. Seed and next-id are not hashed. It allocates nothing once buffers are warm. `ComputeViewHash()` is the same over what a replica holds (random streams only if its authority sends them, no hidden tables, filtered entities by their visible properties); `ComputeAudienceHash(audience, includeRandom)` is what an authority computes for a subscriber. Both equal `ComputeHash()` when nothing is restricted.
- **Restore** into a world that already has the same tables and systems; on error the world is unspecified — discard it.
- Systems keep no state of their own: put it in entities or tables so snapshots and hashes see it.

## Numbers

`Fixed` is a signed fixed-point number with 32 fractional bits in a `long` (arithmetic wraps; `Sqrt/Sin/Cos/Atan2/Atan` are integer-only; `FromRatio`, `FromInt`, `Round/Floor/Ceiling/Frac`, `Lerp`, `ToString`/`TryParse` exist; `ToDouble/ToSingle` are for views). `Rng` is xoshiro256** seeded from `world seed ^ hash(name)`. `Timed<T>.ValueAt(tick)` interpolates linearly between `StartTick` and `EndTick`.

## Harness hooks

Every entity type gets `CommandSignatures` (for example `Buy(int count)`) and `SendFromText(name, string[] args)`, which parses text arguments (int, bool, string, enum, `Fixed`, `EntityId`) and sends the command; commands with other parameter types are left out. The harness uses them for `command_send` and scenarios.
