[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# BrowserWebSocket class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public sealed class BrowserWebSocket
```

A binary WebSocket made with the browser's own `WebSocket`. A page cannot open sockets, so this is how a web build reaches a server. Messages are queued by the browser and read here by polling.

## Constructors

| Member | Description |
| --- | --- |
| [`BrowserWebSocket(string)`](#m-ctor-string) | Starts connecting to `url` (`ws://` or `wss://`). |

### <a id="m-ctor-string"></a>BrowserWebSocket(string)

```csharp
public BrowserWebSocket(string url);
```

Starts connecting to `url` (`ws://` or `wss://`).

**Parameters**

- `url` (`string`)

## Properties

| Member | Description |
| --- | --- |
| [`CloseCode`](#m-closecode) | The WebSocket close code once [`BrowserWebSocket.State`](BrowserWebSocket.md#m-state) is 3 (1006 when the connection never opened). |
| [`State`](#m-state) | 0 connecting, 1 open, 2 closing, 3 closed. |

### <a id="m-closecode"></a>CloseCode

```csharp
public int CloseCode { get; }
```

The WebSocket close code once [`BrowserWebSocket.State`](BrowserWebSocket.md#m-state) is 3 (1006 when the connection never opened).

**Type:** `int`

### <a id="m-state"></a>State

```csharp
public int State { get; }
```

0 connecting, 1 open, 2 closing, 3 closed.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Close()`](#m-close) | Closes the socket with the normal closure code. |
| [`Send(ReadOnlySpan<byte>)`](#m-send-readonlyspan-byte) | Sends one binary message. |
| [`TryReceive(out ReadOnlySpan<byte>)`](#m-tryreceive-readonlyspan-byte) | Takes the next received message, if one has arrived. |

### <a id="m-close"></a>Close()

```csharp
public void Close();
```

Closes the socket with the normal closure code.

### <a id="m-send-readonlyspan-byte"></a>Send(ReadOnlySpan&lt;byte&gt;)

```csharp
public void Send(ReadOnlySpan<byte> message);
```

Sends one binary message. Dropped silently unless the socket is open.

**Parameters**

- `message` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

### <a id="m-tryreceive-readonlyspan-byte"></a>TryReceive(out ReadOnlySpan&lt;byte&gt;)

```csharp
public bool TryReceive(out ReadOnlySpan<byte> message);
```

Takes the next received message, if one has arrived. The span is valid until the next call.

**Parameters**

- `message` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

**Returns:** `bool`
