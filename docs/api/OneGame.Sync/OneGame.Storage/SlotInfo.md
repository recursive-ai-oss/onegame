[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Storage

# SlotInfo record

**Namespace:** `OneGame.Storage`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SlotInfo`](SlotInfo.md)&gt;

```csharp
public sealed record SlotInfo : IEquatable<SlotInfo>
```

The small description of a slot a load screen shows, read without restoring the world.

## Constructors

| Member | Description |
| --- | --- |
| [`SlotInfo(string, string, SlotKind, DateTimeOffset, string, long, ulong, int)`](#m-ctor-string-string-slotkind-datetimeoffset-string-long-ulong-int) | The small description of a slot a load screen shows, read without restoring the world. |

### <a id="m-ctor-string-string-slotkind-datetimeoffset-string-long-ulong-int"></a>SlotInfo(string, string, SlotKind, DateTimeOffset, string, long, ulong, int)

```csharp
public SlotInfo(string Slot, string Label, SlotKind Kind, DateTimeOffset SavedAt, string GameVersion, long Tick, ulong WorldHash, int Bytes);
```

The small description of a slot a load screen shows, read without restoring the world.

**Parameters**

- `Slot` (`string`): The slot's name.
- `Label` (`string`): The text the game gave it (a level name, "Day 12"), up to [`SaveSlots.MaxLabelBytes`](SaveSlots.md#m-maxlabelbytes) UTF-8 bytes.
- `Kind` ([`SlotKind`](SlotKind.md)): Why it was written.
- `SavedAt` ([`DateTimeOffset`](https://learn.microsoft.com/dotnet/api/system.datetimeoffset)): When it was written, by the clock of the [`SaveSlotOptions.Time`](SaveSlotOptions.md#m-time) provider (UTC).
- `GameVersion` (`string`): The [`SaveSlotOptions.GameVersion`](SaveSlotOptions.md#m-gameversion) of the build that wrote it.
- `Tick` (`long`): The world's completed tick count at that moment.
- `WorldHash` (`ulong`): The world hash at that moment.
- `Bytes` (`int`): The size of the slot file.

## Properties

| Member | Description |
| --- | --- |
| [`Bytes`](#m-bytes) | The size of the slot file. |
| [`GameVersion`](#m-gameversion) | The [`SaveSlotOptions.GameVersion`](SaveSlotOptions.md#m-gameversion) of the build that wrote it. |
| [`Kind`](#m-kind) | Why it was written. |
| [`Label`](#m-label) | The text the game gave it (a level name, "Day 12"), up to [`SaveSlots.MaxLabelBytes`](SaveSlots.md#m-maxlabelbytes) UTF-8 bytes. |
| [`SavedAt`](#m-savedat) | When it was written, by the clock of the [`SaveSlotOptions.Time`](SaveSlotOptions.md#m-time) provider (UTC). |
| [`Slot`](#m-slot) | The slot's name. |
| [`Tick`](#m-tick) | The world's completed tick count at that moment. |
| [`WorldHash`](#m-worldhash) | The world hash at that moment. |

### <a id="m-bytes"></a>Bytes

```csharp
public int Bytes { get; init; }
```

The size of the slot file.

**Type:** `int`

### <a id="m-gameversion"></a>GameVersion

```csharp
public string GameVersion { get; init; }
```

The [`SaveSlotOptions.GameVersion`](SaveSlotOptions.md#m-gameversion) of the build that wrote it.

**Type:** `string`

### <a id="m-kind"></a>Kind

```csharp
public SlotKind Kind { get; init; }
```

Why it was written.

**Type:** [`SlotKind`](SlotKind.md)

### <a id="m-label"></a>Label

```csharp
public string Label { get; init; }
```

The text the game gave it (a level name, "Day 12"), up to [`SaveSlots.MaxLabelBytes`](SaveSlots.md#m-maxlabelbytes) UTF-8 bytes.

**Type:** `string`

### <a id="m-savedat"></a>SavedAt

```csharp
public DateTimeOffset SavedAt { get; init; }
```

When it was written, by the clock of the [`SaveSlotOptions.Time`](SaveSlotOptions.md#m-time) provider (UTC).

**Type:** [`DateTimeOffset`](https://learn.microsoft.com/dotnet/api/system.datetimeoffset)

### <a id="m-slot"></a>Slot

```csharp
public string Slot { get; init; }
```

The slot's name.

**Type:** `string`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The world's completed tick count at that moment.

**Type:** `long`

### <a id="m-worldhash"></a>WorldHash

```csharp
public ulong WorldHash { get; init; }
```

The world hash at that moment.

**Type:** `ulong`
