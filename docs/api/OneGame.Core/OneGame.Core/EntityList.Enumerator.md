[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# EntityList.Enumerator struct

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Declared in:** [`EntityList`](EntityList.md)  
**Implements:** [`IEnumerator`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerator-1)&lt;[`Entity`](Entity.md)&gt;, [`IEnumerator`](https://learn.microsoft.com/dotnet/api/system.collections.ienumerator), [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public struct EntityList.Enumerator : IEnumerator<Entity>, IEnumerator, IDisposable
```

Walks live entities; entities added while walking are visited too, removed ones are skipped.

## Properties

| Member | Description |
| --- | --- |
| [`Current`](#m-current) | The entity the walk stands on. |

### <a id="m-current"></a>Current

```csharp
public Entity Current { get; }
```

The entity the walk stands on.

**Type:** [`Entity`](Entity.md)

## Methods

| Member | Description |
| --- | --- |
| [`Dispose()`](#m-dispose) |  |
| [`MoveNext()`](#m-movenext) | Moves to the next live entity. |
| [`Reset()`](#m-reset) | Not supported. |

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Implements [`IDisposable.Dispose()`](https://learn.microsoft.com/dotnet/api/system.idisposable.dispose).

### <a id="m-movenext"></a>MoveNext()

```csharp
public bool MoveNext();
```

Moves to the next live entity.

**Returns:** `bool`

### <a id="m-reset"></a>Reset()

```csharp
public void Reset();
```

Not supported.
