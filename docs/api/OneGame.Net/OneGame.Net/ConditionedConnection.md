[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# ConditionedConnection class

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)  
**Implements:** [`INetConnection`](INetConnection.md), [`IAuthenticatedChannel`](../../OneGame.Sync/OneGame.Sync/IAuthenticatedChannel.md), [`IMessageChannel`](../../OneGame.Sync/OneGame.Sync/IMessageChannel.md)

```csharp
public sealed class ConditionedConnection : INetConnection, IAuthenticatedChannel, IMessageChannel
```

Wraps any [`INetConnection`](INetConnection.md) and applies [`NetworkConditions`](NetworkConditions.md) to what it sends, for transports that are already reliable (WebSocket over TCP). Latency, jitter and bandwidth act as they would on the wire; loss shows up as a retransmission delay, because a lost TCP segment is resent, not lost; order and duplicates are untouched.

## Constructors

| Member | Description |
| --- | --- |
| [`ConditionedConnection(INetConnection, NetworkConditions, NetClock)`](#m-ctor-inetconnection-networkconditions-netclock) | Wraps `inner`. |

### <a id="m-ctor-inetconnection-networkconditions-netclock"></a>ConditionedConnection(INetConnection, NetworkConditions, NetClock)

```csharp
public ConditionedConnection(INetConnection inner, NetworkConditions conditions, NetClock clock);
```

Wraps `inner`.

**Parameters**

- `inner` ([`INetConnection`](INetConnection.md)): The real connection.
- `conditions` ([`NetworkConditions`](NetworkConditions.md)): What to do to traffic.
- `clock` ([`NetClock`](NetClock.md)): The clock the delays follow.

## Properties

| Member | Description |
| --- | --- |
| [`ChannelBinding`](#m-channelbinding) | A 32-byte value both ends derive from the handshake (Noise's handshake hash): it names this one encrypted session, and a fresh server ephemeral key makes it unique to it. |
| [`CloseInfo`](#m-closeinfo) | Why the connection closed; [`DisconnectReason.None`](DisconnectReason.md#m-none) until then. |
| [`IsEncrypted`](#m-isencrypted) | True when the link is encrypted and authenticated ([`SecurityMode.Required`](SecurityMode.md#m-required)). |
| [`RemoteKey`](#m-remotekey) | The peer's authenticated public key, or null on a link without authentication (an `Insecure` transport, a loopback). |
| [`RemoteName`](#m-remotename) | The peer's address, as text, for logs. |
| [`State`](#m-state) | Current state. |
| [`Stats`](#m-stats) | A snapshot of the counters. |

### <a id="m-channelbinding"></a>ChannelBinding

```csharp
public ReadOnlyMemory<byte> ChannelBinding { get; }
```

A 32-byte value both ends derive from the handshake (Noise's handshake hash): it names this one encrypted session, and a fresh server ephemeral key makes it unique to it. It is **public, not a secret**: anyone who watched the handshake can compute it, so never use it as a key or a MAC key. A game that checks a platform or login proof (a Steam ticket, a token) must bind the proof to this value and to the server's public key, or a malicious server could pass the proof on to another server and pose as the player there. Empty on an insecure link and before the handshake finishes.

**Type:** [`ReadOnlyMemory`](https://learn.microsoft.com/dotnet/api/system.readonlymemory-1)`<byte>`

### <a id="m-closeinfo"></a>CloseInfo

```csharp
public DisconnectInfo CloseInfo { get; }
```

Why the connection closed; [`DisconnectReason.None`](DisconnectReason.md#m-none) until then.

**Type:** [`DisconnectInfo`](DisconnectInfo.md)

### <a id="m-isencrypted"></a>IsEncrypted

```csharp
public bool IsEncrypted { get; }
```

True when the link is encrypted and authenticated ([`SecurityMode.Required`](SecurityMode.md#m-required)). Then [`IAuthenticatedChannel.RemoteKey`](../../OneGame.Sync/OneGame.Sync/IAuthenticatedChannel.md#m-remotekey) is the peer's proven key once connected.

**Type:** `bool`

### <a id="m-remotekey"></a>RemoteKey

```csharp
public PeerKey? RemoteKey { get; }
```

The peer's authenticated public key, or null on a link without authentication (an `Insecure` transport, a loopback).

**Type:** [`PeerKey`](../../OneGame.Core/OneGame.Core/PeerKey.md)?

### <a id="m-remotename"></a>RemoteName

```csharp
public string RemoteName { get; }
```

The peer's address, as text, for logs.

**Type:** `string`

### <a id="m-state"></a>State

```csharp
public ConnectionState State { get; }
```

Current state.

**Type:** [`ConnectionState`](ConnectionState.md)

### <a id="m-stats"></a>Stats

```csharp
public NetStats Stats { get; }
```

A snapshot of the counters.

**Type:** [`NetStats`](NetStats.md)

## Events

| Member | Description |
| --- | --- |
| [`Connected`](#m-connected) | Raised once when the handshake completes. |
| [`Disconnected`](#m-disconnected) | Raised once when the connection closes, with the reason. |

### <a id="m-connected"></a>Connected

```csharp
public event Action<INetConnection>? Connected;
```

Raised once when the handshake completes.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`INetConnection`](INetConnection.md)&gt;?

### <a id="m-disconnected"></a>Disconnected

```csharp
public event Action<INetConnection, DisconnectInfo>? Disconnected;
```

Raised once when the connection closes, with the reason.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)&lt;[`INetConnection`](INetConnection.md), [`DisconnectInfo`](DisconnectInfo.md)&gt;?

## Methods

| Member | Description |
| --- | --- |
| [`Disconnect(DisconnectReason, bool)`](#m-disconnect-disconnectreason-bool) | Closes the connection and tells the peer why. |
| [`Pump()`](#m-pump) | Reads and writes whatever the socket allows right now. |
| [`Send(ReadOnlySpan<byte>)`](#m-send-readonlyspan-byte) | Queues a message for the other end. |
| [`TryReceive(out ReadOnlySpan<byte>)`](#m-tryreceive-readonlyspan-byte) | Takes the next message, or returns false if none is waiting. |

### <a id="m-disconnect-disconnectreason-bool"></a>Disconnect(DisconnectReason, bool)

```csharp
public void Disconnect(DisconnectReason reason = 1, bool flush = false);
```

Closes the connection and tells the peer why.

**Parameters**

- `reason` ([`DisconnectReason`](DisconnectReason.md)): The reason the peer will see.
- `flush` (`bool`): Keep pumping until queued messages are acknowledged (or a short linger ends) before closing.

### <a id="m-pump"></a>Pump()

```csharp
public void Pump();
```

Reads and writes whatever the socket allows right now. Connections made by `Connect` must be pumped by their owner; connections handed out by a listener are pumped by the listener (calling this on them is harmless).

### <a id="m-send-readonlyspan-byte"></a>Send(ReadOnlySpan&lt;byte&gt;)

```csharp
public void Send(ReadOnlySpan<byte> message);
```

Queues a message for the other end.

**Parameters**

- `message` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The message is larger than [`Loopback.MaxMessageBytes`](../../OneGame.Sync/OneGame.Sync/Loopback.md#m-maxmessagebytes).

### <a id="m-tryreceive-readonlyspan-byte"></a>TryReceive(out ReadOnlySpan&lt;byte&gt;)

```csharp
public bool TryReceive(out ReadOnlySpan<byte> message);
```

Takes the next message, or returns false if none is waiting. The span is valid until the next call to this method.

**Parameters**

- `message` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

**Returns:** `bool`
