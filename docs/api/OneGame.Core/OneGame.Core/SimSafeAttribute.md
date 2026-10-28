[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# SimSafeAttribute attribute

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Inherits:** [`Attribute`](https://learn.microsoft.com/dotnet/api/system.attribute)  
**Applies to:** `Assembly, Class, Struct, Enum, Constructor, Method, Property, Interface, Delegate`

```csharp
public sealed class SimSafeAttribute : Attribute
```

Vouches that an API gives the same result on every machine and run, so sim code may call it. Put it on a method, a type or a whole assembly. Sim code that calls an API outside its own project without this attribute (or a [`SimTrustAttribute`](SimTrustAttribute.md) entry, or a built-in framework allowance) gets warning OG0015. Helpers in the same project need no attribute: the analyzers read their bodies instead, and the attribute on a helper tells them to stop looking.

## Constructors

| Member | Description |
| --- | --- |
| [`SimSafeAttribute()`](#m-ctor) |  |

### <a id="m-ctor"></a>SimSafeAttribute()

```csharp
public SimSafeAttribute();
```
