[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# KitArtAsset struct

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`KitArtAsset`](KitArtAsset.md)&gt;

```csharp
public readonly struct KitArtAsset : IEquatable<KitArtAsset>
```

One piece of vector art the kit uses: the asset id, the SVG source, and the nine-slice inset (0 for none) in the SVG's own pixels.

## Constructors

| Member | Description |
| --- | --- |
| [`KitArtAsset(string, byte[], float)`](#m-ctor-string-byte-float) | One piece of vector art the kit uses: the asset id, the SVG source, and the nine-slice inset (0 for none) in the SVG's own pixels. |

### <a id="m-ctor-string-byte-float"></a>KitArtAsset(string, byte\[\], float)

```csharp
public KitArtAsset(string Id, byte[] Svg, float Slice);
```

One piece of vector art the kit uses: the asset id, the SVG source, and the nine-slice inset (0 for none) in the SVG's own pixels.

**Parameters**

- `Id` (`string`): The asset id the theme and components refer to.
- `Svg` (`byte[]`): The SVG file's bytes.
- `Slice` (`float`): The same inset on all four sides for nine-slice stretching, or 0.

## Properties

| Member | Description |
| --- | --- |
| [`Id`](#m-id) | The asset id the theme and components refer to. |
| [`Slice`](#m-slice) | The same inset on all four sides for nine-slice stretching, or 0. |
| [`Svg`](#m-svg) | The SVG file's bytes. |

### <a id="m-id"></a>Id

```csharp
public string Id { get; init; }
```

The asset id the theme and components refer to.

**Type:** `string`

### <a id="m-slice"></a>Slice

```csharp
public float Slice { get; init; }
```

The same inset on all four sides for nine-slice stretching, or 0.

**Type:** `float`

### <a id="m-svg"></a>Svg

```csharp
public byte[] Svg { get; init; }
```

The SVG file's bytes.

**Type:** `byte[]`
