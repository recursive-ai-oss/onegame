[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# FogSettings class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public sealed class FogSettings
```

Distance fog: surfaces fade towards a colour with their distance from the camera. It is a per-frame constant, so changing it never touches meshes or pipelines. The amount rises linearly from 0 at [`FogSettings.Start`](FogSettings.md#m-start) to [`FogSettings.MaxDensity`](FogSettings.md#m-maxdensity) at [`FogSettings.End`](FogSettings.md#m-end) and stays there. It applies to every surface style (PBR, toon, flat, unlit), outlines, instanced draws, dynamic meshes and billboards (added light fades out instead of turning into fog). The backdrop and debug geometry are left alone: set [`FogSettings.Color`](FogSettings.md#m-color) to the sky's horizon colour so far surfaces meet it. The colour is linear light and is tone mapped with the rest of the frame.

## Constructors

| Member | Description |
| --- | --- |
| [`FogSettings()`](#m-ctor) |  |

### <a id="m-ctor"></a>FogSettings()

```csharp
public FogSettings();
```

## Properties

| Member | Description |
| --- | --- |
| [`Color`](#m-color) | Linear fog colour (may exceed 1 for a bright sky). |
| [`Enabled`](#m-enabled) | Whether fog is applied. |
| [`End`](#m-end) | Distance where fog reaches [`FogSettings.MaxDensity`](FogSettings.md#m-maxdensity). |
| [`MaxDensity`](#m-maxdensity) | Largest share of the fog colour a surface shows, 0 to 1. |
| [`Start`](#m-start) | Distance from the camera in world units where fog begins. |

### <a id="m-color"></a>Color

```csharp
public Vector3 Color { get; set; }
```

Linear fog colour (may exceed 1 for a bright sky).

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-enabled"></a>Enabled

```csharp
public bool Enabled { get; set; }
```

Whether fog is applied. Off by default.

**Type:** `bool`

### <a id="m-end"></a>End

```csharp
public float End { get; set; }
```

Distance where fog reaches [`FogSettings.MaxDensity`](FogSettings.md#m-maxdensity).

**Type:** `float`

### <a id="m-maxdensity"></a>MaxDensity

```csharp
public float MaxDensity { get; set; }
```

Largest share of the fog colour a surface shows, 0 to 1. Below 1 the far end never fully disappears.

**Type:** `float`

### <a id="m-start"></a>Start

```csharp
public float Start { get; set; }
```

Distance from the camera in world units where fog begins. Orthographic cameras measure view depth.

**Type:** `float`
