[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.Tweens

# TweenException class

**Namespace:** `OneGame.Tweens`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)  
**Inherits:** [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

```csharp
public sealed class TweenException : InvalidOperationException
```

Thrown when a tween handle is used wrongly: after the tween ended, on a tween that is part of a group, or with an unusable shape.

## Constructors

| Member | Description |
| --- | --- |
| [`TweenException(string)`](#m-ctor-string) | Creates the exception. |

### <a id="m-ctor-string"></a>TweenException(string)

```csharp
public TweenException(string message);
```

Creates the exception.

**Parameters**

- `message` (`string`): What was wrong and what to do instead.
