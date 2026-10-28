[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# UiScaleResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`UiScaleResult`](UiScaleResult.md)&gt;

```csharp
public sealed record UiScaleResult : IEquatable<UiScaleResult>
```

Result of `ui_scale`: the scale in effect, the override (null when following the window) and the window's own scale.

## Constructors

| Member | Description |
| --- | --- |
| [`UiScaleResult(float, float?, float)`](#m-ctor-float-float-float) | Result of `ui_scale`: the scale in effect, the override (null when following the window) and the window's own scale. |

### <a id="m-ctor-float-float-float"></a>UiScaleResult(float, float?, float)

```csharp
public UiScaleResult(float Scale, float? Override, float WindowScale);
```

Result of `ui_scale`: the scale in effect, the override (null when following the window) and the window's own scale.

**Parameters**

- `Scale` (`float`): The UI scale in effect.
- `Override` (`float?`): The scale set by this operation, or null when the UI follows the window's scale.
- `WindowScale` (`float`): The window's own scale factor (HiDPI).

## Properties

| Member | Description |
| --- | --- |
| [`Override`](#m-override) | The scale set by this operation, or null when the UI follows the window's scale. |
| [`Scale`](#m-scale) | The UI scale in effect. |
| [`WindowScale`](#m-windowscale) | The window's own scale factor (HiDPI). |

### <a id="m-override"></a>Override

```csharp
public float? Override { get; init; }
```

The scale set by this operation, or null when the UI follows the window's scale.

**Type:** `float?`

### <a id="m-scale"></a>Scale

```csharp
public float Scale { get; init; }
```

The UI scale in effect.

**Type:** `float`

### <a id="m-windowscale"></a>WindowScale

```csharp
public float WindowScale { get; init; }
```

The window's own scale factor (HiDPI).

**Type:** `float`
