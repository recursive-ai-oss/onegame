[API reference](../../index.md) › [OneGame.Navigation](../index.md) › OneGame.Navigation

# NavCrowdOptions struct

**Namespace:** `OneGame.Navigation`  
**Package:** [`RecursiveAI.OneGame.Navigation`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`NavCrowdOptions`](NavCrowdOptions.md)&gt;

```csharp
public readonly struct NavCrowdOptions : IEquatable<NavCrowdOptions>
```

Settings of a [`NavCrowd`](NavCrowd.md). They are level data: build the crowd with the same ones on every node.

## Properties

| Member | Description |
| --- | --- |
| [`AgentRadius`](#m-agentradius) | Radius the walkable ground is shrunk by, so agents up to this radius clear every wall. |
| [`MaxAgents`](#m-maxagents) | Most agents at once; 0 means 256. |
| [`TicksPerSecond`](#m-tickspersecond) | Ticks per second of the simulation: every tick is one step of 1/TicksPerSecond seconds. |
| [`Visibility`](#m-visibility) | Whether subscribers receive the crowd. |

### <a id="m-agentradius"></a>AgentRadius

```csharp
public Fixed AgentRadius { get; init; }
```

Radius the walkable ground is shrunk by, so agents up to this radius clear every wall. Zero means a quarter of a cell.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-maxagents"></a>MaxAgents

```csharp
public int MaxAgents { get; init; }
```

Most agents at once; 0 means 256. At most 65,536.

**Type:** `int`

### <a id="m-tickspersecond"></a>TicksPerSecond

```csharp
public int TicksPerSecond { get; init; }
```

Ticks per second of the simulation: every tick is one step of 1/TicksPerSecond seconds. 0 means 60.

**Type:** `int`

### <a id="m-visibility"></a>Visibility

```csharp
public Visibility Visibility { get; init; }
```

Whether subscribers receive the crowd. [`Visibility.Everyone`](../../OneGame.Core/OneGame.Core/Visibility.md#m-everyone) (the default) sends the whole state, a few hundred bytes per agent, in every frame in which it changed; [`Visibility.Hidden`](../../OneGame.Core/OneGame.Core/Visibility.md#m-hidden) keeps it on the authority.

**Type:** [`Visibility`](../../OneGame.Core/OneGame.Core/Visibility.md)
