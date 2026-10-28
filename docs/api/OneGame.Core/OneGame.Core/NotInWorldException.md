[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# NotInWorldException class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Inherits:** [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

```csharp
public sealed class NotInWorldException : InvalidOperationException
```

Thrown when an entity that is not in a world is asked to do something that needs one.

## Constructors

| Member | Description |
| --- | --- |
| [`NotInWorldException(string)`](#m-ctor-string) | Creates the exception. |

### <a id="m-ctor-string"></a>NotInWorldException(string)

```csharp
public NotInWorldException(string message);
```

Creates the exception.

**Parameters**

- `message` (`string`)
