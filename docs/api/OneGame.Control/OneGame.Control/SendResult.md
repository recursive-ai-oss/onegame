[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# SendResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SendResult`](SendResult.md)&gt;

```csharp
public sealed record SendResult : IEquatable<SendResult>
```

Result of `command_send`: `Queued` means it runs on the next tick.

## Constructors

| Member | Description |
| --- | --- |
| [`SendResult(string, string, string)`](#m-ctor-string-string-string) | Result of `command_send`: `Queued` means it runs on the next tick. |

### <a id="m-ctor-string-string-string"></a>SendResult(string, string, string)

```csharp
public SendResult(string Entity, string Command, string Status);
```

Result of `command_send`: `Queued` means it runs on the next tick.

**Parameters**

- `Entity` (`string`): The entity addressed, as `Type#id`.
- `Command` (`string`): The command name.
- `Status` (`string`): What became of it: the command status's name, `Queued` when accepted. An unknown command or arguments that do not fit are errors instead.

## Properties

| Member | Description |
| --- | --- |
| [`Command`](#m-command) | The command name. |
| [`Entity`](#m-entity) | The entity addressed, as `Type#id`. |
| [`Status`](#m-status) | What became of it: the command status's name, `Queued` when accepted. |

### <a id="m-command"></a>Command

```csharp
public string Command { get; init; }
```

The command name.

**Type:** `string`

### <a id="m-entity"></a>Entity

```csharp
public string Entity { get; init; }
```

The entity addressed, as `Type#id`.

**Type:** `string`

### <a id="m-status"></a>Status

```csharp
public string Status { get; init; }
```

What became of it: the command status's name, `Queued` when accepted. An unknown command or arguments that do not fit are errors instead.

**Type:** `string`
