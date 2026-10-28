[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# ProjectResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ProjectResult`](ProjectResult.md)&gt;

```csharp
public sealed record ProjectResult : IEquatable<ProjectResult>
```

Result of `project`: where a world point lands on the screen.

## Constructors

| Member | Description |
| --- | --- |
| [`ProjectResult(bool, bool, double, double, double, int, int)`](#m-ctor-bool-bool-double-double-double-int-int) | Result of `project`: where a world point lands on the screen. |

### <a id="m-ctor-bool-bool-double-double-double-int-int"></a>ProjectResult(bool, bool, double, double, double, int, int)

```csharp
public ProjectResult(bool OnScreen, bool InFront, double X, double Y, double Depth, int Width, int Height);
```

Result of `project`: where a world point lands on the screen.

**Parameters**

- `OnScreen` (`bool`): True when the point is in front of the camera, between its planes, and inside the target.
- `InFront` (`bool`): True when the point is between the near and far planes (it may still be off to the side).
- `X` (`double`): Pixels from the left.
- `Y` (`double`): Pixels from the top.
- `Depth` (`double`): Distance in front of the camera.
- `Width` (`int`): Target width used.
- `Height` (`int`): Target height used.

## Properties

| Member | Description |
| --- | --- |
| [`Depth`](#m-depth) | Distance in front of the camera. |
| [`Height`](#m-height) | Target height used. |
| [`InFront`](#m-infront) | True when the point is between the near and far planes (it may still be off to the side). |
| [`OnScreen`](#m-onscreen) | True when the point is in front of the camera, between its planes, and inside the target. |
| [`Width`](#m-width) | Target width used. |
| [`X`](#m-x) | Pixels from the left. |
| [`Y`](#m-y) | Pixels from the top. |

### <a id="m-depth"></a>Depth

```csharp
public double Depth { get; init; }
```

Distance in front of the camera.

**Type:** `double`

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Target height used.

**Type:** `int`

### <a id="m-infront"></a>InFront

```csharp
public bool InFront { get; init; }
```

True when the point is between the near and far planes (it may still be off to the side).

**Type:** `bool`

### <a id="m-onscreen"></a>OnScreen

```csharp
public bool OnScreen { get; init; }
```

True when the point is in front of the camera, between its planes, and inside the target.

**Type:** `bool`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Target width used.

**Type:** `int`

### <a id="m-x"></a>X

```csharp
public double X { get; init; }
```

Pixels from the left.

**Type:** `double`

### <a id="m-y"></a>Y

```csharp
public double Y { get; init; }
```

Pixels from the top.

**Type:** `double`
