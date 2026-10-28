[API reference](../../index.md) › [OneGame.Navigation](../index.md) › OneGame.Navigation

# NavAgentOptions struct

**Namespace:** `OneGame.Navigation`  
**Package:** [`RecursiveAI.OneGame.Navigation`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`NavAgentOptions`](NavAgentOptions.md)&gt;

```csharp
public readonly struct NavAgentOptions : IEquatable<NavAgentOptions>
```

An agent's settings, in world units and units per second. They are saved with the agent.

## Properties

| Member | Description |
| --- | --- |
| [`ArrivalRadius`](#m-arrivalradius) | Distance from the destination within which the agent has arrived. |
| [`MaxAcceleration`](#m-maxacceleration) | Largest change of velocity per second. |
| [`MaxSpeed`](#m-maxspeed) | Top speed in world units per second. |
| [`Priority`](#m-priority) | Weight in avoidance: of two agents that must give way, the one with the greater priority gives way less. |
| [`Radius`](#m-radius) | Body radius used between agents (two agents keep their centres apart by the sum of their radii). |

### <a id="m-arrivalradius"></a>ArrivalRadius

```csharp
public Fixed ArrivalRadius { get; init; }
```

Distance from the destination within which the agent has arrived. Zero or more.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-maxacceleration"></a>MaxAcceleration

```csharp
public Fixed MaxAcceleration { get; init; }
```

Largest change of velocity per second. Above 0.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-maxspeed"></a>MaxSpeed

```csharp
public Fixed MaxSpeed { get; init; }
```

Top speed in world units per second. Above 0.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-priority"></a>Priority

```csharp
public Fixed Priority { get; init; }
```

Weight in avoidance: of two agents that must give way, the one with the greater priority gives way less. Above 0.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-radius"></a>Radius

```csharp
public Fixed Radius { get; init; }
```

Body radius used between agents (two agents keep their centres apart by the sum of their radii). Above 0.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)
