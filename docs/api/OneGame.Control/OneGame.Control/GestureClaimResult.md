[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# GestureClaimResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`GestureClaimResult`](GestureClaimResult.md)&gt;

```csharp
public sealed record GestureClaimResult : IEquatable<GestureClaimResult>
```

Result of `system_gestures`: what the game currently claims from the system's gestures.

## Constructors

| Member | Description |
| --- | --- |
| [`GestureClaimResult(string[], string[], int)`](#m-ctor-string-string-int) | Result of `system_gestures`: what the game currently claims from the system's gestures. |

### <a id="m-ctor-string-string-int"></a>GestureClaimResult(string\[\], string\[\], int)

```csharp
public GestureClaimResult(string[] Regions, string[] Edges, int Limit);
```

Result of `system_gestures`: what the game currently claims from the system's gestures.

**Parameters**

- `Regions` (`string[]`): The claimed rectangles as `x,y,w,h` in window pixels.
- `Edges` (`string[]`): The screen edges the claim touches (`left`, `top`, `right`, `bottom`), empty when none.
- `Limit` (`int`): The most height in pixels the system honours along one edge; 0 where there is no limit.

## Properties

| Member | Description |
| --- | --- |
| [`Edges`](#m-edges) | The screen edges the claim touches (`left`, `top`, `right`, `bottom`), empty when none. |
| [`Limit`](#m-limit) | The most height in pixels the system honours along one edge; 0 where there is no limit. |
| [`Regions`](#m-regions) | The claimed rectangles as `x,y,w,h` in window pixels. |

### <a id="m-edges"></a>Edges

```csharp
public string[] Edges { get; init; }
```

The screen edges the claim touches (`left`, `top`, `right`, `bottom`), empty when none.

**Type:** `string[]`

### <a id="m-limit"></a>Limit

```csharp
public int Limit { get; init; }
```

The most height in pixels the system honours along one edge; 0 where there is no limit.

**Type:** `int`

### <a id="m-regions"></a>Regions

```csharp
public string[] Regions { get; init; }
```

The claimed rectangles as `x,y,w,h` in window pixels.

**Type:** `string[]`
