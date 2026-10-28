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

- `Entity` (`string`)
- `Command` (`string`)
- `Status` (`string`)

## Properties

| Member | Description |
| --- | --- |
| [`Command`](#m-command) |  |
| [`Entity`](#m-entity) |  |
| [`Status`](#m-status) |  |

### <a id="m-command"></a>Command

```csharp
public string Command { get; init; }
```

**Type:** `string`

### <a id="m-entity"></a>Entity

```csharp
public string Entity { get; init; }
```

**Type:** `string`

### <a id="m-status"></a>Status

```csharp
public string Status { get; init; }
```

**Type:** `string`
