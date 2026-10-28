[API reference](../../index.md) › [OneGame.Atlas](../index.md) › OneGame.Atlas

# AtlasAssetException class

**Namespace:** `OneGame.Atlas`  
**Package:** [`RecursiveAI.OneGame.Atlas`](../index.md)  
**Inherits:** [`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)

```csharp
public class AtlasAssetException : Exception
```

A registered asset was refused: it is damaged or breaks a size budget.

## Constructors

| Member | Description |
| --- | --- |
| [`AtlasAssetException(string, Exception?)`](#m-ctor-string-exception) | Creates the exception. |

### <a id="m-ctor-string-exception"></a>AtlasAssetException(string, Exception?)

```csharp
public AtlasAssetException(string message, Exception? inner = null);
```

Creates the exception.

**Parameters**

- `message` (`string`): What was refused.
- `inner` ([`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)?): The decoder's exception, if any.
