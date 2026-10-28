[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# SaveFile static class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public static class SaveFile
```

Save files: a header (format, seed, tick, hashes, schema versions) followed by a world snapshot.

## Methods

| Member | Description |
| --- | --- |
| [`Load(ReadOnlySpan<byte>, World)`](#m-load-readonlyspan-byte-world) | Replaces the contents of `world` with a save, running migrations for entity types saved at an older schema. |
| [`ToJson(ReadOnlySpan<byte>, World)`](#m-tojson-readonlyspan-byte-world) | Loads a save into `scratchWorld` and writes the header and the full world state as JSON, for inspection. |
| [`TryReadHeader(ReadOnlySpan<byte>, out SaveHeader?, out ReadOnlySpan<byte>, out SyncError)`](#m-tryreadheader-readonlyspan-byte-saveheader-readonlyspan-byte-syncerror) | Reads the header from untrusted bytes. |
| [`Write(World)`](#m-write-world) | Serializes `world`. |

### <a id="m-load-readonlyspan-byte-world"></a>Load(ReadOnlySpan&lt;byte&gt;, World)

```csharp
public static LoadResult Load(ReadOnlySpan<byte> save, World world);
```

Replaces the contents of `world` with a save, running migrations for entity types saved at an older schema. The world must be built like the saved one (same seed, tables and systems). After a failure the world is unspecified: discard it.

**Parameters**

- `save` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)
- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md))

**Returns:** [`LoadResult`](LoadResult.md)

### <a id="m-tojson-readonlyspan-byte-world"></a>ToJson(ReadOnlySpan&lt;byte&gt;, World)

```csharp
public static string ToJson(ReadOnlySpan<byte> save, World scratchWorld);
```

Loads a save into `scratchWorld` and writes the header and the full world state as JSON, for inspection.

**Parameters**

- `save` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)
- `scratchWorld` ([`World`](../../OneGame.Core/OneGame.Core/World.md))

**Returns:** `string`

### <a id="m-tryreadheader-readonlyspan-byte-saveheader-readonlyspan-byte-syncerror"></a>TryReadHeader(ReadOnlySpan&lt;byte&gt;, out SaveHeader?, out ReadOnlySpan&lt;byte&gt;, out SyncError)

```csharp
public static SaveStatus TryReadHeader(ReadOnlySpan<byte> bytes, out SaveHeader? header, out ReadOnlySpan<byte> payload, out SyncError detail);
```

Reads the header from untrusted bytes.

**Parameters**

- `bytes` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): The file.
- `header` ([`SaveHeader`](SaveHeader.md)?): The header, or null.
- `payload` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): The snapshot bytes (a view into `bytes`).
- `detail` ([`SyncError`](../../OneGame.Core/OneGame.Core/SyncError.md)): The decoder error behind a [`SaveStatus.BadFile`](SaveStatus.md#m-badfile).

**Returns:** [`SaveStatus`](SaveStatus.md)

### <a id="m-write-world"></a>Write(World)

```csharp
public static byte[] Write(World world);
```

Serializes `world`.

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md))

**Returns:** `byte[]`
