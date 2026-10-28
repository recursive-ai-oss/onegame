[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# Draw2DReport record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`Draw2DReport`](Draw2DReport.md)&gt;

```csharp
public sealed record Draw2DReport : IEquatable<Draw2DReport>
```

Result of `draw2d`: what the 2D happy path drew in the last frame and where its camera looks.

## Constructors

| Member | Description |
| --- | --- |
| [`Draw2DReport(string, int, int, long, double, int, int, double[], double, double, double[]?, double[]?)`](#m-ctor-string-int-int-long-double-int-int-double-double-double-double-double) | Result of `draw2d`: what the 2D happy path drew in the last frame and where its camera looks. |

### <a id="m-ctor-string-int-int-long-double-int-int-double-double-double-double-double"></a>Draw2DReport(string, int, int, long, double, int, int, double\[\], double, double, double\[\]?, double\[\]?)

```csharp
public Draw2DReport(string Stage, int Sprites, int Texts, long Tick, double Time, int Width, int Height, double[] Position, double Zoom, double Rotation, double[]? World, double[]? Screen);
```

Result of `draw2d`: what the 2D happy path drew in the last frame and where its camera looks.

**Parameters**

- `Stage` (`string`): The render stage's name.
- `Sprites` (`int`): Sprites and shapes queued by the last drawn frame.
- `Texts` (`int`): Text lines queued by the last drawn frame.
- `Tick` (`long`): The tick the last drawn frame showed.
- `Time` (`double`): The view time of that frame in seconds.
- `Width` (`int`): Target width of that frame.
- `Height` (`int`): Target height of that frame.
- `Position` (`double[]`): The camera's world position \[x, y\].
- `Zoom` (`double`): Physical pixels per world unit.
- `Rotation` (`double`): Camera rotation in radians.
- `World` (`double[]?`): The world point under the `screen` pixel that was asked about, or null.
- `Screen` (`double[]?`): The pixel of the `world` point that was asked about, or null.

## Properties

| Member | Description |
| --- | --- |
| [`Height`](#m-height) | Target height of that frame. |
| [`Position`](#m-position) | The camera's world position \[x, y\]. |
| [`Rotation`](#m-rotation) | Camera rotation in radians. |
| [`Screen`](#m-screen) | The pixel of the `world` point that was asked about, or null. |
| [`Sprites`](#m-sprites) | Sprites and shapes queued by the last drawn frame. |
| [`Stage`](#m-stage) | The render stage's name. |
| [`Texts`](#m-texts) | Text lines queued by the last drawn frame. |
| [`Tick`](#m-tick) | The tick the last drawn frame showed. |
| [`Time`](#m-time) | The view time of that frame in seconds. |
| [`Width`](#m-width) | Target width of that frame. |
| [`World`](#m-world) | The world point under the `screen` pixel that was asked about, or null. |
| [`Zoom`](#m-zoom) | Physical pixels per world unit. |

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Target height of that frame.

**Type:** `int`

### <a id="m-position"></a>Position

```csharp
public double[] Position { get; init; }
```

The camera's world position \[x, y\].

**Type:** `double[]`

### <a id="m-rotation"></a>Rotation

```csharp
public double Rotation { get; init; }
```

Camera rotation in radians.

**Type:** `double`

### <a id="m-screen"></a>Screen

```csharp
public double[]? Screen { get; init; }
```

The pixel of the `world` point that was asked about, or null.

**Type:** `double[]?`

### <a id="m-sprites"></a>Sprites

```csharp
public int Sprites { get; init; }
```

Sprites and shapes queued by the last drawn frame.

**Type:** `int`

### <a id="m-stage"></a>Stage

```csharp
public string Stage { get; init; }
```

The render stage's name.

**Type:** `string`

### <a id="m-texts"></a>Texts

```csharp
public int Texts { get; init; }
```

Text lines queued by the last drawn frame.

**Type:** `int`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The tick the last drawn frame showed.

**Type:** `long`

### <a id="m-time"></a>Time

```csharp
public double Time { get; init; }
```

The view time of that frame in seconds.

**Type:** `double`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Target width of that frame.

**Type:** `int`

### <a id="m-world"></a>World

```csharp
public double[]? World { get; init; }
```

The world point under the `screen` pixel that was asked about, or null.

**Type:** `double[]?`

### <a id="m-zoom"></a>Zoom

```csharp
public double Zoom { get; init; }
```

Physical pixels per world unit.

**Type:** `double`
