[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# FluidWorldSettings class

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)

```csharp
public sealed class FluidWorldSettings
```

The settings that apply to every particle fluid of a space (see [`PhysicsSpace2D.FluidSettings`](PhysicsSpace2D.md#m-fluidsettings)). They are saved with the space. More substeps and iterations make the fluid stiffer and costlier.

## Constructors

| Member | Description |
| --- | --- |
| [`FluidWorldSettings()`](#m-ctor) |  |

### <a id="m-ctor"></a>FluidWorldSettings()

```csharp
public FluidWorldSettings();
```

## Properties

| Member | Description |
| --- | --- |
| [`Iterations`](#m-iterations) | Density-constraint iterations per substep, 1 to 64. |
| [`MaxParticles`](#m-maxparticles) | The most particles that may exist, 0 to 16,777,216. |
| [`SleepSpeed`](#m-sleepspeed) | A fluid whose fastest particle stays below this speed for [`FluidWorldSettings.TimeToSleep`](FluidWorldSettings.md#m-timetosleep) seconds sleeps until something touches it. |
| [`Substeps`](#m-substeps) | Fluid substeps per tick, 1 to 16. |
| [`TimeToSleep`](#m-timetosleep) | Seconds below [`FluidWorldSettings.SleepSpeed`](FluidWorldSettings.md#m-sleepspeed) before the fluid sleeps. |
| [`WakeBodySpeed`](#m-wakebodyspeed) | A sleeping body that the fluid pushes wakes only when one step changes its speed by more than this. |

### <a id="m-iterations"></a>Iterations

```csharp
public int Iterations { get; set; }
```

Density-constraint iterations per substep, 1 to 64. Default 4.

**Type:** `int`

### <a id="m-maxparticles"></a>MaxParticles

```csharp
public int MaxParticles { get; set; }
```

The most particles that may exist, 0 to 16,777,216. Emitters stop at it; filling past it throws [`FluidBudgetExceededException`](FluidBudgetExceededException.md). Default 262,144.

**Type:** `int`

### <a id="m-sleepspeed"></a>SleepSpeed

```csharp
public Fixed SleepSpeed { get; set; }
```

A fluid whose fastest particle stays below this speed for [`FluidWorldSettings.TimeToSleep`](FluidWorldSettings.md#m-timetosleep) seconds sleeps until something touches it. 0 turns sleeping off. Default 0.02.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-substeps"></a>Substeps

```csharp
public int Substeps { get; set; }
```

Fluid substeps per tick, 1 to 16. Default 2.

**Type:** `int`

### <a id="m-timetosleep"></a>TimeToSleep

```csharp
public Fixed TimeToSleep { get; set; }
```

Seconds below [`FluidWorldSettings.SleepSpeed`](FluidWorldSettings.md#m-sleepspeed) before the fluid sleeps. Default 1.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-wakebodyspeed"></a>WakeBodySpeed

```csharp
public Fixed WakeBodySpeed { get; set; }
```

A sleeping body that the fluid pushes wakes only when one step changes its speed by more than this. Default 0.05.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)
