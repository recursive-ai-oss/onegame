[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# OutputLimiterMode enum

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)

```csharp
public enum OutputLimiterMode : int
```

How the output stage keeps samples in range before they reach the device.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-clamp"></a>`Clamp` | `0` | Hard clamp to -1..1. |
| <a id="m-softknee"></a>`SoftKnee` | `1` | Samples below 0.9 pass untouched; above that a smooth knee approaches 1 without reaching it. |
