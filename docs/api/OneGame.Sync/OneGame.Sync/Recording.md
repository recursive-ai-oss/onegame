[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# Recording class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class Recording
```

A run that can be replayed: the world at the start, every command that entered from outside, and hash checkpoints to verify against. Saved as one file so it carries everything replay needs.

## Constructors

| Member | Description |
| --- | --- |
| [`Recording()`](#m-ctor) |  |

### <a id="m-ctor"></a>Recording()

```csharp
public Recording();
```

## Properties

| Member | Description |
| --- | --- |
| [`CheckpointInterval`](#m-checkpointinterval) | The configured checkpoint interval. |
| [`Checkpoints`](#m-checkpoints) | Hash checkpoints in tick order. |
| [`Commands`](#m-commands) | Commands in submission order. |
| [`EndTick`](#m-endtick) | The tick the recording stopped at. |
| [`Seed`](#m-seed) | The seed of the recorded world (informational; the snapshot holds the random streams). |
| [`StartHash`](#m-starthash) | The world hash right after loading the start snapshot. |
| [`StartSnapshot`](#m-startsnapshot) | The world snapshot to start replay from. |
| [`StartTick`](#m-starttick) | The tick of the start snapshot. |

### <a id="m-checkpointinterval"></a>CheckpointInterval

```csharp
public int CheckpointInterval { get; set; }
```

The configured checkpoint interval.

**Type:** `int`

### <a id="m-checkpoints"></a>Checkpoints

```csharp
public List<Checkpoint> Checkpoints { get; }
```

Hash checkpoints in tick order.

**Type:** [`List`](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1)&lt;[`Checkpoint`](Checkpoint.md)&gt;

### <a id="m-commands"></a>Commands

```csharp
public List<LoggedCommand> Commands { get; }
```

Commands in submission order. Editable, so tests can corrupt a log on purpose.

**Type:** [`List`](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1)&lt;[`LoggedCommand`](LoggedCommand.md)&gt;

### <a id="m-endtick"></a>EndTick

```csharp
public long EndTick { get; set; }
```

The tick the recording stopped at.

**Type:** `long`

### <a id="m-seed"></a>Seed

```csharp
public ulong Seed { get; set; }
```

The seed of the recorded world (informational; the snapshot holds the random streams).

**Type:** `ulong`

### <a id="m-starthash"></a>StartHash

```csharp
public ulong StartHash { get; set; }
```

The world hash right after loading the start snapshot.

**Type:** `ulong`

### <a id="m-startsnapshot"></a>StartSnapshot

```csharp
public byte[] StartSnapshot { get; set; }
```

The world snapshot to start replay from.

**Type:** `byte[]`

### <a id="m-starttick"></a>StartTick

```csharp
public long StartTick { get; set; }
```

The tick of the start snapshot.

**Type:** `long`

## Methods

| Member | Description |
| --- | --- |
| [`ToBytes()`](#m-tobytes) | Serializes the recording. |
| [`TryRead(ReadOnlySpan<byte>, out Recording?)`](#m-tryread-readonlyspan-byte-recording) | Decodes a recording from untrusted bytes. |

### <a id="m-tobytes"></a>ToBytes()

```csharp
public byte[] ToBytes();
```

Serializes the recording.

**Returns:** `byte[]`

### <a id="m-tryread-readonlyspan-byte-recording"></a>TryRead(ReadOnlySpan&lt;byte&gt;, out Recording?)

```csharp
public static SyncError TryRead(ReadOnlySpan<byte> bytes, out Recording? recording);
```

Decodes a recording from untrusted bytes.

**Parameters**

- `bytes` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): The file.
- `recording` ([`Recording`](Recording.md)?): The recording, or null on error.

**Returns:** [`SyncError`](../../OneGame.Core/OneGame.Core/SyncError.md) — None, or why decoding failed.
