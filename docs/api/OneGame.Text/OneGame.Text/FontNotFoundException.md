[API reference](../../index.md) › [OneGame.Text](../index.md) › OneGame.Text

# FontNotFoundException class

**Namespace:** `OneGame.Text`  
**Package:** [`RecursiveAI.OneGame.Text`](../index.md)  
**Inherits:** [`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)

```csharp
public sealed class FontNotFoundException : Exception
```

A font could not be found: the library holds no faces, or a face id is unknown.

## Constructors

| Member | Description |
| --- | --- |
| [`FontNotFoundException(string)`](#m-ctor-string) | Creates the exception. |

### <a id="m-ctor-string"></a>FontNotFoundException(string)

```csharp
public FontNotFoundException(string message);
```

Creates the exception.

**Parameters**

- `message` (`string`): What was asked for.
