[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# Camera2DRig class

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)

```csharp
public sealed class Camera2DRig
```

A camera that follows a point: optional smoothing, world bounds, zoom, and conversion between screen pixels and world points. It is view state (it remembers where it is between frames) and is never part of the simulation. [`Camera2D`](Camera2D.md) remains the low-level value the renderer takes; [`Camera2DRig.Current()`](Camera2DRig.md#m-current-vector2) gives it.

**Example**

```csharp
var camera = new Camera2DRig { Zoom = 2, Smoothing = 0.2f, Bounds = new Rect(0, 0, 1600, 900) };
camera.Snap(start);                                  // jump to the start without easing
camera.Follow(playerPosition, seconds);              // each frame; seconds from the view's clock
var screen = new Vector2(1280, 720);
Vector2 onScreen = camera.WorldToScreen(enemyPosition, screen);
Vector2 underMouse = camera.ScreenToWorld(mouse, screen);
```

## Constructors

| Member | Description |
| --- | --- |
| [`Camera2DRig()`](#m-ctor) |  |

### <a id="m-ctor"></a>Camera2DRig()

```csharp
public Camera2DRig();
```

## Properties

| Member | Description |
| --- | --- |
| [`Bounds`](#m-bounds) | The world area the view may not leave, or null for none. |
| [`PixelSnap`](#m-pixelsnap) | Round sprite positions to whole pixels; see [`Camera2D.PixelSnap`](Camera2D.md#m-pixelsnap). |
| [`Position`](#m-position) | The world point at the centre of the screen. |
| [`Rotation`](#m-rotation) | Rotation in radians; positive turns the view clockwise. |
| [`Smoothing`](#m-smoothing) | How quickly the camera catches up with its target, as a time constant in seconds (about 63% of the distance closes in that time). |
| [`Zoom`](#m-zoom) | Physical pixels per world unit. |

### <a id="m-bounds"></a>Bounds

```csharp
public Rect? Bounds { get; set; }
```

The world area the view may not leave, or null for none. A view larger than the bounds is centred on them.

**Type:** [`Rect`](../../OneGame.UI/OneGame.UI/Rect.md)?

### <a id="m-pixelsnap"></a>PixelSnap

```csharp
public bool PixelSnap { get; set; }
```

Round sprite positions to whole pixels; see [`Camera2D.PixelSnap`](Camera2D.md#m-pixelsnap).

**Type:** `bool`

### <a id="m-position"></a>Position

```csharp
public Vector2 Position { get; }
```

The world point at the centre of the screen.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-rotation"></a>Rotation

```csharp
public float Rotation { get; set; }
```

Rotation in radians; positive turns the view clockwise.

**Type:** `float`

### <a id="m-smoothing"></a>Smoothing

```csharp
public float Smoothing { get; set; }
```

How quickly the camera catches up with its target, as a time constant in seconds (about 63% of the distance closes in that time). Zero follows exactly.

**Type:** `float`

### <a id="m-zoom"></a>Zoom

```csharp
public float Zoom { get; set; }
```

Physical pixels per world unit.

**Type:** `float`

## Methods

| Member | Description |
| --- | --- |
| [`Current(Vector2)`](#m-current-vector2) | The camera for a screen size, with the bounds applied. |
| [`Follow(Vector2, float)`](#m-follow-vector2-float) | Moves toward a target. |
| [`ScreenToWorld(Vector2, Vector2)`](#m-screentoworld-vector2-vector2) | The world point under a screen pixel. |
| [`Snap(Vector2)`](#m-snap-vector2) | Puts the camera on a point at once, with no smoothing, and keeps it there until the next [`Camera2DRig.Follow()`](Camera2DRig.md#m-follow-vector2-float). |
| [`WorldToScreen(Vector2, Vector2)`](#m-worldtoscreen-vector2-vector2) | Where a world point lands on the screen, in pixels. |

### <a id="m-current-vector2"></a>Current(Vector2)

```csharp
public Camera2D Current(Vector2 screenSize);
```

The camera for a screen size, with the bounds applied.

**Parameters**

- `screenSize` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Target size in pixels.

**Returns:** [`Camera2D`](Camera2D.md) — The value to pass to the renderer.

### <a id="m-follow-vector2-float"></a>Follow(Vector2, float)

```csharp
public void Follow(Vector2 target, float seconds);
```

Moves toward a target. The first call, and any call after [`Camera2DRig.Snap()`](Camera2DRig.md#m-snap-vector2) was never made, starts exactly on it.

**Parameters**

- `target` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): The world point to centre on.
- `seconds` (`float`): View time since the last call; with [`Camera2DRig.Smoothing`](Camera2DRig.md#m-smoothing) above zero it sets how far the camera closes in.

### <a id="m-screentoworld-vector2-vector2"></a>ScreenToWorld(Vector2, Vector2)

```csharp
public Vector2 ScreenToWorld(Vector2 screen, Vector2 screenSize);
```

The world point under a screen pixel.

**Parameters**

- `screen` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Framebuffer pixels, origin top-left.
- `screenSize` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Target size in pixels.

**Returns:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2) — A world point.

### <a id="m-snap-vector2"></a>Snap(Vector2)

```csharp
public void Snap(Vector2 position);
```

Puts the camera on a point at once, with no smoothing, and keeps it there until the next [`Camera2DRig.Follow()`](Camera2DRig.md#m-follow-vector2-float).

**Parameters**

- `position` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): The world point to centre.

### <a id="m-worldtoscreen-vector2-vector2"></a>WorldToScreen(Vector2, Vector2)

```csharp
public Vector2 WorldToScreen(Vector2 world, Vector2 screenSize);
```

Where a world point lands on the screen, in pixels.

**Parameters**

- `world` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): A world point.
- `screenSize` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Target size in pixels.

**Returns:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2) — Framebuffer pixels, origin top-left.
