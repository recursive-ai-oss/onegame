[API reference](../../index.md) › [OneGame.Atlas](../index.md) › OneGame.Atlas

# NineSlice struct

**Namespace:** `OneGame.Atlas`  
**Package:** [`RecursiveAI.OneGame.Atlas`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`NineSlice`](NineSlice.md)&gt;

```csharp
public readonly struct NineSlice : IEquatable<NineSlice>
```

Insets, in pixels, that cut a frame into nine parts: the corners keep their size, the edges stretch along one axis, the middle in both.

## Constructors

| Member | Description |
| --- | --- |
| [`NineSlice(float, float, float, float)`](#m-ctor-float-float-float-float) | Insets, in pixels, that cut a frame into nine parts: the corners keep their size, the edges stretch along one axis, the middle in both. |

### <a id="m-ctor-float-float-float-float"></a>NineSlice(float, float, float, float)

```csharp
public NineSlice(float Left, float Top, float Right, float Bottom);
```

Insets, in pixels, that cut a frame into nine parts: the corners keep their size, the edges stretch along one axis, the middle in both.

**Parameters**

- `Left` (`float`): Width of the left column.
- `Top` (`float`): Height of the top row.
- `Right` (`float`): Width of the right column.
- `Bottom` (`float`): Height of the bottom row.

## Properties

| Member | Description |
| --- | --- |
| [`Bottom`](#m-bottom) | Height of the bottom row. |
| [`IsZero`](#m-iszero) | Gets whether all four insets are zero (the image is not sliced). |
| [`Left`](#m-left) | Width of the left column. |
| [`Right`](#m-right) | Width of the right column. |
| [`Top`](#m-top) | Height of the top row. |

### <a id="m-bottom"></a>Bottom

```csharp
public float Bottom { get; init; }
```

Height of the bottom row.

**Type:** `float`

### <a id="m-iszero"></a>IsZero

```csharp
public bool IsZero { get; }
```

Gets whether all four insets are zero (the image is not sliced).

**Type:** `bool`

### <a id="m-left"></a>Left

```csharp
public float Left { get; init; }
```

Width of the left column.

**Type:** `float`

### <a id="m-right"></a>Right

```csharp
public float Right { get; init; }
```

Width of the right column.

**Type:** `float`

### <a id="m-top"></a>Top

```csharp
public float Top { get; init; }
```

Height of the top row.

**Type:** `float`

## Methods

| Member | Description |
| --- | --- |
| [`Scaled(float, float)`](#m-scaled-float-float) | Returns the insets scaled per axis. |
| [`ToString()`](#m-tostring) | Writes the insets as `left top right bottom`, the form [`NineSlice.TryParse()`](NineSlice.md#m-tryparse-string-nineslice) reads. |
| [`TryParse(string, out NineSlice)`](#m-tryparse-string-nineslice) | Reads `left top right bottom` (or one number for all four, or two for horizontal and vertical). |

### <a id="m-scaled-float-float"></a>Scaled(float, float)

```csharp
public NineSlice Scaled(float sx, float sy);
```

Returns the insets scaled per axis.

**Parameters**

- `sx` (`float`): Horizontal factor.
- `sy` (`float`): Vertical factor.

**Returns:** [`NineSlice`](NineSlice.md) — The scaled insets.

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

Writes the insets as `left top right bottom`, the form [`NineSlice.TryParse()`](NineSlice.md#m-tryparse-string-nineslice) reads.

**Returns:** `string` — The text form.

### <a id="m-tryparse-string-nineslice"></a>TryParse(string, out NineSlice)

```csharp
public static bool TryParse(string text, out NineSlice slice);
```

Reads `left top right bottom` (or one number for all four, or two for horizontal and vertical). Negative or non-finite numbers are refused.

**Parameters**

- `text` (`string`): The text.
- `slice` ([`NineSlice`](NineSlice.md)): The insets.

**Returns:** `bool` — False when the text is not valid.
