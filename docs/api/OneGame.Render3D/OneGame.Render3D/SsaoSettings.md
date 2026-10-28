[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# SsaoSettings class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public sealed class SsaoSettings
```

Screen-space ambient occlusion, computed from the depth and normals of the scene. Off by default.

## Constructors

| Member | Description |
| --- | --- |
| [`SsaoSettings()`](#m-ctor) |  |

### <a id="m-ctor"></a>SsaoSettings()

```csharp
public SsaoSettings();
```

## Properties

| Member | Description |
| --- | --- |
| [`Enabled`](#m-enabled) | Whether the effect runs (also needs a [`QualitySettings.Ssao`](QualitySettings.md#m-ssao) level). |
| [`Intensity`](#m-intensity) | Strength: 0 none, 1 full. |
| [`Radius`](#m-radius) | Radius in world units inside which occluders count. |

### <a id="m-enabled"></a>Enabled

```csharp
public bool Enabled { get; set; }
```

Whether the effect runs (also needs a [`QualitySettings.Ssao`](QualitySettings.md#m-ssao) level).

**Type:** `bool`

### <a id="m-intensity"></a>Intensity

```csharp
public float Intensity { get; set; }
```

Strength: 0 none, 1 full.

**Type:** `float`

### <a id="m-radius"></a>Radius

```csharp
public float Radius { get; set; }
```

Radius in world units inside which occluders count.

**Type:** `float`
