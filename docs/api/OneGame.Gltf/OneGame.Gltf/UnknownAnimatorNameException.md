[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# UnknownAnimatorNameException class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)  
**Inherits:** [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception)

```csharp
public sealed class UnknownAnimatorNameException : ArgumentException
```

A parameter, state or clip name an [`Animator`](Animator.md) does not have.

## Constructors

| Member | Description |
| --- | --- |
| [`UnknownAnimatorNameException(string, string, IEnumerable<string>)`](#m-ctor-string-string-ienumerable-string) | Creates the exception. |

### <a id="m-ctor-string-string-ienumerable-string"></a>UnknownAnimatorNameException(string, string, IEnumerable&lt;string&gt;)

```csharp
public UnknownAnimatorNameException(string kind, string name, IEnumerable<string> known);
```

Creates the exception.

**Parameters**

- `kind` (`string`): What was looked up: parameter, state or trigger.
- `name` (`string`): The name that was asked for.
- `known` ([`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)`<string>`): The names that exist.
