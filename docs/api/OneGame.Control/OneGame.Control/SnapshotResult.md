[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# SnapshotResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SnapshotResult`](SnapshotResult.md)&gt;

```csharp
public sealed record SnapshotResult : IEquatable<SnapshotResult>
```

Result of `snapshot_save`: the world written as a snapshot, which `snapshot_load` (or a scenario's `snapshot` step) restores exactly.

## Constructors

| Member | Description |
| --- | --- |
| [`SnapshotResult(string?, int, string?, string, long)`](#m-ctor-string-int-string-string-long) | Result of `snapshot_save`: the world written as a snapshot, which `snapshot_load` (or a scenario's `snapshot` step) restores exactly. |

### <a id="m-ctor-string-int-string-string-long"></a>SnapshotResult(string?, int, string?, string, long)

```csharp
public SnapshotResult(string? Path, int Bytes, string? Base64, string Hash, long Tick);
```

Result of `snapshot_save`: the world written as a snapshot, which `snapshot_load` (or a scenario's `snapshot` step) restores exactly.

**Parameters**

- `Path` (`string?`): The file written under the output folder, or null when the snapshot came back inline.
- `Bytes` (`int`): The snapshot's size.
- `Base64` (`string?`): The snapshot as base64 when no path was given, otherwise null.
- `Hash` (`string`): The state hash at the time.
- `Tick` (`long`): The world's tick at the time.

## Properties

| Member | Description |
| --- | --- |
| [`Base64`](#m-base64) | The snapshot as base64 when no path was given, otherwise null. |
| [`Bytes`](#m-bytes) | The snapshot's size. |
| [`Hash`](#m-hash) | The state hash at the time. |
| [`Path`](#m-path) | The file written under the output folder, or null when the snapshot came back inline. |
| [`Tick`](#m-tick) | The world's tick at the time. |

### <a id="m-base64"></a>Base64

```csharp
public string? Base64 { get; init; }
```

The snapshot as base64 when no path was given, otherwise null.

**Type:** `string?`

### <a id="m-bytes"></a>Bytes

```csharp
public int Bytes { get; init; }
```

The snapshot's size.

**Type:** `int`

### <a id="m-hash"></a>Hash

```csharp
public string Hash { get; init; }
```

The state hash at the time.

**Type:** `string`

### <a id="m-path"></a>Path

```csharp
public string? Path { get; init; }
```

The file written under the output folder, or null when the snapshot came back inline.

**Type:** `string?`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The world's tick at the time.

**Type:** `long`
