[API reference](../../index.md) › [OneGame.Server.Live](../index.md) › OneGame.Server.Live

# ServerControl class

**Namespace:** `OneGame.Server.Live`  
**Package:** [`RecursiveAI.OneGame.Server.Live`](../index.md)  
**Implements:** [`IControlEndpoint`](../../OneGame.Control/OneGame.Control/IControlEndpoint.md)

```csharp
public sealed class ServerControl : IControlEndpoint
```

The control surface of a game server: the operations an agent or the command line uses to look at a running server and to load, reload and unload its plugins. It is an [`IControlEndpoint`](../../OneGame.Control/OneGame.Control/IControlEndpoint.md), so it speaks MCP through the same listener a development game uses. Operations run on the thread that calls them, which must be the server's own, between ticks.

## Constructors

| Member | Description |
| --- | --- |
| [`ServerControl(ServerHost)`](#m-ctor-serverhost) | Creates the control surface for a server. |

### <a id="m-ctor-serverhost"></a>ServerControl(ServerHost)

```csharp
public ServerControl(ServerHost host);
```

Creates the control surface for a server.

**Parameters**

- `host` ([`ServerHost`](../../OneGame.Server/OneGame.Server/ServerHost.md)): The server.

## Properties

| Member | Description |
| --- | --- |
| [`Instructions`](#m-instructions) | A sentence or two for an agent that connects: where to start and what changes state. |
| [`Operations`](#m-operations) | The operations, each of which is an MCP tool. |

### <a id="m-instructions"></a>Instructions

```csharp
public string Instructions { get; }
```

A sentence or two for an agent that connects: where to start and what changes state.

**Type:** `string`

### <a id="m-operations"></a>Operations

```csharp
public IReadOnlyList<ControlOp> Operations { get; }
```

The operations, each of which is an MCP tool.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ControlOp`](../../OneGame.Control/OneGame.Control/ControlOp.md)&gt;

## Methods

| Member | Description |
| --- | --- |
| [`Call(string, string?)`](#m-call-string-string) | Runs an operation and returns its JSON text. |
| [`Invoke(string, string?)`](#m-invoke-string-string) | Runs an operation; `argsJson` is a JSON object or null. |

### <a id="m-call-string-string"></a>Call(string, string?)

```csharp
public string Call(string op, string? argsJson = null);
```

Runs an operation and returns its JSON text.

**Parameters**

- `op` (`string`): The operation name.
- `argsJson` (`string?`): A JSON object, or null.

**Returns:** `string`

**Exceptions**

- [`ControlException`](../../OneGame.Control/OneGame.Control/ControlException.md): The operation failed; [`ControlException.Code`](../../OneGame.Control/OneGame.Control/ControlException.md#m-code) says why.

### <a id="m-invoke-string-string"></a>Invoke(string, string?)

```csharp
public ControlResult Invoke(string op, string? argsJson = null);
```

Runs an operation; `argsJson` is a JSON object or null.

**Parameters**

- `op` (`string`): The operation's name.
- `argsJson` (`string?`): The arguments as a JSON object, or null.

**Returns:** [`ControlResult`](../../OneGame.Control/OneGame.Control/ControlResult.md)

**Exceptions**

- [`ControlException`](../../OneGame.Control/OneGame.Control/ControlException.md): The operation is unknown, its arguments are wrong, or it failed.
