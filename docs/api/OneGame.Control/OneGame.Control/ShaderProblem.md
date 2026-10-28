[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# ShaderProblem record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ShaderProblem`](ShaderProblem.md)&gt;

```csharp
public sealed record ShaderProblem : IEquatable<ShaderProblem>
```

One diagnostic of a failed shader compile.

## Constructors

| Member | Description |
| --- | --- |
| [`ShaderProblem(string, int, int, string, string)`](#m-ctor-string-int-int-string-string) | One diagnostic of a failed shader compile. |

### <a id="m-ctor-string-int-int-string-string"></a>ShaderProblem(string, int, int, string, string)

```csharp
public ShaderProblem(string File, int Line, int Column, string Code, string Message);
```

One diagnostic of a failed shader compile.

**Parameters**

- `File` (`string`): The file the problem is in.
- `Line` (`int`): 1-based line.
- `Column` (`int`): 1-based column.
- `Code` (`string`): The diagnostic code, such as `OSH0300`.
- `Message` (`string`): What is wrong.

## Properties

| Member | Description |
| --- | --- |
| [`Code`](#m-code) | The diagnostic code, such as `OSH0300`. |
| [`Column`](#m-column) | 1-based column. |
| [`File`](#m-file) | The file the problem is in. |
| [`Line`](#m-line) | 1-based line. |
| [`Message`](#m-message) | What is wrong. |

### <a id="m-code"></a>Code

```csharp
public string Code { get; init; }
```

The diagnostic code, such as `OSH0300`.

**Type:** `string`

### <a id="m-column"></a>Column

```csharp
public int Column { get; init; }
```

1-based column.

**Type:** `int`

### <a id="m-file"></a>File

```csharp
public string File { get; init; }
```

The file the problem is in.

**Type:** `string`

### <a id="m-line"></a>Line

```csharp
public int Line { get; init; }
```

1-based line.

**Type:** `int`

### <a id="m-message"></a>Message

```csharp
public string Message { get; init; }
```

What is wrong.

**Type:** `string`
