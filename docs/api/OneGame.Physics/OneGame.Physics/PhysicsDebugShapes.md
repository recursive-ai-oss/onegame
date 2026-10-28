[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# PhysicsDebugShapes class

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)

```csharp
public sealed class PhysicsDebugShapes
```

Debug geometry for a [`PhysicsSpace2D`](PhysicsSpace2D.md): every shape's outline (colour tells static, kinematic, awake, asleep and trigger apart), contact points with their normals, as plain lines in world units. It is view-side data (floats, never state): collect it in a view while the debug overlay is on and draw the lines with the 2D calls.

```csharp
if (d.DebugVisible)
{
    _shapes.Clear();
    space.AddDebugShapes(_shapes);
    foreach (var l in _shapes.Lines) { d.Line(l.From, l.To, Color.Rgba(l.Rgba), l.Width, 9); }
}
```

Only the authority holds a space, so replicas have nothing to collect.

## Constructors

| Member | Description |
| --- | --- |
| [`PhysicsDebugShapes()`](#m-ctor) |  |

### <a id="m-ctor"></a>PhysicsDebugShapes()

```csharp
public PhysicsDebugShapes();
```

## Fields

| Member | Description |
| --- | --- |
| [`AsleepColor`](#m-asleepcolor) | Colour of sleeping dynamic shapes. |
| [`AwakeColor`](#m-awakecolor) | Colour of awake dynamic shapes. |
| [`ContactColor`](#m-contactcolor) | Colour of contact points and normals. |
| [`KinematicColor`](#m-kinematiccolor) | Colour of kinematic shapes (characters, moving platforms). |
| [`StaticColor`](#m-staticcolor) | Colour of static shapes. |
| [`TriggerColor`](#m-triggercolor) | Colour of triggers. |

### <a id="m-asleepcolor"></a>AsleepColor

```csharp
public const uint AsleepColor = 1483757823;
```

Colour of sleeping dynamic shapes.

**Type:** `uint`

### <a id="m-awakecolor"></a>AwakeColor

```csharp
public const uint AwakeColor = 2632474879;
```

Colour of awake dynamic shapes.

**Type:** `uint`

### <a id="m-contactcolor"></a>ContactColor

```csharp
public const uint ContactColor = 4283453695;
```

Colour of contact points and normals.

**Type:** `uint`

### <a id="m-kinematiccolor"></a>KinematicColor

```csharp
public const uint KinematicColor = 1621160191;
```

Colour of kinematic shapes (characters, moving platforms).

**Type:** `uint`

### <a id="m-staticcolor"></a>StaticColor

```csharp
public const uint StaticColor = 2359342335;
```

Colour of static shapes.

**Type:** `uint`

### <a id="m-triggercolor"></a>TriggerColor

```csharp
public const uint TriggerColor = 820027647;
```

Colour of triggers.

**Type:** `uint`

## Properties

| Member | Description |
| --- | --- |
| [`Lines`](#m-lines) | The lines collected so far. |

### <a id="m-lines"></a>Lines

```csharp
public IReadOnlyList<PhysicsDebugLine> Lines { get; }
```

The lines collected so far.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`PhysicsDebugLine`](PhysicsDebugLine.md)&gt;

## Methods

| Member | Description |
| --- | --- |
| [`Clear()`](#m-clear) | Forgets everything collected. |

### <a id="m-clear"></a>Clear()

```csharp
public void Clear();
```

Forgets everything collected.
