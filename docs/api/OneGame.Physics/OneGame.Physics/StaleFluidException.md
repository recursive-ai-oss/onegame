[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# StaleFluidException class

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)  
**Inherits:** [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

```csharp
public sealed class StaleFluidException : InvalidOperationException
```

A fluid, emitter or drain reference no longer names anything in the space.

## Constructors

| Member | Description |
| --- | --- |
| [`StaleFluidException(string)`](#m-ctor-string) | Creates the exception. |

### <a id="m-ctor-string"></a>StaleFluidException(string)

```csharp
public StaleFluidException(string message);
```

Creates the exception.

**Parameters**

- `message` (`string`): What happened.
