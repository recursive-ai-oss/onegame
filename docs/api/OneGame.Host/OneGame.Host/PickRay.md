[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# PickRay struct

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PickRay`](PickRay.md)&gt;

```csharp
public readonly struct PickRay : IEquatable<PickRay>
```

A ray in world space.

## Constructors

| Member | Description |
| --- | --- |
| [`PickRay(Vector3, Vector3)`](#m-ctor-vector3-vector3) | A ray in world space. |

### <a id="m-ctor-vector3-vector3"></a>PickRay(Vector3, Vector3)

```csharp
public PickRay(Vector3 Origin, Vector3 Direction);
```

A ray in world space.

**Parameters**

- `Origin` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Where it starts.
- `Direction` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Its direction, of length one.

## Properties

| Member | Description |
| --- | --- |
| [`Direction`](#m-direction) | Its direction, of length one. |
| [`Origin`](#m-origin) | Where it starts. |

### <a id="m-direction"></a>Direction

```csharp
public Vector3 Direction { get; init; }
```

Its direction, of length one.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-origin"></a>Origin

```csharp
public Vector3 Origin { get; init; }
```

Where it starts.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)
