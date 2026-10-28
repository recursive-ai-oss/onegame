[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# PhysicsLimits static class

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)

```csharp
public static class PhysicsLimits
```

The limits of the fixed-point to physics conversion, and the checked conversions themselves.

## Fields

| Member | Description |
| --- | --- |
| [`MaxCoordinate`](#m-maxcoordinate) | Largest absolute coordinate or length, in world units: 2^21. |
| [`MaxCoordinateRaw`](#m-maxcoordinateraw) | The same limit as a raw [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md) value. |

### <a id="m-maxcoordinate"></a>MaxCoordinate

```csharp
public const int MaxCoordinate = 2097152;
```

Largest absolute coordinate or length, in world units: 2^21. Inside it a [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md) (32 fraction bits) is exactly a double (53 bits), which is what the physics world needs to treat positions identically everywhere.

**Type:** `int`

### <a id="m-maxcoordinateraw"></a>MaxCoordinateRaw

```csharp
public const long MaxCoordinateRaw = 9007199254740992;
```

The same limit as a raw [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md) value.

**Type:** `long`

## Methods

| Member | Description |
| --- | --- |
| [`Check(Fixed, string)`](#m-check-fixed-string) | Throws [`PhysicsRangeException`](PhysicsRangeException.md) unless `value` is within ±[`PhysicsLimits.MaxCoordinate`](PhysicsLimits.md#m-maxcoordinate). |
| [`FromDouble(double)`](#m-fromdouble-double) | A double as the nearest [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md), ties to even. |
| [`ToDouble(Fixed, string)`](#m-todouble-fixed-string) | The value as a double, exactly. |
| [`ToSingle(Fixed, string)`](#m-tosingle-fixed-string) | The value as a single-precision float, rounded to nearest even (the physics world's own precision). |

### <a id="m-check-fixed-string"></a>Check(Fixed, string)

```csharp
public static void Check(Fixed value, string paramName);
```

Throws [`PhysicsRangeException`](PhysicsRangeException.md) unless `value` is within ±[`PhysicsLimits.MaxCoordinate`](PhysicsLimits.md#m-maxcoordinate).

**Parameters**

- `value` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): The coordinate or length.
- `paramName` (`string`): Name reported in the exception.

**Exceptions**

- [`PhysicsRangeException`](PhysicsRangeException.md): The value is out of range.

### <a id="m-fromdouble-double"></a>FromDouble(double)

```csharp
public static Fixed FromDouble(double value);
```

A double as the nearest [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md), ties to even. Throws when it is not finite or is outside the range.

**Parameters**

- `value` (`double`): The value.

**Returns:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

**Exceptions**

- [`PhysicsRangeException`](PhysicsRangeException.md): The value is out of range.

### <a id="m-todouble-fixed-string"></a>ToDouble(Fixed, string)

```csharp
public static double ToDouble(Fixed value, string paramName = "value");
```

The value as a double, exactly. Throws when it is outside the range.

**Parameters**

- `value` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): The value.
- `paramName` (`string`): Name reported in the exception.

**Returns:** `double`

**Exceptions**

- [`PhysicsRangeException`](PhysicsRangeException.md): The value is out of range.

### <a id="m-tosingle-fixed-string"></a>ToSingle(Fixed, string)

```csharp
public static float ToSingle(Fixed value, string paramName = "value");
```

The value as a single-precision float, rounded to nearest even (the physics world's own precision). Throws when it is outside the range.

**Parameters**

- `value` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): The value.
- `paramName` (`string`): Name reported in the exception.

**Returns:** `float`

**Exceptions**

- [`PhysicsRangeException`](PhysicsRangeException.md): The value is out of range.
