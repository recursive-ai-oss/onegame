# One-shot sim events

Some things happen once: a wall is completed, a ball hits a paddle, a life is lost. Sound, particles, a camera shake and a test all want to react to each of them exactly once. Keeping the last few in a synced list and remembering a sequence number in every view works until a restore, a replay, a late join or an overflow gets in the way. Sim events are that list, done once, in the engine.

```csharp
[SimEvent]                                         // a struct of synced field types, like [SyncedStruct]
public partial struct WallDone { public int Cell { get; set; } }

[SimEvent(Visibility = Visibility.Owner)]          // only the emitting entity's owner receives it
public partial struct Bonus { public int Points { get; set; } }

[Entity]
public partial class Builder : Entity
{
    [Synced, OwnerKey] public partial uint Player { get; set; }

    [Command]
    public void Build(int cell)
    {
        Emit(new WallDone { Cell = cell });         // sim code: a command handler, OnTick, a system (world.Emit)
    }
}
```

A view makes **one reader** per consumer and drains it each frame:

```csharp
var events = game.World.Events.NewReader();         // starts at the end: only events from now on
// every frame:
while (events.Next<WallDone>(out var done, out var info))
{
    PlayPop(done.Cell);                             // info.Tick is the tick it happened on
}
```

`events.Next(out var e)` takes events of every type in order (`e.TryRead(out WallDone w)`, `e.Is<Bonus>()`, `e.Tick`, `e.Entity`). `Next<T>` passes over events of other types and consumes them, so give the audio code, the particle code and the HUD a reader each. A runnable game is in `examples/events`.

## What you can count on

- **Once, in order.** A reader gets every event after the moment it was made exactly once, in the order the sim emitted them (entities in id order, then systems), each with the tick it happened on. The tick is the one the sim was on when it emitted: the same number a command that ran then reports.
- **Emitting is sim code.** `Emit` is a state change like setting a property: from a `[View]` member it is a compile error (OG0008), and a payload with a float is OG0001. Payloads are the types a `[SyncedStruct]` may hold, at most 512 bytes when encoded (`EventLog.MaxPayloadBytes`; bigger throws, so send big data as state and a small event that points at it).
- **Who sees what.** An event emitted with `entity.Emit` reaches exactly the subscribers that may see that entity: its visibility, owner and team, and interest set. `[SimEvent(Visibility = ...)]` narrows it further: `Owner` and `Team` match the emitting entity's `[OwnerKey]`/`[TeamKey]` (a trusted relay audience counts as both), `Hidden` never leaves the authority. An event emitted with `world.Emit` (from a system) has no entity and reaches every subscriber that may read the scope; it can be `Everyone` or `Hidden`. What a connection may not see is not in its bytes at all. A subscriber receives events only after its snapshot: a late join, a reconnect and a resync start with an empty log.
- **Restore, load and replay.** Events are the output of a tick, not state. They are not in saves or snapshots and not in any state hash. Restoring a snapshot or loading a save empties the log, so nothing old fires again; a reader notices (`EventReader.Restarts`) and carries on from the end. A replay re-runs the sim and emits the same stream as the run it replays (`EventLog.StreamHash` is equal), and two runs of one scenario emit the same stream. A game whose world is replaced (a loaded slot, a reset) makes its readers again from the new world's log (`GameInstance.WorldReplaced`, or compare `game.World`, as the example does).
- **Drift.** The view hash does not include events, because they cannot differ when state does not: they are emitted by the sim from state, and a replica is sent exactly the ones its audience may see, over the same ordered channel as the frame of that tick. Tests that want to compare two streams use `EventLog.StreamHash` or compare the lists.
- **Bounds.** A log keeps the newest 512 events (`EventLog.Capacity`). A tick may emit 128 events by default (`EventLog.EventsPerTickLimit`, up to 512); the rest of that tick are dropped, counted in `EventLog.Dropped`, and the same ones are dropped on every run. A reader that falls more than 512 behind loses the oldest and counts them in `Missed`. Steady state allocates nothing once each slot of the ring has been used once (decoding a string field in a reader allocates the string).

## Limits of the first version

- **Prediction.** A predicted world (`Prediction.World`) records no events, because it re-runs ticks after a correction and would fire twice. Read events from the confirmed replica world (`ReplicaScope.World.Events`); they arrive when the authority's tick does. A view that wants an instant effect for the player's own action draws it from the predicted state.
- **Relays.** A relay node keeps the events it received in its own world's log for its own views, but does not pass them on to its subscribers.
- **Outside a tick.** An event emitted from test or setup code between ticks is delivered with the next frame.

## Harness

- **Scenarios.** `event: { name: WallDone, count: 2, where: { Cell: 12 } }` counts the events of a type emitted since the scenario began (`count`, `min` or `max`; `min: 1` when none is given), optionally only those whose payload fields have the given values. A reset or a loaded slot inside a scenario starts a new log, and its events count.
- **Live control.** `sim_events` lists the log as JSON: `{world, tick, next, first, dropped, hash, events: [{sequence, tick, entity, type, data}]}`. `since` takes the previous call's `next`; `max` caps the list. On a game connected to a server it lists what that audience received.
- **Tests.** `world.Events` is public on authority and replica worlds (`TestCluster` clients: `client.Game.World.Events`): `NewReaderFromStart()`, `TryGet(sequence, out var e)`, `Count`, `StreamHash`.
- **Wire.** Events travel in their own message after the tick's frame (see `docs/interfaces/sync.md`); a scope that emits none sends none, so its traffic is byte-identical to before.
