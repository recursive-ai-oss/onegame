[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# BrowserWebSocketConnection class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`INetConnection`](../../OneGame.Net/OneGame.Net/INetConnection.md), [`IMessageChannel`](../../OneGame.Sync/OneGame.Sync/IMessageChannel.md)

```csharp
public sealed class BrowserWebSocketConnection : INetConnection, IMessageChannel
```

A client connection over the browser's WebSocket, behind the same interface as the native transports, so a web build subscribes to a server's scopes exactly like a desktop client does. Messages are binary and whole; the browser handles framing and ping/pong.

## Constructors

| Member | Description |
| --- | --- |
| [`BrowserWebSocketConnection(string)`](#m-ctor-string) | Starts connecting to a `ws://` or `wss://` address. |

### <a id="m-ctor-string"></a>BrowserWebSocketConnection(string)

```csharp
public BrowserWebSocketConnection(string url);
```

Starts connecting to a `ws://` or `wss://` address.

**Parameters**

- `url` (`string`)

## Properties

| Member | Description |
| --- | --- |
| [`CloseInfo`](#m-closeinfo) | Why the connection closed; [`DisconnectReason.None`](../../OneGame.Net/OneGame.Net/DisconnectReason.md#m-none) until then. |
| [`RemoteName`](#m-remotename) | The peer's address, as text, for logs. |
| [`State`](#m-state) | Current state. |
| [`Stats`](#m-stats) | A snapshot of the counters. |

### <a id="m-closeinfo"></a>CloseInfo

```csharp
public DisconnectInfo CloseInfo { get; }
```

Why the connection closed; [`DisconnectReason.None`](../../OneGame.Net/OneGame.Net/DisconnectReason.md#m-none) until then.

**Type:** [`DisconnectInfo`](../../OneGame.Net/OneGame.Net/DisconnectInfo.md)

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

**Type:** [`ConnectionState`](../../OneGame.Net/OneGame.Net/ConnectionState.md)

### <a id="m-stats"></a>Stats

```csharp
public NetStats Stats { get; }
```

A snapshot of the counters.

**Type:** [`NetStats`](../../OneGame.Net/OneGame.Net/NetStats.md)

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

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`INetConnection`](../../OneGame.Net/OneGame.Net/INetConnection.md)&gt;?

### <a id="m-disconnected"></a>Disconnected

```csharp
public event Action<INetConnection, DisconnectInfo>? Disconnected;
```

Raised once when the connection closes, with the reason.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)&lt;[`INetConnection`](../../OneGame.Net/OneGame.Net/INetConnection.md), [`DisconnectInfo`](../../OneGame.Net/OneGame.Net/DisconnectInfo.md)&gt;?

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

- `reason` ([`DisconnectReason`](../../OneGame.Net/OneGame.Net/DisconnectReason.md)): The reason the peer will see.
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
