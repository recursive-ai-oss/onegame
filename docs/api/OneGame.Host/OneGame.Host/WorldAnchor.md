[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# WorldAnchor static class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public static class WorldAnchor
```

Pins UI elements to points in a 3D world.

## Methods

| Member | Description |
| --- | --- |
| [`PinToWorld(Element, SceneView3D, GameInstance, Func<FVec3?>, Vector2, Vector2?)`](#m-pintoworld-element-sceneview3d-gameinstance-func-fvec3-vector2-vector2) | Keeps an element at the screen position of a world point, and hides it while the point is behind the camera, beyond its near or far plane, or off screen. |

### <a id="m-pintoworld-element-sceneview3d-gameinstance-func-fvec3-vector2-vector2"></a>PinToWorld(Element, SceneView3D, GameInstance, Func&lt;FVec3?&gt;, Vector2, Vector2?)

```csharp
public static Element PinToWorld(this Element element, SceneView3D view, GameInstance game, Func<FVec3?> worldPoint, Vector2 pixelOffset = default, Vector2? pivot = default);
```

Keeps an element at the screen position of a world point, and hides it while the point is behind the camera, beyond its near or far plane, or off screen. The point is read every UI frame, so a label can follow a moving entity. The element should be a direct child of the screen root, because it is placed against the screen.

**Parameters**

- `element` ([`Element`](../../OneGame.UI/OneGame.UI/Element.md)): The element to pin.
- `view` ([`SceneView3D`](SceneView3D.md)): The scene view whose camera projects the point.
- `game` ([`GameInstance`](GameInstance.md)): The game being shown; its world is asked what the scene looks like, and its UI size is the screen.
- `worldPoint` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)&lt;[`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md)?&gt;): Reads the point each frame; return null to hide the element (an entity that is gone).
- `pixelOffset` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Extra shift in pixels (positive is right and down).
- `pivot` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)?): Which point of the element sits on the world point, from (0,0) top left to (1,1) bottom right. Default: bottom centre, so a label floats above.

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md) — The element.
