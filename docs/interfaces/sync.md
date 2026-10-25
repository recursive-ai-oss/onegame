# One-node sync: nodes, scopes, loopback, saves, replay, drift

Header: v2. Adds many authority scopes and replicas per node with runtime subscribe/unsubscribe, visibility-filtered views per audience (`Audience`, `Trust.Who`, `view hashes`), enforced `Trust.Own`, command answers with sequence numbers (`CommandResult`), live trust changes, digest timeouts, and `Node.ConnectTo` with caller-made channel ends. **Wire changes** (not compatible with v1): `Command` and `CommandRefused` carry a sequence number, snapshots are the view format, added entities carry a form byte, new `CommandResult` and `Revoked` messages. Nothing else removed. Source: `src/OneGame.Sync`, namespace `OneGame.Sync`. Builds on the generated surface in `entity-sync.md`. Everything is synchronous and single-threaded: the caller steps the cluster; there are no background threads.

## Model

- A **node** (`Node`) is a process-level participant. A **scope** is a unit of state with exactly one **authority**: an `AuthorityScope` owns a `World` (role Authority) on one node. Other nodes **subscribe** and hold a `ReplicaScope` (a `World` with role Replica) fed snapshot-then-frames.
- A **link** (`Link`, client end) and its **connection** (`Connection`, server end) are a pair of message channels. `Loopback` makes in-process pairs; every message is serialized to bytes, exactly what a network transport would carry (`IMessageChannel`: `Send(span)`, `TryReceive(out span)`; messages are whole, ordered and reliable).
- A node holds any number of authority scopes (`Node.AddScope`) and replicas (`Node.Subscribe`, one per link and scope). Several nodes may subscribe to one scope. `Node.Unsubscribe(replica)` stops following (the replica keeps its last state); `Node.Resubscribe(replica)` follows again with a fresh snapshot, also after a refusal or revocation.
- A connection carries a `Trust` record: which scopes it may **read**, **command** and **own** (`ScopeSet.All`, `.None`, `.Of(ids)`), and **who** it is (`Trust.Who`, an `Audience`: player id and team id, 0 = none). Commands, subscriptions and frames are checked against it. Refusals are reported, never thrown. `Trust.As(player, team)` sets the identity; `Trust.Owner(scopes)` reads, commands and owns them.
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
- The authority queues accepted commands; they run at the start of its next tick, in arrival order. Outcomes land in `AuthorityScope.Outcomes` (ring of the last 256: tick, entity, command id, `CommandStatus`, so handler rejections are visible).
- **Checks, in order**: well-formed → scope exists → `Trust.Command` has the scope (`NotTrustedToCommand`) → for a command marked `[Command(OwnerOnly = true)]`: `Trust.Own` has the scope (`NotTrustedToOwn`) and `Trust.Who.Player` equals the target entity's `[OwnerKey]` (`NotOwner`). Commands the authority's own code sends are not checked. Only accepted commands reach the command log, so replay never sees a refused one.
- **Answers reach the sender.** Every command a replica forwards gets a sequence number: read `ReplicaScope.LastSequence` right after `entity.Send.X(...)`. The authority answers each with a `CommandResult { Scope, Sequence, Entity, CommandId, Verdict, Refusal, Reason }` in `ReplicaScope.Results` (latest 256) and the `CommandCompleted` event; `PendingCommands` counts the unanswered. `Verdict`: `Executed`; `Rejected` (the handler returned false, or `CommandReply.Reject("reason")`: `Reason` is the handler's text, cut at 200 characters); `Refused` (before the handler ran, `Refusal` says why); `UnknownEntity`; `UnknownCommand`; `Malformed`; `Dropped` (the authority loaded a save while it was queued, the replica was unsubscribed or revoked, or more than 1024 were unanswered). A refusal also files a `CommandRefusal` on the authority's `Node.Refusals` and on the sender's. A command sent through `Connection.SubmitCommand` has no sequence and gets no `CommandResult`.
- **Command log**: every command that entered an authority from outside the tick (connections, local `Send` outside `Step`) is recorded with the tick it ran in. Commands the sim itself queues are not logged; the sim recreates them.

## Visibility and filtered views

The generator knows who may see what (`entity-sync.md`: `Visibility`, `[OwnerKey]`, `[TeamKey]`). The authority groups a scope's subscribers by `Trust.Who` and builds one view per group.

- **What a view holds.** An entity is in the view if its entity-level visibility passes for the audience (`Everyone`; `Owner`: the audience's player equals the entity's owner key; `Team`: the owner, or the audience's team equals the entity's team key; `Hidden`: never). Each of its properties is included if its own visibility passes. A property that is left out is not in the bytes at all; the replica entity holds the type's default there. Hot tables marked `Hidden` are not sent. The random streams go to every subscriber unless `AuthorityScope.RandomStreams = Visibility.Hidden` (use it when the next shuffle must stay secret; replicas then cannot predict random draws).
- **Frames and snapshots are per view.** Snapshot = `World.WriteViewSnapshot` (format 2, the same reader as the save format 1). A frame lists removals, additions and deltas for that audience only. An entity enters or leaves the view when its owner or team key changes. An entity whose visible property set changes (ownership moved, trust changed) is sent again whole and replaces the replica's copy. A delta is masked to the visible properties; an entity with all properties visible is sent exactly as before.
- **Hashes and drift.** `ComputeHash()` is the full hash on the authority (everything, including hidden data). A filtered replica cannot match it, so the contract is: the **view hash**. `AuthorityScope.ViewHashFor(connection)` hashes the world as that connection's audience sees it: tick, the random streams when sent, the tables that are not hidden, and each visible entity with only its visible properties (an entity with every property visible hashes as `Entity.ComputeHash()`). `ReplicaScope.ViewHash` (= `World.ComputeViewHash()`) computes the same value from what the replica holds. They are equal at equal ticks when nothing drifted. The hash a frame carries, the per-entity digest a drift report diffs, and `Cluster.CheckReplicas` all use view hashes. Without any restricted data the view hash equals `ComputeHash()`, so the authority and replica hashes compare directly as before. Recording and replay use the full hash on the authority only.
- **Trust changes at runtime.** Setting `Connection.Trust` (between ticks) re-evaluates the connection in every scope it follows. If it may no longer read a scope, it is removed, receives `Revoked`, its replica's entities and tables are emptied (`ReplicaState.Revoked`, a `ReadRevoked` refusal is filed on both nodes), and it gets nothing more; `Node.Resubscribe` asks again. If its `Who` changed, it moves to the view of its new audience and gets a fresh snapshot of it: data it may now see appears, data it may no longer see is gone, and the replica stays hash-equal. Changing only `Command` or `Own` takes effect on the next command.

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
- `ReplayOptions.MaxTicks` (default 2 000 000) caps `EndTick - StartTick`. A longer recording fails at once with `DivergenceReason.TooLong` and runs nothing, so an untrusted file cannot demand an endless run.

## Drift detection

Every `DriftCheckInterval` ticks (default 30, 0 turns off) the frame carries the authority's scope hash. The replica compares it to its own after applying the frame. The hash is the view hash (see above). On mismatch it asks the authority for the per-entity digest of that tick (the authority keeps the last few per audience), and files a `DriftReport { ScopeId, Tick, AuthorityHash, ReplicaHash, Differences, DetailAvailable, TimedOut }` on `Node.DriftReports` and `Cluster.DriftReports`. If the answer has not arrived after `ClusterOptions.DigestTimeoutTicks` (default 60) authority ticks, the replica files the report without detail (`TimedOut = true`, `DetailAvailable = false`) and stops waiting. With `ResyncOnDrift` the replica then re-subscribes (fresh snapshot). `FatalDrift` makes `Cluster.Step` throw `DriftException` carrying the report.

## Fixed-step driver

`FixedStepDriver(tickRate, maxStepsPerFrame = 5)`: `int Advance(TimeSpan frame)` → ticks due now (exact integer arithmetic: N ticks per N/rate seconds regardless of frame jitter); `Alpha` (0..1 interpolation factor for views); `Paused`; `RequestStep()` (one tick on the next `Advance` while paused); `TimeScale`; `DroppedTicks` (time discarded by the catch-up cap). `Cluster.Frame(dt)` = `Advance` + `Step`.

## Wire summary

First byte = type, then the scope id (varint). Client to authority: `Subscribe`, `Unsubscribe`, `Command` (sequence, entity, command id, argument bytes), `DigestRequest` (tick). Authority to client: `Snapshot` (tick + `World.WriteViewSnapshot` bytes), `Frame`, `Digest`, `CommandRefused` (sequence, entity, command id, reason), `CommandResult` (sequence, entity, command id, verdict, refusal, reason text), `SubscribeRefused`, `Revoked` (reason). A `Frame` is: tick, flags (bit 0 random streams follow, bit 1 view hash follows), [random streams], removed ids, added entities (type id, id, schema, **form**, state; form 0 = whole state, 1 = mask + the visible properties), changed entities (id, delta; masked to the visible properties for a filtered entity), changed visible hot tables (name, whole table), [view hash]. Hot tables are sent whole when their bytes change; row-level deltas are a later refinement. One frame per tick even when nothing changed (it carries the tick). A replica rejects a delta that touches a property outside the mask it was given.

## Things a consumer must know

- Start a recording only between ticks with no queued commands (it throws otherwise): snapshots do not hold the command queue.
- Replay needs a fresh authority world built the same way (registry, tables, systems in the same order) as the recorded one.
- The log records commands that entered from outside the tick. A game that sends commands to its own authority world from view code outside `Step` is recorded too; the sim's own `Send` calls inside a tick are not.
- A subscriber that connects late gets the current snapshot, then frames; entities it already has are replaced, so overlaps are harmless.
- A view reading a filtered property of someone else's entity sees the type's default, not an error. Views that need a value must read a member the audience is allowed to hold (analyzer OG0019 flags reads of `Hidden` members from view code).
- `Node.ConnectTo(server, trust, clientEnd, serverEnd)` takes the two channel ends, so tests can wrap them to record exact bytes and transports can supply their own.
- Change `Connection.Trust` between ticks (never from sim code): the snapshot a changed audience receives is the state at the end of the last tick.
- Steady state still allocates nothing per tick with several audiences; a command answer, a new audience and a trust change allocate.
- Steady state allocates nothing per tick (authority publish, loopback, replica apply, hash checks), provided no commands, entity adds or string changes happen in that tick.
- Changes made in `OneGame.Core` for this layer: hot-table `Read` decodes in place (a dry run first keeps it atomic) and `RngStreams.Read` updates matching streams in place, so replicas apply them without allocating; `World` gained internal hooks for replica tick, input logging and pending-command count (`OneGame.Core` has `InternalsVisibleTo(OneGame.Sync)`).

## Untrusted input

Transport messages, saves, recordings and digests are decoded with `SyncReader` and the `SyncLimits` in `OneGame.Core`; sizes are checked before allocation; no decoder throws on bad bytes. A replica that receives a bad frame or a tick gap files a `ProtocolError` on `Node.ProtocolErrors` and re-subscribes (stops after 3 consecutive failures: `ReplicaState.Failed`).

## Seams left for later units

Prediction (replica-side speculative commands; the replica already has `CommandResult` to confirm or roll back and the sequence numbers to match them), authority transfer and relaying (a replica's `ApplyDelta` marks entities dirty so a relay can forward, and `Trust.Own` is where a transfer would be checked), and real transports (implement `IMessageChannel`, hand the ends to `Node.ConnectTo`; fragmentation lives there). Not yet done: commands that wait for an answer have no timeout of their own (they stay in `PendingCommands` until answered, dropped or evicted), and a replica cannot tell that the authority stopped sending (no keepalive; the transport owns liveness).
