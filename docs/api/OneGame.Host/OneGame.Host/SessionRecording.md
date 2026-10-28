[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# SessionRecording class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public sealed class SessionRecording
```

A whole session: one [`Recording`](../../OneGame.Sync/OneGame.Sync/Recording.md) per world the game ran on. A game that starts a new campaign ([`GameInstance.Reset()`](GameInstance.md#m-reset-ulong)), loads a fixture or loads a save replaces its world; each replacement ends one segment and starts the next from the new world's full state (seed included), so replay reproduces resets exactly. A state patch from the harness ends a segment the same way. A session without replacements or patches has one segment.

## Constructors

| Member | Description |
| --- | --- |
| [`SessionRecording(IReadOnlyList<Recording>)`](#m-ctor-ireadonlylist-recording) | Creates a session from its segments, in the order they ran. |

### <a id="m-ctor-ireadonlylist-recording"></a>SessionRecording(IReadOnlyList&lt;Recording&gt;)

```csharp
public SessionRecording(IReadOnlyList<Recording> segments);
```

Creates a session from its segments, in the order they ran.

**Parameters**

- `segments` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`Recording`](../../OneGame.Sync/OneGame.Sync/Recording.md)&gt;)

## Properties

| Member | Description |
| --- | --- |
| [`Commands`](#m-commands) | Logged commands over all segments. |
| [`Segments`](#m-segments) | The recordings, in the order the worlds ran. |
| [`Ticks`](#m-ticks) | Simulation ticks over all segments. |

### <a id="m-commands"></a>Commands

```csharp
public int Commands { get; }
```

Logged commands over all segments.

**Type:** `int`

### <a id="m-segments"></a>Segments

```csharp
public IReadOnlyList<Recording> Segments { get; }
```

The recordings, in the order the worlds ran. Each starts from its own snapshot.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`Recording`](../../OneGame.Sync/OneGame.Sync/Recording.md)&gt;

### <a id="m-ticks"></a>Ticks

```csharp
public long Ticks { get; }
```

Simulation ticks over all segments.

**Type:** `long`
