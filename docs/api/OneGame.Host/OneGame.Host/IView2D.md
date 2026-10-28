[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# IView2D interface

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public interface IView2D
```

Implemented by an entity that draws itself in 2D. The host calls [`IView2D.Draw()`](IView2D.md#m-draw-draw2d) on every such entity once per drawn frame, in entity order, and the 2D render stage shows what was drawn. Mark the implementation `[View]` so the analyzers check that it only reads state. Draw positions are in world units (+X right, +Y down, one unit is one pixel at camera zoom 1); convert synced `Fixed` values with `ToSingle()` here, in the view, never in sim code.

**Example**

```csharp
[Entity]
public partial class Ship : Entity, IView2D
{
    [Synced] public partial Fixed X { get; private set; }
    [Synced] public partial Fixed Y { get; private set; }

    [View]
    public void Draw(Draw2D d) =>
        d.Sprite("art/ship.png", new Vector2(X.ToSingle(), Y.ToSingle()), scale: 2);
}
```

## Methods

| Member | Description |
| --- | --- |
| [`Draw(Draw2D)`](#m-draw-draw2d) | Draws this entity as it looks right now. |

### <a id="m-draw-draw2d"></a>Draw(Draw2D)

```csharp
void Draw(Draw2D d);
```

Draws this entity as it looks right now. Read state; never change it.

**Parameters**

- `d` ([`Draw2D`](Draw2D.md)): Collects the sprites, shapes and text. [`Draw2D.Tick`](Draw2D.md#m-tick), [`Draw2D.Alpha`](Draw2D.md#m-alpha) and [`Draw2D.Time`](Draw2D.md#m-time) say which moment to show.
