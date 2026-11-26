[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# FluidBudgetExceededException class

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)  
**Inherits:** [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

```csharp
public sealed class FluidBudgetExceededException : InvalidOperationException
```

Adding particles would pass [`FluidWorldSettings.MaxParticles`](FluidWorldSettings.md#m-maxparticles).

## Constructors

| Member | Description |
| --- | --- |
| [`FluidBudgetExceededException(string)`](#m-ctor-string) | Creates the exception. |

### <a id="m-ctor-string"></a>FluidBudgetExceededException(string)

```csharp
public FluidBudgetExceededException(string message);
```

Creates the exception.

**Parameters**

- `message` (`string`): What happened.
