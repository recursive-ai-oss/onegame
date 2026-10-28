[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# MessageTooLargeException class

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)  
**Inherits:** [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception)

```csharp
public sealed class MessageTooLargeException : ArgumentException
```

A message was larger than the configured maximum, so it was refused before anything was queued.

## Constructors

| Member | Description |
| --- | --- |
| [`MessageTooLargeException(int, int)`](#m-ctor-int-int) | Creates the exception. |

### <a id="m-ctor-int-int"></a>MessageTooLargeException(int, int)

```csharp
public MessageTooLargeException(int size, int limit);
```

Creates the exception.

**Parameters**

- `size` (`int`): The size that was offered.
- `limit` (`int`): The configured maximum.

## Properties

| Member | Description |
| --- | --- |
| [`Limit`](#m-limit) | The configured maximum. |
| [`Size`](#m-size) | The size that was offered. |

### <a id="m-limit"></a>Limit

```csharp
public int Limit { get; }
```

The configured maximum.

**Type:** `int`

### <a id="m-size"></a>Size

```csharp
public int Size { get; }
```

The size that was offered.

**Type:** `int`
