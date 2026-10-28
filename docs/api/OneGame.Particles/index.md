[API reference](../index.md) › OneGame.Particles

# OneGame.Particles

Particle emitters: a seedable, allocation-free simulation for sparks, smoke, dust and trails, drawn by the 2D and 3D renderers.

**Package:** `RecursiveAI.OneGame.Particles`

```sh
dotnet add package RecursiveAI.OneGame.Particles
```

## OneGame.Particles

| Type | Kind | Description |
| --- | --- | --- |
| [`FloatRange`](OneGame.Particles/FloatRange.md) | struct | A range of values; each particle draws one uniformly between [`FloatRange.Min`](OneGame.Particles/FloatRange.md#m-min) and [`FloatRange.Max`](OneGame.Particles/FloatRange.md#m-max). |
| [`ParticleBlend`](OneGame.Particles/ParticleBlend.md) | enum | How particles are combined with what is behind them. |
| [`ParticleCurve`](OneGame.Particles/ParticleCurve.md) | class | A number that changes over a particle's life, as straight lines between stops. |
| [`ParticleEmitter`](OneGame.Particles/ParticleEmitter.md) | class | A source of particles and the particles it made. |
| [`ParticleGradient`](OneGame.Particles/ParticleGradient.md) | class | A colour that changes over a particle's life, as straight lines between stops. |
| [`ParticleSample`](OneGame.Particles/ParticleSample.md) | struct | One live particle as a renderer needs it, from [`ParticleEmitter.Sample()`](OneGame.Particles/ParticleEmitter.md#m-sample-int). |
| [`ParticleSettings`](OneGame.Particles/ParticleSettings.md) | class | Everything that describes how an emitter looks and moves. |
| [`ParticleSpace`](OneGame.Particles/ParticleSpace.md) | enum | Whether particles move in a flat plane (2D games) or in space. |
