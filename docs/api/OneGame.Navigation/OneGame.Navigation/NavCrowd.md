[API reference](../../index.md) › [OneGame.Navigation](../index.md) › OneGame.Navigation

# NavCrowd class

**Namespace:** `OneGame.Navigation`  
**Package:** [`RecursiveAI.OneGame.Navigation`](../index.md)  
**Implements:** [`IHotTable`](../../OneGame.Core/OneGame.Core/IHotTable.md)

```csharp
public sealed class NavCrowd : IHotTable
```

A crowd of agents walking a navigation mesh built from a [`NavGrid`](NavGrid.md), with avoidance of walls and of each other, kept in the world as a hot table so that saves, replays, replicas and the world hash carry it. The simulation steps it once per tick (it is also stepped by a system the world runs); agents join, leave and get destinations through [`NavCrowd.TryAdd()`](NavCrowd.md#m-tryadd-fvec2-int), [`NavCrowd.Remove()`](NavCrowd.md#m-remove-int) and [`NavCrowd.SetDestination()`](NavCrowd.md#m-setdestination-int-fvec2), which a command handler calls. Sim code sees only [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md) and [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md).

```csharp
// while building the world, on every node, in the same order:
var crowd = NavCrowd.AddTo(world, map, new NavCrowdOptions { AgentRadius = Fixed.FromInt(3) });

// in a command handler or OnTick:
if (NavCrowd.Of(World!)!.TryAdd(spawn, out var id)) { NavCrowd.Of(World!)!.SetDestination(id, goal); }
```

What is synced: the crowd's complete state (every agent's position, velocity, settings, path and avoidance memory) is the table, written with OnePath's byte-exact format and hashed with the world. The mesh is not state: it is built from the map, which is level data every node builds the same way, so a crowd must be created with the same map and options everywhere. The views of the agents ([`NavCrowd.Agents`](NavCrowd.md#m-agents)) are a cache rebuilt from that state.

## Fields

