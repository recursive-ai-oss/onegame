[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# PointerEdge struct

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PointerEdge`](PointerEdge.md)&gt;

```csharp
public readonly struct PointerEdge : IEquatable<PointerEdge>
```

One pointer event in the order it happened, with the position it happened at. Reading [`InputState.PointerX`](InputState.md#m-pointerx) at frame time gives where the pointer ended up, which is not where a press or lift happened when the pointer moved within the same frame; the edges are.

## Constructors

| Member | Description |
| --- | --- |
| [`PointerEdge(PointerEdgeKind, int, MouseButton, float, float, bool)`](#m-ctor-pointeredgekind-int-mousebutton-float-float-bool) | One pointer event in the order it happened, with the position it happened at. |

### <a id="m-ctor-pointeredgekind-int-mousebutton-float-float-bool"></a>PointerEdge(PointerEdgeKind, int, MouseButton, float, float, bool)

```csharp
public PointerEdge(PointerEdgeKind Kind, int TouchId, MouseButton Button, float X, float Y, bool FromTouch);
```

One pointer event in the order it happened, with the position it happened at. Reading [`InputState.PointerX`](InputState.md#m-pointerx) at frame time gives where the pointer ended up, which is not where a press or lift happened when the pointer moved within the same frame; the edges are.

**Parameters**

- `Kind` ([`PointerEdgeKind`](PointerEdgeKind.md)): What happened.
- `TouchId` (`int`): The finger, or -1 for the mouse or pen.
- `Button` ([`MouseButton`](MouseButton.md)): The button for mouse edges of kind Down and Up; Left for fingers.
- `X` (`float`): Window x in pixels, with the fraction the system reported.
- `Y` (`float`): Window y in pixels, with the fraction the system reported.
- `FromTouch` (`bool`): True for a mouse edge made from the first finger ([`PlatformOptions.TouchEmulatesPointer`](PlatformOptions.md#m-touchemulatespointer)): the finger's own edge is listed too, so a game that reads both should skip these.

## Properties

| Member | Description |
| --- | --- |
| [`Button`](#m-button) | The button for mouse edges of kind Down and Up; Left for fingers. |
| [`FromTouch`](#m-fromtouch) | True for a mouse edge made from the first finger ([`PlatformOptions.TouchEmulatesPointer`](PlatformOptions.md#m-touchemulatespointer)): the finger's own edge is listed too, so a game that reads both should skip these. |
| [`Kind`](#m-kind) | What happened. |
| [`TouchId`](#m-touchid) | The finger, or -1 for the mouse or pen. |
| [`X`](#m-x) | Window x in pixels, with the fraction the system reported. |
| [`Y`](#m-y) | Window y in pixels, with the fraction the system reported. |

### <a id="m-button"></a>Button

```csharp
public MouseButton Button { get; init; }
```

The button for mouse edges of kind Down and Up; Left for fingers.

**Type:** [`MouseButton`](MouseButton.md)

### <a id="m-fromtouch"></a>FromTouch

```csharp
public bool FromTouch { get; init; }
```

True for a mouse edge made from the first finger ([`PlatformOptions.TouchEmulatesPointer`](PlatformOptions.md#m-touchemulatespointer)): the finger's own edge is listed too, so a game that reads both should skip these.

**Type:** `bool`

### <a id="m-kind"></a>Kind

```csharp
public PointerEdgeKind Kind { get; init; }
```

What happened.

**Type:** [`PointerEdgeKind`](PointerEdgeKind.md)

### <a id="m-touchid"></a>TouchId

```csharp
public int TouchId { get; init; }
```

The finger, or -1 for the mouse or pen.

**Type:** `int`

### <a id="m-x"></a>X

```csharp
public float X { get; init; }
```

Window x in pixels, with the fraction the system reported.

**Type:** `float`

### <a id="m-y"></a>Y

```csharp
public float Y { get; init; }
```

Window y in pixels, with the fraction the system reported.

**Type:** `float`
