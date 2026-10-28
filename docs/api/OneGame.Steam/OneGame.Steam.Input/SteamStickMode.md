[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam.Input

# SteamStickMode enum

**Namespace:** `OneGame.Steam.Input`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public enum SteamStickMode : int
```

How Steam interprets a two-axis action. Both values are the names the in-game actions file uses.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-joystickmove"></a>`JoystickMove` | `0` | A joystick: constant deflection from a centre point. Use for movement. |
| <a id="m-absolutemouse"></a>`AbsoluteMouse` | `1` | Mouse-like deltas that also work on pads, gyro and sticks. Valve recommends it for cameras and cursors. |
