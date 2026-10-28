[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# StyleValue struct

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`StyleValue`](StyleValue.md)&gt;

```csharp
public readonly struct StyleValue : IEquatable<StyleValue>
```

A value for a style property or a theme token: a length, a number, a color, a keyword or a string.

## Methods

| Member | Description |
| --- | --- |
| [`Equals(StyleValue)`](#m-equals-stylevalue) |  |
| [`Equals(object?)`](#m-equals-object) |  |
| [`GetHashCode()`](#m-gethashcode) |  |
| [`Number(float)`](#m-number-float) | A plain number (for `opacity`, `flex-grow`, `z-index` and similar). |
| [`ToString()`](#m-tostring) | The CSS spelling of the value (keywords need the property, so enums print as numbers here). |

### <a id="m-equals-stylevalue"></a>Equals(StyleValue)

```csharp
public bool Equals(StyleValue other);
```

Implements [`IEquatable<StyleValue>.Equals()`](https://learn.microsoft.com/dotnet/api/system.iequatable-1.equals).

**Parameters**

- `other` ([`StyleValue`](StyleValue.md))

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

### <a id="m-number-float"></a>Number(float)

```csharp
public static StyleValue Number(float value);
```

A plain number (for `opacity`, `flex-grow`, `z-index` and similar).

**Parameters**

- `value` (`float`)

**Returns:** [`StyleValue`](StyleValue.md)

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

The CSS spelling of the value (keywords need the property, so enums print as numbers here).

**Returns:** `string`

## Operators

| Member | Description |
| --- | --- |
| [`implicit operator StyleValue(Color)`](#m-implicit-operator-stylevalue-color) | A color. |
| [`implicit operator StyleValue(Length)`](#m-implicit-operator-stylevalue-length) | A length. |
| [`implicit operator StyleValue(string)`](#m-implicit-operator-stylevalue-string) | A string (font family, image id, transition list). |
| [`operator !=(StyleValue, StyleValue)`](#m-operator-stylevalue-stylevalue) | Inequality. |
| [`operator ==(StyleValue, StyleValue)`](#m-operator-stylevalue-stylevalue-2) | Equality. |

### <a id="m-implicit-operator-stylevalue-color"></a>implicit operator StyleValue(Color)

```csharp
public static implicit operator StyleValue(Color value);
```

A color.

**Parameters**

- `value` ([`Color`](Color.md))

**Returns:** [`StyleValue`](StyleValue.md)

### <a id="m-implicit-operator-stylevalue-length"></a>implicit operator StyleValue(Length)

```csharp
public static implicit operator StyleValue(Length value);
```

A length.

**Parameters**

- `value` ([`Length`](Length.md))

**Returns:** [`StyleValue`](StyleValue.md)

### <a id="m-implicit-operator-stylevalue-string"></a>implicit operator StyleValue(string)

```csharp
public static implicit operator StyleValue(string value);
```

A string (font family, image id, transition list).

**Parameters**

- `value` (`string`)

**Returns:** [`StyleValue`](StyleValue.md)

### <a id="m-operator-stylevalue-stylevalue"></a>operator !=(StyleValue, StyleValue)

```csharp
public static bool operator !=(StyleValue a, StyleValue b);
```

Inequality.

**Parameters**

- `a` ([`StyleValue`](StyleValue.md))
- `b` ([`StyleValue`](StyleValue.md))

**Returns:** `bool`

### <a id="m-operator-stylevalue-stylevalue-2"></a>operator ==(StyleValue, StyleValue)

```csharp
public static bool operator ==(StyleValue a, StyleValue b);
```

Equality.

**Parameters**

- `a` ([`StyleValue`](StyleValue.md))
- `b` ([`StyleValue`](StyleValue.md))

**Returns:** `bool`
