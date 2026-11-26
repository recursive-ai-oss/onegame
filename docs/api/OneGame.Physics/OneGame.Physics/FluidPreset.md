[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# FluidPreset enum

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)

```csharp
public enum FluidPreset : byte
```

The material a particle fluid starts from. Each is a tuned set of viscosity, density, friction and cohesion; [`FluidOptions`](FluidOptions.md) overrides single values.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-water"></a>`Water` | `0` | Thin, free-flowing liquid (1000 kg/m³). |
| <a id="m-oil"></a>`Oil` | `1` | A lighter, slightly thicker liquid. |
| <a id="m-honey"></a>`Honey` | `2` | A very thick liquid that creeps. |
| <a id="m-sand"></a>`Sand` | `3` | Dry grains that pile up at a repose angle. |
| <a id="m-gravel"></a>`Gravel` | `4` | Coarse grains. |
| <a id="m-snow"></a>`Snow` | `5` | Loose packed snow. |
| <a id="m-mud"></a>`Mud` | `6` | Wet, sticky grains. |
| <a id="m-clay"></a>`Clay` | `7` | A plastic paste that holds a shape. |
| <a id="m-dough"></a>`Dough` | `8` | An elastic paste. |
