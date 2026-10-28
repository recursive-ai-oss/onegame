[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# LiveClient class

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class LiveClient : IDisposable
```

A client of a running process's live control socket: one connection that sends operations as MCP tool calls and returns their JSON results. This is what an agent's MCP bridge does, so a harness that uses it sees exactly what an agent sees.

## Constructors

| Member | Description |
| --- | --- |
| [`LiveClient(string, int)`](#m-ctor-string-int) | Creates a client; it connects on the first call. |

### <a id="m-ctor-string-int"></a>LiveClient(string, int)

```csharp
public LiveClient(string socketPath, int timeoutMs = 60000);
```

Creates a client; it connects on the first call.

**Parameters**

- `socketPath` (`string`): The Unix socket the process listens on.
- `timeoutMs` (`int`): How long to wait for a reply to one call.

## Properties

| Member | Description |
| --- | --- |
| [`Path`](#m-path) | The socket path. |

### <a id="m-path"></a>Path

```csharp
public string Path { get; }
```

The socket path.

**Type:** `string`

## Methods

| Member | Description |
| --- | --- |
| [`Call(string, string?)`](#m-call-string-string) | Runs an operation and returns its result JSON. |
| [`Dispose()`](#m-dispose) |  |
| [`TcpEndPoint(string)`](#m-tcpendpoint-string) | Parses `tcp:host:port` (an IPv4 address or `localhost`). |
| [`Tools()`](#m-tools) | The names of the operations the process offers (its MCP tool list). |
| [`ToolsJson()`](#m-toolsjson) | The process's MCP tool list as JSON: each tool's name, description and input schema. |

### <a id="m-call-string-string"></a>Call(string, string?)

```csharp
public string Call(string op, string? argsJson = null);
```

Runs an operation and returns its result JSON.

**Parameters**

- `op` (`string`): The operation name.
- `argsJson` (`string?`): A JSON object, or null.

**Returns:** `string`

**Exceptions**

- [`ControlException`](../../OneGame.Control/OneGame.Control/ControlException.md): The operation failed (its code is the process's), or the process could not be reached (`unreachable`).

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Implements [`IDisposable.Dispose()`](https://learn.microsoft.com/dotnet/api/system.idisposable.dispose).

### <a id="m-tcpendpoint-string"></a>TcpEndPoint(string)

```csharp
public static IPEndPoint TcpEndPoint(string spec);
```

Parses `tcp:host:port` (an IPv4 address or `localhost`).

**Parameters**

- `spec` (`string`)

**Returns:** [`IPEndPoint`](https://learn.microsoft.com/dotnet/api/system.net.ipendpoint)

### <a id="m-tools"></a>Tools()

```csharp
public string[] Tools();
```

The names of the operations the process offers (its MCP tool list).

**Returns:** `string[]`

### <a id="m-toolsjson"></a>ToolsJson()

```csharp
public string ToolsJson();
```

The process's MCP tool list as JSON: each tool's name, description and input schema.

**Returns:** `string`
