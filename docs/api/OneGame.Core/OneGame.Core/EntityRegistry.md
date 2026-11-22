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
| [`AddEvent<T>()`](#m-addevent-t) | Registers a sim event type so the harness can name it and print its payload. |
| [`FindEvent(string)`](#m-findevent-string) | The id of a registered event type by name, or null. |

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

### <a id="m-addevent-t"></a>AddEvent&lt;T&gt;()

```csharp
public void AddEvent<T>() where T : ISimEvent<T>;
```

Registers a sim event type so the harness can name it and print its payload. The generated `EntityTypes.Register` calls it for every [`SimEventAttribute`](SimEventAttribute.md) struct.

**Type parameters**

- `T`: The event type.

**Exceptions**

- [`DuplicateTypeException`](DuplicateTypeException.md): Another event type has the same id.

### <a id="m-findevent-string"></a>FindEvent(string)

```csharp
public uint? FindEvent(string name);
```

The id of a registered event type by name, or null.

**Parameters**

- `name` (`string`): The event's name (its type name).

**Returns:** `uint?`
