[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# UnknownClipException class

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)  
**Inherits:** [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

```csharp
public sealed class UnknownClipException : InvalidOperationException
```

A clip name that a [`SpriteSheet`](SpriteSheet.md) does not have.

## Constructors

| Member | Description |
| --- | --- |
| [`UnknownClipException(string, IEnumerable<string>)`](#m-ctor-string-ienumerable-string) | Creates the exception. |

### <a id="m-ctor-string-ienumerable-string"></a>UnknownClipException(string, IEnumerable&lt;string&gt;)

```csharp
public UnknownClipException(string name, IEnumerable<string> known);
```

Creates the exception.

**Parameters**

- `name` (`string`): The name that was asked for.
- `known` ([`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)`<string>`): The clips the sheet has.

## Properties

| Member | Description |
| --- | --- |
| [`Name`](#m-name) | The name that was asked for. |

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The name that was asked for.

**Type:** `string`
