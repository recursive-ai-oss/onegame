[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# GpuDeviceLostException class

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Inherits:** [`GpuException`](GpuException.md)

```csharp
public sealed class GpuDeviceLostException : GpuException
```

The device was lost (driver reset, GPU removed).

## Constructors

| Member | Description |
| --- | --- |
| [`GpuDeviceLostException(string)`](#m-ctor-string) | Creates the exception. |

### <a id="m-ctor-string"></a>GpuDeviceLostException(string)

```csharp
public GpuDeviceLostException(string message);
```

Creates the exception.

**Parameters**

- `message` (`string`)
