[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# DeterminismException class

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Inherits:** [`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)

```csharp
public sealed class DeterminismException : Exception
```

A determinism check found two runs that should be identical and are not.

## Constructors

| Member | Description |
| --- | --- |
| [`DeterminismException(string)`](#m-ctor-string) | Creates the exception. |

### <a id="m-ctor-string"></a>DeterminismException(string)

```csharp
public DeterminismException(string message);
```

Creates the exception.

**Parameters**

- `message` (`string`)
