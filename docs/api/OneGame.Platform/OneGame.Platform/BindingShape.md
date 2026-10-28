[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# BindingShape enum

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public enum BindingShape : byte
```

The arrangement of an [`ActionBinding`](ActionBinding.md).

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-one"></a>`One` | `0` | One source. Buttons take it as pressed; a 1D axis reads its value. |
| <a id="m-pair"></a>`Pair` | `1` | Two sources for a 1D axis: the first pulls it to -1, the second to +1. |
| <a id="m-quad"></a>`Quad` | `2` | Four sources for a 2D vector: left, right, up, down (y grows downward, like the screen). |
| <a id="m-stick"></a>`Stick` | `3` | A pad stick as a 2D vector. Source 0 is the left stick or the right stick ([`GamepadAxis.LeftX`](GamepadAxis.md#m-leftx) or [`GamepadAxis.RightX`](GamepadAxis.md#m-rightx)). |
