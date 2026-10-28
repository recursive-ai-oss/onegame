[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# ImageDiff struct

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ImageDiff`](ImageDiff.md)&gt;

```csharp
public readonly struct ImageDiff : IEquatable<ImageDiff>
```

How two images differ.

## Constructors

| Member | Description |
| --- | --- |
| [`ImageDiff(bool, int, int, long, double, int)`](#m-ctor-bool-int-int-long-double-int) | How two images differ. |

### <a id="m-ctor-bool-int-int-long-double-int"></a>ImageDiff(bool, int, int, long, double, int)

```csharp
public ImageDiff(bool SameSize, int Width, int Height, long DifferingPixels, double Fraction, int MaxChannelDelta);
```

How two images differ.

**Parameters**

- `SameSize` (`bool`): False when the sizes differ; the other fields are then zero.
- `Width` (`int`): Compared width.
- `Height` (`int`): Compared height.
- `DifferingPixels` (`long`): Pixels where some channel differs by more than the tolerance.
- `Fraction` (`double`): Differing pixels as a fraction of all pixels.
- `MaxChannelDelta` (`int`): The largest difference of any channel of any pixel (regardless of tolerance).

## Properties

| Member | Description |
| --- | --- |
| [`DifferingPixels`](#m-differingpixels) | Pixels where some channel differs by more than the tolerance. |
| [`Fraction`](#m-fraction) | Differing pixels as a fraction of all pixels. |
| [`Height`](#m-height) | Compared height. |
| [`MaxChannelDelta`](#m-maxchanneldelta) | The largest difference of any channel of any pixel (regardless of tolerance). |
| [`SameSize`](#m-samesize) | False when the sizes differ; the other fields are then zero. |
| [`Width`](#m-width) | Compared width. |

### <a id="m-differingpixels"></a>DifferingPixels

```csharp
public long DifferingPixels { get; init; }
```

Pixels where some channel differs by more than the tolerance.

**Type:** `long`

### <a id="m-fraction"></a>Fraction

```csharp
public double Fraction { get; init; }
```

Differing pixels as a fraction of all pixels.

**Type:** `double`

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Compared height.

**Type:** `int`

### <a id="m-maxchanneldelta"></a>MaxChannelDelta

```csharp
public int MaxChannelDelta { get; init; }
```

The largest difference of any channel of any pixel (regardless of tolerance).

**Type:** `int`

### <a id="m-samesize"></a>SameSize

```csharp
public bool SameSize { get; init; }
```

False when the sizes differ; the other fields are then zero.

**Type:** `bool`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Compared width.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Within(double)`](#m-within-double) | Whether the images match when up to `maxFraction` of the pixels may differ. |

### <a id="m-within-double"></a>Within(double)

```csharp
public bool Within(double maxFraction);
```

Whether the images match when up to `maxFraction` of the pixels may differ.

**Parameters**

- `maxFraction` (`double`)

**Returns:** `bool`
