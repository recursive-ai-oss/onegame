[API reference](../../index.md) › [OneGame.Server.Live](../index.md) › OneGame.Server.Live

# ServerSendResult record

**Namespace:** `OneGame.Server.Live`  
**Package:** [`RecursiveAI.OneGame.Server.Live`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ServerSendResult`](ServerSendResult.md)&gt;

```csharp
public sealed record ServerSendResult : IEquatable<ServerSendResult>
```

Result of `command_send`.

## Constructors

| Member | Description |
| --- | --- |
| [`ServerSendResult(uint, ulong, string, string)`](#m-ctor-uint-ulong-string-string) | Result of `command_send`. |

### <a id="m-ctor-uint-ulong-string-string"></a>ServerSendResult(uint, ulong, string, string)

```csharp
public ServerSendResult(uint Scope, ulong Entity, string Command, string Status);
```

Result of `command_send`.

**Parameters**

- `Scope` (`uint`)
- `Entity` (`ulong`)
- `Command` (`string`)
- `Status` (`string`)

## Properties

| Member | Description |
| --- | --- |
| [`Command`](#m-command) |  |
| [`Entity`](#m-entity) |  |
| [`Scope`](#m-scope) |  |
| [`Status`](#m-status) |  |

### <a id="m-command"></a>Command

```csharp
public string Command { get; init; }
```

**Type:** `string`

### <a id="m-entity"></a>Entity

```csharp
public ulong Entity { get; init; }
```

**Type:** `ulong`

### <a id="m-scope"></a>Scope

```csharp
public uint Scope { get; init; }
```

**Type:** `uint`

### <a id="m-status"></a>Status

```csharp
public string Status { get; init; }
```

**Type:** `string`
