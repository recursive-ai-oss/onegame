[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# StepKind enum

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)

```csharp
public enum StepKind : int
```

What a scenario step does.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-click"></a>`Click` | `0` | Click an element with pointer events. |
| <a id="m-clickat"></a>`ClickAt` | `1` | Click at window coordinates. |
| <a id="m-tap"></a>`Tap` | `2` | Tap an element with a finger. |
| <a id="m-tapat"></a>`TapAt` | `3` | Tap at window coordinates. |
| <a id="m-touch"></a>`Touch` | `4` | One touch action (down, move, up, cancel) for a finger, for gestures and multi-touch. |
| <a id="m-invoke"></a>`Invoke` | `5` | Invoke an element without a pointer. |
| <a id="m-press"></a>`Press` | `6` | Press a key. |
| <a id="m-action"></a>`Action` | `7` | Drive an input action as the harness source (tap, hold, release). |
| <a id="m-pad"></a>`Pad` | `8` | Press, hold or release a button, or set an axis, on the harness's virtual gamepad. |
| <a id="m-type"></a>`Type` | `9` | Type text. |
| <a id="m-nav"></a>`Nav` | `10` | A controller action. |
| <a id="m-command"></a>`Command` | `11` | Send a command. |
| <a id="m-set"></a>`Set` | `12` | Patch a property. |
| <a id="m-screen"></a>`Screen` | `13` | Open a screen. |
| <a id="m-fixture"></a>`Fixture` | `14` | Load a fixture. |
| <a id="m-snapshot"></a>`Snapshot` | `15` | Load a snapshot file. |
| <a id="m-reset"></a>`Reset` | `16` | Start a new game, optionally with another seed. |
| <a id="m-uiscale"></a>`UiScale` | `17` | Set the UI scale override, or clear it. |
| <a id="m-step"></a>`Step` | `18` | Run frames. |
| <a id="m-wheel"></a>`Wheel` | `19` | Scroll the wheel. |
| <a id="m-capture"></a>`Capture` | `20` | Save a capture. |
| <a id="m-expect"></a>`Expect` | `21` | Check an expectation now. |
| <a id="m-camera"></a>`Camera` | `22` | Move or reset the 3D camera (a view-only override). |
| <a id="m-store"></a>`Store` | `23` | Drive the game's in-app purchases (and script the fake store). |
| <a id="m-saves"></a>`Saves` | `24` | Save, load, list or delete save slots, and reload or save settings (the `saves` operation). |
| <a id="m-assets"></a>`Assets` | `25` | Read changed asset files again (`check`), swap in staged data (`apply-data`) or reload one asset (`reload: path`). |
| <a id="m-tweens"></a>`Tweens` | `26` | Move view time (and with it every tween and UI style transition) forward by a number of seconds, or `settle` to let them all finish, without running ticks. |
| <a id="m-steaminput"></a>`SteamInput` | `27` | Drive the harness's stand-in Steam Input controller (connect, press, release, analog), then run two frames so the game sees it. |
| <a id="m-overlay"></a>`Overlay` | `28` | Show (`on`), hide (`off`) or flip (`toggle`) the on-screen debug panel; it is drawn into later captures. |
| <a id="m-wait"></a>`Wait` | `29` | Wait until a node of a process cluster reaches a tick (cluster scenarios only). |
| <a id="m-kill"></a>`Kill` | `30` | Kill a node's process outright (cluster scenarios only). |
| <a id="m-start"></a>`Start` | `31` | Start a node that was killed, with the same arguments, data folder and ports (cluster scenarios only). |
| <a id="m-restart"></a>`Restart` | `32` | Kill a node and start it again (cluster scenarios only). |
