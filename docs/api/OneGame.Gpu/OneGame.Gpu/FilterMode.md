[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# FilterMode enum

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public enum FilterMode : int
```

Texture filtering.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-nearest"></a>`Nearest` | `0` | Take the nearest texel: hard edges, right for pixel art. |
| <a id="m-linear"></a>`Linear` | `1` | Blend the neighbouring texels: smooth. |
