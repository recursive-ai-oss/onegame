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

- `Scale` (`float`)
- `Override` (`float?`)
- `WindowScale` (`float`)

## Properties

| Member | Description |
| --- | --- |
| [`Override`](#m-override) |  |
| [`Scale`](#m-scale) |  |
| [`WindowScale`](#m-windowscale) |  |

### <a id="m-override"></a>Override

```csharp
public float? Override { get; init; }
```

**Type:** `float?`

### <a id="m-scale"></a>Scale

```csharp
public float Scale { get; init; }
```

**Type:** `float`

### <a id="m-windowscale"></a>WindowScale

```csharp
public float WindowScale { get; init; }
```

**Type:** `float`
