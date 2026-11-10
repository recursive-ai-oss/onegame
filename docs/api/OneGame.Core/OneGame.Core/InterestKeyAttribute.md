[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# InterestKeyAttribute attribute

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Inherits:** [`Attribute`](https://learn.microsoft.com/dotnet/api/system.attribute)  
**Applies to:** `Property`

```csharp
public sealed class InterestKeyAttribute : Attribute
```

Marks the synced [`UInt32`](https://learn.microsoft.com/dotnet/api/system.uint32) or [`Int32`](https://learn.microsoft.com/dotnet/api/system.int32) property that says which interest cell the entity is in (for example a grid cell from `InterestGrid2D`). A connection that has been given an interest set (`AuthorityScope.SetInterest`) receives the entity only while the set holds its key. Zero means global: always sent. The property must be [`Visibility.Everyone`](Visibility.md#m-everyone). At most one per entity.

## Constructors

| Member | Description |
| --- | --- |
| [`InterestKeyAttribute()`](#m-ctor) |  |

### <a id="m-ctor"></a>InterestKeyAttribute()

```csharp
public InterestKeyAttribute();
```
