[API reference](../../index.md) › [OneGame.Server.Live](../index.md) › OneGame.Server.Live

# ServerSendResult record

**Namespace:** `OneGame.Server.Live`  
**Package:** [`RecursiveAI.OneGame.Server.Live`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ServerSendResult`](ServerSendResult.md)&gt;

```csharp
public sealed record ServerSendResult : IEquatable<ServerSendResult>
```

Result of `command_send` on a server: the command is queued for the next tick, trusted as if the host sent it.

## Constructors

| Member | Description |
| --- | --- |
| [`ServerSendResult(uint, ulong, string, string)`](#m-ctor-uint-ulong-string-string) | Result of `command_send` on a server: the command is queued for the next tick, trusted as if the host sent it. |

### <a id="m-ctor-uint-ulong-string-string"></a>ServerSendResult(uint, ulong, string, string)

```csharp
public ServerSendResult(uint Scope, ulong Entity, string Command, string Status);
```

Result of `command_send` on a server: the command is queued for the next tick, trusted as if the host sent it.

**Parameters**

- `Scope` (`uint`): The scope id.
- `Entity` (`ulong`): The entity id in that scope.
- `Command` (`string`): The command name.
- `Status` (`string`): The command status's name, `Queued` when accepted.

## Properties

| Member | Description |
| --- | --- |
| [`Command`](#m-command) | The command name. |
| [`Entity`](#m-entity) | The entity id in that scope. |
| [`Scope`](#m-scope) | The scope id. |
| [`Status`](#m-status) | The command status's name, `Queued` when accepted. |

### <a id="m-command"></a>Command

```csharp
public string Command { get; init; }
```

The command name.

**Type:** `string`

### <a id="m-entity"></a>Entity

```csharp
public ulong Entity { get; init; }
```

The entity id in that scope.

**Type:** `ulong`

### <a id="m-scope"></a>Scope

```csharp
public uint Scope { get; init; }
```

The scope id.

**Type:** `uint`

### <a id="m-status"></a>Status

```csharp
public string Status { get; init; }
```

The command status's name, `Queued` when accepted.

**Type:** `string`
