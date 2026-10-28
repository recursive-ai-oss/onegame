[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.Tweens

# ITweenTarget interface

**Namespace:** `OneGame.Tweens`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public interface ITweenTarget
```

Something a tween drives that is not a plain value: it is told the eased progress each time the tween moves.

## Properties

| Member | Description |
| --- | --- |
| [`Label`](#m-label) | A short description for the harness's tween listing, or null. |

### <a id="m-label"></a>Label

```csharp
string? Label { get; }
```

A short description for the harness's tween listing, or null.

**Type:** `string?`

## Methods

| Member | Description |
| --- | --- |
| [`Apply(float)`](#m-apply-float) | Called when the tween's eased progress changes: 0 at the start and 1 at the end, outside that range for easings that overshoot. |

### <a id="m-apply-float"></a>Apply(float)

```csharp
void Apply(float progress);
```

Called when the tween's eased progress changes: 0 at the start and 1 at the end, outside that range for easings that overshoot.

**Parameters**

- `progress` (`float`): The eased progress.
