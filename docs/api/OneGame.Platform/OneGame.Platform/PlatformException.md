[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# PlatformException class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)  
**Inherits:** [`Exception`](https://learn.microsoft.com/dotnet/api/system.exception)

```csharp
public sealed class PlatformException : Exception
```

Thrown when the platform layer cannot do what was asked.

## Constructors

| Member | Description |
| --- | --- |
| [`PlatformException()`](#m-ctor) | Creates the exception. |
| [`PlatformException(string)`](#m-ctor-string) | Creates the exception. |
| [`PlatformException(string, Exception)`](#m-ctor-string-exception) | Creates the exception. |

### <a id="m-ctor"></a>PlatformException()

```csharp
public PlatformException();
```

Creates the exception.

### <a id="m-ctor-string"></a>PlatformException(string)

```csharp
public PlatformException(string message);
```

Creates the exception.

**Parameters**

- `message` (`string`)

### <a id="m-ctor-string-exception"></a>PlatformException(string, Exception)

```csharp
public PlatformException(string message, Exception inner);
```

Creates the exception.

**Parameters**

- `message` (`string`)
- `inner` ([`Exception`](https://learn.microsoft.com/dotnet/api/system.exception))
