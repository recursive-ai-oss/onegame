[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# ReflectionSettings class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public sealed class ReflectionSettings
```

One horizontal mirror plane for materials that opt in with [`Material3D.PlanarReflection`](Material3D.md#m-planarreflection) (water, polished floors). When such a material is drawn, the scene is rendered once more as seen in the mirror, into an image sized by the quality level, and the material's specular light comes from it instead of from the environment. There is only one plane per frame, and it is always horizontal (normal +Y). With no opted-in material in the frame, nothing extra is drawn or bound. The mirror image holds the sky, opaque and blended surfaces, instanced batches, directional and spot light shadows, fog, and shading from the mirrored camera. It leaves out the opted-in surfaces themselves, everything below the plane, outlines, billboards and debug geometry, and clustered point and spot lights do not light it (directional lights do). A camera at or below the plane sees no reflection (the environment is used instead), as does the Low quality level.

## Constructors

| Member | Description |
| --- | --- |
| [`ReflectionSettings()`](#m-ctor) |  |

### <a id="m-ctor"></a>ReflectionSettings()

```csharp
public ReflectionSettings();
```

## Properties

| Member | Description |
| --- | --- |
| [`Enabled`](#m-enabled) | Whether planar reflection runs at all (on by default; it still needs an opted-in material in view). |
| [`Height`](#m-height) | World height (y) of the mirror plane. |

### <a id="m-enabled"></a>Enabled

```csharp
public bool Enabled { get; set; }
```

Whether planar reflection runs at all (on by default; it still needs an opted-in material in view).

**Type:** `bool`

### <a id="m-height"></a>Height

```csharp
public float Height { get; set; }
```

World height (y) of the mirror plane.

**Type:** `float`
