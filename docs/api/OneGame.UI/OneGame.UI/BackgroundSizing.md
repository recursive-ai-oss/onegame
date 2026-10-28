[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# BackgroundSizing enum

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public enum BackgroundSizing : int
```

The keyword forms of `background-size` (CSS Backgrounds 3 §3.9).

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-fill"></a>`Fill` | `0` | The image is stretched to the whole box. This is the default and what an image element does. |
| <a id="m-auto"></a>`Auto` | `1` | The image's natural size times the UI scale. `background-width` and `background-height` override it. |
| <a id="m-cover"></a>`Cover` | `2` | Scaled, keeping its aspect ratio, to the smallest size that covers the box. |
| <a id="m-contain"></a>`Contain` | `3` | Scaled, keeping its aspect ratio, to the largest size that fits inside the box. |
