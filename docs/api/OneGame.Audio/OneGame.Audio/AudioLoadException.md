[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# AudioLoadException class

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Inherits:** [`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)

```csharp
public sealed class AudioLoadException : Exception
```

A source file could not be decoded: it is malformed, uses an unsupported format, or asks for more than the limits allow.

## Constructors

| Member | Description |
| --- | --- |
| [`AudioLoadException(string, Exception?)`](#m-ctor-string-exception) | Creates the exception. |

### <a id="m-ctor-string-exception"></a>AudioLoadException(string, Exception?)

```csharp
public AudioLoadException(string message, Exception? inner = null);
```

Creates the exception.

**Parameters**

- `message` (`string`): What was wrong.
- `inner` ([`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)?): The decoder's own error.
