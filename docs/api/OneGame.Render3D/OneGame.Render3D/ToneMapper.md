[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# ToneMapper enum

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public enum ToneMapper : int
```

The curve that maps scene-referred HDR light to display values.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-pbrneutral"></a>`PbrNeutral` | `0` | Khronos PBR Neutral: keeps base colours as authored, compresses only bright highlights. The default. |
| <a id="m-aces"></a>`Aces` | `1` | A filmic curve (the ACES rational fit) with more contrast and a stronger highlight roll-off. |
| <a id="m-none"></a>`None` | `2` | No curve: values are clamped to 0..1 after exposure. |
