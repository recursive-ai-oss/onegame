[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# ControlServer class

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)

```csharp
public sealed class ControlServer
```

A game's control operations served as Model Context Protocol tools by RecursiveAI.OneModelContext: every [`ControlOp`](ControlOp.md) is a tool of the same name with the same schema, and an operation's [`ControlResult`](ControlResult.md) is the tool's text (and, for a capture, an image). Version negotiation, framing, errors and cancellation are the library's. The class knows nothing about sockets: `OneGame.Live` hands it streams, and a page-dialled channel hands it one message at a time through [`ControlServer.OpenSession()`](ControlServer.md#m-opensession).

**Example**

```csharp
var control = new ControlServer(new ControlSurface(game));
await control.Server.RunAsync(ContextTransport.Stream(stream));   // a socket's stream
```

## Constructors

| Member | Description |
| --- | --- |
| [`ControlServer(IControlEndpoint)`](#m-ctor-icontrolendpoint) | Creates the server for a control endpoint; every call runs on the thread that handles the message. |
| [`ControlServer(IControlEndpoint, Func<string, string?, ValueTask<ControlResult>>?)`](#m-ctor-icontrolendpoint-func-string-string-valuetask-controlresult) | Creates the server, running every call through `invoke` (a host that runs operations on its own thread). |

### <a id="m-ctor-icontrolendpoint"></a>ControlServer(IControlEndpoint)

```csharp
public ControlServer(IControlEndpoint endpoint);
```

Creates the server for a control endpoint; every call runs on the thread that handles the message.

**Parameters**

- `endpoint` ([`IControlEndpoint`](IControlEndpoint.md)): The endpoint whose operations become tools.

**Exceptions**

- [`ArgumentNullException`](https://learn.microsoft.com/dotnet/api/system.argumentnullexception): `endpoint` is null.

### <a id="m-ctor-icontrolendpoint-func-string-string-valuetask-controlresult"></a>ControlServer(IControlEndpoint, Func&lt;string, string?, ValueTask&lt;ControlResult&gt;&gt;?)

```csharp
public ControlServer(IControlEndpoint endpoint, Func<string, string?, ValueTask<ControlResult>>? invoke);
```

Creates the server, running every call through `invoke` (a host that runs operations on its own thread).

**Parameters**

- `endpoint` ([`IControlEndpoint`](IControlEndpoint.md)): The endpoint whose operations become tools.
- `invoke` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-3)`<string, string?,` [`ValueTask`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.valuetask-1)&lt;[`ControlResult`](ControlResult.md)&gt;&gt;?): Runs an operation by name with its arguments as JSON text; null calls the endpoint directly.

**Exceptions**

- [`ArgumentNullException`](https://learn.microsoft.com/dotnet/api/system.argumentnullexception): `endpoint` is null.

## Fields

| Member | Description |
| --- | --- |
| [`LatestVersion`](#m-latestversion) | The newest protocol revision of the pre-2026 era that live control has always spoken; older ones negotiate as before. |
| [`MaxMessageBytes`](#m-maxmessagebytes) | Longest message accepted, in bytes. |

### <a id="m-latestversion"></a>LatestVersion

```csharp
public const string LatestVersion = "2025-06-18";
```

The newest protocol revision of the pre-2026 era that live control has always spoken; older ones negotiate as before.

**Type:** `string`

### <a id="m-maxmessagebytes"></a>MaxMessageBytes

```csharp
public const int MaxMessageBytes = 8388608;
```

Longest message accepted, in bytes.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`Server`](#m-server) | The library's server, to run over a stream (`ContextTransport.Stream`) or any other transport. |

### <a id="m-server"></a>Server

```csharp
public ContextServer Server { get; }
```

The library's server, to run over a stream (`ContextTransport.Stream`) or any other transport.

**Type:** `ContextServer`

## Methods

| Member | Description |
| --- | --- |
| [`OpenSession()`](#m-opensession) | Starts a session that takes whole JSON-RPC messages and returns the answers, for a channel with no stream (a page's WebSocket). |

### <a id="m-opensession"></a>OpenSession()

```csharp
public ContextSession OpenSession();
```

Starts a session that takes whole JSON-RPC messages and returns the answers, for a channel with no stream (a page's WebSocket).

**Returns:** `ContextSession`
