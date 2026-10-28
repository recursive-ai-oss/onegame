[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# ServerConfigException class

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Inherits:** [`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)

```csharp
public sealed class ServerConfigException : Exception
```

The server configuration file is not valid: the message names the file, the line and what to change.

## Constructors

| Member | Description |
| --- | --- |
| [`ServerConfigException(string, string?, int)`](#m-ctor-string-string-int) | Creates the exception. |

### <a id="m-ctor-string-string-int"></a>ServerConfigException(string, string?, int)

```csharp
public ServerConfigException(string message, string? source, int line);
```

Creates the exception.

**Parameters**

- `message` (`string`)
- `source` (`string?`)
- `line` (`int`)

## Properties

| Member | Description |
| --- | --- |
| [`Line`](#m-line) | The 1-based line, or 0. |
| [`Source`](#m-source) | The file, when known. |

### <a id="m-line"></a>Line

```csharp
public int Line { get; }
```

The 1-based line, or 0.

**Type:** `int`

### <a id="m-source"></a>Source

```csharp
public string? Source { get; }
```

The file, when known.

**Type:** `string?`
