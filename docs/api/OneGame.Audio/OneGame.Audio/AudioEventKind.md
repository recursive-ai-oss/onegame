[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# AudioEventKind enum

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)

```csharp
public enum AudioEventKind : int
```

What a core reports back to the game thread.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-voiceended"></a>`VoiceEnded` | `0` | A voice reached the end of its source (or finished a fade-out). |
| <a id="m-voicestolen"></a>`VoiceStolen` | `1` | A voice was cut to make room for a higher-priority one. |
| <a id="m-playdropped"></a>`PlayDropped` | `2` | A play request was dropped because every voice was busy with something more important. |
| <a id="m-underrun"></a>`Underrun` | `3` | A streaming voice ran out of decoded data and played silence. |
