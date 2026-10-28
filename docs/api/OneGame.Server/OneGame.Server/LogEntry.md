[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# LogEntry record

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`LogEntry`](LogEntry.md)&gt;

```csharp
public sealed record LogEntry : IEquatable<LogEntry>
```

One line of the server log.

## Constructors

| Member | Description |
| --- | --- |
| [`LogEntry(long, long, string, string)`](#m-ctor-long-long-string-string) | One line of the server log. |

### <a id="m-ctor-long-long-string-string"></a>LogEntry(long, long, string, string)

```csharp
public LogEntry(long Sequence, long Tick, string Level, string Message);
```

One line of the server log.

**Parameters**

- `Sequence` (`long`): 1, 2, 3... in the order written.
- `Tick` (`long`): The server tick when it was written.
- `Level` (`string`): `info`, `warn` or `error`.
- `Message` (`string`): The text.

## Properties

| Member | Description |
| --- | --- |
| [`Level`](#m-level) | `info`, `warn` or `error`. |
| [`Message`](#m-message) | The text. |
| [`Sequence`](#m-sequence) | 1, 2, 3... in the order written. |
| [`Tick`](#m-tick) | The server tick when it was written. |

### <a id="m-level"></a>Level

```csharp
public string Level { get; init; }
```

`info`, `warn` or `error`.

**Type:** `string`

### <a id="m-message"></a>Message

```csharp
public string Message { get; init; }
```

The text.

**Type:** `string`

### <a id="m-sequence"></a>Sequence

```csharp
public long Sequence { get; init; }
```

1, 2, 3... in the order written.

**Type:** `long`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The server tick when it was written.

**Type:** `long`
