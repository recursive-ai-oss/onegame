[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# HapticKind enum

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public enum HapticKind : int
```

The kinds of touch feedback a game can ask for. Each maps to the nearest system effect on the platform.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-tick"></a>`Tick` | `0` | A very light tick: a piece snaps into place, a slider crosses a step, a selection changes. |
| <a id="m-click"></a>`Click` | `1` | A medium click: a button press. |
| <a id="m-buzz"></a>`Buzz` | `2` | A strong, longer pulse: a hit, an explosion, a big moment. |
| <a id="m-success"></a>`Success` | `3` | The action worked. |
| <a id="m-warning"></a>`Warning` | `4` | The action went through but something needs attention. |
| <a id="m-error"></a>`Error` | `5` | The action failed or was refused. |
