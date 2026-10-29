[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# PhysicsEvents struct

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)

```csharp
public readonly struct PhysicsEvents
```

The events of the last step, in the order the physics world delivered them: by kind, then by the bodies' slots. The order is the same on every node. Read them in `OnTick`; the list is replaced by the next step and is saved and restored with the space.

## Nested types

| Type | Description |
| --- | --- |
| [`PhysicsEvents.Enumerator`](PhysicsEvents.Enumerator.md) | Walks the events without allocating. |

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | The number of events. |
| [`this[int]`](#m-item-int) | The event at an index, from 0 to [`PhysicsEvents.Count`](PhysicsEvents.md#m-count) - 1. |

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

The number of events.

**Type:** `int`

### <a id="m-item-int"></a>this\[int\]

```csharp
public PhysicsEvent this[int index] { get; }
```

The event at an index, from 0 to [`PhysicsEvents.Count`](PhysicsEvents.md#m-count) - 1.

**Parameters**

- `index` (`int`): The index.

**Type:** [`PhysicsEvent`](PhysicsEvent.md)

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The index is outside the list.

## Methods

| Member | Description |
| --- | --- |
| [`GetEnumerator()`](#m-getenumerator) | Returns an enumerator over the events. |

### <a id="m-getenumerator"></a>GetEnumerator()

```csharp
public PhysicsEvents.Enumerator GetEnumerator();
```

Returns an enumerator over the events.

**Returns:** `PhysicsEvents.`[`Enumerator`](PhysicsEvents.Enumerator.md)
