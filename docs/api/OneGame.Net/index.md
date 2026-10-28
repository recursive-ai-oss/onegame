[API reference](../index.md) › OneGame.Net

# OneGame.Net

Network transports behind the message-channel seam: reliable UDP, WebSocket, and a network conditioner for tests.

**Package:** `RecursiveAI.OneGame.Net`

```sh
dotnet add package RecursiveAI.OneGame.Net
```

## OneGame.Net

| Type | Kind | Description |
| --- | --- | --- |
| [`ConditionedConnection`](OneGame.Net/ConditionedConnection.md) | class | Wraps any [`INetConnection`](OneGame.Net/INetConnection.md) and applies [`NetworkConditions`](OneGame.Net/NetworkConditions.md) to what it sends, for transports that are already reliable (WebSocket over TCP). |
| [`ConditionedDatagramSocket`](OneGame.Net/ConditionedDatagramSocket.md) | class | Wraps a datagram socket and applies [`NetworkConditions`](OneGame.Net/NetworkConditions.md) to what it sends. |
| [`ConditionedListener`](OneGame.Net/ConditionedListener.md) | class | Wraps a listener so every connection it accepts is a [`ConditionedConnection`](OneGame.Net/ConditionedConnection.md). |
| [`ConnectionState`](OneGame.Net/ConnectionState.md) | enum | Where a connection is in its life. |
| [`DisconnectInfo`](OneGame.Net/DisconnectInfo.md) | struct | How and why a connection closed. |
| [`DisconnectReason`](OneGame.Net/DisconnectReason.md) | enum | Why a connection ended. |
| [`IDatagramSocket`](OneGame.Net/IDatagramSocket.md) | interface | The datagram socket the UDP transport talks to. |
| [`INetConnection`](OneGame.Net/INetConnection.md) | interface | One live connection to a peer. |
| [`INetListener`](OneGame.Net/INetListener.md) | interface | A server endpoint that turns incoming handshakes into [`INetConnection`](OneGame.Net/INetConnection.md)s. |
| [`ManualNetClock`](OneGame.Net/ManualNetClock.md) | class | A clock the caller advances by hand. |
| [`MemoryDatagramNetwork`](OneGame.Net/MemoryDatagramNetwork.md) | class | An in-process datagram network: sockets that deliver to each other by address, with no operating system involved. |
| [`MessageTooLargeException`](OneGame.Net/MessageTooLargeException.md) | class | A message was larger than the configured maximum, so it was refused before anything was queued. |
| [`NetClock`](OneGame.Net/NetClock.md) | class | The time source transports use for timeouts, retransmission and the conditioner. |
| [`NetModule`](OneGame.Net/NetModule.md) | static class | Identifies the Net assembly. |
| [`NetSecurity`](OneGame.Net/NetSecurity.md) | class | How a transport secures its links, passed as `UdpOptions.Security` or `WebSocketOptions.Security`. |
| [`NetStats`](OneGame.Net/NetStats.md) | struct | Counters and estimates for one connection. |
| [`NetworkConditions`](OneGame.Net/NetworkConditions.md) | class | What a bad network does to traffic. |
| [`SecurityMode`](OneGame.Net/SecurityMode.md) | enum | Whether a transport encrypts and authenticates its links. |
| [`SecurityNotConfiguredException`](OneGame.Net/SecurityNotConfiguredException.md) | class | A transport was opened without saying how its links are secured, or with keys that do not fit what it was asked to do. |
| [`SystemDatagramSocket`](OneGame.Net/SystemDatagramSocket.md) | class | A non-blocking operating-system UDP socket. |
| [`UdpConnection`](OneGame.Net/UdpConnection.md) | class | One reliable-UDP connection, either the client end made by [`UdpTransport.Connect()`](OneGame.Net/UdpTransport.md#m-connect-ipendpoint-udpoptions-netclock) or a server end handed out by a [`UdpListener`](OneGame.Net/UdpListener.md). |
| [`UdpListener`](OneGame.Net/UdpListener.md) | class | The server end of the reliable-UDP transport. |
| [`UdpOptions`](OneGame.Net/UdpOptions.md) | class | Settings for the reliable-UDP transport. |
| [`UdpTransport`](OneGame.Net/UdpTransport.md) | static class | Creates reliable-UDP listeners and connections. |
| [`WebSocketConnection`](OneGame.Net/WebSocketConnection.md) | class | A WebSocket connection (RFC 6455) over a non-blocking TCP socket, client or server end. |
| [`WebSocketListener`](OneGame.Net/WebSocketListener.md) | class | The server end of the WebSocket transport. |
| [`WebSocketOptions`](OneGame.Net/WebSocketOptions.md) | class | Settings for the WebSocket transport (RFC 6455 over plain TCP). |
| [`WebSocketTransport`](OneGame.Net/WebSocketTransport.md) | static class | Creates WebSocket listeners and connections. |
