[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# SimAttribute attribute

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Inherits:** [`Attribute`](https://learn.microsoft.com/dotnet/api/system.attribute)  
**Applies to:** `Class, Struct, Method`

```csharp
public sealed class SimAttribute : Attribute
```

Marks a class, struct or method outside an entity as sim code, so the sim analyzers check it. Entities and [`ISimSystem`](ISimSystem.md) types are sim code already.

## Constructors

| Member | Description |
| --- | --- |
| [`SimAttribute()`](#m-ctor) |  |

### <a id="m-ctor"></a>SimAttribute()

```csharp
public SimAttribute();
```
