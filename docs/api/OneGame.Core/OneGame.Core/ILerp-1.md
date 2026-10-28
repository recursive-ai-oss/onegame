[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# ILerp&lt;TSelf&gt; interface

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public interface ILerp<TSelf> where TSelf : ILerp<TSelf>
```

A value that can be blended between two of its kind; [`Timed<T>`](Timed-1.md) uses it to interpolate.

**Type parameters**

- `TSelf`: The implementing type.

## Methods

| Member | Description |
| --- | --- |
| [`Lerp(TSelf, TSelf, Fixed)`](#m-lerp-tself-tself-fixed) | Blends from `a` (at 0) to `b` (at 1). |

### <a id="m-lerp-tself-tself-fixed"></a>Lerp(TSelf, TSelf, Fixed)

```csharp
static abstract TSelf Lerp(TSelf a, TSelf b, Fixed t);
```

Blends from `a` (at 0) to `b` (at 1).

**Parameters**

- `a` (`TSelf`)
- `b` (`TSelf`)
- `t` ([`Fixed`](Fixed.md))

**Returns:** `TSelf`
