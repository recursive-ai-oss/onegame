[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# ActionBinding struct

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ActionBinding`](ActionBinding.md)&gt;

```csharp
public readonly struct ActionBinding : IEquatable<ActionBinding>
```

What one slot of an action listens to: a single source or a composite (two keys making an axis, WASD making a vector, a stick). An action holds several, and any of them can drive it. Make them with [`Bind`](Bind.md); a bare [`InputSource`](InputSource.md) converts.

## Properties

| Member | Description |
| --- | --- |
| [`IsEmpty`](#m-isempty) | True when every part is empty. |
| [`PartCount`](#m-partcount) | How many sources the shape has (1, 2, 4 or 1). |
| [`Shape`](#m-shape) | The arrangement. |

### <a id="m-isempty"></a>IsEmpty

```csharp
public bool IsEmpty { get; }
```

True when every part is empty.

**Type:** `bool`

### <a id="m-partcount"></a>PartCount

```csharp
public int PartCount { get; }
```

How many sources the shape has (1, 2, 4 or 1).

**Type:** `int`

### <a id="m-shape"></a>Shape

```csharp
public BindingShape Shape { get; }
```

The arrangement.

**Type:** [`BindingShape`](BindingShape.md)

## Methods

| Member | Description |
| --- | --- |
| [`Describe()`](#m-describe) | A short label for a binding list, such as `W A S D` or `Space`. |
| [`Equals(ActionBinding)`](#m-equals-actionbinding) |  |
| [`Equals(object?)`](#m-equals-object) |  |
| [`GetHashCode()`](#m-gethashcode) |  |
| [`Part(int)`](#m-part-int) | One source of the binding. |
| [`ToString()`](#m-tostring) | The file spelling: a source, `pair(a,b)`, `quad(l,r,u,d)` or `stick:left`/`stick:right`. |
| [`TryParse(ReadOnlySpan<char>, out ActionBinding)`](#m-tryparse-readonlyspan-char-actionbinding) | Reads the spelling [`ActionBinding.ToString()`](ActionBinding.md#m-tostring) writes. |
| [`Uses(InputSource)`](#m-uses-inputsource) | True when any part reads exactly `source`. |
| [`With(int, InputSource)`](#m-with-int-inputsource) | Returns a copy with one part replaced. |

### <a id="m-describe"></a>Describe()

```csharp
public string Describe();
```

A short label for a binding list, such as `W A S D` or `Space`.

**Returns:** `string`

### <a id="m-equals-actionbinding"></a>Equals(ActionBinding)

```csharp
public bool Equals(ActionBinding other);
```

**Parameters**

- `other` ([`ActionBinding`](ActionBinding.md))

**Returns:** `bool`

### <a id="m-equals-object"></a>Equals(object?)

```csharp
public override bool Equals(object? obj);
```

**Parameters**

- `obj` (`object?`)

**Returns:** `bool`

### <a id="m-gethashcode"></a>GetHashCode()

```csharp
public override int GetHashCode();
```

**Returns:** `int`

### <a id="m-part-int"></a>Part(int)

```csharp
public InputSource Part(int part);
```

One source of the binding.

**Parameters**

- `part` (`int`): 0 for a single source; 0..1 for a pair; 0..3 (left, right, up, down) for a quad.

**Returns:** [`InputSource`](InputSource.md) — The source.

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

The file spelling: a source, `pair(a,b)`, `quad(l,r,u,d)` or `stick:left`/`stick:right`.

**Returns:** `string`

### <a id="m-tryparse-readonlyspan-char-actionbinding"></a>TryParse(ReadOnlySpan&lt;char&gt;, out ActionBinding)

```csharp
public static bool TryParse(ReadOnlySpan<char> text, out ActionBinding binding);
```

Reads the spelling [`ActionBinding.ToString()`](ActionBinding.md#m-tostring) writes.

**Parameters**

- `text` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<char>`): The text.
- `binding` ([`ActionBinding`](ActionBinding.md)): The binding, when it parsed.

**Returns:** `bool` — True when `text` is a binding.

### <a id="m-uses-inputsource"></a>Uses(InputSource)

```csharp
public bool Uses(InputSource source);
```

True when any part reads exactly `source`.

**Parameters**

- `source` ([`InputSource`](InputSource.md))

**Returns:** `bool`

### <a id="m-with-int-inputsource"></a>With(int, InputSource)

```csharp
public ActionBinding With(int part, InputSource source);
```

Returns a copy with one part replaced.

**Parameters**

- `part` (`int`): The part index.
- `source` ([`InputSource`](InputSource.md)): The new source.

**Returns:** [`ActionBinding`](ActionBinding.md) — The changed binding.

## Operators

| Member | Description |
| --- | --- |
| [`implicit operator ActionBinding(InputSource)`](#m-implicit-operator-actionbinding-inputsource) | A single source as a binding. |
| [`implicit operator ActionBinding(Key)`](#m-implicit-operator-actionbinding-key) | A key as a binding. |
| [`operator !=(ActionBinding, ActionBinding)`](#m-operator-actionbinding-actionbinding) | Inequality. |
| [`operator ==(ActionBinding, ActionBinding)`](#m-operator-actionbinding-actionbinding-2) | Equality. |

### <a id="m-implicit-operator-actionbinding-inputsource"></a>implicit operator ActionBinding(InputSource)

```csharp
public static implicit operator ActionBinding(InputSource source);
```

A single source as a binding.

**Parameters**

- `source` ([`InputSource`](InputSource.md))

**Returns:** [`ActionBinding`](ActionBinding.md)

### <a id="m-implicit-operator-actionbinding-key"></a>implicit operator ActionBinding(Key)

```csharp
public static implicit operator ActionBinding(Key key);
```

A key as a binding.

**Parameters**

- `key` ([`Key`](Key.md))

**Returns:** [`ActionBinding`](ActionBinding.md)

### <a id="m-operator-actionbinding-actionbinding"></a>operator !=(ActionBinding, ActionBinding)

```csharp
public static bool operator !=(ActionBinding left, ActionBinding right);
```

Inequality.

**Parameters**

- `left` ([`ActionBinding`](ActionBinding.md))
- `right` ([`ActionBinding`](ActionBinding.md))

**Returns:** `bool`

### <a id="m-operator-actionbinding-actionbinding-2"></a>operator ==(ActionBinding, ActionBinding)

```csharp
public static bool operator ==(ActionBinding left, ActionBinding right);
```

Equality.

**Parameters**

- `left` ([`ActionBinding`](ActionBinding.md))
- `right` ([`ActionBinding`](ActionBinding.md))

**Returns:** `bool`
