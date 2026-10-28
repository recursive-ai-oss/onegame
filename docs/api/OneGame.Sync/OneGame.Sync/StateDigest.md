[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# StateDigest class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class StateDigest
```

A per-part hash list of a world at one tick: the random streams, each table, each entity. Two digests can be diffed to name exactly what drifted. Capturing reuses the digest's buffers.

## Constructors

| Member | Description |
| --- | --- |
| [`StateDigest()`](#m-ctor) |  |

### <a id="m-ctor"></a>StateDigest()

```csharp
public StateDigest();
```

## Properties

| Member | Description |
| --- | --- |
| [`Entries`](#m-entries) | The entries: random streams, tables in world order, entities in id order. |
| [`Tick`](#m-tick) | The completed tick count the digest describes. |
| [`Truncated`](#m-truncated) | True when the world had more entities than the limit and the entries were left out. |
| [`WorldHash`](#m-worldhash) | The whole-world hash at that tick ([`World.ComputeHash()`](../../OneGame.Core/OneGame.Core/World.md#m-computehash)). |

### <a id="m-entries"></a>Entries

```csharp
public IReadOnlyList<DigestEntry> Entries { get; }
```

The entries: random streams, tables in world order, entities in id order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`DigestEntry`](DigestEntry.md)&gt;

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; }
```

The completed tick count the digest describes.

**Type:** `long`

### <a id="m-truncated"></a>Truncated

```csharp
public bool Truncated { get; }
```

True when the world had more entities than the limit and the entries were left out.

**Type:** `bool`

### <a id="m-worldhash"></a>WorldHash

```csharp
public ulong WorldHash { get; }
```

The whole-world hash at that tick ([`World.ComputeHash()`](../../OneGame.Core/OneGame.Core/World.md#m-computehash)).

**Type:** `ulong`

## Methods

| Member | Description |
| --- | --- |
| [`Capture(World, StateDigest, SyncWriter, int)`](#m-capture-world-statedigest-syncwriter-int) | Captures `world` into `into`, replacing its content. |
| [`CaptureView(World, StateDigest, SyncWriter, int, Audience?, bool)`](#m-captureview-world-statedigest-syncwriter-int-audience-bool) | Captures what a subscriber holds of `world`, hashed the way [`World.ComputeViewHash()`](../../OneGame.Core/OneGame.Core/World.md#m-computeviewhash) and [`World.ComputeAudienceHash()`](../../OneGame.Core/OneGame.Core/World.md#m-computeaudiencehash-audience-bool) do: hidden tables and invisible entities are left out, filtered entities hash by their visible properties, and the random streams are listed only when `includeRandom`. |
| [`CopyFrom(StateDigest)`](#m-copyfrom-statedigest) | Copies another digest into this one. |
| [`Diff(StateDigest, StateDigest, World?, World?, List<StateDifference>)`](#m-diff-statedigest-statedigest-world-world-list-statedifference) | Lists what differs between two digests. |
| [`Read(ref SyncReader)`](#m-read-syncreader) | Reads a digest from untrusted bytes. |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes the digest. |

### <a id="m-capture-world-statedigest-syncwriter-int"></a>Capture(World, StateDigest, SyncWriter, int)

```csharp
public static void Capture(World world, StateDigest into, SyncWriter scratch, int entityLimit = 2147483647);
```

Captures `world` into `into`, replacing its content.

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): The world.
- `into` ([`StateDigest`](StateDigest.md)): The digest to fill.
- `scratch` ([`SyncWriter`](../../OneGame.Core/OneGame.Core/SyncWriter.md)): A writer to use for serializing parts.
- `entityLimit` (`int`): Entities beyond this count are not listed.

### <a id="m-captureview-world-statedigest-syncwriter-int-audience-bool"></a>CaptureView(World, StateDigest, SyncWriter, int, Audience?, bool)

```csharp
public static void CaptureView(World world, StateDigest into, SyncWriter scratch, int entityLimit, Audience? audience, bool includeRandom);
```

Captures what a subscriber holds of `world`, hashed the way [`World.ComputeViewHash()`](../../OneGame.Core/OneGame.Core/World.md#m-computeviewhash) and [`World.ComputeAudienceHash()`](../../OneGame.Core/OneGame.Core/World.md#m-computeaudiencehash-audience-bool) do: hidden tables and invisible entities are left out, filtered entities hash by their visible properties, and the random streams are listed only when `includeRandom`.

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): The world: a replica (leave `audience` null) or the authority world as one audience sees it.
- `into` ([`StateDigest`](StateDigest.md)): The digest to fill.
- `scratch` ([`SyncWriter`](../../OneGame.Core/OneGame.Core/SyncWriter.md)): A writer to use for serializing parts.
- `entityLimit` (`int`): Entities beyond this count are not listed.
- `audience` ([`Audience`](../../OneGame.Core/OneGame.Core/Audience.md)?): The audience whose view of an authority world to capture; null captures the world as it is held.
- `includeRandom` (`bool`): Whether the random streams are part of the view.

### <a id="m-copyfrom-statedigest"></a>CopyFrom(StateDigest)

```csharp
public void CopyFrom(StateDigest other);
```

Copies another digest into this one.

**Parameters**

- `other` ([`StateDigest`](StateDigest.md))

### <a id="m-diff-statedigest-statedigest-world-world-list-statedifference"></a>Diff(StateDigest, StateDigest, World?, World?, List&lt;StateDifference&gt;)

```csharp
public static void Diff(StateDigest expected, StateDigest actual, World? expectedWorld, World? actualWorld, List<StateDifference> output);
```

Lists what differs between two digests. Entity type names are looked up in `expectedWorld` then `actualWorld`.

**Parameters**

- `expected` ([`StateDigest`](StateDigest.md))
- `actual` ([`StateDigest`](StateDigest.md))
- `expectedWorld` ([`World`](../../OneGame.Core/OneGame.Core/World.md)?)
- `actualWorld` ([`World`](../../OneGame.Core/OneGame.Core/World.md)?)
- `output` ([`List`](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1)&lt;[`StateDifference`](StateDifference.md)&gt;)

### <a id="m-read-syncreader"></a>Read(ref SyncReader)

```csharp
public void Read(ref SyncReader r);
```

Reads a digest from untrusted bytes. On error the reader holds the reason and the digest is left empty.

**Parameters**

- `r` ([`SyncReader`](../../OneGame.Core/OneGame.Core/SyncReader.md))

### <a id="m-write-syncwriter"></a>Write(SyncWriter)

```csharp
public void Write(SyncWriter w);
```

Writes the digest.

**Parameters**

- `w` ([`SyncWriter`](../../OneGame.Core/OneGame.Core/SyncWriter.md))
