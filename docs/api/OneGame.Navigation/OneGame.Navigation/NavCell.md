[API reference](../../index.md) › [OneGame.Navigation](../index.md) › OneGame.Navigation

# NavCell struct

**Namespace:** `OneGame.Navigation`  
**Package:** [`RecursiveAI.OneGame.Navigation`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`NavCell`](NavCell.md)&gt;

```csharp
public readonly struct NavCell : IEquatable<NavCell>
```

A grid cell.

## Constructors

| Member | Description |
| --- | --- |
| [`NavCell(int, int)`](#m-ctor-int-int) | A grid cell. |

### <a id="m-ctor-int-int"></a>NavCell(int, int)

```csharp
public NavCell(int X, int Y);
```

A grid cell.

**Parameters**

- `X` (`int`): Column, from the left.
- `Y` (`int`): Row, from the top.

## Properties

| Member | Description |
| --- | --- |
| [`X`](#m-x) | Column, from the left. |
| [`Y`](#m-y) | Row, from the top. |

### <a id="m-x"></a>X

```csharp
public int X { get; init; }
```

Column, from the left.

**Type:** `int`

### <a id="m-y"></a>Y

```csharp
public int Y { get; init; }
```

Row, from the top.

**Type:** `int`
