[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# UnknownModelException class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Inherits:** [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

```csharp
public sealed class UnknownModelException : InvalidOperationException
```

A model name that no [`ModelLibrary`](ModelLibrary.md) entry has.

## Constructors

| Member | Description |
| --- | --- |
| [`UnknownModelException(string, IEnumerable<string>)`](#m-ctor-string-ienumerable-string) | Creates the exception. |

### <a id="m-ctor-string-ienumerable-string"></a>UnknownModelException(string, IEnumerable&lt;string&gt;)

```csharp
public UnknownModelException(string id, IEnumerable<string> known);
```

Creates the exception.

**Parameters**

- `id` (`string`)
- `known` ([`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)`<string>`)

## Properties

| Member | Description |
| --- | --- |
| [`Id`](#m-id) | The name that was asked for. |

### <a id="m-id"></a>Id

```csharp
public string Id { get; }
```

The name that was asked for.

**Type:** `string`
