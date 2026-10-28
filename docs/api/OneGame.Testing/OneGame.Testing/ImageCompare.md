[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# ImageCompare static class

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)

```csharp
public static class ImageCompare
```

Per-pixel image comparison for capture checks.

## Methods

| Member | Description |
| --- | --- |
| [`Compare(CapturedFrame, CapturedFrame, int)`](#m-compare-capturedframe-capturedframe-int) | Compares two frames. |
| [`DiffImage(CapturedFrame, CapturedFrame, int)`](#m-diffimage-capturedframe-capturedframe-int) | An image that shows where two same-sized frames differ: the expected frame dimmed, with differing pixels in red. |

### <a id="m-compare-capturedframe-capturedframe-int"></a>Compare(CapturedFrame, CapturedFrame, int)

```csharp
public static ImageDiff Compare(CapturedFrame actual, CapturedFrame expected, int channelTolerance = 0);
```

Compares two frames. A pixel differs when any of its four channels differs by more than `channelTolerance`.

**Parameters**

- `actual` ([`CapturedFrame`](../../OneGame.Host/OneGame.Host/CapturedFrame.md))
- `expected` ([`CapturedFrame`](../../OneGame.Host/OneGame.Host/CapturedFrame.md))
- `channelTolerance` (`int`)

**Returns:** [`ImageDiff`](ImageDiff.md)

### <a id="m-diffimage-capturedframe-capturedframe-int"></a>DiffImage(CapturedFrame, CapturedFrame, int)

```csharp
public static CapturedFrame DiffImage(CapturedFrame actual, CapturedFrame expected, int channelTolerance = 0);
```

An image that shows where two same-sized frames differ: the expected frame dimmed, with differing pixels in red.

**Parameters**

- `actual` ([`CapturedFrame`](../../OneGame.Host/OneGame.Host/CapturedFrame.md))
- `expected` ([`CapturedFrame`](../../OneGame.Host/OneGame.Host/CapturedFrame.md))
- `channelTolerance` (`int`)

**Returns:** [`CapturedFrame`](../../OneGame.Host/OneGame.Host/CapturedFrame.md)
