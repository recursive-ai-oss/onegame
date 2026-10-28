[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# SnapshotResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SnapshotResult`](SnapshotResult.md)&gt;

```csharp
public sealed record SnapshotResult : IEquatable<SnapshotResult>
```

Result of `snapshot_save`.

## Constructors

| Member | Description |
| --- | --- |
| [`SnapshotResult(string?, int, string?, string, long)`](#m-ctor-string-int-string-string-long) | Result of `snapshot_save`. |

### <a id="m-ctor-string-int-string-string-long"></a>SnapshotResult(string?, int, string?, string, long)

```csharp
public SnapshotResult(string? Path, int Bytes, string? Base64, string Hash, long Tick);
```

Result of `snapshot_save`.

**Parameters**

- `Path` (`string?`)
- `Bytes` (`int`)
- `Base64` (`string?`)
- `Hash` (`string`)
- `Tick` (`long`)

## Properties

| Member | Description |
| --- | --- |
| [`Base64`](#m-base64) |  |
| [`Bytes`](#m-bytes) |  |
| [`Hash`](#m-hash) |  |
| [`Path`](#m-path) |  |
| [`Tick`](#m-tick) |  |

### <a id="m-base64"></a>Base64

```csharp
public string? Base64 { get; init; }
```

**Type:** `string?`

### <a id="m-bytes"></a>Bytes

```csharp
public int Bytes { get; init; }
```

**Type:** `int`

### <a id="m-hash"></a>Hash

```csharp
public string Hash { get; init; }
```

**Type:** `string`

### <a id="m-path"></a>Path

```csharp
public string? Path { get; init; }
```

**Type:** `string?`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

**Type:** `long`
