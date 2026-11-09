[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# WorkAreaInfo record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`WorkAreaInfo`](WorkAreaInfo.md)&gt;

```csharp
public sealed record WorkAreaInfo : IEquatable<WorkAreaInfo>
```

A screen's usable rectangle in [`WindowResult`](WindowResult.md).

## Constructors

| Member | Description |
| --- | --- |
| [`WorkAreaInfo(int, int, int, int)`](#m-ctor-int-int-int-int) | A screen's usable rectangle in [`WindowResult`](WindowResult.md). |

### <a id="m-ctor-int-int-int-int"></a>WorkAreaInfo(int, int, int, int)

```csharp
public WorkAreaInfo(int X, int Y, int Width, int Height);
```

A screen's usable rectangle in [`WindowResult`](WindowResult.md).

**Parameters**

- `X` (`int`): Left edge in screen pixels.
- `Y` (`int`): Top edge in screen pixels.
- `Width` (`int`): Width in pixels.
- `Height` (`int`): Height in pixels.

## Properties

| Member | Description |
| --- | --- |
| [`Height`](#m-height) | Height in pixels. |
| [`Width`](#m-width) | Width in pixels. |
| [`X`](#m-x) | Left edge in screen pixels. |
| [`Y`](#m-y) | Top edge in screen pixels. |

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Height in pixels.

**Type:** `int`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Width in pixels.

**Type:** `int`

### <a id="m-x"></a>X

```csharp
public int X { get; init; }
```

Left edge in screen pixels.

**Type:** `int`

### <a id="m-y"></a>Y

```csharp
public int Y { get; init; }
```

Top edge in screen pixels.

**Type:** `int`
