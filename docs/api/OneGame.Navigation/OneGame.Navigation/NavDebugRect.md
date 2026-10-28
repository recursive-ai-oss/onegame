[API reference](../../index.md) › [OneGame.Navigation](../index.md) › OneGame.Navigation

# NavDebugRect struct

**Namespace:** `OneGame.Navigation`  
**Package:** [`RecursiveAI.OneGame.Navigation`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`NavDebugRect`](NavDebugRect.md)&gt;

```csharp
public readonly struct NavDebugRect : IEquatable<NavDebugRect>
```

A filled rectangle of debug geometry in world units.

## Constructors

| Member | Description |
| --- | --- |
| [`NavDebugRect(Vector2, Vector2, uint)`](#m-ctor-vector2-vector2-uint) | A filled rectangle of debug geometry in world units. |

### <a id="m-ctor-vector2-vector2-uint"></a>NavDebugRect(Vector2, Vector2, uint)

```csharp
public NavDebugRect(Vector2 Position, Vector2 Size, uint Rgba);
```

A filled rectangle of debug geometry in world units.

**Parameters**

- `Position` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Top-left corner.
- `Size` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Width and height.
- `Rgba` (`uint`): Colour as `0xRRGGBBAA`.

## Properties

| Member | Description |
| --- | --- |
| [`Position`](#m-position) | Top-left corner. |
| [`Rgba`](#m-rgba) | Colour as `0xRRGGBBAA`. |
| [`Size`](#m-size) | Width and height. |

### <a id="m-position"></a>Position

```csharp
public Vector2 Position { get; init; }
```

Top-left corner.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-rgba"></a>Rgba

```csharp
public uint Rgba { get; init; }
```

Colour as `0xRRGGBBAA`.

**Type:** `uint`

### <a id="m-size"></a>Size

```csharp
public Vector2 Size { get; init; }
```

Width and height.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)
