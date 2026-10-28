[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# InputSourceKind enum

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public enum InputSourceKind : byte
```

What kind of control an [`InputSource`](InputSource.md) reads.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-none"></a>`None` | `0` | Nothing: an empty slot. |
| <a id="m-key"></a>`Key` | `1` | A keyboard key (physical position, like [`InputSourceKind.Key`](InputSourceKind.md#m-key)). |
| <a id="m-mouse"></a>`Mouse` | `2` | A mouse button. |
| <a id="m-padbutton"></a>`PadButton` | `3` | A gamepad button on the standard layout; any connected pad counts. |
| <a id="m-padaxis"></a>`PadAxis` | `4` | A gamepad stick or trigger axis, in one direction (or inverted when used on its own). |
| <a id="m-wheel"></a>`Wheel` | `5` | The mouse wheel, in notches per frame. |
| <a id="m-touch"></a>`Touch` | `6` | A finger down in part of the window. |
