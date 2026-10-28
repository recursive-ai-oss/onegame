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
| [`Gravity`](#m-gravity) | Gravity in world units per second squared. |
| [`HitEvents`](#m-hitevents) | Whether [`PhysicsEventKind.ContactHit`](PhysicsEventKind.md#m-contacthit) events are recorded: contacts that approach faster than one unit per second. |
| [`Name`](#m-name) | The table's name in snapshots; unique within a world. |
| [`Sleep`](#m-sleep) | Whether resting bodies fall asleep (cheaper; any contact or touch wakes them). |
| [`StayEvents`](#m-stayevents) | Whether [`PhysicsEventKind.ContactStay`](PhysicsEventKind.md#m-contactstay) events are recorded (one per touching contact per step). |
| [`Substeps`](#m-substeps) | Solver substeps per step (more is steadier stacking and costs more). |
| [`TicksPerSecond`](#m-tickspersecond) | The sim's ticks per second; one tick is one physics step of 1/TicksPerSecond seconds. |

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

### <a id="m-name"></a>Name

```csharp
public string Name { get; set; }
```

The table's name in snapshots; unique within a world. Default `physics`.

**Type:** `string`

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
