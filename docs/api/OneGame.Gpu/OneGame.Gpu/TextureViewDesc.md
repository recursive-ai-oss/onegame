[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# TextureViewDesc struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`TextureViewDesc`](TextureViewDesc.md)&gt;

```csharp
public readonly struct TextureViewDesc : IEquatable<TextureViewDesc>
```

Selects part of a texture for a shader to see: some mip levels, some layers, and how they are presented.

## Properties

| Member | Description |
| --- | --- |
| [`BaseLayer`](#m-baselayer) | First array layer visible. |
| [`BaseMip`](#m-basemip) | First mip level visible. |
| [`Kind`](#m-kind) | How the shader sees it. |
| [`LayerCount`](#m-layercount) | Number of layers visible; 0 means all from [`TextureViewDesc.BaseLayer`](TextureViewDesc.md#m-baselayer). |
| [`MipCount`](#m-mipcount) | Number of mip levels visible; 0 means all from [`TextureViewDesc.BaseMip`](TextureViewDesc.md#m-basemip). |

### <a id="m-baselayer"></a>BaseLayer

```csharp
public uint BaseLayer { get; init; }
```

First array layer visible.

**Type:** `uint`

### <a id="m-basemip"></a>BaseMip

```csharp
public uint BaseMip { get; init; }
```

First mip level visible.

**Type:** `uint`

### <a id="m-kind"></a>Kind

```csharp
public TextureViewKind Kind { get; init; }
```

How the shader sees it.

**Type:** [`TextureViewKind`](TextureViewKind.md)

### <a id="m-layercount"></a>LayerCount

```csharp
public uint LayerCount { get; init; }
```

Number of layers visible; 0 means all from [`TextureViewDesc.BaseLayer`](TextureViewDesc.md#m-baselayer).

**Type:** `uint`

### <a id="m-mipcount"></a>MipCount

```csharp
public uint MipCount { get; init; }
```

Number of mip levels visible; 0 means all from [`TextureViewDesc.BaseMip`](TextureViewDesc.md#m-basemip).

**Type:** `uint`

## Methods

| Member | Description |
| --- | --- |
| [`Layer(uint)`](#m-layer-uint) | One layer of an array or cube texture, seen as a 2D image. |
| [`Mip(uint)`](#m-mip-uint) | One mip level of a texture. |

### <a id="m-layer-uint"></a>Layer(uint)

```csharp
public static TextureViewDesc Layer(uint layer);
```

One layer of an array or cube texture, seen as a 2D image.

**Parameters**

- `layer` (`uint`)

**Returns:** [`TextureViewDesc`](TextureViewDesc.md)

### <a id="m-mip-uint"></a>Mip(uint)

```csharp
public static TextureViewDesc Mip(uint mip);
```

One mip level of a texture.

**Parameters**

- `mip` (`uint`)

**Returns:** [`TextureViewDesc`](TextureViewDesc.md)
