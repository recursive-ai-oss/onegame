[API reference](../../index.md) › [OneGame.Particles](../index.md) › OneGame.Particles

# ParticleBlend enum

**Namespace:** `OneGame.Particles`  
**Package:** [`RecursiveAI.OneGame.Particles`](../index.md)

```csharp
public enum ParticleBlend : int
```

How particles are combined with what is behind them.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-alpha"></a>`Alpha` | `0` | Drawn over the background with their opacity: smoke, dust, leaves. |
| <a id="m-additive"></a>`Additive` | `1` | Added to the background, brighter where they overlap and order independent: sparks, fire, glows, magic. |
