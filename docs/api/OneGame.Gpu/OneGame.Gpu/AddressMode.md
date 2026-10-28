[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# AddressMode enum

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public enum AddressMode : int
```

Texture coordinate handling outside 0..1.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-clamptoedge"></a>`ClampToEdge` | `0` | Clamp to the edge texel. |
| <a id="m-repeat"></a>`Repeat` | `1` | Repeat. |
| <a id="m-mirrorrepeat"></a>`MirrorRepeat` | `2` | Repeat, mirroring every other tile. |
