[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# EntityAttribute attribute

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Inherits:** [`Attribute`](https://learn.microsoft.com/dotnet/api/system.attribute)  
**Applies to:** `Class`

```csharp
public sealed class EntityAttribute : Attribute
```

Declares a partial class as an entity: synced properties, commands and sim logic in one class. The class must derive from [`Entity`](Entity.md) directly. The generator completes it with change tracking, codecs, deltas, snapshots and a `Send` property for commands; add the generated types to an [`EntityRegistry`](EntityRegistry.md) with the generated `EntityTypes.Register(registry)` (a game definition does this in `RegisterEntities`).

**Example**

A counter whose state changes only through a command and the per-tick step:

```csharp
[Entity]
public partial class Counter : Entity
{
    [Synced] public partial int Value { get; private set; }
    [Synced] public partial int Step { get; private set; }

    [Command]
    public void SetStep(int step) => Step = step;

    protected override void OnTick() => Value += Step;

    [View]
    public string Label() => $"count {Value}";   // views read state; assigning Value here is a compile error
}

// From a view or a test: counter.Send.SetStep(2);
```

## Constructors

| Member | Description |
| --- | --- |
| [`EntityAttribute()`](#m-ctor) |  |

### <a id="m-ctor"></a>EntityAttribute()

```csharp
public EntityAttribute();
```

## Properties

| Member | Description |
| --- | --- |
| [`Id`](#m-id) | A fixed type id (default 0 means "derive it"). |
| [`Schema`](#m-schema) | The schema version of the synced state. |
| [`Visibility`](#m-visibility) | Who may know the entity exists at all. |

### <a id="m-id"></a>Id

```csharp
public uint Id { get; set; }
```

A fixed type id (default 0 means "derive it"). By default the id is a hash of the full type name, which stays stable until the type is renamed or moved.

**Type:** `uint`

### <a id="m-schema"></a>Schema

```csharp
public uint Schema { get; set; }
```

The schema version of the synced state. Raise it whenever properties are added, removed, reordered or retyped, and add a [`MigrateAttribute`](MigrateAttribute.md) method for each older version still in saved data. Defaults to 1.

**Type:** `uint`

### <a id="m-visibility"></a>Visibility

```csharp
public Visibility Visibility { get; set; }
```

Who may know the entity exists at all. Subscribers outside the audience never receive the entity. Members can be narrower still (see [`SyncedAttribute.Visibility`](SyncedAttribute.md#m-visibility)). [`Visibility.Owner`](Visibility.md#m-owner) and [`Visibility.Team`](Visibility.md#m-team) need an [`OwnerKeyAttribute`](OwnerKeyAttribute.md) or [`TeamKeyAttribute`](TeamKeyAttribute.md) property.

**Type:** [`Visibility`](Visibility.md)
