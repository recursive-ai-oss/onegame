[API reference](../../index.md) › [OneGame.Navigation](../index.md) › OneGame.Navigation

# NavAgent struct

**Namespace:** `OneGame.Navigation`  
**Package:** [`RecursiveAI.OneGame.Navigation`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`NavAgent`](NavAgent.md)&gt;

```csharp
public readonly struct NavAgent : IEquatable<NavAgent>
```

One agent of a [`NavCrowd`](NavCrowd.md) as sim code sees it: fixed-point positions only. A snapshot taken after the last tick, add or removal.

## Properties

| Member | Description |
| --- | --- |
| [`Destination`](#m-destination) | Where it is going (its own position when it has no destination). |
| [`Id`](#m-id) | The agent's number: 0, 1, 2 in the order agents were added, never reused. |
| [`Position`](#m-position) | Where the agent stands (always on the walkable ground). |
| [`Radius`](#m-radius) | Its body radius. |
| [`Status`](#m-status) | What it is doing. |
| [`Velocity`](#m-velocity) | How far and which way it moved in the last tick, in world units per second. |

### <a id="m-destination"></a>Destination

```csharp
public FVec2 Destination { get; init; }
```

Where it is going (its own position when it has no destination).

**Type:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)

### <a id="m-id"></a>Id

```csharp
public int Id { get; init; }
```

The agent's number: 0, 1, 2 in the order agents were added, never reused.

**Type:** `int`

### <a id="m-position"></a>Position

```csharp
public FVec2 Position { get; init; }
```

Where the agent stands (always on the walkable ground).

**Type:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)

### <a id="m-radius"></a>Radius

```csharp
public Fixed Radius { get; init; }
```

Its body radius.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-status"></a>Status

```csharp
public NavAgentStatus Status { get; init; }
```

What it is doing.

**Type:** [`NavAgentStatus`](NavAgentStatus.md)

### <a id="m-velocity"></a>Velocity

```csharp
public FVec2 Velocity { get; init; }
```

How far and which way it moved in the last tick, in world units per second.

**Type:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)
