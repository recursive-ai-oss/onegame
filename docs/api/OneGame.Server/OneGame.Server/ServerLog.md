[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# ServerLog class

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)

```csharp
public sealed class ServerLog
```

The server log: lines go to a writer (standard error for the runner) and the last few hundred are kept for the control surface. Entries hold only text, never objects, so a plugin's types are not kept alive by its own log lines.

## Constructors

| Member | Description |
| --- | --- |
| [`ServerLog(TextWriter?)`](#m-ctor-textwriter) | Creates a log writing to `sink`, or only keeping entries when it is null. |

### <a id="m-ctor-textwriter"></a>ServerLog(TextWriter?)

```csharp
public ServerLog(TextWriter? sink = null);
```

Creates a log writing to `sink`, or only keeping entries when it is null.

**Parameters**

- `sink` ([`TextWriter`](https://learn.microsoft.com/dotnet/api/system.io.textwriter)?): Where lines are written, such as `Console.Error`.

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | Entries written so far. |
| [`Tick`](#m-tick) | The tick stamped on new entries; the host keeps it current. |

### <a id="m-count"></a>Count

```csharp
public long Count { get; }
```

Entries written so far.

**Type:** `long`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; set; }
```

The tick stamped on new entries; the host keeps it current.

**Type:** `long`

## Methods

| Member | Description |
| --- | --- |
| [`Error(string)`](#m-error-string) | Writes an error. |
| [`Info(string)`](#m-info-string) | Writes an informational line. |
| [`Tail(int)`](#m-tail-int) | The last `max` entries, oldest first. |
| [`Warn(string)`](#m-warn-string) | Writes a warning. |

### <a id="m-error-string"></a>Error(string)

```csharp
public void Error(string message);
```

Writes an error.

**Parameters**

- `message` (`string`): The text.

### <a id="m-info-string"></a>Info(string)

```csharp
public void Info(string message);
```

Writes an informational line.

**Parameters**

- `message` (`string`): The text.

### <a id="m-tail-int"></a>Tail(int)

```csharp
public IReadOnlyList<LogEntry> Tail(int max = 50);
```

The last `max` entries, oldest first.

**Parameters**

- `max` (`int`): At most this many (up to the 512 kept).

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`LogEntry`](LogEntry.md)&gt;

### <a id="m-warn-string"></a>Warn(string)

```csharp
public void Warn(string message);
```

Writes a warning.

**Parameters**

- `message` (`string`): The text.
