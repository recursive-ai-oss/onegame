[API reference](../../index.md) › [OneGame.Particles](../index.md) › OneGame.Particles

# ParticleSettings class

**Namespace:** `OneGame.Particles`  
**Package:** [`RecursiveAI.OneGame.Particles`](../index.md)

```csharp
public sealed class ParticleSettings
```

Everything that describes how an emitter looks and moves. Set what differs from the defaults when you make it; the values are read as the emitter runs, so changing one (a rate that follows a throttle, a gravity that flips) takes effect on the next step. Units are the world's own: pixels in 2D (+Y down), metres in 3D. The emitter has a fixed [`ParticleSettings.Capacity`](ParticleSettings.md#m-capacity); spawns past it are dropped, never reallocated.

## Constructors

| Member | Description |
| --- | --- |
| [`ParticleSettings()`](#m-ctor) |  |

### <a id="m-ctor"></a>ParticleSettings()

```csharp
public ParticleSettings();
```

## Properties

| Member | Description |
| --- | --- |
| [`AngularVelocity`](#m-angularvelocity) | Spin in radians per second. |
| [`AnimateFrames`](#m-animateframes) | True to play the frames once over each particle's life (an explosion animation); false to give each particle one random frame. |
| [`Blend`](#m-blend) | How particles are combined with the background. |
| [`Capacity`](#m-capacity) | The most particles alive at once; fixed when the emitter is made. |
| [`Color`](#m-color) | Colour over the life, sRGB with straight alpha. |
| [`Direction`](#m-direction) | The direction particles are launched in (need not be normalised). |
| [`Drag`](#m-drag) | Air resistance: the fraction of velocity lost per second is about this much (0 = none, 3 = settles within a second). |
| [`Duration`](#m-duration) | Seconds a non-looping emitter emits for. |
| [`Emission`](#m-emission) | Brightness multiplier for 3D, where values above 1 glow through bloom. |
| [`FrameCount`](#m-framecount) | How many frames of the sheet to use, counting row by row; 0 uses all of them. |
| [`Gravity`](#m-gravity) | Acceleration in world units per second squared (+Y is down in 2D, up in 3D). |
| [`Lifetime`](#m-lifetime) | Seconds each particle lives. |
| [`LocalSpace`](#m-localspace) | True to keep particles in the emitter's own frame, so they move with it; false (the default) leaves them where they were born, so a moving emitter draws a trail. |
| [`Loop`](#m-loop) | True to emit until stopped; false to emit for [`ParticleSettings.Duration`](ParticleSettings.md#m-duration) seconds after `Play` and then stop. |
| [`Prewarm`](#m-prewarm) | Seconds to run the emitter for before it is first shown, so a steady effect (smoke, a campfire) is already full on the first frame. |
| [`Rate`](#m-rate) | Particles per second while the emitter is playing; 0 emits only bursts. |
| [`Rotation`](#m-rotation) | Starting angle in radians. |
| [`SheetColumns`](#m-sheetcolumns) | Columns of frames in the texture (1 for a single image). |
| [`SheetRows`](#m-sheetrows) | Rows of frames in the texture. |
| [`Size`](#m-size) | Particle size at birth, in world units (width and height; sprites are square). |
| [`SizeOverLife`](#m-sizeoverlife) | Multiplier on the size over the particle's life. |
| [`Space`](#m-space) | Plane for 2D, Volume for 3D. |
| [`SpawnRadius`](#m-spawnradius) | Radius of the disc or ball particles are born in around the emitter. |
| [`Speed`](#m-speed) | Initial speed in world units per second. |
| [`Spread`](#m-spread) | Half-angle in radians around [`ParticleSettings.Direction`](ParticleSettings.md#m-direction) that launches are spread over: 0 is a beam, pi is every direction. |

### <a id="m-angularvelocity"></a>AngularVelocity

```csharp
public FloatRange AngularVelocity { get; set; }
```

Spin in radians per second.

**Type:** [`FloatRange`](FloatRange.md)

### <a id="m-animateframes"></a>AnimateFrames

```csharp
public bool AnimateFrames { get; set; }
```

True to play the frames once over each particle's life (an explosion animation); false to give each particle one random frame.

**Type:** `bool`

### <a id="m-blend"></a>Blend

```csharp
public ParticleBlend Blend { get; set; }
```

How particles are combined with the background.

**Type:** [`ParticleBlend`](ParticleBlend.md)

### <a id="m-capacity"></a>Capacity

```csharp
public int Capacity { get; init; }
```

The most particles alive at once; fixed when the emitter is made. Default 256.

**Type:** `int`

### <a id="m-color"></a>Color

```csharp
public ParticleGradient Color { get; set; }
```

Colour over the life, sRGB with straight alpha. Default opaque white.

**Type:** [`ParticleGradient`](ParticleGradient.md)

### <a id="m-direction"></a>Direction

```csharp
public Vector3 Direction { get; set; }
```

The direction particles are launched in (need not be normalised). Default up on screen in 2D (-Y).

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-drag"></a>Drag

```csharp
public float Drag { get; set; }
```

Air resistance: the fraction of velocity lost per second is about this much (0 = none, 3 = settles within a second).

**Type:** `float`

### <a id="m-duration"></a>Duration

```csharp
public float Duration { get; set; }
```

Seconds a non-looping emitter emits for.

**Type:** `float`

### <a id="m-emission"></a>Emission

```csharp
public float Emission { get; set; }
```

Brightness multiplier for 3D, where values above 1 glow through bloom. 2D ignores it.

**Type:** `float`

### <a id="m-framecount"></a>FrameCount

```csharp
public int FrameCount { get; set; }
```

How many frames of the sheet to use, counting row by row; 0 uses all of them.

**Type:** `int`

### <a id="m-gravity"></a>Gravity

```csharp
public Vector3 Gravity { get; set; }
```

Acceleration in world units per second squared (+Y is down in 2D, up in 3D).

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-lifetime"></a>Lifetime

```csharp
public FloatRange Lifetime { get; set; }
```

Seconds each particle lives.

**Type:** [`FloatRange`](FloatRange.md)

### <a id="m-localspace"></a>LocalSpace

```csharp
public bool LocalSpace { get; set; }
```

True to keep particles in the emitter's own frame, so they move with it; false (the default) leaves them where they were born, so a moving emitter draws a trail.

**Type:** `bool`

### <a id="m-loop"></a>Loop

```csharp
public bool Loop { get; set; }
```

True to emit until stopped; false to emit for [`ParticleSettings.Duration`](ParticleSettings.md#m-duration) seconds after `Play` and then stop.

**Type:** `bool`

### <a id="m-prewarm"></a>Prewarm

```csharp
public float Prewarm { get; set; }
```

Seconds to run the emitter for before it is first shown, so a steady effect (smoke, a campfire) is already full on the first frame. Capped at 30.

**Type:** `float`

### <a id="m-rate"></a>Rate

```csharp
public float Rate { get; set; }
```

Particles per second while the emitter is playing; 0 emits only bursts.

**Type:** `float`

### <a id="m-rotation"></a>Rotation

```csharp
public FloatRange Rotation { get; set; }
```

Starting angle in radians.

**Type:** [`FloatRange`](FloatRange.md)

### <a id="m-sheetcolumns"></a>SheetColumns

```csharp
public int SheetColumns { get; set; }
```

Columns of frames in the texture (1 for a single image).

**Type:** `int`

### <a id="m-sheetrows"></a>SheetRows

```csharp
public int SheetRows { get; set; }
```

Rows of frames in the texture.

**Type:** `int`

### <a id="m-size"></a>Size

```csharp
public FloatRange Size { get; set; }
```

Particle size at birth, in world units (width and height; sprites are square).

**Type:** [`FloatRange`](FloatRange.md)

### <a id="m-sizeoverlife"></a>SizeOverLife

```csharp
public ParticleCurve SizeOverLife { get; set; }
```

Multiplier on the size over the particle's life.

**Type:** [`ParticleCurve`](ParticleCurve.md)

### <a id="m-space"></a>Space

```csharp
public ParticleSpace Space { get; init; }
```

Plane for 2D, Volume for 3D.

**Type:** [`ParticleSpace`](ParticleSpace.md)

### <a id="m-spawnradius"></a>SpawnRadius

```csharp
public float SpawnRadius { get; set; }
```

Radius of the disc or ball particles are born in around the emitter.

**Type:** `float`

### <a id="m-speed"></a>Speed

```csharp
public FloatRange Speed { get; set; }
```

Initial speed in world units per second.

**Type:** [`FloatRange`](FloatRange.md)

### <a id="m-spread"></a>Spread

```csharp
public float Spread { get; set; }
```

Half-angle in radians around [`ParticleSettings.Direction`](ParticleSettings.md#m-direction) that launches are spread over: 0 is a beam, pi is every direction.

**Type:** `float`
