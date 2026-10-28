[API reference](../../index.md) › [OneGame.Particles](../index.md) › OneGame.Particles

# ParticleEmitter class

**Namespace:** `OneGame.Particles`  
**Package:** [`RecursiveAI.OneGame.Particles`](../index.md)

```csharp
public sealed class ParticleEmitter
```

A source of particles and the particles it made. Create one when a view sets up, move [`ParticleEmitter.Position`](ParticleEmitter.md#m-position) to where it should be each frame, and hand it to a renderer glue (`Draw2D.Particles`, `SceneWriter.Particles`), which steps and draws it. It is view-only: it reads no simulation state and uses its own random stream, so the same seed and the same sequence of step sizes give the same particles. All storage is allocated here; stepping and sampling allocate nothing.

**Example**

```csharp
// In a [View] method, once: create the emitter and keep it in a view-side field.
_sparks ??= new ParticleEmitter(new ParticleSettings { Rate = 60, Lifetime = 0.8f, Speed = new FloatRange(30, 90), Spread = MathF.PI, Size = 4f }, seed: 7, name: "sparks");

// Each frame: draw it at a world position; this also steps it by the frame's time.
d.Particles(_sparks, new Vector2(X.ToSingle(), Y.ToSingle()));
```

## Constructors

| Member | Description |
| --- | --- |
| [`ParticleEmitter(ParticleSettings, uint, string)`](#m-ctor-particlesettings-uint-string) | Makes an emitter. |

### <a id="m-ctor-particlesettings-uint-string"></a>ParticleEmitter(ParticleSettings, uint, string)

```csharp
public ParticleEmitter(ParticleSettings settings, uint seed = 1, string name = "");
```

Makes an emitter.

**Parameters**

- `settings` ([`ParticleSettings`](ParticleSettings.md)): How it behaves. Its [`ParticleSettings.Capacity`](ParticleSettings.md#m-capacity) is fixed now.
- `seed` (`uint`): Seed of the emitter's random stream; fixed seeds make captures repeatable.
- `name` (`string`): A name for the harness (`particles` operation and scenario expectation).

## Properties

| Member | Description |
| --- | --- |
| [`Alive`](#m-alive) | True while it is emitting or any particle is alive; a one-shot effect is finished when this turns false. |
| [`Capacity`](#m-capacity) | The most particles that can be alive. |
| [`Count`](#m-count) | How many particles are alive. |
| [`Dropped`](#m-dropped) | How many births were dropped because the emitter was full. |
| [`Elapsed`](#m-elapsed) | Seconds of stepping since the last [`ParticleEmitter.Play()`](ParticleEmitter.md#m-play). |
| [`Emitting`](#m-emitting) | True while the emitter is still producing particles from its rate. |
| [`Name`](#m-name) | The name shown to the harness. |
| [`Position`](#m-position) | Where the emitter is. |
| [`Settings`](#m-settings) | The settings, which can be changed while it runs. |
| [`Spawned`](#m-spawned) | How many particles have been born in total. |

### <a id="m-alive"></a>Alive

```csharp
public bool Alive { get; }
```

True while it is emitting or any particle is alive; a one-shot effect is finished when this turns false.

**Type:** `bool`

### <a id="m-capacity"></a>Capacity

```csharp
public int Capacity { get; }
```

The most particles that can be alive.

**Type:** `int`

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

How many particles are alive.

**Type:** `int`

### <a id="m-dropped"></a>Dropped

```csharp
public long Dropped { get; }
```

How many births were dropped because the emitter was full. A non-zero value means the capacity is too small for the effect.

**Type:** `long`

### <a id="m-elapsed"></a>Elapsed

```csharp
public float Elapsed { get; }
```

Seconds of stepping since the last [`ParticleEmitter.Play()`](ParticleEmitter.md#m-play).

**Type:** `float`

### <a id="m-emitting"></a>Emitting

```csharp
public bool Emitting { get; }
```

True while the emitter is still producing particles from its rate.

**Type:** `bool`

### <a id="m-name"></a>Name

```csharp
public string Name { get; set; }
```

The name shown to the harness.

**Type:** `string`

### <a id="m-position"></a>Position

```csharp
public Vector3 Position { get; set; }
```

Where the emitter is. Set it every frame to follow a view; in world space the particles stay behind, so a moving emitter leaves a trail.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-settings"></a>Settings

```csharp
public ParticleSettings Settings { get; }
```

The settings, which can be changed while it runs.

**Type:** [`ParticleSettings`](ParticleSettings.md)

### <a id="m-spawned"></a>Spawned

```csharp
public long Spawned { get; }
```

How many particles have been born in total.

**Type:** `long`

## Methods

| Member | Description |
| --- | --- |
| [`Burst(int)`](#m-burst-int) | Emits `count` particles right now at the current position: an explosion, a hit spark, a pickup. |
| [`Burst(int, Vector3)`](#m-burst-int-vector3) | Moves the emitter to a place and emits `count` particles there right now. |
| [`Clear()`](#m-clear) | Removes every live particle at once. |
| [`Play()`](#m-play) | Starts (or restarts) emitting from the rate. |
| [`Reseed(uint)`](#m-reseed-uint) | Restarts the random stream. |
| [`Sample(int)`](#m-sample-int) | One live particle as a renderer needs it. |
| [`Step(float)`](#m-step-float) | Moves time forward: ages and moves the particles, retires the old ones and makes new ones from the rate. |
| [`Stop()`](#m-stop) | Stops emitting; particles already alive live out their lives. |
| [`Teleport(Vector3)`](#m-teleport-vector3) | Moves the emitter without a trail: the next step spawns from the new place only. |

### <a id="m-burst-int"></a>Burst(int)

```csharp
public void Burst(int count);
```

Emits `count` particles right now at the current position: an explosion, a hit spark, a pickup.

**Parameters**

- `count` (`int`): How many to make. Births past the capacity are dropped and counted in [`ParticleEmitter.Dropped`](ParticleEmitter.md#m-dropped).

### <a id="m-burst-int-vector3"></a>Burst(int, Vector3)

```csharp
public void Burst(int count, Vector3 position);
```

Moves the emitter to a place and emits `count` particles there right now.

**Parameters**

- `count` (`int`): How many to make.
- `position` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Where the burst happens; the emitter stays there.

### <a id="m-clear"></a>Clear()

```csharp
public void Clear();
```

Removes every live particle at once.

### <a id="m-play"></a>Play()

```csharp
public void Play();
```

Starts (or restarts) emitting from the rate. Live particles stay.

### <a id="m-reseed-uint"></a>Reseed(uint)

```csharp
public void Reseed(uint seed);
```

Restarts the random stream.

**Parameters**

- `seed` (`uint`): The new seed.

### <a id="m-sample-int"></a>Sample(int)

```csharp
public ParticleSample Sample(int index);
```

One live particle as a renderer needs it.

**Parameters**

- `index` (`int`): 0 to [`ParticleEmitter.Count`](ParticleEmitter.md#m-count) - 1; lower indices are older.

**Returns:** [`ParticleSample`](ParticleSample.md) — The particle.

### <a id="m-step-float"></a>Step(float)

```csharp
public void Step(float seconds);
```

Moves time forward: ages and moves the particles, retires the old ones and makes new ones from the rate.

**Parameters**

- `seconds` (`float`): Time since the last step. It is applied in sub-steps of at most 1/30 s and values above one second are cut to one; zero or less does nothing.

### <a id="m-stop"></a>Stop()

```csharp
public void Stop();
```

Stops emitting; particles already alive live out their lives.

### <a id="m-teleport-vector3"></a>Teleport(Vector3)

```csharp
public void Teleport(Vector3 position);
```

Moves the emitter without a trail: the next step spawns from the new place only.

**Parameters**

- `position` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): The new position.
