[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# ExpectKind enum

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)

```csharp
public enum ExpectKind : int
```

What an expectation looks at.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-state"></a>`State` | `0` | A synced property. |
| <a id="m-ui"></a>`Ui` | `1` | UI elements. |
| <a id="m-tick"></a>`Tick` | `2` | The tick number. |
| <a id="m-hash"></a>`Hash` | `3` | The state hash. |
| <a id="m-capture"></a>`Capture` | `4` | A captured frame against a reference image. |
| <a id="m-pick"></a>`Pick` | `5` | The entity under a pixel of the 3D view: [`ScenarioExpectation.Entity`](ScenarioExpectation.md#m-entity) is what must be there, or `none`. |
| <a id="m-agree"></a>`Agree` | `6` | Client replicas have the server's state hash at the replica's tick (cluster scenarios only). |
| <a id="m-drift"></a>`Drift` | `7` | Nodes have filed no drift reports (cluster scenarios only). |
| <a id="m-store"></a>`Store` | `8` | The purchase service's state: what is owned, pending or unfinished. |
| <a id="m-saves"></a>`Saves` | `9` | The save slots and settings: how many slots, which exist, which is newest, what the settings read. |
| <a id="m-asset"></a>`Asset` | `10` | A loaded asset: its state, version, and a part of its last error. |
| <a id="m-steaminput"></a>`SteamInput` | `11` | The state of Steam Input: the active action set, the number of controllers, actions held down, the on-screen keyboard. |
| <a id="m-sound"></a>`Sound` | `12` | Sounds the game asked the audio service to play: how many, by name, optionally within the last few frames. |
| <a id="m-action"></a>`Action` | `13` | The state of an input action this frame: held, pressed, released, value. |
| <a id="m-particles"></a>`Particles` | `14` | Live particles in the frame just drawn: in total or in the emitter of a given name, exactly, at least or at most. |
| <a id="m-tweens"></a>`Tweens` | `15` | How many top-level tweens are running (style transitions included). |
