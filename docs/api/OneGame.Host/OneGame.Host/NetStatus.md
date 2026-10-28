[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# NetStatus record

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`NetStatus`](NetStatus.md)&gt;

```csharp
public sealed record NetStatus : IEquatable<NetStatus>
```

A snapshot of a client's connection, for `net_status`.

## Constructors

| Member | Description |
| --- | --- |
| [`NetStatus(string, string, string, string?, uint, string, long, string, long, int, int, double, long, double, double, long, long, DriftInfo[], FollowedScope[], int, bool)`](#m-ctor-string-string-string-string-uint-string-long-string-long-int-int-double-long-double-double-long-long-driftinfo-followedscope-int-bool) | A snapshot of a client's connection, for `net_status`. |

### <a id="m-ctor-string-string-string-string-uint-string-long-string-long-int-int-double-long-double-double-long-long-driftinfo-followedscope-int-bool"></a>NetStatus(string, string, string, string?, uint, string, long, string, long, int, int, double, long, double, double, long, long, DriftInfo\[\], FollowedScope\[\], int, bool)

```csharp
public NetStatus(string Address, string Transport, string Connection, string? CloseReason, uint Scope, string Replica, long Tick, string Hash, long FramesApplied, int Resyncs, int Drift, double RttMs, long Retransmits, double InboundLoss, double OutboundLoss, long MessagesSent, long MessagesReceived, DriftInfo[] Reports, FollowedScope[] Followed, int Reconnects = 0, bool Reconnecting = false);
```

A snapshot of a client's connection, for `net_status`.

**Parameters**

- `Address` (`string`): The server address as given.
- `Transport` (`string`): `udp` or `ws`.
- `Connection` (`string`): Transport state: connecting, connected, closing, closed.
- `CloseReason` (`string?`): Why the connection closed, or null while it is open.
- `Scope` (`uint`): The subscribed scope.
- `Replica` (`string`): Replica state: subscribing, live, resyncing, refused, failed.
- `Tick` (`long`): The replica's tick.
- `Hash` (`string`): The hash of what the replica holds (its view of the scope) as 16 hex digits; the server's hash for this client's audience is equal at the same tick.
- `FramesApplied` (`long`): Frames applied so far.
- `Resyncs` (`int`): How often the replica re-subscribed after a gap or drift.
- `Drift` (`int`): Drift reports filed so far.
- `RttMs` (`double`): Smoothed round-trip time.
- `Retransmits` (`long`): Reliable fragments sent again.
- `InboundLoss` (`double`): Estimated share of the server's datagrams that never arrived.
- `OutboundLoss` (`double`): Share of our reliable transmissions that were repeats.
- `MessagesSent` (`long`): Messages sent to the server.
- `MessagesReceived` (`long`): Messages received from the server.
- `Reports` ([`DriftInfo`](DriftInfo.md)\[\]): The latest drift reports (at most 8), oldest first.
- `Followed` ([`FollowedScope`](FollowedScope.md)\[\]): The other scopes the client follows through its server, in subscription order.
- `Reconnects` (`int`): How many times the session connected again after losing the server.
- `Reconnecting` (`bool`): True while the connection is lost and the session is trying to get it back.

## Properties

| Member | Description |
| --- | --- |
| [`Address`](#m-address) | The server address as given. |
| [`CloseReason`](#m-closereason) | Why the connection closed, or null while it is open. |
| [`Connection`](#m-connection) | Transport state: connecting, connected, closing, closed. |
| [`Drift`](#m-drift) | Drift reports filed so far. |
| [`Followed`](#m-followed) | The other scopes the client follows through its server, in subscription order. |
| [`FramesApplied`](#m-framesapplied) | Frames applied so far. |
| [`Hash`](#m-hash) | The hash of what the replica holds (its view of the scope) as 16 hex digits; the server's hash for this client's audience is equal at the same tick. |
| [`InboundLoss`](#m-inboundloss) | Estimated share of the server's datagrams that never arrived. |
| [`MessagesReceived`](#m-messagesreceived) | Messages received from the server. |
| [`MessagesSent`](#m-messagessent) | Messages sent to the server. |
| [`OutboundLoss`](#m-outboundloss) | Share of our reliable transmissions that were repeats. |
| [`Reconnecting`](#m-reconnecting) | True while the connection is lost and the session is trying to get it back. |
| [`Reconnects`](#m-reconnects) | How many times the session connected again after losing the server. |
| [`Replica`](#m-replica) | Replica state: subscribing, live, resyncing, refused, failed. |
| [`Reports`](#m-reports) | The latest drift reports (at most 8), oldest first. |
| [`Resyncs`](#m-resyncs) | How often the replica re-subscribed after a gap or drift. |
| [`Retransmits`](#m-retransmits) | Reliable fragments sent again. |
| [`RttMs`](#m-rttms) | Smoothed round-trip time. |
| [`Scope`](#m-scope) | The subscribed scope. |
| [`Tick`](#m-tick) | The replica's tick. |
| [`Transport`](#m-transport) | `udp` or `ws`. |

### <a id="m-address"></a>Address

```csharp
public string Address { get; init; }
```

The server address as given.

**Type:** `string`

### <a id="m-closereason"></a>CloseReason

```csharp
public string? CloseReason { get; init; }
```

Why the connection closed, or null while it is open.

**Type:** `string?`

### <a id="m-connection"></a>Connection

```csharp
public string Connection { get; init; }
```

Transport state: connecting, connected, closing, closed.

**Type:** `string`

### <a id="m-drift"></a>Drift

```csharp
public int Drift { get; init; }
```

Drift reports filed so far.

**Type:** `int`

### <a id="m-followed"></a>Followed

```csharp
public FollowedScope[] Followed { get; init; }
```

The other scopes the client follows through its server, in subscription order.

**Type:** [`FollowedScope`](FollowedScope.md)\[\]

### <a id="m-framesapplied"></a>FramesApplied

```csharp
public long FramesApplied { get; init; }
```

Frames applied so far.

**Type:** `long`

### <a id="m-hash"></a>Hash

```csharp
public string Hash { get; init; }
```

The hash of what the replica holds (its view of the scope) as 16 hex digits; the server's hash for this client's audience is equal at the same tick.

**Type:** `string`

### <a id="m-inboundloss"></a>InboundLoss

```csharp
public double InboundLoss { get; init; }
```

Estimated share of the server's datagrams that never arrived.

**Type:** `double`

### <a id="m-messagesreceived"></a>MessagesReceived

```csharp
public long MessagesReceived { get; init; }
```

Messages received from the server.

**Type:** `long`

### <a id="m-messagessent"></a>MessagesSent

```csharp
public long MessagesSent { get; init; }
```

Messages sent to the server.

**Type:** `long`

### <a id="m-outboundloss"></a>OutboundLoss

```csharp
public double OutboundLoss { get; init; }
```

Share of our reliable transmissions that were repeats.

**Type:** `double`

### <a id="m-reconnecting"></a>Reconnecting

```csharp
public bool Reconnecting { get; init; }
```

True while the connection is lost and the session is trying to get it back.

**Type:** `bool`

### <a id="m-reconnects"></a>Reconnects

```csharp
public int Reconnects { get; init; }
```

How many times the session connected again after losing the server.

**Type:** `int`

### <a id="m-replica"></a>Replica

```csharp
public string Replica { get; init; }
```

Replica state: subscribing, live, resyncing, refused, failed.

**Type:** `string`

### <a id="m-reports"></a>Reports

```csharp
public DriftInfo[] Reports { get; init; }
```

The latest drift reports (at most 8), oldest first.

**Type:** [`DriftInfo`](DriftInfo.md)\[\]

### <a id="m-resyncs"></a>Resyncs

```csharp
public int Resyncs { get; init; }
```

How often the replica re-subscribed after a gap or drift.

**Type:** `int`

### <a id="m-retransmits"></a>Retransmits

```csharp
public long Retransmits { get; init; }
```

Reliable fragments sent again.

**Type:** `long`

### <a id="m-rttms"></a>RttMs

```csharp
public double RttMs { get; init; }
```

Smoothed round-trip time.

**Type:** `double`

### <a id="m-scope"></a>Scope

```csharp
public uint Scope { get; init; }
```

The subscribed scope.

**Type:** `uint`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The replica's tick.

**Type:** `long`

### <a id="m-transport"></a>Transport

```csharp
public string Transport { get; init; }
```

`udp` or `ws`.

**Type:** `string`
