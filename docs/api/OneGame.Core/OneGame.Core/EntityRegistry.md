[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# EntityRegistry class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public sealed class EntityRegistry
```

The entity types a world can create while decoding a snapshot. Generated code fills it: call `EntityTypes.Register(registry)` from your assembly's generated namespace.

## Constructors

| Member | Description |
| --- | --- |
| [`EntityRegistry()`](#m-ctor) |  |

### <a id="m-ctor"></a>EntityRegistry()

```csharp
public EntityRegistry();
```

## Methods

| Member | Description |
| --- | --- |
| [`Add(uint, string, Func<Entity>)`](#m-add-uint-string-func-entity) | Registers an entity type. |

### <a id="m-add-uint-string-func-entity"></a>Add(uint, string, Func&lt;Entity&gt;)

```csharp
public void Add(uint typeId, string name, Func<Entity> factory);
```

Registers an entity type.

**Parameters**

- `typeId` (`uint`)
- `name` (`string`)
- `factory` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)&lt;[`Entity`](Entity.md)&gt;)

**Exceptions**

- [`DuplicateTypeException`](DuplicateTypeException.md): The id is already taken by another type.
