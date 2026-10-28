[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# INetConnection interface

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)  
**Implements:** [`IMessageChannel`](../../OneGame.Sync/OneGame.Sync/IMessageChannel.md)

```csharp
public interface INetConnection : IMessageChannel
```

One live connection to a peer. Messages are whole, ordered and reliable ([`IMessageChannel`](../../OneGame.Sync/OneGame.Sync/IMessageChannel.md)). Nothing happens on its own: [`INetConnection.Pump()`](INetConnection.md#m-pump) does the socket work, on the caller's thread. The events fire inside [`INetConnection.Pump()`](INetConnection.md#m-pump) (or inside [`INetConnection.Disconnect()`](INetConnection.md#m-disconnect-disconnectreason-bool)).

## Properties

| Member | Description |
| --- | --- |
| [`CloseInfo`](#m-closeinfo) | Why the connection closed; [`DisconnectReason.None`](DisconnectReason.md#m-none) until then. |
| [`RemoteName`](#m-remotename) | The peer's address, as text, for logs. |
| [`State`](#m-state) | Current state. |
| [`Stats`](#m-stats) | A snapshot of the counters. |

### <a id="m-closeinfo"></a>CloseInfo

```csharp
DisconnectInfo CloseInfo { get; }
```

Why the connection closed; [`DisconnectReason.None`](DisconnectReason.md#m-none) until then.

**Type:** [`DisconnectInfo`](DisconnectInfo.md)

### <a id="m-remotename"></a>RemoteName

```csharp
string RemoteName { get; }
```

The peer's address, as text, for logs.

**Type:** `string`

### <a id="m-state"></a>State

```csharp
ConnectionState State { get; }
```

Current state.

**Type:** [`ConnectionState`](ConnectionState.md)

### <a id="m-stats"></a>Stats

```csharp
NetStats Stats { get; }
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
event Action<INetConnection>? Connected;
```

Raised once when the handshake completes.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`INetConnection`](INetConnection.md)&gt;?

### <a id="m-disconnected"></a>Disconnected

```csharp
event Action<INetConnection, DisconnectInfo>? Disconnected;
```

Raised once when the connection closes, with the reason.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)&lt;[`INetConnection`](INetConnection.md), [`DisconnectInfo`](DisconnectInfo.md)&gt;?

## Methods

| Member | Description |
| --- | --- |
| [`Disconnect(DisconnectReason, bool)`](#m-disconnect-disconnectreason-bool) | Closes the connection and tells the peer why. |
| [`Pump()`](#m-pump) | Reads and writes whatever the socket allows right now. |

### <a id="m-disconnect-disconnectreason-bool"></a>Disconnect(DisconnectReason, bool)

```csharp
void Disconnect(DisconnectReason reason = 1, bool flush = false);
```

Closes the connection and tells the peer why.

**Parameters**

- `reason` ([`DisconnectReason`](DisconnectReason.md)): The reason the peer will see.
- `flush` (`bool`): Keep pumping until queued messages are acknowledged (or a short linger ends) before closing.

### <a id="m-pump"></a>Pump()

```csharp
void Pump();
```

Reads and writes whatever the socket allows right now. Connections made by `Connect` must be pumped by their owner; connections handed out by a listener are pumped by the listener (calling this on them is harmless).
