[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# AnimTransition class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class AnimTransition
```

A rule for leaving one state for another. Build it with the `When`/`AtExit`/`Fade` calls, which return it.

## Properties

| Member | Description |
| --- | --- |
| [`ExitTime`](#m-exittime) | The progress the source state must have reached, or a negative number for none. |
| [`FadeSeconds`](#m-fadeseconds) | The crossfade length in seconds. |
| [`From`](#m-from) | The state it leaves, or null for a transition that applies from every state. |
| [`To`](#m-to) | The state it enters. |

### <a id="m-exittime"></a>ExitTime

```csharp
public float ExitTime { get; }
```

The progress the source state must have reached, or a negative number for none.

**Type:** `float`

### <a id="m-fadeseconds"></a>FadeSeconds

```csharp
public float FadeSeconds { get; }
```

The crossfade length in seconds.

**Type:** `float`

### <a id="m-from"></a>From

```csharp
public AnimState? From { get; }
```

The state it leaves, or null for a transition that applies from every state.

**Type:** [`AnimState`](AnimState.md)?

### <a id="m-to"></a>To

```csharp
public AnimState To { get; }
```

The state it enters.

**Type:** [`AnimState`](AnimState.md)

## Methods

| Member | Description |
| --- | --- |
| [`AtExit(float)`](#m-atexit-float) | Waits until the source state has played this much (1 = one full pass). |
| [`Fade(float)`](#m-fade-float) | Sets the crossfade length. |
| [`WhenBool(string, bool)`](#m-whenbool-string-bool) | Also requires a bool parameter to have a value. |
| [`WhenFloat(string, Compare, float)`](#m-whenfloat-string-compare-float) | Also requires a float parameter to compare true. |
| [`WhenTrigger(string)`](#m-whentrigger-string) | Also requires a trigger to be set. |

### <a id="m-atexit-float"></a>AtExit(float)

```csharp
public AnimTransition AtExit(float progress = 1f);
```

Waits until the source state has played this much (1 = one full pass). With no other condition the transition fires then.

**Parameters**

- `progress` (`float`)

**Returns:** [`AnimTransition`](AnimTransition.md)

### <a id="m-fade-float"></a>Fade(float)

```csharp
public AnimTransition Fade(float seconds);
```

Sets the crossfade length.

**Parameters**

- `seconds` (`float`)

**Returns:** [`AnimTransition`](AnimTransition.md)

### <a id="m-whenbool-string-bool"></a>WhenBool(string, bool)

```csharp
public AnimTransition WhenBool(string parameter, bool value);
```

Also requires a bool parameter to have a value.

**Parameters**

- `parameter` (`string`)
- `value` (`bool`)

**Returns:** [`AnimTransition`](AnimTransition.md)

### <a id="m-whenfloat-string-compare-float"></a>WhenFloat(string, Compare, float)

```csharp
public AnimTransition WhenFloat(string parameter, Compare compare, float value);
```

Also requires a float parameter to compare true.

**Parameters**

- `parameter` (`string`)
- `compare` ([`Compare`](Compare.md))
- `value` (`float`)

**Returns:** [`AnimTransition`](AnimTransition.md)

### <a id="m-whentrigger-string"></a>WhenTrigger(string)

```csharp
public AnimTransition WhenTrigger(string trigger);
```

Also requires a trigger to be set. The transition that fires clears it, so one trigger starts one transition.

**Parameters**

- `trigger` (`string`)

**Returns:** [`AnimTransition`](AnimTransition.md)
