[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# LengthUnit enum

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public enum LengthUnit : byte
```

The unit of a [`Length`](Length.md).

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-auto"></a>`Auto` | `0` | No value: CSS `auto` (or `none` for maximum sizes). |
| <a id="m-px"></a>`Px` | `1` | Device pixels; unaffected by the UI scale. |
| <a id="m-percent"></a>`Percent` | `2` | A percentage of the reference box for the property, as in CSS. |
| <a id="m-ui"></a>`Ui` | `3` | UI units: one unit is [`UiTree.Scale`](UiTree.md#m-scale) pixels, so a layout written in UI units follows the player's UI scale setting. |
