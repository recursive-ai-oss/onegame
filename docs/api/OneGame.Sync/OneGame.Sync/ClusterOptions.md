[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# ClusterOptions class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class ClusterOptions
```

Cluster-wide behavior switches.

## Constructors

| Member | Description |
| --- | --- |
| [`ClusterOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>ClusterOptions()

```csharp
public ClusterOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`Clock`](#m-clock) | Milliseconds clock for the two timeouts. |
| [`CommandTimeoutMs`](#m-commandtimeoutms) | Milliseconds a replica waits for the answer to a forwarded command before it gives up and completes it as [`CommandVerdict.TimedOut`](CommandVerdict.md#m-timedout). |
| [`DigestEntityLimit`](#m-digestentitylimit) | Most entities a drift digest lists; above it the digest holds the scope hash only. |
| [`DigestTimeoutTicks`](#m-digesttimeoutticks) | Authority ticks a replica waits for the answer to a digest request before it reports the drift without detail. |
| [`DriftCheckInterval`](#m-driftcheckinterval) | Ticks between hash comparisons of authority and replicas; 0 turns drift detection off. |
| [`FatalDrift`](#m-fataldrift) | When true, [`Cluster.Step()`](Cluster.md#m-step-int) throws [`DriftException`](DriftException.md) as soon as a drift report is filed. |
| [`MaxAssembledBytes`](#m-maxassembledbytes) | Largest message a node reassembles from pieces. |
| [`PieceBytes`](#m-piecebytes) | Messages from an authority longer than this are sent in pieces (transports cap one message at 1 MiB by default, so keep this well below). |
| [`ResyncOnDrift`](#m-resyncondrift) | When true, a replica that found drift re-subscribes and takes a fresh snapshot. |
| [`SilenceTimeoutMs`](#m-silencetimeoutms) | Milliseconds without any message from an authority after which its replica is [`ReplicaScope.IsSilent`](ReplicaScope.md#m-issilent) and its pending commands time out. |

### <a id="m-clock"></a>Clock

```csharp
public Func<long>? Clock { get; init; }
```

Milliseconds clock for the two timeouts. Null uses the system's monotonic clock, except in a [`Cluster`](Cluster.md), which counts 60 ticks per second so runs stay repeatable.

**Type:** [`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<long>?`

### <a id="m-commandtimeoutms"></a>CommandTimeoutMs

```csharp
public int CommandTimeoutMs { get; init; }
```

Milliseconds a replica waits for the answer to a forwarded command before it gives up and completes it as [`CommandVerdict.TimedOut`](CommandVerdict.md#m-timedout). 0 turns the timeout off.

**Type:** `int`

### <a id="m-digestentitylimit"></a>DigestEntityLimit

```csharp
public int DigestEntityLimit { get; init; }
```

Most entities a drift digest lists; above it the digest holds the scope hash only.

**Type:** `int`

### <a id="m-digesttimeoutticks"></a>DigestTimeoutTicks

```csharp
public int DigestTimeoutTicks { get; init; }
```

Authority ticks a replica waits for the answer to a digest request before it reports the drift without detail.

**Type:** `int`

### <a id="m-driftcheckinterval"></a>DriftCheckInterval

```csharp
public int DriftCheckInterval { get; init; }
```

Ticks between hash comparisons of authority and replicas; 0 turns drift detection off. On by default.

**Type:** `int`

### <a id="m-fataldrift"></a>FatalDrift

```csharp
public bool FatalDrift { get; init; }
```

When true, [`Cluster.Step()`](Cluster.md#m-step-int) throws [`DriftException`](DriftException.md) as soon as a drift report is filed. Tests turn it on.

**Type:** `bool`

### <a id="m-maxassembledbytes"></a>MaxAssembledBytes

```csharp
public int MaxAssembledBytes { get; init; }
```

Largest message a node reassembles from pieces. A longer one is refused from its first piece, before anything is allocated.

**Type:** `int`

### <a id="m-piecebytes"></a>PieceBytes

```csharp
public int PieceBytes { get; init; }
```

Messages from an authority longer than this are sent in pieces (transports cap one message at 1 MiB by default, so keep this well below).

**Type:** `int`

### <a id="m-resyncondrift"></a>ResyncOnDrift

```csharp
public bool ResyncOnDrift { get; init; }
```

When true, a replica that found drift re-subscribes and takes a fresh snapshot.

**Type:** `bool`

### <a id="m-silencetimeoutms"></a>SilenceTimeoutMs

```csharp
public int SilenceTimeoutMs { get; init; }
```

Milliseconds without any message from an authority after which its replica is [`ReplicaScope.IsSilent`](ReplicaScope.md#m-issilent) and its pending commands time out. A live authority sends a frame every tick, so silence this long means it stopped ticking, crashed or lost the link. 0 turns the check off.

**Type:** `int`
