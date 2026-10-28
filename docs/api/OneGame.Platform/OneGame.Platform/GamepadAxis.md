[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# GamepadAxis enum

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public enum GamepadAxis : byte
```

An analog axis on the standard gamepad layout. Sticks run from -1 to 1 with up and left negative, as in screen coordinates; triggers run from 0 to 1.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-leftx"></a>`LeftX` | `0` | Left stick horizontal; right is positive. |
| <a id="m-lefty"></a>`LeftY` | `1` | Left stick vertical; down is positive. |
| <a id="m-rightx"></a>`RightX` | `2` | Right stick horizontal; right is positive. |
| <a id="m-righty"></a>`RightY` | `3` | Right stick vertical; down is positive. |
| <a id="m-lefttrigger"></a>`LeftTrigger` | `4` | Left trigger, 0 released to 1 fully pressed. |
| <a id="m-righttrigger"></a>`RightTrigger` | `5` | Right trigger, 0 released to 1 fully pressed. |
