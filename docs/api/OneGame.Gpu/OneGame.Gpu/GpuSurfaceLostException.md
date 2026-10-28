[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# GpuSurfaceLostException class

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Inherits:** [`GpuException`](GpuException.md)

```csharp
public sealed class GpuSurfaceLostException : GpuException
```

The window's drawing surface is gone, as when an app is sent to the background or the system rebuilds its window. Dispose the swapchain; make a new one once the window has a surface again.

## Constructors

| Member | Description |
| --- | --- |
| [`GpuSurfaceLostException()`](#m-ctor) | Creates the exception. |

### <a id="m-ctor"></a>GpuSurfaceLostException()

```csharp
public GpuSurfaceLostException();
```

Creates the exception.
