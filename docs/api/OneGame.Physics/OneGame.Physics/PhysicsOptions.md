[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# PhysicsOptions class

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)

```csharp
public sealed class PhysicsOptions
```

The settings of a [`PhysicsSpace2D`](PhysicsSpace2D.md). They are fixed when the space is created, and they must be the same on every node: build the space the same way everywhere (the same code in `Setup`), as for any other level data.

## Constructors

| Member | Description |
| --- | --- |
| [`PhysicsOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>PhysicsOptions()

```csharp
public PhysicsOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`ContactCapacity`](#m-contactcapacity) | Room for this many contacts (see [`PhysicsOptions.PairCapacity`](PhysicsOptions.md#m-paircapacity)). |
| [`Gravity`](#m-gravity) | Gravity in world units per second squared. |
| [`HitEvents`](#m-hitevents) | Whether [`PhysicsEventKind.ContactHit`](PhysicsEventKind.md#m-contacthit) events are recorded: contacts that approach faster than one unit per second. |
| [`IslandCapacity`](#m-islandcapacity) | Room for this many islands of touching bodies (see [`PhysicsOptions.PairCapacity`](PhysicsOptions.md#m-paircapacity)). |
| [`Name`](#m-name) | The table's name in snapshots; unique within a world. |
| [`PairCapacity`](#m-paircapacity) | Room for this many touching shape pairs, reserved up front so a step never grows its tables. |
| [`QueryScratchCapacity`](#m-queryscratchcapacity) | Hits a ray cast or overlap can return before its scratch grows. |
| [`Sleep`](#m-sleep) | Whether resting bodies fall asleep (cheaper; any contact or touch wakes them). |
| [`StayEvents`](#m-stayevents) | Whether [`PhysicsEventKind.ContactStay`](PhysicsEventKind.md#m-contactstay) events are recorded (one per touching contact per step). |
| [`Substeps`](#m-substeps) | Solver substeps per step (more is steadier stacking and costs more). |
| [`TicksPerSecond`](#m-tickspersecond) | The sim's ticks per second; one tick is one physics step of 1/TicksPerSecond seconds. |

### <a id="m-contactcapacity"></a>ContactCapacity

```csharp
public int ContactCapacity { get; set; }
```

Room for this many contacts (see [`PhysicsOptions.PairCapacity`](PhysicsOptions.md#m-paircapacity)). 0 sizes from the body count.

**Type:** `int`

### <a id="m-gravity"></a>Gravity

```csharp
public FVec2 Gravity { get; set; }
```

Gravity in world units per second squared. Default (0, -9.81), so one world unit is one metre; scale the world units the same way for the view.

**Type:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)

### <a id="m-hitevents"></a>HitEvents

```csharp
public bool HitEvents { get; set; }
```

Whether [`PhysicsEventKind.ContactHit`](PhysicsEventKind.md#m-contacthit) events are recorded: contacts that approach faster than one unit per second. Default false.

**Type:** `bool`

### <a id="m-islandcapacity"></a>IslandCapacity

```csharp
public int IslandCapacity { get; set; }
```

Room for this many islands of touching bodies (see [`PhysicsOptions.PairCapacity`](PhysicsOptions.md#m-paircapacity)). 0 sizes from the body count.

**Type:** `int`

### <a id="m-name"></a>Name

```csharp
public string Name { get; set; }
```

The table's name in snapshots; unique within a world. Default `physics`.

**Type:** `string`

### <a id="m-paircapacity"></a>PairCapacity

```csharp
public int PairCapacity { get; set; }
```

Room for this many touching shape pairs, reserved up front so a step never grows its tables. 0 sizes them from the body count when [`PhysicsSpace2D.Warmup()`](PhysicsSpace2D.md#m-warmup) runs. Never changes a result.

**Type:** `int`

### <a id="m-queryscratchcapacity"></a>QueryScratchCapacity

```csharp
public int QueryScratchCapacity { get; set; }
```

Hits a ray cast or overlap can return before its scratch grows. 0 keeps the library's default of 64.

**Type:** `int`

### <a id="m-sleep"></a>Sleep

```csharp
public bool Sleep { get; set; }
```

Whether resting bodies fall asleep (cheaper; any contact or touch wakes them). Default true.

**Type:** `bool`

### <a id="m-stayevents"></a>StayEvents

```csharp
public bool StayEvents { get; set; }
```

Whether [`PhysicsEventKind.ContactStay`](PhysicsEventKind.md#m-contactstay) events are recorded (one per touching contact per step). Default false.

**Type:** `bool`

### <a id="m-substeps"></a>Substeps

```csharp
public int Substeps { get; set; }
```

Solver substeps per step (more is steadier stacking and costs more). Default 4.

**Type:** `int`

### <a id="m-tickspersecond"></a>TicksPerSecond

```csharp
public int TicksPerSecond { get; set; }
```

The sim's ticks per second; one tick is one physics step of 1/TicksPerSecond seconds. Default 60. Changing it changes every result.

**Type:** `int`
