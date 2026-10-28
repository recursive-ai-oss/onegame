[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# IControlEndpoint interface

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)

```csharp
public interface IControlEndpoint
```

Anything the MCP face can drive: a list of operations and a way to run one. A game's [`ControlSurface`](ControlSurface.md) is one; a server's is another.

## Properties

| Member | Description |
| --- | --- |
| [`Instructions`](#m-instructions) | A sentence or two for an agent that connects: where to start and what changes state. |
| [`Operations`](#m-operations) | The operations, each of which is an MCP tool. |

### <a id="m-instructions"></a>Instructions

```csharp
string Instructions { get; }
```

A sentence or two for an agent that connects: where to start and what changes state.

**Type:** `string`

### <a id="m-operations"></a>Operations

```csharp
IReadOnlyList<ControlOp> Operations { get; }
```

The operations, each of which is an MCP tool.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ControlOp`](ControlOp.md)&gt;

## Methods

| Member | Description |
| --- | --- |
| [`Invoke(string, string?)`](#m-invoke-string-string) | Runs an operation; `argsJson` is a JSON object or null. |
| [`InvokeAsync(string, string?)`](#m-invokeasync-string-string) | Runs an operation; operations that wait for the graphics device finish here without blocking the caller. |

### <a id="m-invoke-string-string"></a>Invoke(string, string?)

```csharp
ControlResult Invoke(string op, string? argsJson = null);
```

Runs an operation; `argsJson` is a JSON object or null.

**Parameters**

- `op` (`string`)
- `argsJson` (`string?`)

**Returns:** [`ControlResult`](ControlResult.md)

**Exceptions**

- [`ControlException`](ControlException.md): The operation is unknown, its arguments are wrong, or it failed.

### <a id="m-invokeasync-string-string"></a>InvokeAsync(string, string?)

```csharp
ValueTask<ControlResult> InvokeAsync(string op, string? argsJson = null);
```

Runs an operation; operations that wait for the graphics device finish here without blocking the caller.

**Parameters**

- `op` (`string`)
- `argsJson` (`string?`)

**Returns:** [`ValueTask`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.valuetask-1)&lt;[`ControlResult`](ControlResult.md)&gt;
