[API reference](../../index.md) › [OneGame.Navigation](../index.md) › OneGame.Navigation

# NavConvert static class

**Namespace:** `OneGame.Navigation`  
**Package:** [`RecursiveAI.OneGame.Navigation`](../index.md)

```csharp
public static class NavConvert
```

Converts between [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md) numbers and doubles, for the cases where a double is unavoidable.

## Methods

| Member | Description |
| --- | --- |
| [`FromDouble(double)`](#m-fromdouble-double) | A double as a fixed-point number, rounded to the nearest value (ties to even). |
| [`ToDouble(Fixed)`](#m-todouble-fixed) | A fixed-point number as a double: exact, because the value is within ±2^21 and so needs at most 53 bits. |

### <a id="m-fromdouble-double"></a>FromDouble(double)

```csharp
public static Fixed FromDouble(double value);
```

A double as a fixed-point number, rounded to the nearest value (ties to even). Exact for any double that came from a [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md).

**Parameters**

- `value` (`double`): The number.

**Returns:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md) — The nearest fixed-point value.

**Exceptions**

- [`NavRangeException`](NavRangeException.md): The value is NaN or beyond ±2^21.

### <a id="m-todouble-fixed"></a>ToDouble(Fixed)

```csharp
public static double ToDouble(Fixed value);
```

A fixed-point number as a double: exact, because the value is within ±2^21 and so needs at most 53 bits.

**Parameters**

- `value` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): The number.

**Returns:** `double` — The same value.

**Exceptions**

- [`NavRangeException`](NavRangeException.md): The value is beyond ±2^21.
