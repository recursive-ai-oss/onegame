[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.Tweens

# EasingFamily enum

**Namespace:** `OneGame.Tweens`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public enum EasingFamily : byte
```

The shape family of a named easing curve (see [`Easing.Of()`](Easing.md#m-of-easingfamily-easingmode)).

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-sine"></a>`Sine` | `0` | A quarter sine wave: the gentlest curve. |
| <a id="m-quad"></a>`Quad` | `1` | t squared. |
| <a id="m-cubic"></a>`Cubic` | `2` | t cubed. |
| <a id="m-quart"></a>`Quart` | `3` | t to the fourth. |
| <a id="m-quint"></a>`Quint` | `4` | t to the fifth. |
| <a id="m-expo"></a>`Expo` | `5` | An exponential: almost flat, then a sharp rise (or the reverse). |
| <a id="m-circ"></a>`Circ` | `6` | A circle's arc. |
| <a id="m-back"></a>`Back` | `7` | Pulls back past the start (or overshoots the end) before settling. |
| <a id="m-elastic"></a>`Elastic` | `8` | A damped spring: overshoots and rings. |
| <a id="m-bounce"></a>`Bounce` | `9` | Bounces off the end like a dropped ball. |
