[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.Tweens

# EasingMode enum

**Namespace:** `OneGame.Tweens`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public enum EasingMode : byte
```

Which end of a named easing curve is eased.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-in"></a>`In` | `0` | Starts slowly, ends at full speed. |
| <a id="m-out"></a>`Out` | `1` | Starts at full speed, ends slowly. |
| <a id="m-inout"></a>`InOut` | `2` | [`EasingMode.In`](EasingMode.md#m-in) for the first half and [`EasingMode.Out`](EasingMode.md#m-out) for the second. |
