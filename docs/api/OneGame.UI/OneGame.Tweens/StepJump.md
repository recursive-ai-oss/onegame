[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.Tweens

# StepJump enum

**Namespace:** `OneGame.Tweens`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public enum StepJump : byte
```

Where a [`Easing.Steps()`](Easing.md#m-steps-int-stepjump) function jumps (CSS Easing Functions Level 1, §2.3).

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-end"></a>`End` | `0` | The last rise happens at the very end (CSS `jump-end`, the default). |
| <a id="m-start"></a>`Start` | `1` | The first rise happens at the very start (CSS `jump-start`). |
| <a id="m-none"></a>`None` | `2` | All rises happen strictly inside the range (CSS `jump-none`); needs at least 2 steps. |
| <a id="m-both"></a>`Both` | `3` | A rise at both the start and the end (CSS `jump-both`). |
