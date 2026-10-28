[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# DrawKind enum

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public enum DrawKind : int
```

The kind of a [`DrawCommand`](DrawCommand.md).

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-rect"></a>`Rect` | `0` | Fill [`DrawCommand.Bounds`](DrawCommand.md#m-bounds) with [`DrawCommand.Color`](DrawCommand.md#m-color), rounded by [`DrawCommand.Radii`](DrawCommand.md#m-radii). |
| <a id="m-border"></a>`Border` | `1` | Stroke the inside edge of [`DrawCommand.Bounds`](DrawCommand.md#m-bounds), [`DrawCommand.Widths`](DrawCommand.md#m-widths) thick per side, rounded by [`DrawCommand.Radii`](DrawCommand.md#m-radii). |
| <a id="m-shadow"></a>`Shadow` | `2` | A blurred shadow of the rounded box [`DrawCommand.Bounds`](DrawCommand.md#m-bounds), moved by [`DrawCommand.Offset`](DrawCommand.md#m-offset), grown by [`DrawCommand.Spread`](DrawCommand.md#m-spread), blurred by [`DrawCommand.Value`](DrawCommand.md#m-value). It is not drawn inside the box itself. |
| <a id="m-image"></a>`Image` | `3` | Draw the image or SVG named [`DrawCommand.Resource`](DrawCommand.md#m-resource) into [`DrawCommand.Bounds`](DrawCommand.md#m-bounds), multiplied by [`DrawCommand.Color`](DrawCommand.md#m-color); [`DrawCommand.Slice`](DrawCommand.md#m-slice) gives nine-slice insets in pixels (zero: stretch). When [`DrawCommand.TileSize`](DrawCommand.md#m-tilesize) is set the image is placed or tiled instead. |
| <a id="m-text"></a>`Text` | `4` | Draw text run number [`DrawCommand.TextRun`](DrawCommand.md#m-textrun). |
| <a id="m-pushclip"></a>`PushClip` | `5` | Intersect the clip with the rounded rectangle [`DrawCommand.Bounds`](DrawCommand.md#m-bounds) until the matching [`DrawKind.PopClip`](DrawKind.md#m-popclip). |
| <a id="m-popclip"></a>`PopClip` | `6` | Restore the clip from before the matching [`DrawKind.PushClip`](DrawKind.md#m-pushclip). |
| <a id="m-pushtransform"></a>`PushTransform` | `7` | Apply [`DrawCommand.Transform`](DrawCommand.md#m-transform) to everything until the matching [`DrawKind.PopTransform`](DrawKind.md#m-poptransform). |
| <a id="m-poptransform"></a>`PopTransform` | `8` | Remove the transform pushed by the matching [`DrawKind.PushTransform`](DrawKind.md#m-pushtransform). |
| <a id="m-pushopacity"></a>`PushOpacity` | `9` | Draw everything until the matching [`DrawKind.PopOpacity`](DrawKind.md#m-popopacity) as a group, then blend it with [`DrawCommand.Value`](DrawCommand.md#m-value) opacity. |
| <a id="m-popopacity"></a>`PopOpacity` | `10` | End the group started by the matching [`DrawKind.PushOpacity`](DrawKind.md#m-pushopacity). |