| Member | Description |
| --- | --- |
| [`TableName`](#m-tablename) | The default table name in snapshots. |

### <a id="m-tablename"></a>TableName

```csharp
public const string TableName = "og.nav.crowd";
```

The default table name in snapshots.

**Type:** `string`

## Properties

| Member | Description |
| --- | --- |
| [`AgentRadius`](#m-agentradius) | The radius the walkable ground was shrunk by. |
| [`Agents`](#m-agents) | The agents in slot order, as of the last tick, add, removal or restore. |
| [`Count`](#m-count) | The number of rows. |
| [`DefaultAgent`](#m-defaultagent) | Settings used by [`NavCrowd.TryAdd()`](NavCrowd.md#m-tryadd-fvec2-int): the mesh radius, three cells per second, arrival within half a radius. |
| [`Map`](#m-map) | The map the mesh was built from. |
| [`MaxAgents`](#m-maxagents) | Most agents at once. |
| [`Name`](#m-name) | The table's name in snapshots; unique within a world. |
| [`Options`](#m-options) | The settings the crowd was made with. |
| [`StepCount`](#m-stepcount) | Steps taken since the crowd was made or last restored from a state (part of the state). |
| [`Visibility`](#m-visibility) | Whether subscribers receive the table: [`Visibility.Everyone`](../../OneGame.Core/OneGame.Core/Visibility.md#m-everyone) or [`Visibility.Hidden`](../../OneGame.Core/OneGame.Core/Visibility.md#m-hidden). |

### <a id="m-agentradius"></a>AgentRadius

```csharp
public Fixed AgentRadius { get; }
```

The radius the walkable ground was shrunk by.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-agents"></a>Agents

```csharp
public ReadOnlySpan<NavAgent> Agents { get; }
```

The agents in slot order, as of the last tick, add, removal or restore. Nothing is allocated reading it.

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`NavAgent`](NavAgent.md)&gt;

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

The number of rows.

**Type:** `int`

### <a id="m-defaultagent"></a>DefaultAgent

```csharp
public NavAgentOptions DefaultAgent { get; }
```

Settings used by [`NavCrowd.TryAdd()`](NavCrowd.md#m-tryadd-fvec2-int): the mesh radius, three cells per second, arrival within half a radius.

**Type:** [`NavAgentOptions`](NavAgentOptions.md)

### <a id="m-map"></a>Map

```csharp
public NavGrid Map { get; }
```

The map the mesh was built from.

**Type:** [`NavGrid`](NavGrid.md)

### <a id="m-maxagents"></a>MaxAgents

```csharp
public int MaxAgents { get; }
```

Most agents at once.

**Type:** `int`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The table's name in snapshots; unique within a world.

**Type:** `string`

### <a id="m-options"></a>Options

```csharp
public NavCrowdOptions Options { get; }
```

The settings the crowd was made with.

**Type:** [`NavCrowdOptions`](NavCrowdOptions.md)

### <a id="m-stepcount"></a>StepCount

```csharp
public long StepCount { get; }
```

Steps taken since the crowd was made or last restored from a state (part of the state).

**Type:** `long`

### <a id="m-visibility"></a>Visibility

```csharp
public Visibility Visibility { get; }
```

Whether subscribers receive the table: [`Visibility.Everyone`](../../OneGame.Core/OneGame.Core/Visibility.md#m-everyone) or [`Visibility.Hidden`](../../OneGame.Core/OneGame.Core/Visibility.md#m-hidden).

**Type:** [`Visibility`](../../OneGame.Core/OneGame.Core/Visibility.md)

## Methods

| Member | Description |
| --- | --- |
| [`AddTo(World, NavGrid, NavCrowdOptions, string)`](#m-addto-world-navgrid-navcrowdoptions-string) | Makes a crowd over a map and adds it to a world as a table and as a system (it steps after the entities, once per tick). |
| [`Clear()`](#m-clear) | Removes every row. |
| [`CopyFrom(IHotTable)`](#m-copyfrom-ihottable) | Makes this table hold exactly the rows of `source`, a table of the same type. |
| [`Of(World, string)`](#m-of-world-string) | The world's crowd with this name, or null. |
| [`Read(ref SyncReader)`](#m-read-syncreader) | Replaces all rows. |
| [`Remove(int)`](#m-remove-int) | Takes an agent out of the crowd. |
| [`SetDestination(int, FVec2)`](#m-setdestination-int-fvec2) | Sends an agent to a position; a spot off the walkable ground means the nearest walkable point. |
| [`StateHash()`](#m-statehash) | OnePath's 64-bit hash of the crowd state (FNV-1a of the saved bytes): equal on every node and platform when the states are equal. |
| [`Step()`](#m-step) | Steps the crowd once. |
| [`Stop(int)`](#m-stop-int) | Stops an agent where it stands (other agents can still push it). |
| [`TryAdd(FVec2, out int)`](#m-tryadd-fvec2-int) | Adds an agent with [`NavCrowd.DefaultAgent`](NavCrowd.md#m-defaultagent) at the walkable point nearest to a position. |
| [`TryAdd(FVec2, NavAgentOptions, out int)`](#m-tryadd-fvec2-navagentoptions-int) | Adds an agent at the walkable point nearest to a position. |
| [`TryGet(int, out NavAgent)`](#m-tryget-int-navagent) | Finds an agent by number. |
| [`TrySetFromText(int, string, string)`](#m-trysetfromtext-int-string-string) | Sets one scalar cell from text (numbers, bool, string, Fixed, enums by number), for the harness. |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes all rows. |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the rows as a JSON array of objects. |

### <a id="m-addto-world-navgrid-navcrowdoptions-string"></a>AddTo(World, NavGrid, NavCrowdOptions, string)

```csharp
public static NavCrowd AddTo(World world, NavGrid map, NavCrowdOptions options = default, string name = "og.nav.crowd");
```

Makes a crowd over a map and adds it to a world as a table and as a system (it steps after the entities, once per tick).

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): The world; add the crowd before any save is loaded, in the same order on every node.
- `map` ([`NavGrid`](NavGrid.md)): The map. Its walls become the mesh; later edits do not change the mesh.
- `options` ([`NavCrowdOptions`](NavCrowdOptions.md)): Settings; the same on every node.
- `name` (`string`): The table name; give each crowd of a world its own.

**Returns:** [`NavCrowd`](NavCrowd.md) — The crowd.

**Exceptions**

- [`NavRangeException`](NavRangeException.md): The map reaches beyond ±2^21 units.
- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): An option is out of range.

### <a id="m-clear"></a>Clear()

```csharp
public void Clear();
```

Removes every row.

### <a id="m-copyfrom-ihottable"></a>CopyFrom(IHotTable)

```csharp
public void CopyFrom(IHotTable source);
```

Makes this table hold exactly the rows of `source`, a table of the same type. Generated tables copy the columns in place without allocating once they have room; the default goes through the encoded form.

**Parameters**

- `source` ([`IHotTable`](../../OneGame.Core/OneGame.Core/IHotTable.md))

### <a id="m-of-world-string"></a>Of(World, string)

```csharp
public static NavCrowd? Of(World world, string name = "og.nav.crowd");
```

The world's crowd with this name, or null.

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): The world.
- `name` (`string`): The table name.

**Returns:** [`NavCrowd`](NavCrowd.md)?

### <a id="m-read-syncreader"></a>Read(ref SyncReader)

```csharp
public void Read(ref SyncReader reader);
```

Replaces all rows. All rows are decoded before any is stored, so an error leaves the table unchanged.

**Parameters**

- `reader` ([`SyncReader`](../../OneGame.Core/OneGame.Core/SyncReader.md))

### <a id="m-remove-int"></a>Remove(int)

```csharp
public bool Remove(int id);
```

Takes an agent out of the crowd.

**Parameters**

- `id` (`int`): The agent's number.

**Returns:** `bool` — False when there is no such agent.

**Exceptions**

- [`ReplicaWriteException`](../../OneGame.Core/OneGame.Core/ReplicaWriteException.md): The world is a replica.

### <a id="m-setdestination-int-fvec2"></a>SetDestination(int, FVec2)

```csharp
public bool SetDestination(int id, FVec2 destination);
```

Sends an agent to a position; a spot off the walkable ground means the nearest walkable point.

**Parameters**

- `id` (`int`): The agent's number.
- `destination` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): Where to go.

**Returns:** `bool` — False when there is no such agent.

**Exceptions**

- [`NavRangeException`](NavRangeException.md): The position is beyond ±2^21 units.
- [`ReplicaWriteException`](../../OneGame.Core/OneGame.Core/ReplicaWriteException.md): The world is a replica.

### <a id="m-statehash"></a>StateHash()

```csharp
public ulong StateHash();
```

OnePath's 64-bit hash of the crowd state (FNV-1a of the saved bytes): equal on every node and platform when the states are equal.

**Returns:** `ulong`

### <a id="m-step"></a>Step()

```csharp
public void Step();
```

Steps the crowd once. The world does this every tick; call it yourself only for a crowd that is not in a world.

### <a id="m-stop-int"></a>Stop(int)

```csharp
public bool Stop(int id);
```

Stops an agent where it stands (other agents can still push it).

**Parameters**

- `id` (`int`): The agent's number.

**Returns:** `bool` — False when there is no such agent.

**Exceptions**

- [`ReplicaWriteException`](../../OneGame.Core/OneGame.Core/ReplicaWriteException.md): The world is a replica.

### <a id="m-tryadd-fvec2-int"></a>TryAdd(FVec2, out int)

```csharp
public bool TryAdd(FVec2 position, out int id);
```

Adds an agent with [`NavCrowd.DefaultAgent`](NavCrowd.md#m-defaultagent) at the walkable point nearest to a position.

**Parameters**

- `position` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): Where to put it.
- `id` (`int`): The agent's number.

**Returns:** `bool` — False when the crowd is full or the map has no walkable ground.

**Exceptions**

- [`NavRangeException`](NavRangeException.md): The position is beyond ±2^21 units.
- [`ReplicaWriteException`](../../OneGame.Core/OneGame.Core/ReplicaWriteException.md): The world is a replica.

### <a id="m-tryadd-fvec2-navagentoptions-int"></a>TryAdd(FVec2, NavAgentOptions, out int)

```csharp
public bool TryAdd(FVec2 position, NavAgentOptions options, out int id);
```

Adds an agent at the walkable point nearest to a position.

**Parameters**

- `position` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): Where to put it.
- `options` ([`NavAgentOptions`](NavAgentOptions.md)): Its settings.
- `id` (`int`): The agent's number.

**Returns:** `bool` — False when the crowd is full or the map has no walkable ground.

**Exceptions**

- [`NavRangeException`](NavRangeException.md): The position is beyond ±2^21 units.
- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): A setting is out of range.
- [`ReplicaWriteException`](../../OneGame.Core/OneGame.Core/ReplicaWriteException.md): The world is a replica.

### <a id="m-tryget-int-navagent"></a>TryGet(int, out NavAgent)

```csharp
public bool TryGet(int id, out NavAgent agent);
```

Finds an agent by number.

**Parameters**

- `id` (`int`): The agent's number.
- `agent` ([`NavAgent`](NavAgent.md)): Its view.

**Returns:** `bool` — False when there is no such agent.

### <a id="m-trysetfromtext-int-string-string"></a>TrySetFromText(int, string, string)

```csharp
public bool TrySetFromText(int row, string column, string text);
```

Sets one scalar cell from text (numbers, bool, string, Fixed, enums by number), for the harness. The change bypasses commands.

**Parameters**

- `row` (`int`): The row index.
- `column` (`string`): The column (field) name.
- `text` (`string`): The new value.

**Returns:** `bool` — False when the row or column does not exist, the column has no text form, or the text does not parse. Generated tables implement this; a hand-written table does not support it.

### <a id="m-write-syncwriter"></a>Write(SyncWriter)

```csharp
public void Write(SyncWriter writer);
```

Writes all rows.

**Parameters**

- `writer` ([`SyncWriter`](../../OneGame.Core/OneGame.Core/SyncWriter.md))

### <a id="m-writejson-jsonout"></a>WriteJson(JsonOut)

```csharp
public void WriteJson(JsonOut json);
```

Writes the rows as a JSON array of objects.

**Parameters**

- `json` ([`JsonOut`](../../OneGame.Core/OneGame.Core/JsonOut.md))
