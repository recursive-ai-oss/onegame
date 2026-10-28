[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# ScenarioException class

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Inherits:** [`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)

```csharp
public sealed class ScenarioException : Exception
```

A scenario file is wrong. The message names the line and what to change.

## Constructors

| Member | Description |
| --- | --- |
| [`ScenarioException(string, string?, int)`](#m-ctor-string-string-int) | Creates the exception. |

### <a id="m-ctor-string-string-int"></a>ScenarioException(string, string?, int)

```csharp
public ScenarioException(string message, string? source, int line);
```

Creates the exception.

**Parameters**

- `message` (`string`)
- `source` (`string?`)
- `line` (`int`)

## Properties

| Member | Description |
| --- | --- |
| [`Line`](#m-line) | The line of the problem, 1-based; 0 when unknown. |
| [`Source`](#m-source) | The file, when known. |

### <a id="m-line"></a>Line

```csharp
public int Line { get; }
```

The line of the problem, 1-based; 0 when unknown.

**Type:** `int`

### <a id="m-source"></a>Source

```csharp
public string? Source { get; }
```

The file, when known.

**Type:** `string?`
