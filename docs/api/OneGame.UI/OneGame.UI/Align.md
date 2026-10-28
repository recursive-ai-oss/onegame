[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# Align enum

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public enum Align : int
```

Values for `justify-content`, `align-items`, `align-self` and `align-content`. `flex-start` and `start` are the same here, as are `flex-end` and `end`.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-auto"></a>`Auto` | `0` | For `align-self`: use the parent's `align-items`. For `align-content`: `normal`, which stretches. |
| <a id="m-start"></a>`Start` | `1` | Pack toward the start. |
| <a id="m-end"></a>`End` | `2` | Pack toward the end. |
| <a id="m-center"></a>`Center` | `3` | Center. |
| <a id="m-stretch"></a>`Stretch` | `4` | Fill the cross axis (items without a cross size). |
| <a id="m-baseline"></a>`Baseline` | `5` | Align on text baselines. Baselines are not computed yet; this behaves as [`Align.Start`](Align.md#m-start). |
| <a id="m-spacebetween"></a>`SpaceBetween` | `6` | First and last at the edges, equal space between. |
| <a id="m-spacearound"></a>`SpaceAround` | `7` | Equal space around each item (half at the edges). |
| <a id="m-spaceevenly"></a>`SpaceEvenly` | `8` | Equal space between and at the edges. |
