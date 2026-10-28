[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# PostSettings class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public sealed class PostSettings
```

Bloom, exposure and tone mapping.

## Constructors

| Member | Description |
| --- | --- |
| [`PostSettings()`](#m-ctor) |  |

### <a id="m-ctor"></a>PostSettings()

```csharp
public PostSettings();
```

## Properties

| Member | Description |
| --- | --- |
| [`Bloom`](#m-bloom) | Whether bright areas glow. |
| [`BloomStrength`](#m-bloomstrength) | How much of the blurred bright image is added back (linear light). |
| [`Exposure`](#m-exposure) | Linear multiplier applied to the HDR image before tone mapping (2^EV). |
| [`ToneMapper`](#m-tonemapper) | The tone mapping curve. |

### <a id="m-bloom"></a>Bloom

```csharp
public bool Bloom { get; set; }
```

Whether bright areas glow.

**Type:** `bool`

### <a id="m-bloomstrength"></a>BloomStrength

```csharp
public float BloomStrength { get; set; }
```

How much of the blurred bright image is added back (linear light).

**Type:** `float`

### <a id="m-exposure"></a>Exposure

```csharp
public float Exposure { get; set; }
```

Linear multiplier applied to the HDR image before tone mapping (2^EV).

**Type:** `float`

### <a id="m-tonemapper"></a>ToneMapper

```csharp
public ToneMapper ToneMapper { get; set; }
```

The tone mapping curve.

**Type:** [`ToneMapper`](ToneMapper.md)
