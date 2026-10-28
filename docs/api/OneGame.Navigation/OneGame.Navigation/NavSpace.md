[API reference](../../index.md) › [OneGame.Navigation](../index.md) › OneGame.Navigation

# NavSpace struct

**Namespace:** `OneGame.Navigation`  
**Package:** [`RecursiveAI.OneGame.Navigation`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`NavSpace`](NavSpace.md)&gt;

```csharp
public readonly struct NavSpace : IEquatable<NavSpace>
```

Where a grid sits in the world: the top-left corner and the edge length of a cell, both in [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md) units. Cell `(x, y)` covers `origin + (x, y) * CellSize` to `origin + (x + 1, y + 1) * CellSize`.

## Constructors

| Member | Description |
| --- | --- |
| [`NavSpace(FVec2, Fixed)`](#m-ctor-fvec2-fixed) | Creates a mapping. |

### <a id="m-ctor-fvec2-fixed"></a>NavSpace(FVec2, Fixed)

```csharp
public NavSpace(FVec2 origin, Fixed cellSize);
```

Creates a mapping.

**Parameters**

- `origin` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): World position of the grid's top-left corner.
- `cellSize` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): Edge length of a cell; positive.

**Exceptions**

- [`NavRangeException`](NavRangeException.md): A value is beyond ±2^21 units.
- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The cell size is not positive.

## Properties

| Member | Description |
| --- | --- |
| [`CellSize`](#m-cellsize) | Edge length of a cell. |
| [`Origin`](#m-origin) | World position of the top-left corner. |
| [`Unit`](#m-unit) | Unit cells with the corner at (0, 0). |

### <a id="m-cellsize"></a>CellSize

```csharp
public Fixed CellSize { get; }
```

Edge length of a cell. Zero only for `default`, which behaves as [`NavSpace.Unit`](NavSpace.md#m-unit).

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-origin"></a>Origin

```csharp
public FVec2 Origin { get; }
```

World position of the top-left corner.

**Type:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)

### <a id="m-unit"></a>Unit

```csharp
public static NavSpace Unit { get; }
```

Unit cells with the corner at (0, 0).

**Type:** [`NavSpace`](NavSpace.md)

## Methods

| Member | Description |
| --- | --- |
| [`Equals(NavSpace)`](#m-equals-navspace) | Equality of origin and cell size. |
| [`Equals(object?)`](#m-equals-object) |  |
| [`GetHashCode()`](#m-gethashcode) |  |

### <a id="m-equals-navspace"></a>Equals(NavSpace)

```csharp
public bool Equals(NavSpace other);
```

Equality of origin and cell size.

**Parameters**

- `other` ([`NavSpace`](NavSpace.md))

**Returns:** `bool`

### <a id="m-equals-object"></a>Equals(object?)

```csharp
public override bool Equals(object? obj);
```

Overrides [`ValueType.Equals()`](https://learn.microsoft.com/dotnet/api/system.valuetype.equals).

**Parameters**

- `obj` (`object?`)

**Returns:** `bool`

### <a id="m-gethashcode"></a>GetHashCode()

```csharp
public override int GetHashCode();
```

Overrides [`ValueType.GetHashCode()`](https://learn.microsoft.com/dotnet/api/system.valuetype.gethashcode).

**Returns:** `int`

## Operators

| Member | Description |
| --- | --- |
| [`operator !=(NavSpace, NavSpace)`](#m-operator-navspace-navspace) | Inequality. |
| [`operator ==(NavSpace, NavSpace)`](#m-operator-navspace-navspace-2) | Equality. |

### <a id="m-operator-navspace-navspace"></a>operator !=(NavSpace, NavSpace)

```csharp
public static bool operator !=(NavSpace a, NavSpace b);
```

Inequality.

**Parameters**

- `a` ([`NavSpace`](NavSpace.md))
- `b` ([`NavSpace`](NavSpace.md))

**Returns:** `bool`

### <a id="m-operator-navspace-navspace-2"></a>operator ==(NavSpace, NavSpace)

```csharp
public static bool operator ==(NavSpace a, NavSpace b);
```

Equality.

**Parameters**

- `a` ([`NavSpace`](NavSpace.md))
- `b` ([`NavSpace`](NavSpace.md))

**Returns:** `bool`
