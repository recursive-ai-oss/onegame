[API reference](../../index.md) › [OneGame.Particles](../index.md) › OneGame.Particles

# FloatRange struct

**Namespace:** `OneGame.Particles`  
**Package:** [`RecursiveAI.OneGame.Particles`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`FloatRange`](FloatRange.md)&gt;

```csharp
public readonly struct FloatRange : IEquatable<FloatRange>
```

A range of values; each particle draws one uniformly between [`FloatRange.Min`](FloatRange.md#m-min) and [`FloatRange.Max`](FloatRange.md#m-max). A plain number converts to a range of one value.

## Constructors

| Member | Description |
| --- | --- |
| [`FloatRange(float, float)`](#m-ctor-float-float) | A range of values; each particle draws one uniformly between [`FloatRange.Min`](FloatRange.md#m-min) and [`FloatRange.Max`](FloatRange.md#m-max). |

### <a id="m-ctor-float-float"></a>FloatRange(float, float)

```csharp
public FloatRange(float Min, float Max);
```

A range of values; each particle draws one uniformly between [`FloatRange.Min`](FloatRange.md#m-min) and [`FloatRange.Max`](FloatRange.md#m-max). A plain number converts to a range of one value.

**Parameters**

- `Min` (`float`): Lowest value.
- `Max` (`float`): Highest value.

## Properties

| Member | Description |
| --- | --- |
| [`Max`](#m-max) | Highest value. |
| [`Min`](#m-min) | Lowest value. |

### <a id="m-max"></a>Max

```csharp
public float Max { get; init; }
```

Highest value.

**Type:** `float`

### <a id="m-min"></a>Min

```csharp
public float Min { get; init; }
```

Lowest value.

**Type:** `float`

## Methods

| Member | Description |
| --- | --- |
| [`FromSingle(float)`](#m-fromsingle-float) | A range that always gives `value`. |

### <a id="m-fromsingle-float"></a>FromSingle(float)

```csharp
public static FloatRange FromSingle(float value);
```

A range that always gives `value`.

**Parameters**

- `value` (`float`): The value.

**Returns:** [`FloatRange`](FloatRange.md) — The range.

## Operators

| Member | Description |
| --- | --- |
| [`implicit operator FloatRange(float)`](#m-implicit-operator-floatrange-float) | A range that always gives `value`. |

### <a id="m-implicit-operator-floatrange-float"></a>implicit operator FloatRange(float)

```csharp
public static implicit operator FloatRange(float value);
```

A range that always gives `value`.

**Parameters**

- `value` (`float`): The value.

**Returns:** [`FloatRange`](FloatRange.md)
