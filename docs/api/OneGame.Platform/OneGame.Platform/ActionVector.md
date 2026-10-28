[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# ActionVector struct

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ActionVector`](ActionVector.md)&gt;

```csharp
public readonly struct ActionVector : IEquatable<ActionVector>
```

A 2D action value.

## Constructors

| Member | Description |
| --- | --- |
| [`ActionVector(float, float)`](#m-ctor-float-float) | A 2D action value. |

### <a id="m-ctor-float-float"></a>ActionVector(float, float)

```csharp
public ActionVector(float X, float Y);
```

A 2D action value.

**Parameters**

- `X` (`float`): Right is positive.
- `Y` (`float`): Down is positive.

## Properties

| Member | Description |
| --- | --- |
| [`Length`](#m-length) | The vector's length. |
| [`X`](#m-x) | Right is positive. |
| [`Y`](#m-y) | Down is positive. |

### <a id="m-length"></a>Length

```csharp
public float Length { get; }
```

The vector's length.

**Type:** `float`

### <a id="m-x"></a>X

```csharp
public float X { get; init; }
```

Right is positive.

**Type:** `float`

### <a id="m-y"></a>Y

```csharp
public float Y { get; init; }
```

Down is positive.

**Type:** `float`
