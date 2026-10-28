[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# CornerRadii struct

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`CornerRadii`](CornerRadii.md)&gt;

```csharp
public readonly struct CornerRadii : IEquatable<CornerRadii>
```

Four corner radii in pixels.

## Constructors

| Member | Description |
| --- | --- |
| [`CornerRadii(float, float, float, float)`](#m-ctor-float-float-float-float) | Four corner radii in pixels. |

### <a id="m-ctor-float-float-float-float"></a>CornerRadii(float, float, float, float)

```csharp
public CornerRadii(float TopLeft, float TopRight, float BottomRight, float BottomLeft);
```

Four corner radii in pixels.

**Parameters**

- `TopLeft` (`float`): Top-left corner.
- `TopRight` (`float`): Top-right corner.
- `BottomRight` (`float`): Bottom-right corner.
- `BottomLeft` (`float`): Bottom-left corner.

## Properties

| Member | Description |
| --- | --- |
| [`BottomLeft`](#m-bottomleft) | Bottom-left corner. |
| [`BottomRight`](#m-bottomright) | Bottom-right corner. |
| [`IsZero`](#m-iszero) | True when every corner is square. |
| [`TopLeft`](#m-topleft) | Top-left corner. |
| [`TopRight`](#m-topright) | Top-right corner. |

### <a id="m-bottomleft"></a>BottomLeft

```csharp
public float BottomLeft { get; init; }
```

Bottom-left corner.

**Type:** `float`

### <a id="m-bottomright"></a>BottomRight

```csharp
public float BottomRight { get; init; }
```

Bottom-right corner.

**Type:** `float`

### <a id="m-iszero"></a>IsZero

```csharp
public bool IsZero { get; }
```

True when every corner is square.

**Type:** `bool`

### <a id="m-topleft"></a>TopLeft

```csharp
public float TopLeft { get; init; }
```

Top-left corner.

**Type:** `float`

### <a id="m-topright"></a>TopRight

```csharp
public float TopRight { get; init; }
```

Top-right corner.

**Type:** `float`
