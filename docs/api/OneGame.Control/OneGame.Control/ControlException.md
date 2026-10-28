[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# ControlException class

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Inherits:** [`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)

```csharp
public sealed class ControlException : Exception
```

A control operation failed in a way the caller can act on. [`ControlException.Code`](ControlException.md#m-code) is stable; the message says what to change.

## Constructors

| Member | Description |
| --- | --- |
| [`ControlException(string, string)`](#m-ctor-string-string) | Creates the exception. |
| [`ControlException(string, string, Exception)`](#m-ctor-string-string-exception) | Creates the exception. |

### <a id="m-ctor-string-string"></a>ControlException(string, string)

```csharp
public ControlException(string code, string message);
```

Creates the exception.

**Parameters**

- `code` (`string`)
- `message` (`string`)

### <a id="m-ctor-string-string-exception"></a>ControlException(string, string, Exception)

```csharp
public ControlException(string code, string message, Exception inner);
```

Creates the exception.

**Parameters**

- `code` (`string`)
- `message` (`string`)
- `inner` ([`Exception`](https://learn.microsoft.com/dotnet/api/system.exception))

## Properties

| Member | Description |
| --- | --- |
| [`Code`](#m-code) | A short stable identifier such as `unknown_op`, `bad_args`, `not_found` or `rejected`. |

### <a id="m-code"></a>Code

```csharp
public string Code { get; }
```

A short stable identifier such as `unknown_op`, `bad_args`, `not_found` or `rejected`.

**Type:** `string`
