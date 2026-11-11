[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# PhysicsEvents3D.Enumerator struct

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)  
**Declared in:** [`PhysicsEvents3D`](PhysicsEvents3D.md)

```csharp
public struct PhysicsEvents3D.Enumerator
```

Walks the events without allocating.

## Properties

| Member | Description |
| --- | --- |
| [`Current`](#m-current) | The event the enumerator stands on. |

### <a id="m-current"></a>Current

```csharp
public PhysicsEvent3D Current { get; }
```

The event the enumerator stands on.

**Type:** [`PhysicsEvent3D`](PhysicsEvent3D.md)

## Methods

| Member | Description |
| --- | --- |
| [`MoveNext()`](#m-movenext) | Moves to the next event. |

### <a id="m-movenext"></a>MoveNext()

```csharp
public bool MoveNext();
```

Moves to the next event.

**Returns:** `bool` — False when there are no more.
