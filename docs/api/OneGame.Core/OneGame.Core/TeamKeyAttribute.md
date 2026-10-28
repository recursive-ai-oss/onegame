[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# TeamKeyAttribute attribute

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Inherits:** [`Attribute`](https://learn.microsoft.com/dotnet/api/system.attribute)  
**Applies to:** `Property`

```csharp
public sealed class TeamKeyAttribute : Attribute
```

Marks the synced [`UInt32`](https://learn.microsoft.com/dotnet/api/system.uint32) or [`Int32`](https://learn.microsoft.com/dotnet/api/system.int32) property that says which team the entity belongs to. A connection whose trust names the same team (`Audience.Team`) is on the entity's team, and so is the owner. Zero means no team. At most one per entity.

## Constructors

| Member | Description |
| --- | --- |
| [`TeamKeyAttribute()`](#m-ctor) |  |

### <a id="m-ctor"></a>TeamKeyAttribute()

```csharp
public TeamKeyAttribute();
```
