[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# HdrException class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)  
**Inherits:** [`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)

```csharp
public sealed class HdrException : Exception
```

The file is not a valid Radiance (.hdr) image, or exceeds a limit.

## Constructors

| Member | Description |
| --- | --- |
| [`HdrException(string)`](#m-ctor-string) | Creates the exception. |

### <a id="m-ctor-string"></a>HdrException(string)

```csharp
public HdrException(string message);
```

Creates the exception.

**Parameters**

- `message` (`string`)
