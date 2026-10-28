[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# BodyKind enum

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)

```csharp
public enum BodyKind : byte
```

How a body takes part in the simulation.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-static"></a>`Static` | `0` | Never moves: ground, walls, platforms. |
| <a id="m-kinematic"></a>`Kinematic` | `1` | Moves only as the game tells it (by velocity or teleport); pushes dynamic bodies and is never pushed. |
| <a id="m-dynamic"></a>`Dynamic` | `2` | Moves under gravity, forces and contacts. |
