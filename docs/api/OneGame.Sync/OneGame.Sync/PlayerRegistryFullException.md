[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# PlayerRegistryFullException class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Inherits:** [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

```csharp
public sealed class PlayerRegistryFullException : InvalidOperationException
```

The registry holds as many players as it may and every one of them has been activated, so there is nobody to forget.

## Constructors

| Member | Description |
| --- | --- |
| [`PlayerRegistryFullException(int)`](#m-ctor-int) | Creates the exception. |

### <a id="m-ctor-int"></a>PlayerRegistryFullException(int)

```csharp
public PlayerRegistryFullException(int limit);
```

Creates the exception.

**Parameters**

- `limit` (`int`): The registry's limit.
