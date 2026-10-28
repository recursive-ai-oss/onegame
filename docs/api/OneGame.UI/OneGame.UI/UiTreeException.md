[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# UiTreeException class

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)  
**Inherits:** [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

```csharp
public sealed class UiTreeException : InvalidOperationException
```

A tree operation was invalid (adding an element that already has a parent, or creating a cycle).

## Constructors

| Member | Description |
| --- | --- |
| [`UiTreeException(string)`](#m-ctor-string) | Creates the exception with a message that says what to fix. |

### <a id="m-ctor-string"></a>UiTreeException(string)

```csharp
public UiTreeException(string message);
```

Creates the exception with a message that says what to fix.

**Parameters**

- `message` (`string`)
