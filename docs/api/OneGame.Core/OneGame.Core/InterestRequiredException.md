[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# InterestRequiredException class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Inherits:** [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

```csharp
public sealed class InterestRequiredException : InvalidOperationException
```

A change would let data past a scope's `RequireInterest` guarantee: a random stream or hot table sent to everyone cannot be filtered by interest. Hide it (`Visibility.Hidden`), or keep its data in entities with an `[InterestKey]`.

## Constructors

| Member | Description |
| --- | --- |
| [`InterestRequiredException(string)`](#m-ctor-string) | Creates the exception. |

### <a id="m-ctor-string"></a>InterestRequiredException(string)

```csharp
public InterestRequiredException(string message);
```

Creates the exception.

**Parameters**

- `message` (`string`): What was refused and the fix.
