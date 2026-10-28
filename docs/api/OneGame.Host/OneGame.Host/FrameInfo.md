[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# FrameInfo struct

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`FrameInfo`](FrameInfo.md)&gt;

```csharp
public readonly struct FrameInfo : IEquatable<FrameInfo>
```

What a render stage is given for one frame.

## Constructors

| Member | Description |
| --- | --- |
| [`FrameInfo(int, int, float, long, DisplayList, World, Fixed)`](#m-ctor-int-int-float-long-displaylist-world-fixed) | What a render stage is given for one frame. |

### <a id="m-ctor-int-int-float-long-displaylist-world-fixed"></a>FrameInfo(int, int, float, long, DisplayList, World, Fixed)

```csharp
public FrameInfo(int Width, int Height, float Scale, long Tick, DisplayList List, World World, Fixed Alpha = default);
```

What a render stage is given for one frame.

**Parameters**

- `Width` (`int`): Target width in pixels.
- `Height` (`int`): Target height in pixels.
- `Scale` (`float`): UI scale the frame was laid out at.
- `Tick` (`long`): The simulation tick the frame shows.
- `List` ([`DisplayList`](../../OneGame.UI/OneGame.UI/DisplayList.md)): What the UI wants drawn.
- `World` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): The simulation state, for stages that draw the world.
- `Alpha` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): How far past `Tick` the frame is, from 0 to just under 1, for stages that draw between ticks.

## Properties

| Member | Description |
| --- | --- |
| [`Alpha`](#m-alpha) | How far past `Tick` the frame is, from 0 to just under 1, for stages that draw between ticks. |
| [`Height`](#m-height) | Target height in pixels. |
| [`List`](#m-list) | What the UI wants drawn. |
| [`Scale`](#m-scale) | UI scale the frame was laid out at. |
| [`Tick`](#m-tick) | The simulation tick the frame shows. |
| [`Width`](#m-width) | Target width in pixels. |
| [`World`](#m-world) | The simulation state, for stages that draw the world. |

### <a id="m-alpha"></a>Alpha

```csharp
public Fixed Alpha { get; init; }
```

How far past `Tick` the frame is, from 0 to just under 1, for stages that draw between ticks.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Target height in pixels.

**Type:** `int`

### <a id="m-list"></a>List

```csharp
public DisplayList List { get; init; }
```

What the UI wants drawn.

**Type:** [`DisplayList`](../../OneGame.UI/OneGame.UI/DisplayList.md)

### <a id="m-scale"></a>Scale

```csharp
public float Scale { get; init; }
```

UI scale the frame was laid out at.

**Type:** `float`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The simulation tick the frame shows.

**Type:** `long`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Target width in pixels.

**Type:** `int`

### <a id="m-world"></a>World

```csharp
public World World { get; init; }
```

The simulation state, for stages that draw the world.

**Type:** [`World`](../../OneGame.Core/OneGame.Core/World.md)
