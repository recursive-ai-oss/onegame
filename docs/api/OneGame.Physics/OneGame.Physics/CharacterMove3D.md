[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# CharacterMove3D struct

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`CharacterMove3D`](CharacterMove3D.md)&gt;

```csharp
public readonly struct CharacterMove3D : IEquatable<CharacterMove3D>
```

Result of a 3D character displacement.

## Constructors

| Member | Description |
| --- | --- |
| [`CharacterMove3D(FVec3, bool, bool, bool)`](#m-ctor-fvec3-bool-bool-bool) | Result of a 3D character displacement. |

### <a id="m-ctor-fvec3-bool-bool-bool"></a>CharacterMove3D(FVec3, bool, bool, bool)

```csharp
public CharacterMove3D(FVec3 Position, bool Grounded, bool HitCeiling, bool HitSide);
```

Result of a 3D character displacement.

**Parameters**

- `Position` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md)): Local position after the move.
- `Grounded` (`bool`): Touches walkable ground.
- `HitCeiling` (`bool`): Upward movement was blocked.
- `HitSide` (`bool`): Sideways movement was blocked.

## Properties

| Member | Description |
| --- | --- |
| [`Grounded`](#m-grounded) | Touches walkable ground. |
| [`HitCeiling`](#m-hitceiling) | Upward movement was blocked. |
| [`HitSide`](#m-hitside) | Sideways movement was blocked. |
| [`Position`](#m-position) | Local position after the move. |

### <a id="m-grounded"></a>Grounded

```csharp
public bool Grounded { get; init; }
```

Touches walkable ground.

**Type:** `bool`

### <a id="m-hitceiling"></a>HitCeiling

```csharp
public bool HitCeiling { get; init; }
```

Upward movement was blocked.

**Type:** `bool`

### <a id="m-hitside"></a>HitSide

```csharp
public bool HitSide { get; init; }
```

Sideways movement was blocked.

**Type:** `bool`

### <a id="m-position"></a>Position

```csharp
public FVec3 Position { get; init; }
```

Local position after the move.

**Type:** [`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md)
