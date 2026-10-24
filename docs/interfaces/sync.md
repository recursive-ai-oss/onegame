# One-node sync: nodes, scopes, loopback, saves, replay, drift

Header: v1.1. The first version was written before the code; since then added `Cluster.Settle()`, `Node.ConnectTo`, `Node.Connections`, `AuthorityScope.StepAndPublish()`, and the `detail` out parameter of `SaveFile.TryReadHeader`; `Cluster.Connect` returns the `Link` (its `.Connection` is the server end). Nothing removed. Source: `src/OneGame.Sync`, namespace `OneGame.Sync`. Builds on the generated surface in `entity-sync.md`. Everything is synchronous and single-threaded: the caller steps the cluster; there are no background threads.

## Model

- A **node** (`Node`) is a process-level participant. A **scope** is a unit of state with exactly one **authority**: an `AuthorityScope` owns a `World` (role Authority) on one node. Other nodes **subscribe** and hold a `ReplicaScope` (a `World` with role Replica) fed snapshot-then-frames.
- A **link** (`Link`, client end) and its **connection** (`Connection`, server end) are a pair of message channels. `Loopback` makes in-process pairs; every message is serialized to bytes, exactly what a network transport would carry (`IMessageChannel`: `Send(span)`, `TryReceive(out span)`; messages are whole, ordered and reliable).
- A connection carries a `Trust` record: which scopes it may **read**, **command** and **own** (`ScopeSet.All`, `.None`, `.Of(ids)`). Commands and subscriptions are checked against it. Refusals are reported, never thrown.
- Single-player = a `Cluster` with one authority node and one local replica (`AddLocalReplica`). The game's views read the replica; input goes through the replica as commands (`entity.Send.X(...)` on a replica world forwards over the link).

## Cluster (the harness entry point)

```csharp
var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 30, FatalDrift = false, ResyncOnDrift = true });
Node host = cluster.AddNode("host");
AuthorityScope scope = cluster.AddScope(host, new ScopeId(1), world);            // world: authority, fully built
ReplicaScope view = cluster.AddLocalReplica(scope, replicaWorld, Trust.Player(scope.Id));  // replicaWorld: same registry and tables, role Replica
cluster.Settle();                       // deliver the subscription so the replica gets its snapshot
cluster.Step(10);                       // 10 ticks: deliver input, step authorities, publish frames, deliver frames, check drift
cluster.Frame(TimeSpan.FromMilliseconds(16.7));   // fixed-step driver: steps however many ticks are due (see FixedStepDriver)
ulong h = scope.World.ComputeHash();    // authority hash; view.World.ComputeHash() for the replica
IReadOnlyList<DriftReport> now = cluster.CheckReplicas();   // direct compare of every replica to its authority at this instant
```

- `Cluster`: `Options`, `Nodes`, `Tick` (cluster steps), `AddNode`, `AddScope`, `Connect(client, server, trust)` → `Link` (`link.Connection` is the authority end), `AddLocalReplica`, `Step(n)`, `Frame(dt)`, `Driver` (`FixedStepDriver`), `CheckReplicas()`, `Hashes()` (scope id, role, tick, hash for every authority and replica), `DriftReports`, `Refusals`.
- Step order per tick: pump all nodes (commands and subscribes arrive) → each authority scope: `World.Step()` then publish one frame to its live subscribers → pump (replicas apply frames, hash checks run, replies flow) → throw `DriftException` if `FatalDrift` and a drift report appeared.
- `Node` can be used without `Cluster` (`new Node(name, options)`, `Pump()`) for real transports later: only `IMessageChannel` changes.

## Commands

- Entry points: a replica's `entity.Send.X()` (forwarded and trust-checked), `Connection.SubmitCommand(scope, entity, commandId, payload)` (as that connection, no transport: the harness path), `entity.Send.X()` on the authority world (local, trusted, queued). `Connection.SubmitCommand` returns `SubmitResult`: `Queued`, `NotTrusted`, `UnknownScope`, `Malformed`.
- The authority queues accepted commands; they run at the start of its next tick, in arrival order. Outcomes land in `AuthorityScope.Outcomes` (ring of the last 256: tick, entity, command id, `CommandStatus`, so handler rejections are visible). Refusals (trust, unknown scope) are `CommandRefusal` records on `Node.Refusals` of the authority node and are sent back to the submitting node (its `Node.Refusals` too).
- **Command log**: every command that entered an authority from outside the tick (connections, local `Send` outside `Step`) is recorded with the tick it ran in. Commands the sim itself queues are not logged; the sim recreates them.

## Saves

`SaveFile.Write(world)` → bytes: header (`"OGSV"`, format 1, seed, tick, world hash, payload hash, schema version per entity type) + the world snapshot. `SaveFile.TryReadHeader(bytes, out header, out payload, out detail)`; `SaveFile.Load(bytes, world)` → `LoadResult { Status, Detail, Header, Migrated, HashVerified }` (checks the payload hash, restores through the generated migrations, verifies the world hash when no migration ran). `SaveFile.ToJson(bytes, scratchWorld)` is the inspection dump. Restore into a world built the same way as the one saved (registry, tables, systems), then add it to a cluster or step it. `AuthorityScope.Save()` / `AuthorityScope.Load(bytes)` do this for a live scope (replicas are re-sent a snapshot).

