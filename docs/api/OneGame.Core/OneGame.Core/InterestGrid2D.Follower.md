[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# InterestGrid2D.Follower class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Declared in:** [`InterestGrid2D`](InterestGrid2D.md)

```csharp
public sealed class InterestGrid2D.Follower
```

Tracks the cells one connection should receive. Call [`InterestGrid2D.Follower.Update()`](InterestGrid2D.Follower.md#m-update-int-int) as the player moves and pass [`InterestGrid2D.Follower.Keys`](InterestGrid2D.Follower.md#m-keys) to `SetInterest`.

## Properties

| Member | Description |
| --- | --- |
| [`Keys`](#m-keys) | The cells to receive now. |

### <a id="m-keys"></a>Keys

```csharp
public ReadOnlySpan<uint> Keys { get; }
```

The cells to receive now. Valid until the next update.

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<uint>`

## Methods

| Member | Description |
| --- | --- |
| [`Update(Fixed, Fixed)`](#m-update-fixed-fixed) | As [`InterestGrid2D.Follower.Update()`](InterestGrid2D.Follower.md#m-update-int-int) with fixed-point positions. |
| [`Update(int, int)`](#m-update-int-int) | Moves the cursor to a position in blocks: cells within the enter radius are added, cells beyond the leave radius dropped. |

### <a id="m-update-fixed-fixed"></a>Update(Fixed, Fixed)

```csharp
public bool Update(Fixed x, Fixed y);
```

As [`InterestGrid2D.Follower.Update()`](InterestGrid2D.Follower.md#m-update-int-int) with fixed-point positions.

**Parameters**

- `x` ([`Fixed`](Fixed.md)): Position.
- `y` ([`Fixed`](Fixed.md)): Position.

**Returns:** `bool`

### <a id="m-update-int-int"></a>Update(int, int)

```csharp
public bool Update(int x, int y);
```

Moves the cursor to a position in blocks: cells within the enter radius are added, cells beyond the leave radius dropped.

**Parameters**

- `x` (`int`): Position in blocks.
- `y` (`int`): Position in blocks.

**Returns:** `bool` — True when the set of cells changed.
