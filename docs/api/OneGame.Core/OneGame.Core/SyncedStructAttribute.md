[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# SyncedStructAttribute attribute

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Inherits:** [`Attribute`](https://learn.microsoft.com/dotnet/api/system.attribute)  
**Applies to:** `Struct`

```csharp
public sealed class SyncedStructAttribute : Attribute
```

Marks a partial struct whose public fields and properties form a synced value. The generator adds the codec, equality and JSON dump.

## Constructors

| Member | Description |
| --- | --- |
| [`SyncedStructAttribute()`](#m-ctor) |  |

### <a id="m-ctor"></a>SyncedStructAttribute()

```csharp
public SyncedStructAttribute();
```