## Record and replay

```csharp
Recorder rec = scope.StartRecording(new RecordingOptions { CheckpointInterval = 1, DetailLimit = 4096 });
cluster.Step(1000);
Recording recording = rec.Stop();                 // start snapshot + command log + hash checkpoints
byte[] file = recording.ToBytes();                // "OGRP"; Recording.TryRead(bytes, out recording) -> SyncError
ReplayResult r = Replayer.Replay(recording, freshAuthorityWorld);   // r.Ok, r.TicksRun, r.CheckpointsVerified, r.Divergence
```

- A checkpoint = (completed tick count, world hash, optional per-entity/table/rng digest) every `CheckpointInterval` ticks plus the last tick. Interval 1 pins a divergence to the exact tick; larger intervals name the first checkpoint after it (`Divergence.LastGoodTick` bounds it).
- `Divergence { Reason, Tick, LastGoodTick, ExpectedHash, ActualHash, Differences }`. `StateDifference { Kind (Entity|Table|Rng), EntityId, Name, TypeName, Change (Changed|OnlyInExpected|OnlyInActual), hashes }`. All reports have `ToJson()`.
- `Recording.Commands` is an editable list (`LoggedCommand`: tick, entity, command id, payload) so tests can corrupt a log on purpose.
- `ReplayOptions.OnTick(World)` runs after every replayed tick (capture frames, assertions).

## Drift detection

Every `DriftCheckInterval` ticks (default 30, 0 turns off) the frame carries the authority's scope hash. The replica compares it to its own after applying the frame. On mismatch it asks the authority for the per-entity digest of that tick (the authority keeps the last few), and files a `DriftReport { ScopeId, Tick, AuthorityHash, ReplicaHash, Differences, DetailAvailable }` on `Node.DriftReports` and `Cluster.DriftReports`. With `ResyncOnDrift` the replica then re-subscribes (fresh snapshot). `FatalDrift` makes `Cluster.Step` throw `DriftException` carrying the report.

## Fixed-step driver

`FixedStepDriver(tickRate, maxStepsPerFrame = 5)`: `int Advance(TimeSpan frame)` → ticks due now (exact integer arithmetic: N ticks per N/rate seconds regardless of frame jitter); `Alpha` (0..1 interpolation factor for views); `Paused`; `RequestStep()` (one tick on the next `Advance` while paused); `TimeScale`; `DroppedTicks` (time discarded by the catch-up cap). `Cluster.Frame(dt)` = `Advance` + `Step`.

## Wire summary

First byte = type, then the scope id (varint). Client to authority: `Subscribe`, `Unsubscribe`, `Command` (entity, command id, argument bytes), `DigestRequest` (tick). Authority to client: `Snapshot` (tick + `World.WriteSnapshot` bytes), `Frame`, `Digest`, `CommandRefused`, `SubscribeRefused`. A `Frame` is: tick, flags (bit 0 random streams follow, bit 1 authority hash follows), [random streams], removed ids, added entities (type id, id, schema, state), changed entities (id, delta), changed hot tables (name, whole table), [hash]. Hot tables are sent whole when their bytes change; row-level deltas are a later refinement. One frame per tick even when nothing changed (it carries the tick).

## Things a consumer must know

- Start a recording only between ticks with no queued commands (it throws otherwise): snapshots do not hold the command queue.
- Replay needs a fresh authority world built the same way (registry, tables, systems in the same order) as the recorded one.
- The log records commands that entered from outside the tick. A game that sends commands to its own authority world from view code outside `Step` is recorded too; the sim's own `Send` calls inside a tick are not.
- A subscriber that connects late gets the current snapshot, then frames; entities it already has are replaced, so overlaps are harmless.
- Steady state allocates nothing per tick (authority publish, loopback, replica apply, hash checks), provided no commands, entity adds or string changes happen in that tick.
- Changes made in `OneGame.Core` for this layer: hot-table `Read` decodes in place (a dry run first keeps it atomic) and `RngStreams.Read` updates matching streams in place, so replicas apply them without allocating; `World` gained internal hooks for replica tick, input logging and pending-command count (`OneGame.Core` has `InternalsVisibleTo(OneGame.Sync)`).

## Untrusted input

Transport messages, saves, recordings and digests are decoded with `SyncReader` and the `SyncLimits` in `OneGame.Core`; sizes are checked before allocation; no decoder throws on bad bytes. A replica that receives a bad frame or a tick gap files a `ProtocolError` on `Node.ProtocolErrors` and re-subscribes (stops after 3 consecutive failures: `ReplicaState.Failed`).

## Seams left for later units

Visibility filtering (a per-subscriber filter between the frame builder and `Send`), prediction (replica-side speculative commands), authority transfer and relaying (a replica's `ApplyDelta` marks entities dirty so a relay can forward) and real transports (implement `IMessageChannel`; fragmentation lives there).
