[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# EntityList class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Implements:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`Entity`](Entity.md)&gt;, [`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)&lt;[`Entity`](Entity.md)&gt;, [`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.ienumerable), [`IReadOnlyCollection`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlycollection-1)&lt;[`Entity`](Entity.md)&gt;

```csharp
public sealed class EntityList : IReadOnlyList<Entity>, IEnumerable<Entity>, IEnumerable, IReadOnlyCollection<Entity>
```

The live entities of a world, in id order. An entity removed during the current tick is already gone from this view, although the world keeps it until the tick ends: code that removes an entity and then looks again (or runs a second command in the same tick) never meets it twice. The view follows the world, so it can be kept; `foreach` over it allocates nothing.

## Nested types

| Type | Description |
| --- | --- |
| [`EntityList.Enumerator`](EntityList.Enumerator.md) | Walks live entities; entities added while walking are visited too, removed ones are skipped. |

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | The number of live entities. |
| [`this[int]`](#m-item-int) | The live entity at a position in id order. |

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

The number of live entities.

**Type:** `int`

### <a id="m-item-int"></a>this\[int\]

```csharp
public Entity this[int index] { get; }
```

The live entity at a position in id order.

**Parameters**

- `index` (`int`): A position from 0 to [`EntityList.Count`](EntityList.md#m-count) - 1.

**Type:** [`Entity`](Entity.md)

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The position is outside the list.

## Methods

| Member | Description |
| --- | --- |
| [`GetEnumerator()`](#m-getenumerator) | Walks the live entities. |

### <a id="m-getenumerator"></a>GetEnumerator()

```csharp
public EntityList.Enumerator GetEnumerator();
```

Walks the live entities.

**Returns:** `EntityList.`[`Enumerator`](EntityList.Enumerator.md)
