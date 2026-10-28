[API reference](../../index.md) › [OneGame.Text](../index.md) › OneGame.Text

# TextLimitException class

**Namespace:** `OneGame.Text`  
**Package:** [`RecursiveAI.OneGame.Text`](../index.md)  
**Inherits:** [`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)

```csharp
public sealed class TextLimitException : Exception
```

A text or font request broke a budget: nothing was shaped or allocated for it.

## Constructors

| Member | Description |
| --- | --- |
| [`TextLimitException(string)`](#m-ctor-string) | Creates the exception. |

### <a id="m-ctor-string"></a>TextLimitException(string)

```csharp
public TextLimitException(string message);
```

Creates the exception.

**Parameters**

- `message` (`string`): Which limit and by how much.
