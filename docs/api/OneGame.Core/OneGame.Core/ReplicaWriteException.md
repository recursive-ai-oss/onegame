[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# ReplicaWriteException class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Inherits:** [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

```csharp
public sealed class ReplicaWriteException : InvalidOperationException
```

Thrown when code changes state on a replica. Only the authority changes state; replicas send commands.

## Constructors

| Member | Description |
| --- | --- |
| [`ReplicaWriteException(string)`](#m-ctor-string) | Creates the exception. |

### <a id="m-ctor-string"></a>ReplicaWriteException(string)

```csharp
public ReplicaWriteException(string message);
```

Creates the exception.

**Parameters**

- `message` (`string`)
