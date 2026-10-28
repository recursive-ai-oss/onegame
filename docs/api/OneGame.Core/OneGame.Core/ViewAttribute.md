[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# ViewAttribute attribute

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Inherits:** [`Attribute`](https://learn.microsoft.com/dotnet/api/system.attribute)  
**Applies to:** `Class, Struct, Method, Property, Field`

```csharp
public sealed class ViewAttribute : Attribute
```

Marks a member or a whole class as view code. Views read state and send commands; the analyzers make writing synced state from a view a compile error. View code may use floats and other things sim code may not.

## Constructors

| Member | Description |
| --- | --- |
| [`ViewAttribute()`](#m-ctor) |  |

### <a id="m-ctor"></a>ViewAttribute()

```csharp
public ViewAttribute();
```
