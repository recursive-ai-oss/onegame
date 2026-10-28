[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# SaveHeader record

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SaveHeader`](SaveHeader.md)&gt;

```csharp
public sealed record SaveHeader : IEquatable<SaveHeader>
```

The fixed part of a save file.

## Constructors

| Member | Description |
| --- | --- |
| [`SaveHeader(uint, ulong, long, ulong, ulong, IReadOnlyList<SchemaEntry>)`](#m-ctor-uint-ulong-long-ulong-ulong-ireadonlylist-schemaentry) | The fixed part of a save file. |

### <a id="m-ctor-uint-ulong-long-ulong-ulong-ireadonlylist-schemaentry"></a>SaveHeader(uint, ulong, long, ulong, ulong, IReadOnlyList&lt;SchemaEntry&gt;)

```csharp
public SaveHeader(uint Format, ulong Seed, long Tick, ulong WorldHash, ulong PayloadHash, IReadOnlyList<SchemaEntry> Schemas);
```

The fixed part of a save file.

**Parameters**

- `Format` (`uint`): The save container format version.
- `Seed` (`ulong`): The world's seed.
- `Tick` (`long`): The completed tick count.
- `WorldHash` (`ulong`): The world hash when saved.
- `PayloadHash` (`ulong`): Hash of the snapshot bytes, to catch damaged files.
- `Schemas` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`SchemaEntry`](SchemaEntry.md)&gt;): Schema version of every entity type present, by type id.

## Properties

| Member | Description |
| --- | --- |
| [`Format`](#m-format) | The save container format version. |
| [`PayloadHash`](#m-payloadhash) | Hash of the snapshot bytes, to catch damaged files. |
| [`Schemas`](#m-schemas) | Schema version of every entity type present, by type id. |
| [`Seed`](#m-seed) | The world's seed. |
| [`Tick`](#m-tick) | The completed tick count. |
| [`WorldHash`](#m-worldhash) | The world hash when saved. |

### <a id="m-format"></a>Format

```csharp
public uint Format { get; init; }
```

The save container format version.

**Type:** `uint`

### <a id="m-payloadhash"></a>PayloadHash

```csharp
public ulong PayloadHash { get; init; }
```

Hash of the snapshot bytes, to catch damaged files.

**Type:** `ulong`

### <a id="m-schemas"></a>Schemas

```csharp
public IReadOnlyList<SchemaEntry> Schemas { get; init; }
```

Schema version of every entity type present, by type id.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`SchemaEntry`](SchemaEntry.md)&gt;

### <a id="m-seed"></a>Seed

```csharp
public ulong Seed { get; init; }
```

The world's seed.

**Type:** `ulong`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The completed tick count.

**Type:** `long`

### <a id="m-worldhash"></a>WorldHash

```csharp
public ulong WorldHash { get; init; }
```

The world hash when saved.

**Type:** `ulong`
