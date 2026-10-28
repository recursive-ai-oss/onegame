[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# SimTrustAttribute attribute

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Inherits:** [`Attribute`](https://learn.microsoft.com/dotnet/api/system.attribute)  
**Applies to:** `Assembly`

```csharp
public sealed class SimTrustAttribute : Attribute
```

Lets sim code call a library you trust to be deterministic. [`SimTrustAttribute.Name`](SimTrustAttribute.md#m-name) is a namespace, a type's full name or an assembly name. The same list can be given as `onegame_sim_trusted = A.B; C.D` in `.editorconfig`.

## Constructors

| Member | Description |
| --- | --- |
| [`SimTrustAttribute(string)`](#m-ctor-string) | Trusts the namespace, type or assembly called `name`. |

### <a id="m-ctor-string"></a>SimTrustAttribute(string)

```csharp
public SimTrustAttribute(string name);
```

Trusts the namespace, type or assembly called `name`.

**Parameters**

- `name` (`string`): A namespace, a type's full name or an assembly name.

## Properties

| Member | Description |
| --- | --- |
| [`Name`](#m-name) | The trusted namespace, type or assembly name. |

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The trusted namespace, type or assembly name.

**Type:** `string`
