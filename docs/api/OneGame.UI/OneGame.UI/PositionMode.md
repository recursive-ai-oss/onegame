[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# PositionMode enum

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public enum PositionMode : int
```

The `position` property.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-relative"></a>`Relative` | `0` | In flow; `left/top/right/bottom` shift the box after layout. |
| <a id="m-absolute"></a>`Absolute` | `1` | Out of flow, placed against the padding box of its parent. |
| <a id="m-fixed"></a>`Fixed` | `2` | Out of flow, placed against the screen (the root's size). |
