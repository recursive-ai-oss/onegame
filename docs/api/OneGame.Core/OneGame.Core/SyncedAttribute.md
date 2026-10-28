[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# SyncedAttribute attribute

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Inherits:** [`Attribute`](https://learn.microsoft.com/dotnet/api/system.attribute)  
**Applies to:** `Property`

```csharp
public sealed class SyncedAttribute : Attribute
```

Marks a partial property as synced state. It becomes tracked, serialized, hashed and replicated. Properties are written in declaration order.

## Constructors

| Member | Description |
| --- | --- |
| [`SyncedAttribute()`](#m-ctor) |  |

### <a id="m-ctor"></a>SyncedAttribute()

```csharp
public SyncedAttribute();
```

## Properties

| Member | Description |
| --- | --- |
| [`Visibility`](#m-visibility) | Who may receive the property. |

### <a id="m-visibility"></a>Visibility

```csharp
public Visibility Visibility { get; set; }
```

Who may receive the property. The authority leaves it out of the snapshots and deltas it builds for everyone else, so a replica holds the type's default there. Defaults to everyone who may read the scope.

**Type:** [`Visibility`](Visibility.md)
