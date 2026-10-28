[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# NetworkConditions class

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)

```csharp
public sealed class NetworkConditions
```

What a bad network does to traffic. Applied to what a conditioned endpoint sends, so wrapping both ends doubles the latency.

## Constructors

| Member | Description |
| --- | --- |
| [`NetworkConditions()`](#m-ctor) |  |

### <a id="m-ctor"></a>NetworkConditions()

```csharp
public NetworkConditions();
```

## Properties

| Member | Description |
| --- | --- |
| [`BandwidthBytesPerSecond`](#m-bandwidthbytespersecond) | Link speed in bytes per second; 0 is unlimited. |
| [`DuplicateRate`](#m-duplicaterate) | Chance (0 to 1) that a datagram arrives twice. |
| [`JitterMs`](#m-jitterms) | Each delay varies uniformly by up to this much either way. |
| [`LatencyMs`](#m-latencyms) | Delay added to every datagram or message, in milliseconds. |
| [`LossRate`](#m-lossrate) | Chance (0 to 1) that a datagram vanishes. |
| [`QueueLimitMs`](#m-queuelimitms) | The most a datagram may queue behind the bandwidth cap before it is dropped. |
| [`ReorderDelayMs`](#m-reorderdelayms) | Extra delay of a reordered datagram, in milliseconds. |
| [`ReorderRate`](#m-reorderrate) | Chance (0 to 1) that a datagram is held back by [`NetworkConditions.ReorderDelayMs`](NetworkConditions.md#m-reorderdelayms) so later ones overtake it. |
| [`Seed`](#m-seed) | Seed of the random choices, so a run can be repeated. |

### <a id="m-bandwidthbytespersecond"></a>BandwidthBytesPerSecond

```csharp
public int BandwidthBytesPerSecond { get; init; }
```

Link speed in bytes per second; 0 is unlimited. Traffic queues behind it, and datagrams that would wait longer than [`NetworkConditions.QueueLimitMs`](NetworkConditions.md#m-queuelimitms) are dropped.

**Type:** `int`

### <a id="m-duplicaterate"></a>DuplicateRate

```csharp
public double DuplicateRate { get; init; }
```

Chance (0 to 1) that a datagram arrives twice. Streams ignore it: TCP discards duplicates.

**Type:** `double`

### <a id="m-jitterms"></a>JitterMs

```csharp
public int JitterMs { get; init; }
```

Each delay varies uniformly by up to this much either way.

**Type:** `int`

### <a id="m-latencyms"></a>LatencyMs

```csharp
public int LatencyMs { get; init; }
```

Delay added to every datagram or message, in milliseconds.

**Type:** `int`

### <a id="m-lossrate"></a>LossRate

```csharp
public double LossRate { get; init; }
```

Chance (0 to 1) that a datagram vanishes. On a message stream, which is reliable, a "lost" message instead waits for a retransmission timeout, as TCP would.

**Type:** `double`

### <a id="m-queuelimitms"></a>QueueLimitMs

```csharp
public int QueueLimitMs { get; init; }
```

The most a datagram may queue behind the bandwidth cap before it is dropped.

**Type:** `int`

### <a id="m-reorderdelayms"></a>ReorderDelayMs

```csharp
public int ReorderDelayMs { get; init; }
```

Extra delay of a reordered datagram, in milliseconds.

**Type:** `int`

### <a id="m-reorderrate"></a>ReorderRate

```csharp
public double ReorderRate { get; init; }
```

Chance (0 to 1) that a datagram is held back by [`NetworkConditions.ReorderDelayMs`](NetworkConditions.md#m-reorderdelayms) so later ones overtake it. Streams ignore it: TCP keeps order.

**Type:** `double`

### <a id="m-seed"></a>Seed

```csharp
public int Seed { get; init; }
```

Seed of the random choices, so a run can be repeated.

**Type:** `int`
