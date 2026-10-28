[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# Length struct

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`Length`](Length.md)&gt;

```csharp
public readonly struct Length : IEquatable<Length>
```

A CSS-style length: a number and a unit. A bare number converts implicitly to pixels.

## Constructors

| Member | Description |
| --- | --- |
| [`Length(float, LengthUnit)`](#m-ctor-float-lengthunit) | A CSS-style length: a number and a unit. |

### <a id="m-ctor-float-lengthunit"></a>Length(float, LengthUnit)

```csharp
public Length(float Value, LengthUnit Unit);
```

A CSS-style length: a number and a unit. A bare number converts implicitly to pixels.

**Parameters**

- `Value` (`float`): The number, in `Unit`.
- `Unit` ([`LengthUnit`](LengthUnit.md)): The unit.

## Properties

| Member | Description |
| --- | --- |
| [`Auto`](#m-auto) | The `auto` length. |
| [`IsAuto`](#m-isauto) | True for `auto`. |
| [`Unit`](#m-unit) | The unit. |
| [`Value`](#m-value) | The number, in `Unit`. |

### <a id="m-auto"></a>Auto

```csharp
public static Length Auto { get; }
```

The `auto` length.

**Type:** [`Length`](Length.md)

### <a id="m-isauto"></a>IsAuto

```csharp
public bool IsAuto { get; }
```

True for `auto`.

**Type:** `bool`

### <a id="m-unit"></a>Unit

```csharp
public LengthUnit Unit { get; init; }
```

The unit.

**Type:** [`LengthUnit`](LengthUnit.md)

### <a id="m-value"></a>Value

```csharp
public float Value { get; init; }
```

The number, in `Unit`.

**Type:** `float`

## Methods

| Member | Description |
| --- | --- |
| [`Percent(float)`](#m-percent-float) | A percentage (50 means half). |
| [`Px(float)`](#m-px-float) | Pixels. |
| [`ToString()`](#m-tostring) | The CSS spelling, such as `12px`, `50%`, `2ui` or `auto`. |
| [`Ui(float)`](#m-ui-float) | UI units, scaled by [`UiTree.Scale`](UiTree.md#m-scale). |

### <a id="m-percent-float"></a>Percent(float)

```csharp
public static Length Percent(float value);
```

A percentage (50 means half).

**Parameters**

- `value` (`float`)

**Returns:** [`Length`](Length.md)

### <a id="m-px-float"></a>Px(float)

```csharp
public static Length Px(float value);
```

Pixels.

**Parameters**

- `value` (`float`)

**Returns:** [`Length`](Length.md)

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

The CSS spelling, such as `12px`, `50%`, `2ui` or `auto`.

**Returns:** `string`

### <a id="m-ui-float"></a>Ui(float)

```csharp
public static Length Ui(float value);
```

UI units, scaled by [`UiTree.Scale`](UiTree.md#m-scale).

**Parameters**

- `value` (`float`)

**Returns:** [`Length`](Length.md)

## Operators

| Member | Description |
| --- | --- |
| [`implicit operator Length(float)`](#m-implicit-operator-length-float) | Converts a pixel count to a length. |

### <a id="m-implicit-operator-length-float"></a>implicit operator Length(float)

```csharp
public static implicit operator Length(float px);
```

Converts a pixel count to a length.

**Parameters**

- `px` (`float`)

**Returns:** [`Length`](Length.md)
