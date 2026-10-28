[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# ShadowSettings class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public sealed class ShadowSettings
```

Scene-wide shadow controls. A light casts shadows when its [`Light3D.CastShadows`](Light3D.md#m-castshadows) is set.

## Constructors

| Member | Description |
| --- | --- |
| [`ShadowSettings()`](#m-ctor) |  |

### <a id="m-ctor"></a>ShadowSettings()

```csharp
public ShadowSettings();
```

## Properties

| Member | Description |
| --- | --- |
| [`MaxDistance`](#m-maxdistance) | Farthest distance from the camera, in world units, that directional light shadows reach (capped by the camera's far plane). |
| [`NormalOffset`](#m-normaloffset) | How far, in shadow map texels, a surface point is moved along its normal before the lookup. |
| [`SplitLambda`](#m-splitlambda) | Mix of logarithmic (1) and uniform (0) placement of the cascade splits. |

### <a id="m-maxdistance"></a>MaxDistance

```csharp
public float MaxDistance { get; set; }
```

Farthest distance from the camera, in world units, that directional light shadows reach (capped by the camera's far plane).

**Type:** `float`

### <a id="m-normaloffset"></a>NormalOffset

```csharp
public float NormalOffset { get; set; }
```

How far, in shadow map texels, a surface point is moved along its normal before the lookup. It removes self-shadowing acne.

**Type:** `float`

### <a id="m-splitlambda"></a>SplitLambda

```csharp
public float SplitLambda { get; set; }
```

Mix of logarithmic (1) and uniform (0) placement of the cascade splits.

**Type:** `float`
