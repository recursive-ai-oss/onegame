[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# BlendOperation enum

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public enum BlendOperation : int
```

Operation combining the blended source and destination.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-add"></a>`Add` | `0` | Source + destination. |
| <a id="m-subtract"></a>`Subtract` | `1` | Source - destination. |
| <a id="m-reversesubtract"></a>`ReverseSubtract` | `2` | Destination - source. |
| <a id="m-min"></a>`Min` | `3` | Per-channel minimum. |
| <a id="m-max"></a>`Max` | `4` | Per-channel maximum. |
