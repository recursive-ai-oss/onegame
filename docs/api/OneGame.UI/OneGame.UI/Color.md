[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# Color struct

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`Color`](Color.md)&gt;

```csharp
public readonly struct Color : IEquatable<Color>
```

An 8-bit-per-channel RGBA color (not premultiplied).

## Constructors

| Member | Description |
| --- | --- |
| [`Color(byte, byte, byte, byte)`](#m-ctor-byte-byte-byte-byte) | An 8-bit-per-channel RGBA color (not premultiplied). |

### <a id="m-ctor-byte-byte-byte-byte"></a>Color(byte, byte, byte, byte)

```csharp
public Color(byte R, byte G, byte B, byte A = 255);
```

An 8-bit-per-channel RGBA color (not premultiplied).

**Parameters**

- `R` (`byte`): Red.
- `G` (`byte`): Green.
- `B` (`byte`): Blue.
- `A` (`byte`): Alpha; 255 is opaque.

## Properties

| Member | Description |
| --- | --- |
| [`A`](#m-a) | Alpha; 255 is opaque. |
| [`B`](#m-b) | Blue. |
| [`Black`](#m-black) | Opaque black. |
| [`G`](#m-g) | Green. |
| [`R`](#m-r) | Red. |
| [`Transparent`](#m-transparent) | Fully transparent black. |
| [`White`](#m-white) | Opaque white. |

### <a id="m-a"></a>A

```csharp
public byte A { get; init; }
```

Alpha; 255 is opaque.

**Type:** `byte`

### <a id="m-b"></a>B

```csharp
public byte B { get; init; }
```

Blue.

**Type:** `byte`

### <a id="m-black"></a>Black

```csharp
public static Color Black { get; }
```

Opaque black.

**Type:** [`Color`](Color.md)

### <a id="m-g"></a>G

```csharp
public byte G { get; init; }
```

Green.

**Type:** `byte`

### <a id="m-r"></a>R

```csharp
public byte R { get; init; }
```

Red.

**Type:** `byte`

### <a id="m-transparent"></a>Transparent

```csharp
public static Color Transparent { get; }
```

Fully transparent black.

**Type:** [`Color`](Color.md)

### <a id="m-white"></a>White

```csharp
public static Color White { get; }
```

Opaque white.

**Type:** [`Color`](Color.md)

## Methods

| Member | Description |
| --- | --- |
| [`Hex(string)`](#m-hex-string) | Parses `#rgb`, `#rrggbb` or `#rrggbbaa`. |
| [`Rgb(uint)`](#m-rgb-uint) | Builds an opaque color from `0xRRGGBB`. |
| [`Rgba(uint)`](#m-rgba-uint) | Builds a color from `0xRRGGBBAA`. |
| [`ToString()`](#m-tostring) | The CSS hex spelling; the alpha digits appear only when not opaque. |
| [`WithOpacity(float)`](#m-withopacity-float) | This color with its alpha multiplied by `factor` (0..1). |

### <a id="m-hex-string"></a>Hex(string)

```csharp
public static Color Hex(string text);
```

Parses `#rgb`, `#rrggbb` or `#rrggbbaa`.

**Parameters**

- `text` (`string`)

**Returns:** [`Color`](Color.md)

**Exceptions**

- [`FormatException`](https://learn.microsoft.com/dotnet/api/system.formatexception): The text is not one of those forms.

### <a id="m-rgb-uint"></a>Rgb(uint)

```csharp
public static Color Rgb(uint rgb);
```

Builds an opaque color from `0xRRGGBB`.

**Parameters**

- `rgb` (`uint`)

**Returns:** [`Color`](Color.md)

### <a id="m-rgba-uint"></a>Rgba(uint)

```csharp
public static Color Rgba(uint rgba);
```

Builds a color from `0xRRGGBBAA`.

**Parameters**

- `rgba` (`uint`)

**Returns:** [`Color`](Color.md)

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

The CSS hex spelling; the alpha digits appear only when not opaque.

**Returns:** `string`

### <a id="m-withopacity-float"></a>WithOpacity(float)

```csharp
public Color WithOpacity(float factor);
```

This color with its alpha multiplied by `factor` (0..1).

**Parameters**

- `factor` (`float`)

**Returns:** [`Color`](Color.md)
