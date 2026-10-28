[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# RngStreams class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public sealed class RngStreams
```

The world's random streams, found by name. Each stream is seeded from the world seed and its name, so adding a stream never disturbs the others. Streams are kept in name order, so snapshots and hashes do not depend on creation order.

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | The number of streams created so far. |
| [`this[string]`](#m-item-string) | Returns the stream with this name, creating it on first use. |

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

The number of streams created so far.

**Type:** `int`

### <a id="m-item-string"></a>this\[string\]

```csharp
public Rng this[string name] { get; }
```

Returns the stream with this name, creating it on first use.

**Parameters**

- `name` (`string`)

**Type:** [`Rng`](Rng.md)
