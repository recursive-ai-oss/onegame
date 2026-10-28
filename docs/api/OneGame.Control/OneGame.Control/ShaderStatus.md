[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# ShaderStatus record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ShaderStatus`](ShaderStatus.md)&gt;

```csharp
public sealed record ShaderStatus : IEquatable<ShaderStatus>
```

The state of one hot-reloadable shader.

## Constructors

| Member | Description |
| --- | --- |
| [`ShaderStatus(string, string, string, int, string?, ShaderProblem[])`](#m-ctor-string-string-string-int-string-shaderproblem) | The state of one hot-reloadable shader. |

### <a id="m-ctor-string-string-string-int-string-shaderproblem"></a>ShaderStatus(string, string, string, int, string?, ShaderProblem\[\])

```csharp
public ShaderStatus(string Name, string Path, string State, int Reloads, string? Message, ShaderProblem[] Problems);
```

The state of one hot-reloadable shader.

**Parameters**

- `Name` (`string`): The registered name.
- `Path` (`string`): The shader file.
- `State` (`string`): `ok` (the running shader matches the file), `compile_error` or `incompatible` (the old shader keeps running).
- `Reloads` (`int`): How many times a new version was swapped in.
- `Message` (`string?`): For a failed reload, the reason in one text, ready to show; otherwise null.
- `Problems` ([`ShaderProblem`](ShaderProblem.md)\[\]): The compile diagnostics, with file and line.

## Properties

| Member | Description |
| --- | --- |
| [`Message`](#m-message) | For a failed reload, the reason in one text, ready to show; otherwise null. |
| [`Name`](#m-name) | The registered name. |
| [`Path`](#m-path) | The shader file. |
| [`Problems`](#m-problems) | The compile diagnostics, with file and line. |
| [`Reloads`](#m-reloads) | How many times a new version was swapped in. |
| [`State`](#m-state) | `ok` (the running shader matches the file), `compile_error` or `incompatible` (the old shader keeps running). |

### <a id="m-message"></a>Message

```csharp
public string? Message { get; init; }
```

For a failed reload, the reason in one text, ready to show; otherwise null.

**Type:** `string?`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The registered name.

**Type:** `string`

### <a id="m-path"></a>Path

```csharp
public string Path { get; init; }
```

The shader file.

**Type:** `string`

### <a id="m-problems"></a>Problems

```csharp
public ShaderProblem[] Problems { get; init; }
```

The compile diagnostics, with file and line.

**Type:** [`ShaderProblem`](ShaderProblem.md)\[\]

### <a id="m-reloads"></a>Reloads

```csharp
public int Reloads { get; init; }
```

How many times a new version was swapped in.

**Type:** `int`

### <a id="m-state"></a>State

```csharp
public string State { get; init; }
```

`ok` (the running shader matches the file), `compile_error` or `incompatible` (the old shader keeps running).

**Type:** `string`
