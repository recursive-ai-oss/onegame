[API reference](../../index.md) › [OneGame.Navigation](../index.md) › OneGame.Navigation

# NavRangeException class

**Namespace:** `OneGame.Navigation`  
**Package:** [`RecursiveAI.OneGame.Navigation`](../index.md)  
**Inherits:** [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

```csharp
public sealed class NavRangeException : ArgumentOutOfRangeException
```

A world position was outside what navigation can convert exactly (±2^21 world units).

## Constructors

| Member | Description |
| --- | --- |
| [`NavRangeException(string, Fixed)`](#m-ctor-string-fixed) | Creates the exception. |
| [`NavRangeException(string, string)`](#m-ctor-string-string) | Creates the exception with a custom message. |

### <a id="m-ctor-string-fixed"></a>NavRangeException(string, Fixed)

```csharp
public NavRangeException(string paramName, Fixed value);
```

Creates the exception.

**Parameters**

- `paramName` (`string`): The offending argument.
- `value` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): Its value.

### <a id="m-ctor-string-string"></a>NavRangeException(string, string)

```csharp
public NavRangeException(string paramName, string message);
```

Creates the exception with a custom message.

**Parameters**

- `paramName` (`string`): The offending argument.
- `message` (`string`): What is wrong.
