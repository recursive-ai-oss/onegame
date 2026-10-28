[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# McpProtocol class

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)

```csharp
public sealed class McpProtocol
```

The Model Context Protocol (https://modelcontextprotocol.io, revision 2025-06-18) for one game: JSON-RPC 2.0 messages in, messages out. Every control operation is a tool of the same name. The class knows nothing about sockets; `OneGame.Live` moves the messages.

## Constructors

| Member | Description |
| --- | --- |
| [`McpProtocol(ControlSurface)`](#m-ctor-controlsurface) | Creates the protocol handler for a game's control surface. |
| [`McpProtocol(IControlEndpoint)`](#m-ctor-icontrolendpoint) | Creates the protocol handler for any control endpoint, such as a server's. |

### <a id="m-ctor-controlsurface"></a>McpProtocol(ControlSurface)

```csharp
public McpProtocol(ControlSurface surface);
```

Creates the protocol handler for a game's control surface.

**Parameters**

- `surface` ([`ControlSurface`](ControlSurface.md))

### <a id="m-ctor-icontrolendpoint"></a>McpProtocol(IControlEndpoint)

```csharp
public McpProtocol(IControlEndpoint endpoint);
```

Creates the protocol handler for any control endpoint, such as a server's.

**Parameters**

- `endpoint` ([`IControlEndpoint`](IControlEndpoint.md))

## Fields

| Member | Description |
| --- | --- |
| [`LatestVersion`](#m-latestversion) | The newest protocol revision this server speaks. |

### <a id="m-latestversion"></a>LatestVersion

```csharp
public const string LatestVersion = "2025-06-18";
```

The newest protocol revision this server speaks.

**Type:** `string`

## Methods

| Member | Description |
| --- | --- |
| [`Handle(string)`](#m-handle-string) | Handles one JSON-RPC message (one line of the stdio transport). |
| [`HandleAsync(string)`](#m-handleasync-string) | Like [`McpProtocol.Handle()`](McpProtocol.md#m-handle-string), for hosts that cannot block while a tool waits for the graphics device (a browser page). |

### <a id="m-handle-string"></a>Handle(string)

```csharp
public string? Handle(string message);
```

Handles one JSON-RPC message (one line of the stdio transport). Returns the response message, or null for a notification. Never throws on bad input: malformed messages get a JSON-RPC error.

**Parameters**

- `message` (`string`)

**Returns:** `string?`

### <a id="m-handleasync-string"></a>HandleAsync(string)

```csharp
public ValueTask<string?> HandleAsync(string message);
```

Like [`McpProtocol.Handle()`](McpProtocol.md#m-handle-string), for hosts that cannot block while a tool waits for the graphics device (a browser page).

**Parameters**

- `message` (`string`)

**Returns:** [`ValueTask`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.valuetask-1)`<string?>`
