[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# KeyFormatException class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Inherits:** [`FormatException`](https://learn.microsoft.com/dotnet/api/system.formatexception)

```csharp
public sealed class KeyFormatException : FormatException
```

A key or key file was malformed.

## Constructors

| Member | Description |
| --- | --- |
| [`KeyFormatException(string)`](#m-ctor-string) | Creates the exception. |

### <a id="m-ctor-string"></a>KeyFormatException(string)

```csharp
public KeyFormatException(string message);
```

Creates the exception.

**Parameters**

- `message` (`string`): What was wrong with the key text.
