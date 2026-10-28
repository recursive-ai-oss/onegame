[API reference](../../index.md) › [OneGame.Navigation](../index.md) › OneGame.Navigation

# NavLimits static class

**Namespace:** `OneGame.Navigation`  
**Package:** [`RecursiveAI.OneGame.Navigation`](../index.md)

```csharp
public static class NavLimits
```

The limits of the fixed-point to grid conversion.

## Fields

| Member | Description |
| --- | --- |
| [`MaxCells`](#m-maxcells) | Largest number of cells in one grid. |
| [`MaxCoordinate`](#m-maxcoordinate) | Largest absolute world coordinate, in world units: 2^21. |
| [`MaxCoordinateRaw`](#m-maxcoordinateraw) | The same limit as a raw [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md) value. |
| [`MaxPathCells`](#m-maxpathcells) | Longest stored path, in cells. |

### <a id="m-maxcells"></a>MaxCells

```csharp
public const int MaxCells = 16777216;
```

Largest number of cells in one grid.

**Type:** `int`

### <a id="m-maxcoordinate"></a>MaxCoordinate

```csharp
public const int MaxCoordinate = 2097152;
```

Largest absolute world coordinate, in world units: 2^21. Inside it a [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md) (32 fraction bits) is exactly a double (53 bits), which is what the search library needs to give identical results everywhere.

**Type:** `int`

### <a id="m-maxcoordinateraw"></a>MaxCoordinateRaw

```csharp
public const long MaxCoordinateRaw = 9007199254740992;
```

The same limit as a raw [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md) value.

**Type:** `long`

### <a id="m-maxpathcells"></a>MaxPathCells

```csharp
public const int MaxPathCells = 65536;
```

Longest stored path, in cells. A longer path is reported as [`NavStatus.NoPath`](NavStatus.md#m-nopath).

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Check(Fixed, string)`](#m-check-fixed-string) | Throws [`NavRangeException`](NavRangeException.md) unless `value` is within ±[`NavLimits.MaxCoordinate`](NavLimits.md#m-maxcoordinate). |

### <a id="m-check-fixed-string"></a>Check(Fixed, string)

```csharp
public static void Check(Fixed value, string paramName);
```

Throws [`NavRangeException`](NavRangeException.md) unless `value` is within ±[`NavLimits.MaxCoordinate`](NavLimits.md#m-maxcoordinate).

**Parameters**

- `value` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): The coordinate.
- `paramName` (`string`): Name reported in the exception.

**Exceptions**

- [`NavRangeException`](NavRangeException.md): The value is out of range.
