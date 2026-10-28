[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# Replayer static class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public static class Replayer
```

Re-runs a [`Recording`](Recording.md) and checks the hashes.

## Methods

| Member | Description |
| --- | --- |
| [`Replay(Recording, World, ReplayOptions?)`](#m-replay-recording-world-replayoptions) | Loads the start snapshot into `world`, then submits the logged commands before the tick they ran in and steps, comparing the world hash at every checkpoint. |

### <a id="m-replay-recording-world-replayoptions"></a>Replay(Recording, World, ReplayOptions?)

```csharp
public static ReplayResult Replay(Recording recording, World world, ReplayOptions? options = null);
```

Loads the start snapshot into `world`, then submits the logged commands before the tick they ran in and steps, comparing the world hash at every checkpoint. Stops at the first mismatch.

**Parameters**

- `recording` ([`Recording`](Recording.md)): The recording.
- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): A fresh authority world built like the recorded one: same registry, tables and systems, added in the same order.
- `options` ([`ReplayOptions`](ReplayOptions.md)?): Optional hooks and the tick limit.

**Returns:** [`ReplayResult`](ReplayResult.md)
