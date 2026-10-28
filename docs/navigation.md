# Navigation

**Preview.** `OneGame.Navigation` is a prerelease package built on a prerelease of OnePath, and its API may change before it is stable. Add it with `dotnet add package RecursiveAI.OneGame.Navigation --prerelease`.

`OneGame.Navigation` finds paths on a grid for units whose positions are `Fixed` numbers, in a way that is the same on every node, keeps its state in the world (so saves, replays and replicas carry it), and spreads long searches over ticks. It is built on [OnePath](https://www.nuget.org/packages/RecursiveAI.OnePath): grids, A\*, jump point search, Theta\* and the repairing search come from there. Reference it from a game with `RecursiveAI.OneGame.Navigation` (in a checkout: a project reference to `src/OneGame.Navigation`). It is not part of the default game template or `Game.targets`: a game that wants paths adds it, and OnePath comes with it. It is published as a prerelease (`<engine version>-preview.N`) because OnePath is one; the other engine packages stay stable and never depend on it. The example `examples/pathing` shows all of it in one file.

```csharp
using OneGame.Navigation;
```

Everything you call from sim code takes and returns onegame types (`Fixed`, `FVec2`, `NavCell`), and the assembly is marked `[SimSafe]`, so the sim analyzers accept it. You never write a `double`.

## The map: `NavGrid`

```csharp
var map = NavGrid.FromText("""
    ........
    .######.
    .#....#.
    .#.##.#.
    ...#....
    """, new NavSpace(new FVec2(-160, -88), Fixed.FromInt(16)));   // top-left corner and cell size in world units

map.SetBlocked(3, 2, true);          // edit from sim code only
map.SetAreaCost(1, 3);               // area 1 costs three times open ground
map.TryGetCell(position, out var cell);
FVec2 centre = map.CellCenter(cell); // exact: origin + cell * size + size / 2, in integers
```

`.` is open, `#` a wall, `1` to `9` a costed area. The grid is level data: build it the same way on every node, from the level and from synced state (the example's `Terrain` entity owns its world's map and rebuilds it from a synced list of walls). Editing it outside the tick, or differently on two nodes, is the same bug as any other desync.

**One-shot search.** `map.FindPath(from, to, options)` runs a whole search in the call and returns a `NavPath` (`Status`, `Cost`, `Cells`, `CopyPoints`). Use it for tools, level checks and anything that is not a per-tick unit. `NavOptions` sets the moves (`Eight` without corner cutting, `Four`, `EightNoSqueeze`), the unit's width in cells and, for one-shot searches, `AStar`, `Jps` or `ThetaStar`.

## A unit's navigation: `NavState`, `NavSearcher`, `Follow`

`NavState` is a synced value: what the unit asked for, how far the search got and the path. `NavSearcher` runs the search a few iterations per tick and is a cache you keep in a non-synced field.

```csharp
[Entity]
public partial class Unit : Entity
{
    [Synced] public partial FVec2 Position { get; private set; }
    [Synced] public partial NavState Nav { get; private set; }

    private readonly NavSearcher _searcher = new();

    [Command]
    public void GoTo(int x, int y) =>
        Nav = _searcher.Begin(Map, Position, Map.CellCenter(new NavCell(x, y)));

    protected override void OnTick()
    {
        var nav = Nav;
        if (_searcher.Advance(ref nav, Map, budget: 200)) { Nav = nav; }   // search a little; store only when it changed
        if (nav.Status == NavStatus.Found)
        {
            var p = Position;
            Nav = nav.Follow(Map, ref p, Fixed.FromInt(2));                // walk 2 units, turn at waypoints, Arrived at the end
            Position = p;
        }
    }
}
```

- **Statuses:** `Idle`, `Searching`, `Found`, `NoPath`, `StartInvalid`, `GoalInvalid`, `Arrived`. A unit wider than one cell is placed by its top-left cell; a goal too close to the edge for its width is `GoalInvalid`.
- **Budget.** `Advance(ref state, map, budget)` spends at most `budget` search iterations (queue pops; counted, never timed). Any split of the same total gives the same search, so the budget changes when the path arrives, never which path it is. Ticks that stay in the search allocate nothing; the tick that finishes copies the path out once; walking and idle ticks allocate nothing.
- **The map changed.** `NavState` remembers `NavGrid.Stamp`, OnePath's `Grid.ContentHash` (the dimensions, every cell and area cost). When it differs, a `Found`, `NoPath` or `Searching` state restarts from `Start`, which follows the unit as it passes waypoints. This also catches a change made before a restore. A state saved by 1.12 holds the hash that version computed itself, which never equals this one, so a `Found`, `NoPath` or `Searching` state from such a save loads fine and searches again from where the unit stands (the path is the same one, found again; nothing fails and nothing is lost).
- **Save, restore, replicas, drift.** Because `Nav` is synced state it goes through snapshots, deltas and the world hash like any property. A restore needs nothing else: a search in progress is stored as its start, goal and iterations spent, and the searcher rebuilds the identical search by replaying those iterations when it meets a state it does not recognise (`NavSearcher.Rebuilds` counts that). A replica shows the path and never searches.
- **Hostile saves.** `NavState.Read` rejects impossible values (it never throws), and `Advance` and `Follow` drop a state whose cells do not fit the grid (`StartInvalid`, `GoalInvalid`, `NoPath`) rather than reading outside it.
- **Memory.** A searcher works on a private copy of the map (one byte per cell) made when a search starts, so edits mid-search cannot give a path through half a change, and there is nothing to dispose. Many units searching at once on a very large map cost that much each; for crowds of units heading for one place, a flow field (`NavGrid.Grid.CreateFlowField`, OnePath) is the better tool, and is not wrapped yet.

## Range

Positions are limited to ±2^21 (2,097,152) world units, so every `Fixed` in range is exactly a double (32 + 21 bits fit in 53) and the search library gives the same answer everywhere. A position or grid outside that range throws `NavRangeException` with a message saying so; nothing is clamped or rounded silently. `NavConvert.ToDouble` and `FromDouble` are the checked conversions for the rare place a double is needed (`FromDouble` rounds to the nearest `Fixed`, ties to even). Cell centres and cell lookups use integer arithmetic only. A `Cost` over 2^21 is capped at 2^21.

## Seeing it

While the debug overlay is on (F3, or `overlay: on` in a scenario) `Draw2D.DebugVisible` is true, and a view can collect `NavDebugShapes` (`AddGrid`, `AddPath`) and draw the rectangles and lines it holds with the 2D calls (the example does, in 20 lines). Release builds never set the flag. `state_query` shows any unit's `Nav` as JSON (`status`, `start`, `goal`, `spent`, `next`, `path`).

## Crowds: `NavCrowd`

A crowd is many units walking at once with avoidance of walls and of each other: OnePath's crowd on a navigation mesh built from the grid's walls, wrapped as a table of the world.

```csharp
// Where the world is built, on every node, in the same order (GameDefinition.RegisterSystems):
NavCrowd.AddTo(world, map, new NavCrowdOptions { AgentRadius = Fixed.FromInt(3), MaxAgents = 64 });

// In a command handler (or OnTick) of any entity:
var crowd = NavCrowd.Of(World!)!;
if (crowd.TryAdd(spawn, out var id)) { crowd.SetDestination(id, goal); }   // crowd.Remove(id), crowd.Stop(id)

// In a view or in sim code: fixed-point values, no allocation.
foreach (var a in crowd.Agents) { /* a.Id, a.Position, a.Velocity, a.Destination, a.Status, a.Radius */ }
```

`examples/crowd` is twelve people squeezing through a gap, in about 130 lines.

**What is synced, and how.** The crowd is one hot table, `og.nav.crowd` (`NavCrowd.TableName`). Its whole state is the table: every agent's position, velocity, settings, destination, path corridor and avoidance memory, the agent numbers and the step counter, written in OnePath's own byte-exact format. Everything that holds a world holds it: snapshots and saves, the command-log replay, the world hash and the per-part drift digest (the table is named in drift reports), predicted worlds (`CopyFrom`), and replicas. There is no second copy: `NavCrowd.Agents` is a cache rebuilt from that state after every step, add, removal and restore. The mesh is not state; it is built from the `NavGrid`, which is level data like any map, so every node must call `AddTo` with the same map and options (if you change walls at run time, the crowd does not notice: its mesh is the map as it was when `AddTo` ran).

**The tick.** `AddTo` also registers a system that steps the crowd once per world tick, after the entities, by 1/`TicksPerSecond` seconds (60 by default; the step is one IEEE division, the same everywhere). Commands run before the tick, so a destination set by a command is walked in that same tick. Agents are added, removed and sent through `TryAdd`, `Remove`, `SetDestination` and `Stop` (call them from commands: on a replica they throw `ReplicaWriteException`). `TryAdd` puts the agent on the nearest walkable ground and returns false when the crowd is full or there is none; agent numbers count up from 0 and are never reused.

**Fixed-point rule.** OnePath keeps positions as doubles within ±2^21 and its results are bit-identical on every platform (IEEE `+ - * /` and `sqrt` only, its own transcendental functions, fixed reduction order). Positions, velocities and destinations that sim code reads are converted by `NavConvert.FromDouble`: one exact scaling by 2^32 and a round to nearest, ties to even, so the same bits give the same `Fixed` everywhere. Going the other way (`TryAdd`, `SetDestination`, agent settings) is the checked, exact `NavConvert.ToDouble`; a value beyond ±2^21 throws `NavRangeException`. The mesh, the crowd and the converted views agree across platforms: the known-answer test `ACrossingHasTheSameStateHashesOnEveryPlatform` pins the hashes of a 600-tick crossing, and the same values came out of linux-arm64 (under qemu) as on x64.

**Cost and traffic.** A step and a world hash allocate nothing once the buffers have grown (the world's hash buffer grows once while the crowd's state grows, then stays). A replica receives the whole table in any frame in which it changed, which for a walking crowd is every tick and a few hundred bytes per agent, so a crowd of fifty is tens of kilobytes per tick for each subscriber. If the clients do not need the agents (a server-side crowd of monsters whose positions reach clients another way), pass `Visibility = Visibility.Hidden`: the table then stays on its authority, and replicas see an empty crowd. Row-level deltas would shrink the traffic and are not built.

**Hostile bytes.** `Read` takes untrusted bytes: the size is limited before anything is read (16 KiB per agent allowed), OnePath's reader checks every count and range and restores all or nothing, and a bad state makes the snapshot fail with `SyncError.InvalidValue` instead of throwing. Positions inside an accepted state are not checked against the mesh; the steps pull stray agents back onto it.

**Limits today.** One agent class per crowd (the mesh is shrunk by `AgentRadius`; an agent may have a smaller radius for avoidance). No editing of the mesh at run time (OnePath's tile cache could do it; it is not wrapped), no off-mesh links, no per-agent filters.

## Not covered yet

Hex and voxel grids, flow fields and navigation meshes of your own (outlines, 3D) have no wrapper here; reach them through `NavGrid.Grid` and OnePath's own API outside sim code, or ask for the wrapper.
