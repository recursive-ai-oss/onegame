[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu.Graph

# RenderGraphException class

**Namespace:** `OneGame.Gpu.Graph`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Inherits:** [`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)

```csharp
public sealed class RenderGraphException : Exception
```

A problem in how a graph was declared: reading something no pass wrote, using a stale handle, touching a resource a pass did not declare.

## Constructors

| Member | Description |
| --- | --- |
| [`RenderGraphException(string)`](#m-ctor-string) | Creates the exception. |

### <a id="m-ctor-string"></a>RenderGraphException(string)

```csharp
public RenderGraphException(string message);
```

Creates the exception.

**Parameters**

- `message` (`string`)
