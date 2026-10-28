[API reference](../index.md) › OneGame.Control

# OneGame.Control

The control layer shared by the command line, the test API and live control.

**Package:** `RecursiveAI.OneGame.Control`

```sh
dotnet add package RecursiveAI.OneGame.Control
```

## OneGame.Control

| Type | Kind | Description |
| --- | --- | --- |
| [`AssetEventInfo`](OneGame.Control/AssetEventInfo.md) | record | One asset event in the `assets` report. |
| [`AssetInfo`](OneGame.Control/AssetInfo.md) | record | One loaded asset in the `assets` report. |
| [`AssetReloader`](OneGame.Control/AssetReloader.md) | class | Development-build asset hot reload. |
| [`AssetsResult`](OneGame.Control/AssetsResult.md) | record | Result of the `assets` operation. |
| [`AudioCaptureResult`](OneGame.Control/AudioCaptureResult.md) | record | Result of `audio_capture`: what the capture sink holds after the advance. |
| [`AudioEventsResult`](OneGame.Control/AudioEventsResult.md) | record | Result of `audio_events`. |
| [`BusLoudnessStatus`](OneGame.Control/BusLoudnessStatus.md) | record | One metered bus in [`AudioCaptureResult`](OneGame.Control/AudioCaptureResult.md). |
| [`CameraResult`](OneGame.Control/CameraResult.md) | record | Result of `camera`: the camera the 3D view is drawn with. |
| [`CaptureResult`](OneGame.Control/CaptureResult.md) | record | Result of `capture_frame`. |
| [`ClipboardResult`](OneGame.Control/ClipboardResult.md) | record | Result of `clipboard_get` and `clipboard_set`. |
| [`CommandsResult`](OneGame.Control/CommandsResult.md) | record | One entity type's commands. |
| [`ControlException`](OneGame.Control/ControlException.md) | class | A control operation failed in a way the caller can act on. |
| [`ControlJson`](OneGame.Control/ControlJson.md) | class | Source-generated JSON metadata for control results; nothing here uses reflection. |
| [`ControlModule`](OneGame.Control/ControlModule.md) | static class | Identifies the Control assembly; a placeholder until its first feature lands. |
| [`ControlOp`](OneGame.Control/ControlOp.md) | record | One operation of the control layer: the name every face uses, what it does, and its arguments as a JSON schema. |
| [`ControlOps`](OneGame.Control/ControlOps.md) | static class | The operation table. |
| [`ControlResult`](OneGame.Control/ControlResult.md) | record | What an operation returns: a JSON text and, for captures, the PNG bytes. |
| [`ControlSurface`](OneGame.Control/ControlSurface.md) | class | The control layer: every operation of [`ControlOps`](OneGame.Control/ControlOps.md) run against one [`GameInstance`](../OneGame.Host/OneGame.Host/GameInstance.md). |
| [`DebugOverlay`](OneGame.Control/DebugOverlay.md) | class | The on-screen debug panel of development builds: frame time, entity count, network figures when the game is connected, and the audio device's counters. |
| [`Draw2DReport`](OneGame.Control/Draw2DReport.md) | record | Result of `draw2d`: what the 2D happy path drew in the last frame and where its camera looks. |
| [`HitResult`](OneGame.Control/HitResult.md) | record | Result of `ui_hit`. |
| [`IControlEndpoint`](OneGame.Control/IControlEndpoint.md) | interface | Anything the MCP face can drive: a list of operations and a way to run one. |
| [`InfoResult`](OneGame.Control/InfoResult.md) | record | Result of `info`. |
| [`InputResult`](OneGame.Control/InputResult.md) | record | Result of the input operations. |
| [`InvokeResult`](OneGame.Control/InvokeResult.md) | record | Result of `ui_invoke`. |
| [`McpProtocol`](OneGame.Control/McpProtocol.md) | class | The Model Context Protocol (https://modelcontextprotocol.io, revision 2025-06-18) for one game: JSON-RPC 2.0 messages in, messages out. |
| [`NavActions`](OneGame.Control/NavActions.md) | static class | The names the `input_nav` operation (and the scenario `nav` step) accepts for a virtual gamepad button: the menu actions (`activate`, `cancel`...) and every [`GamepadButton`](../OneGame.Platform/OneGame.Platform/GamepadButton.md) by name. |
| [`NetDivergeResult`](OneGame.Control/NetDivergeResult.md) | record | Result of `net_diverge`: which entity was corrupted in the local replica. |
| [`NetWaitResult`](OneGame.Control/NetWaitResult.md) | record | Result of `net_wait`: whether the tick was reached before the timeout, and where the replica is. |
| [`OverlayResult`](OneGame.Control/OverlayResult.md) | record | Result of the `overlay` operation. |
| [`ParticleEmitterInfo`](OneGame.Control/ParticleEmitterInfo.md) | record | One emitter in a `particles` report. |
| [`ParticlesReport`](OneGame.Control/ParticlesReport.md) | record | Result of `particles`: the emitters the last drawn frame showed and how many particles are alive. |
| [`PatchResult`](OneGame.Control/PatchResult.md) | record | Result of `state_patch`. |
| [`PickResult`](OneGame.Control/PickResult.md) | record | Result of `pick`: what is under a pixel, or along a ray. |
| [`ProjectResult`](OneGame.Control/ProjectResult.md) | record | Result of `project`: where a world point lands on the screen. |
| [`QualityResult`](OneGame.Control/QualityResult.md) | record | Result of `quality`: the level of the 3D renderer and what it means, with what the last frame did. |
| [`SavesResult`](OneGame.Control/SavesResult.md) | record | Result of the `saves` operation. |
| [`SendResult`](OneGame.Control/SendResult.md) | record | Result of `command_send`: `Queued` means it runs on the next tick. |
| [`SequenceFrame`](OneGame.Control/SequenceFrame.md) | record | One frame of a captured sequence. |
| [`SequenceManifest`](OneGame.Control/SequenceManifest.md) | record | The manifest of a captured sequence (also written to `manifest.json` beside the frames). |
| [`SequenceResult`](OneGame.Control/SequenceResult.md) | record | Result of `capture_sequence`. |
| [`SettingsEntry`](OneGame.Control/SettingsEntry.md) | record | One settings file in the `saves` report. |
| [`ShaderProblem`](OneGame.Control/ShaderProblem.md) | record | One diagnostic of a failed shader compile. |
| [`ShaderReloader`](OneGame.Control/ShaderReloader.md) | class | Development-build shader hot reload. |
| [`ShaderStatus`](OneGame.Control/ShaderStatus.md) | record | The state of one hot-reloadable shader. |
| [`ShadersResult`](OneGame.Control/ShadersResult.md) | record | Result of the `shaders` operation. |
| [`SlotEntry`](OneGame.Control/SlotEntry.md) | record | One save slot in the `saves` report. |
| [`SnapshotResult`](OneGame.Control/SnapshotResult.md) | record | Result of `snapshot_save`. |
| [`StateResult`](OneGame.Control/StateResult.md) | record | Result of `step`, `hash` and the operations that change the world. |
| [`TweensResult`](OneGame.Control/TweensResult.md) | record | Result of the `tweens` operation. |
| [`UiScaleResult`](OneGame.Control/UiScaleResult.md) | record | Result of `ui_scale`: the scale in effect, the override (null when following the window) and the window's own scale. |
