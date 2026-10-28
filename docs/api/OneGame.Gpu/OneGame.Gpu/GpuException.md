[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# GpuException class

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Inherits:** [`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)

```csharp
public class GpuException : Exception
```

An error from the graphics layer.

## Constructors

| Member | Description |
| --- | --- |
| [`GpuException(string)`](#m-ctor-string) | Creates the exception. |
| [`GpuException(string, Exception)`](#m-ctor-string-exception) | Creates the exception with a cause. |

### <a id="m-ctor-string"></a>GpuException(string)

```csharp
public GpuException(string message);
```

Creates the exception.

**Parameters**

- `message` (`string`)

### <a id="m-ctor-string-exception"></a>GpuException(string, Exception)

```csharp
public GpuException(string message, Exception inner);
```

Creates the exception with a cause.

**Parameters**

- `message` (`string`)
- `inner` ([`Exception`](https://learn.microsoft.com/dotnet/api/system.exception))
