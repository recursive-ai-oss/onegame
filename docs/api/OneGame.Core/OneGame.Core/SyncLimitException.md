[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# SyncLimitException class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Inherits:** [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

```csharp
public sealed class SyncLimitException : InvalidOperationException
```

Thrown when state about to be written would be refused by the decoder's limits.

## Constructors

| Member | Description |
| --- | --- |
| [`SyncLimitException(string)`](#m-ctor-string) | Creates the exception. |

### <a id="m-ctor-string"></a>SyncLimitException(string)

```csharp
public SyncLimitException(string message);
```

Creates the exception.

**Parameters**

- `message` (`string`)
