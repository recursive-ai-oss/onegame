[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# CharacterMove struct

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`CharacterMove`](CharacterMove.md)&gt;

```csharp
public readonly struct CharacterMove : IEquatable<CharacterMove>
```

The result of [`PhysicsSpace2D.Move()`](PhysicsSpace2D.md#m-move-characterref-fvec2).

## Constructors

| Member | Description |
| --- | --- |
| [`CharacterMove(bool, bool, bool, FVec2)`](#m-ctor-bool-bool-bool-fvec2) | The result of [`PhysicsSpace2D.Move()`](PhysicsSpace2D.md#m-move-characterref-fvec2). |

### <a id="m-ctor-bool-bool-bool-fvec2"></a>CharacterMove(bool, bool, bool, FVec2)

```csharp
public CharacterMove(bool Grounded, bool HitCeiling, bool HitSide, FVec2 Position);
```

The result of [`PhysicsSpace2D.Move()`](PhysicsSpace2D.md#m-move-characterref-fvec2).

**Parameters**

- `Grounded` (`bool`): The character ended the move standing on walkable ground.
- `HitCeiling` (`bool`): It touched something above it.
- `HitSide` (`bool`): It touched something to its side.
- `Position` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): Where the capsule centre ended.

## Properties

| Member | Description |
| --- | --- |
| [`Grounded`](#m-grounded) | The character ended the move standing on walkable ground. |
| [`HitCeiling`](#m-hitceiling) | It touched something above it. |
| [`HitSide`](#m-hitside) | It touched something to its side. |
| [`Position`](#m-position) | Where the capsule centre ended. |

### <a id="m-grounded"></a>Grounded

```csharp
public bool Grounded { get; init; }
```

The character ended the move standing on walkable ground.

**Type:** `bool`

### <a id="m-hitceiling"></a>HitCeiling

```csharp
public bool HitCeiling { get; init; }
```

It touched something above it.

**Type:** `bool`

### <a id="m-hitside"></a>HitSide

```csharp
public bool HitSide { get; init; }
```

It touched something to its side.

**Type:** `bool`

### <a id="m-position"></a>Position

```csharp
public FVec2 Position { get; init; }
```

Where the capsule centre ended.

**Type:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)
