[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# SimEventAttribute attribute

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Inherits:** [`Attribute`](https://learn.microsoft.com/dotnet/api/system.attribute)  
**Applies to:** `Struct`

```csharp
public sealed class SimEventAttribute : Attribute
```

Declares a partial struct as a one-shot sim event: something that happened on one tick ("wall completed at cell 12", "life lost") that views, audio and the harness react to exactly once. Its public fields and properties are the payload, with the same rules as [`SyncedStructAttribute`](SyncedStructAttribute.md) (numbers, bool, string, enums, `Fixed` and the other synced value types; never floats). Sim code emits it with [`Entity.Emit()`](Entity.md#m-emit-t-t) or [`World.Emit()`](World.md#m-emit-t-t), and views read it through [`World.Events`](World.md#m-events).

**Example**

```csharp
[SimEvent] public partial struct WallDone { public int Cell { get; set; } }

// sim:  Emit(new WallDone { Cell = 12 });
// view: var events = world.Events.NewReader();   // once
//       while (events.Next<WallDone>(out var done, out var info)) PlayPop(done.Cell);
```

## Constructors

| Member | Description |
| --- | --- |
| [`SimEventAttribute()`](#m-ctor) |  |

### <a id="m-ctor"></a>SimEventAttribute()

```csharp
public SimEventAttribute();
```

## Properties

| Member | Description |
| --- | --- |
| [`Visibility`](#m-visibility) | Who may receive the event. |

### <a id="m-visibility"></a>Visibility

```csharp
public Visibility Visibility { get; set; }
```

Who may receive the event. [`Visibility.Everyone`](Visibility.md#m-everyone) (default) follows the emitting entity's own visibility and interest; [`Visibility.Owner`](Visibility.md#m-owner) and [`Visibility.Team`](Visibility.md#m-team) narrow it to the emitting entity's owner or team (they need an entity, so emit them with [`Entity.Emit()`](Entity.md#m-emit-t-t)); [`Visibility.Hidden`](Visibility.md#m-hidden) keeps the event on the authority.

**Type:** [`Visibility`](Visibility.md)
