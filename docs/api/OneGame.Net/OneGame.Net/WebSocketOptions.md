[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# WebSocketOptions class

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)

```csharp
public sealed class WebSocketOptions
```

Settings for the WebSocket transport (RFC 6455 over plain TCP).

## Constructors

| Member | Description |
| --- | --- |
| [`WebSocketOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>WebSocketOptions()

```csharp
public WebSocketOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`AllowedOrigins`](#m-allowedorigins) | Listener only: if set, a browser's Origin header must be one of these (compared ignoring case). |
| [`CloseTimeoutMs`](#m-closetimeoutms) | How long to wait for the peer's close frame before closing the socket anyway. |
| [`HandshakeTimeoutMs`](#m-handshaketimeoutms) | The TCP connect and the opening handshake must finish within this long. |
| [`Host`](#m-host) | Client only: the Host header; the remote address if null. |
| [`KeepAliveMs`](#m-keepalivems) | Send a ping after this long without hearing anything. |
| [`MaxConnections`](#m-maxconnections) | Listener only: connections allowed at once. |
| [`MaxHeaderBytes`](#m-maxheaderbytes) | Largest HTTP header block accepted during the opening handshake. |
| [`MaxMessageBytes`](#m-maxmessagebytes) | Largest message accepted by Send and from the wire. |
| [`MaxPendingHandshakes`](#m-maxpendinghandshakes) | Listener only: sockets allowed to sit in the opening handshake at once. |
| [`MaxReceiveQueueBytes`](#m-maxreceivequeuebytes) | Most completed-message bytes waiting for TryReceive. |
| [`MaxSendQueueBytes`](#m-maxsendqueuebytes) | Most message bytes queued for sending. |
| [`Path`](#m-path) | Client only: the request path. |
| [`ReceiveBufferBytes`](#m-receivebufferbytes) | Size of the read buffer. |
| [`SendFragmentBytes`](#m-sendfragmentbytes) | Largest frame payload this side writes. |
| [`TimeoutMs`](#m-timeoutms) | Close with [`DisconnectReason.Timeout`](DisconnectReason.md#m-timeout) after this long without hearing anything. |

### <a id="m-allowedorigins"></a>AllowedOrigins

```csharp
public IReadOnlyList<string>? AllowedOrigins { get; init; }
```

Listener only: if set, a browser's Origin header must be one of these (compared ignoring case). Requests without an Origin header (non-browser clients) are always accepted.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>?`

### <a id="m-closetimeoutms"></a>CloseTimeoutMs

```csharp
public int CloseTimeoutMs { get; init; }
```

How long to wait for the peer's close frame before closing the socket anyway.

**Type:** `int`

### <a id="m-handshaketimeoutms"></a>HandshakeTimeoutMs

```csharp
public int HandshakeTimeoutMs { get; init; }
```

The TCP connect and the opening handshake must finish within this long.

**Type:** `int`

### <a id="m-host"></a>Host

```csharp
public string? Host { get; init; }
```

Client only: the Host header; the remote address if null.

**Type:** `string?`

### <a id="m-keepalivems"></a>KeepAliveMs

```csharp
public int KeepAliveMs { get; init; }
```

Send a ping after this long without hearing anything.

**Type:** `int`

### <a id="m-maxconnections"></a>MaxConnections

```csharp
public int MaxConnections { get; init; }
```

Listener only: connections allowed at once.

**Type:** `int`

### <a id="m-maxheaderbytes"></a>MaxHeaderBytes

```csharp
public int MaxHeaderBytes { get; init; }
```

Largest HTTP header block accepted during the opening handshake.

**Type:** `int`

### <a id="m-maxmessagebytes"></a>MaxMessageBytes

```csharp
public int MaxMessageBytes { get; init; }
```

Largest message accepted by Send and from the wire. A longer incoming message closes the connection with code 1009.

**Type:** `int`

### <a id="m-maxpendinghandshakes"></a>MaxPendingHandshakes

```csharp
public int MaxPendingHandshakes { get; init; }
```

Listener only: sockets allowed to sit in the opening handshake at once. More are closed on accept.

**Type:** `int`

### <a id="m-maxreceivequeuebytes"></a>MaxReceiveQueueBytes

```csharp
public int MaxReceiveQueueBytes { get; init; }
```

Most completed-message bytes waiting for TryReceive. Past it the socket is not read, so TCP flow control slows the sender.

**Type:** `int`

### <a id="m-maxsendqueuebytes"></a>MaxSendQueueBytes

```csharp
public int MaxSendQueueBytes { get; init; }
```

Most message bytes queued for sending. Past it the connection closes with [`DisconnectReason.SendQueueFull`](DisconnectReason.md#m-sendqueuefull).

**Type:** `int`

### <a id="m-path"></a>Path

```csharp
public string Path { get; init; }
```

Client only: the request path.

**Type:** `string`

### <a id="m-receivebufferbytes"></a>ReceiveBufferBytes

```csharp
public int ReceiveBufferBytes { get; init; }
```

Size of the read buffer.

**Type:** `int`

### <a id="m-sendfragmentbytes"></a>SendFragmentBytes

```csharp
public int SendFragmentBytes { get; init; }
```

Largest frame payload this side writes. Longer messages are split into continuation frames so pings and closes can go between them.

**Type:** `int`

### <a id="m-timeoutms"></a>TimeoutMs

```csharp
public int TimeoutMs { get; init; }
```

Close with [`DisconnectReason.Timeout`](DisconnectReason.md#m-timeout) after this long without hearing anything.

**Type:** `int`
