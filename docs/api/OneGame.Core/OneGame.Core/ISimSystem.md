[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# ISimSystem interface

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public interface ISimSystem
```

Sim logic that is not tied to one entity. Systems tick after all entities, in the order they were added. Keep their state in entities or tables, never in fields, so snapshots and hashes cover it.

## Methods

| Member | Description |
| --- | --- |
| [`Tick(World)`](#m-tick-world) | Runs one tick. |

### <a id="m-tick-world"></a>Tick(World)

```csharp
void Tick(World world);
```

Runs one tick.

**Parameters**

- `world` ([`World`](World.md))
