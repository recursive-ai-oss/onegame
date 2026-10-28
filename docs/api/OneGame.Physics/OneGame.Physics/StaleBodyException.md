[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# StaleBodyException class

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)  
**Inherits:** [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

```csharp
public sealed class StaleBodyException : InvalidOperationException
```

A body or character reference no longer names anything in the space (it was removed, or the space was restored from a save that does not have it).

## Constructors

| Member | Description |
| --- | --- |
| [`StaleBodyException(string)`](#m-ctor-string) | Creates the exception. |

### <a id="m-ctor-string"></a>StaleBodyException(string)

```csharp
public StaleBodyException(string message);
```

Creates the exception.

**Parameters**

- `message` (`string`): What happened.
