[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# Crossfade struct

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public struct Crossfade
```

A crossfade clock: how much of the new pose shows, over a duration.

## Constructors

| Member | Description |
| --- | --- |
| [`Crossfade(float)`](#m-ctor-float) | Starts a fade. |

### <a id="m-ctor-float"></a>Crossfade(float)

```csharp
public Crossfade(float seconds);
```

Starts a fade.

**Parameters**

- `seconds` (`float`): How long it lasts; zero or less is finished at once.

## Properties

| Member | Description |
| --- | --- |
| [`Done`](#m-done) | True once the fade reached its end. |
| [`Weight`](#m-weight) | The share of the new pose, 0 at the start and 1 at the end. |

### <a id="m-done"></a>Done

```csharp
public bool Done { get; }
```

True once the fade reached its end.

**Type:** `bool`

### <a id="m-weight"></a>Weight

```csharp
public float Weight { get; }
```

The share of the new pose, 0 at the start and 1 at the end. Linear in time.

**Type:** `float`

## Methods

| Member | Description |
| --- | --- |
| [`Advance(float)`](#m-advance-float) | Moves the clock on. |

### <a id="m-advance-float"></a>Advance(float)

```csharp
public void Advance(float dt);
```

Moves the clock on.

**Parameters**

- `dt` (`float`): Seconds, not negative.
