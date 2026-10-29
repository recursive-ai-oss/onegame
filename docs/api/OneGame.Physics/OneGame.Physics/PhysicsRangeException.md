[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# PhysicsRangeException class

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)  
**Inherits:** [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

```csharp
public sealed class PhysicsRangeException : ArgumentOutOfRangeException
```

A value was outside what the physics world can convert exactly (±2^21 world units), or was not finite.

## Constructors

| Member | Description |
| --- | --- |
| [`PhysicsRangeException(string, Fixed)`](#m-ctor-string-fixed) | Creates the exception. |

### <a id="m-ctor-string-fixed"></a>PhysicsRangeException(string, Fixed)

```csharp
public PhysicsRangeException(string paramName, Fixed value);
```

Creates the exception.

**Parameters**

- `paramName` (`string`): The offending argument.
- `value` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): Its value.
