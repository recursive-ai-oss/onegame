[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# SecurityNotConfiguredException class

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)  
**Inherits:** [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

```csharp
public sealed class SecurityNotConfiguredException : InvalidOperationException
```

A transport was opened without saying how its links are secured, or with keys that do not fit what it was asked to do.

## Constructors

| Member | Description |
| --- | --- |
| [`SecurityNotConfiguredException(string)`](#m-ctor-string) | Creates the exception. |

### <a id="m-ctor-string"></a>SecurityNotConfiguredException(string)

```csharp
public SecurityNotConfiguredException(string message);
```

Creates the exception.

**Parameters**

- `message` (`string`): What is missing and how to supply it.
