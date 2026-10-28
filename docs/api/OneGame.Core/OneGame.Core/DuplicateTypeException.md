[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# DuplicateTypeException class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Inherits:** [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

```csharp
public sealed class DuplicateTypeException : InvalidOperationException
```

Thrown when an entity type is registered twice or two types share an id.

## Constructors

| Member | Description |
| --- | --- |
| [`DuplicateTypeException(string)`](#m-ctor-string) | Creates the exception. |

### <a id="m-ctor-string"></a>DuplicateTypeException(string)

```csharp
public DuplicateTypeException(string message);
```

Creates the exception.

**Parameters**

- `message` (`string`)
