[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# ActionKind enum

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public enum ActionKind : byte
```

What shape of value an action produces.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-button"></a>`Button` | `0` | Down or up, with edges: [`InputAction.Pressed`](InputAction.md#m-pressed), [`InputAction.Held`](InputAction.md#m-held), [`InputAction.Released`](InputAction.md#m-released). [`InputAction.Value`](InputAction.md#m-value) is the analog strength 0..1. |
| <a id="m-axis"></a>`Axis` | `1` | One axis, -1..1 (the mouse wheel reports notches). |
| <a id="m-vector"></a>`Vector` | `2` | Two axes. x grows right and y grows down, like the screen and the pad's sticks; a diagonal from keys has length 1. |
