[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# SpriteClip class

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)

```csharp
public sealed class SpriteClip
```

A named run of frames of a [`SpriteSheet`](SpriteSheet.md) played at a rate. Pure data: it picks a frame for a time and never holds state.

## Properties

| Member | Description |
| --- | --- |
| [`Duration`](#m-duration) | How long one pass takes, in seconds. |
| [`Fps`](#m-fps) | Frames per second. |
| [`Length`](#m-length) | How many frames the clip shows before it repeats or ends. |
| [`Loop`](#m-loop) | True to start over after the last frame; false to hold it. |
| [`Name`](#m-name) | The clip's name. |

### <a id="m-duration"></a>Duration

```csharp
public float Duration { get; }
```

How long one pass takes, in seconds.

**Type:** `float`

### <a id="m-fps"></a>Fps

```csharp
public float Fps { get; }
```

Frames per second.

**Type:** `float`

### <a id="m-length"></a>Length

```csharp
public int Length { get; }
```

How many frames the clip shows before it repeats or ends.

**Type:** `int`

### <a id="m-loop"></a>Loop

```csharp
public bool Loop { get; }
```

True to start over after the last frame; false to hold it.

**Type:** `bool`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The clip's name.

**Type:** `string`

## Methods

| Member | Description |
| --- | --- |
| [`FrameAt(float)`](#m-frameat-float) | Which sheet frame the clip shows `seconds` after it started. |
| [`IsDone(float)`](#m-isdone-float) | True when a non-looping clip has played through at `seconds`. |

### <a id="m-frameat-float"></a>FrameAt(float)

```csharp
public int FrameAt(float seconds);
```

Which sheet frame the clip shows `seconds` after it started. Negative times show the first frame.

**Parameters**

- `seconds` (`float`): Time since the clip started, from the view's clock.

**Returns:** `int` — An index into the sheet's frames.

### <a id="m-isdone-float"></a>IsDone(float)

```csharp
public bool IsDone(float seconds);
```

True when a non-looping clip has played through at `seconds`.

**Parameters**

- `seconds` (`float`): Time since the clip started.

**Returns:** `bool` — Whether it is finished.
