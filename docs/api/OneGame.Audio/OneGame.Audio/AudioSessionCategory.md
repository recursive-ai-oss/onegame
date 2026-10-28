[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# AudioSessionCategory enum

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)

```csharp
public enum AudioSessionCategory : int
```

How an iOS app's audio relates to other apps' audio and the ring/silent switch (AVAudioSession categories).

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-ambient"></a>`Ambient` | `0` | Mixes with other apps' audio and follows the silent switch. The default for games. |
| <a id="m-soloambient"></a>`SoloAmbient` | `1` | Silences other apps' audio and follows the silent switch. |
| <a id="m-playback"></a>`Playback` | `2` | Plays even with the silent switch on and silences other apps' audio. |
