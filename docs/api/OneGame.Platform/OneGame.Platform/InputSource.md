[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# InputSource struct

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`InputSource`](InputSource.md)&gt;

```csharp
public readonly struct InputSource : IEquatable<InputSource>
```

One physical control an action can listen to: a key, a mouse button, a pad button or axis, the wheel, or a touch region. Small and comparable, so bindings can be stored, compared for conflicts and written to a file. Make them with [`Bind`](Bind.md).

## Constructors

| Member | Description |
| --- | --- |
| [`InputSource(InputSourceKind, int, int)`](#m-ctor-inputsourcekind-int-int) | One physical control an action can listen to: a key, a mouse button, a pad button or axis, the wheel, or a touch region. |

### <a id="m-ctor-inputsourcekind-int-int"></a>InputSource(InputSourceKind, int, int)

```csharp
public InputSource(InputSourceKind Kind, int Code, int Sign = 1);
```

One physical control an action can listen to: a key, a mouse button, a pad button or axis, the wheel, or a touch region. Small and comparable, so bindings can be stored, compared for conflicts and written to a file. Make them with [`Bind`](Bind.md).

**Parameters**

- `Kind` ([`InputSourceKind`](InputSourceKind.md)): The kind of control.
- `Code` (`int`): The [`Key`](Key.md), [`MouseButton`](MouseButton.md), [`GamepadButton`](GamepadButton.md), [`GamepadAxis`](GamepadAxis.md) or [`TouchRegion`](TouchRegion.md) value; for the wheel 0 = vertical, 1 = horizontal.
- `Sign` (`int`): For axes and the wheel: +1 reads the axis as it is (right, down, trigger pulled; wheel away from the user), -1 reads it inverted.

## Properties

| Member | Description |
| --- | --- |
| [`Code`](#m-code) | The [`Key`](Key.md), [`MouseButton`](MouseButton.md), [`GamepadButton`](GamepadButton.md), [`GamepadAxis`](GamepadAxis.md) or [`TouchRegion`](TouchRegion.md) value; for the wheel 0 = vertical, 1 = horizontal. |
| [`IsNone`](#m-isnone) | True for an empty slot. |
| [`IsPad`](#m-ispad) | The device this source belongs to, to pick a glyph set. |
| [`Kind`](#m-kind) | The kind of control. |
| [`Sign`](#m-sign) | For axes and the wheel: +1 reads the axis as it is (right, down, trigger pulled; wheel away from the user), -1 reads it inverted. |

### <a id="m-code"></a>Code

```csharp
public int Code { get; init; }
```

The [`Key`](Key.md), [`MouseButton`](MouseButton.md), [`GamepadButton`](GamepadButton.md), [`GamepadAxis`](GamepadAxis.md) or [`TouchRegion`](TouchRegion.md) value; for the wheel 0 = vertical, 1 = horizontal.

**Type:** `int`

### <a id="m-isnone"></a>IsNone

```csharp
public bool IsNone { get; }
```

True for an empty slot.

**Type:** `bool`

### <a id="m-ispad"></a>IsPad

```csharp
public bool IsPad { get; }
```

The device this source belongs to, to pick a glyph set.

**Type:** `bool`

### <a id="m-kind"></a>Kind

```csharp
public InputSourceKind Kind { get; init; }
```

The kind of control.

**Type:** [`InputSourceKind`](InputSourceKind.md)

### <a id="m-sign"></a>Sign

```csharp
public int Sign { get; init; }
```

For axes and the wheel: +1 reads the axis as it is (right, down, trigger pulled; wheel away from the user), -1 reads it inverted.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Describe()`](#m-describe) | A short label for a binding list: `Space`, `Left mouse`, `Pad South`, `Left stick left`. |
| [`ToString()`](#m-tostring) | The file and harness spelling: `key:Space`, `mouse:Left`, `pad:South`, `axis:LeftX+`, `wheel:y+`, `touch:Left`, `none`. |
| [`TryParse(ReadOnlySpan<char>, out InputSource)`](#m-tryparse-readonlyspan-char-inputsource) | Reads the spelling [`InputSource.ToString()`](InputSource.md#m-tostring) writes. |

### <a id="m-describe"></a>Describe()

```csharp
public string Describe();
```

A short label for a binding list: `Space`, `Left mouse`, `Pad South`, `Left stick left`.

**Returns:** `string`

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

The file and harness spelling: `key:Space`, `mouse:Left`, `pad:South`, `axis:LeftX+`, `wheel:y+`, `touch:Left`, `none`.

**Returns:** `string`

### <a id="m-tryparse-readonlyspan-char-inputsource"></a>TryParse(ReadOnlySpan&lt;char&gt;, out InputSource)

```csharp
public static bool TryParse(ReadOnlySpan<char> text, out InputSource source);
```

Reads the spelling [`InputSource.ToString()`](InputSource.md#m-tostring) writes.

**Parameters**

- `text` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<char>`): The text.
- `source` ([`InputSource`](InputSource.md)): The source, when it parsed.

**Returns:** `bool` — True when `text` names a source.

## Operators

| Member | Description |
| --- | --- |
| [`implicit operator InputSource(Key)`](#m-implicit-operator-inputsource-key) | A key as a source. |

### <a id="m-implicit-operator-inputsource-key"></a>implicit operator InputSource(Key)

```csharp
public static implicit operator InputSource(Key key);
```

A key as a source.

**Parameters**

- `key` ([`Key`](Key.md))

**Returns:** [`InputSource`](InputSource.md)
