[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# Edges struct

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`Edges`](Edges.md)&gt;

```csharp
public readonly struct Edges : IEquatable<Edges>
```

Four per-side values (border widths, nine-slice insets).

## Constructors

| Member | Description |
| --- | --- |
| [`Edges(float, float, float, float)`](#m-ctor-float-float-float-float) | Four per-side values (border widths, nine-slice insets). |

### <a id="m-ctor-float-float-float-float"></a>Edges(float, float, float, float)

```csharp
public Edges(float Left, float Top, float Right, float Bottom);
```

Four per-side values (border widths, nine-slice insets).

**Parameters**

- `Left` (`float`): Left side.
- `Top` (`float`): Top side.
- `Right` (`float`): Right side.
- `Bottom` (`float`): Bottom side.

## Properties

| Member | Description |
| --- | --- |
| [`Bottom`](#m-bottom) | Bottom side. |
| [`IsZero`](#m-iszero) | True when every side is zero. |
| [`Left`](#m-left) | Left side. |
| [`Right`](#m-right) | Right side. |
| [`Top`](#m-top) | Top side. |

### <a id="m-bottom"></a>Bottom

```csharp
public float Bottom { get; init; }
```

Bottom side.

**Type:** `float`

### <a id="m-iszero"></a>IsZero

```csharp
public bool IsZero { get; }
```

True when every side is zero.

**Type:** `bool`

### <a id="m-left"></a>Left

```csharp
public float Left { get; init; }
```

Left side.

**Type:** `float`

### <a id="m-right"></a>Right

```csharp
public float Right { get; init; }
```

Right side.

**Type:** `float`

### <a id="m-top"></a>Top

```csharp
public float Top { get; init; }
```

Top side.

**Type:** `float`
