[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# ColumnWidth struct

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ColumnWidth`](ColumnWidth.md)&gt;

```csharp
public readonly struct ColumnWidth : IEquatable<ColumnWidth>
```

How wide a table column is: a fixed length, or a share of the space the fixed columns leave.

## Constructors

| Member | Description |
| --- | --- |
| [`ColumnWidth(Length, float)`](#m-ctor-length-float) | How wide a table column is: a fixed length, or a share of the space the fixed columns leave. |

### <a id="m-ctor-length-float"></a>ColumnWidth(Length, float)

```csharp
public ColumnWidth(Length Size, float Grow);
```

How wide a table column is: a fixed length, or a share of the space the fixed columns leave.

**Parameters**

- `Size` ([`Length`](../../OneGame.UI/OneGame.UI/Length.md)): The fixed width in pixels or UI units; ignored for a flexible column.
- `Grow` (`float`): Zero for a fixed column; otherwise the column's share of the remaining width (a column with 2 gets twice a column with 1).

## Properties

| Member | Description |
| --- | --- |
| [`Grow`](#m-grow) | Zero for a fixed column; otherwise the column's share of the remaining width (a column with 2 gets twice a column with 1). |
| [`Size`](#m-size) | The fixed width in pixels or UI units; ignored for a flexible column. |

### <a id="m-grow"></a>Grow

```csharp
public float Grow { get; init; }
```

Zero for a fixed column; otherwise the column's share of the remaining width (a column with 2 gets twice a column with 1).

**Type:** `float`

### <a id="m-size"></a>Size

```csharp
public Length Size { get; init; }
```

The fixed width in pixels or UI units; ignored for a flexible column.

**Type:** [`Length`](../../OneGame.UI/OneGame.UI/Length.md)

## Methods

| Member | Description |
| --- | --- |
| [`Fixed(Length)`](#m-fixed-length) | A column of exactly this width. |
| [`Flex(float)`](#m-flex-float) | A column that shares the remaining width in proportion to `grow`. |

### <a id="m-fixed-length"></a>Fixed(Length)

```csharp
public static ColumnWidth Fixed(Length size);
```

A column of exactly this width.

**Parameters**

- `size` ([`Length`](../../OneGame.UI/OneGame.UI/Length.md))

**Returns:** [`ColumnWidth`](ColumnWidth.md)

### <a id="m-flex-float"></a>Flex(float)

```csharp
public static ColumnWidth Flex(float grow = 1f);
```

A column that shares the remaining width in proportion to `grow`.

**Parameters**

- `grow` (`float`)

**Returns:** [`ColumnWidth`](ColumnWidth.md)
