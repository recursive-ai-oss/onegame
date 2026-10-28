[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# PhysicsEvent struct

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)

```csharp
public readonly struct PhysicsEvent
```

One event of the last step, resolved to game types.

## Properties

| Member | Description |
| --- | --- |
| [`A`](#m-a) | The first body (for a trigger event, the trigger; for sleep and wake, the body). |
| [`B`](#m-b) | The second body; null for sleep and wake. |
| [`Kind`](#m-kind) | What happened. |
| [`Normal`](#m-normal) | The unit contact normal pointing from [`PhysicsEvent.A`](PhysicsEvent.md#m-a) towards [`PhysicsEvent.B`](PhysicsEvent.md#m-b), for the same kinds; zero otherwise. |
| [`Point`](#m-point) | A point of contact for [`PhysicsEventKind.ContactBegin`](PhysicsEventKind.md#m-contactbegin), [`PhysicsEventKind.ContactStay`](PhysicsEventKind.md#m-contactstay) and [`PhysicsEventKind.ContactHit`](PhysicsEventKind.md#m-contacthit); zero otherwise. |
| [`Speed`](#m-speed) | The approach speed of a [`PhysicsEventKind.ContactHit`](PhysicsEventKind.md#m-contacthit); zero otherwise. |

### <a id="m-a"></a>A

```csharp
public BodyRef A { get; }
```

The first body (for a trigger event, the trigger; for sleep and wake, the body).

**Type:** [`BodyRef`](BodyRef.md)

### <a id="m-b"></a>B

```csharp
public BodyRef B { get; }
```

The second body; null for sleep and wake.

**Type:** [`BodyRef`](BodyRef.md)

### <a id="m-kind"></a>Kind

```csharp
public PhysicsEventKind Kind { get; }
```

What happened.

**Type:** [`PhysicsEventKind`](PhysicsEventKind.md)

### <a id="m-normal"></a>Normal

```csharp
public FVec2 Normal { get; }
```

The unit contact normal pointing from [`PhysicsEvent.A`](PhysicsEvent.md#m-a) towards [`PhysicsEvent.B`](PhysicsEvent.md#m-b), for the same kinds; zero otherwise.

**Type:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)

### <a id="m-point"></a>Point

```csharp
public FVec2 Point { get; }
```

A point of contact for [`PhysicsEventKind.ContactBegin`](PhysicsEventKind.md#m-contactbegin), [`PhysicsEventKind.ContactStay`](PhysicsEventKind.md#m-contactstay) and [`PhysicsEventKind.ContactHit`](PhysicsEventKind.md#m-contacthit); zero otherwise.

**Type:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)

### <a id="m-speed"></a>Speed

```csharp
public Fixed Speed { get; }
```

The approach speed of a [`PhysicsEventKind.ContactHit`](PhysicsEventKind.md#m-contacthit); zero otherwise.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

## Methods

| Member | Description |
| --- | --- |
| [`Involves(BodyRef)`](#m-involves-bodyref) | True when `body` is [`PhysicsEvent.A`](PhysicsEvent.md#m-a) or [`PhysicsEvent.B`](PhysicsEvent.md#m-b). |
| [`Other(BodyRef)`](#m-other-bodyref) | The other body of the pair, or the null reference when `body` is not part of it. |
| [`ToString()`](#m-tostring) |  |

### <a id="m-involves-bodyref"></a>Involves(BodyRef)

```csharp
public bool Involves(BodyRef body);
```

True when `body` is [`PhysicsEvent.A`](PhysicsEvent.md#m-a) or [`PhysicsEvent.B`](PhysicsEvent.md#m-b).

**Parameters**

- `body` ([`BodyRef`](BodyRef.md)): The body to look for.

**Returns:** `bool`

### <a id="m-other-bodyref"></a>Other(BodyRef)

```csharp
public BodyRef Other(BodyRef body);
```

The other body of the pair, or the null reference when `body` is not part of it.

**Parameters**

- `body` ([`BodyRef`](BodyRef.md)): One of the two bodies.

**Returns:** [`BodyRef`](BodyRef.md)

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

Overrides [`ValueType.ToString()`](https://learn.microsoft.com/dotnet/api/system.valuetype.tostring).

**Returns:** `string`
