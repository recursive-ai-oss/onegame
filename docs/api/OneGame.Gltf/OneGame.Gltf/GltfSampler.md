[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfSampler class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class GltfSampler
```

Texture sampling state.

## Constructors

| Member | Description |
| --- | --- |
| [`GltfSampler()`](#m-ctor) |  |

### <a id="m-ctor"></a>GltfSampler()

```csharp
public GltfSampler();
```

## Properties

| Member | Description |
| --- | --- |
| [`MagFilter`](#m-magfilter) | Magnification filter. |
| [`MinFilter`](#m-minfilter) | Minification filter. |
| [`Name`](#m-name) | The sampler's name, or empty. |
| [`WrapS`](#m-wraps) | Wrapping along U. |
| [`WrapT`](#m-wrapt) | Wrapping along V. |

### <a id="m-magfilter"></a>MagFilter

```csharp
public GltfMagFilter MagFilter { get; }
```

Magnification filter.

**Type:** [`GltfMagFilter`](GltfMagFilter.md)

### <a id="m-minfilter"></a>MinFilter

```csharp
public GltfMinFilter MinFilter { get; }
```

Minification filter.

**Type:** [`GltfMinFilter`](GltfMinFilter.md)

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The sampler's name, or empty.

**Type:** `string`

### <a id="m-wraps"></a>WrapS

```csharp
public GltfWrap WrapS { get; }
```

Wrapping along U.

**Type:** [`GltfWrap`](GltfWrap.md)

### <a id="m-wrapt"></a>WrapT

```csharp
public GltfWrap WrapT { get; }
```

Wrapping along V.

**Type:** [`GltfWrap`](GltfWrap.md)
