[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# ClusterException class

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Inherits:** [`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)

```csharp
public sealed class ClusterException : Exception
```

A process cluster could not be started, reached or stopped. The message says which node and why.

## Constructors

| Member | Description |
| --- | --- |
| [`ClusterException(string, string)`](#m-ctor-string-string) | Creates the exception. |

### <a id="m-ctor-string-string"></a>ClusterException(string, string)

```csharp
public ClusterException(string code, string message);
```

Creates the exception.

**Parameters**

- `code` (`string`)
- `message` (`string`)

## Properties

| Member | Description |
| --- | --- |
| [`Code`](#m-code) | A short stable identifier such as `start_failed`, `no_node` or `orphans`. |

### <a id="m-code"></a>Code

```csharp
public string Code { get; }
```

A short stable identifier such as `start_failed`, `no_node` or `orphans`.

**Type:** `string`
