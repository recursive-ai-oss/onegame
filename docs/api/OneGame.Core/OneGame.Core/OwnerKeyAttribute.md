[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# OwnerKeyAttribute attribute

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Inherits:** [`Attribute`](https://learn.microsoft.com/dotnet/api/system.attribute)  
**Applies to:** `Property`

```csharp
public sealed class OwnerKeyAttribute : Attribute
```

Marks the synced [`UInt32`](https://learn.microsoft.com/dotnet/api/system.uint32) or [`Int32`](https://learn.microsoft.com/dotnet/api/system.int32) property that says which player owns the entity. A connection whose trust names the same player (`Audience.Player`) is the entity's owner. Zero means nobody. At most one per entity.

## Constructors

| Member | Description |
| --- | --- |
| [`OwnerKeyAttribute()`](#m-ctor) |  |

### <a id="m-ctor"></a>OwnerKeyAttribute()

```csharp
public OwnerKeyAttribute();
```
