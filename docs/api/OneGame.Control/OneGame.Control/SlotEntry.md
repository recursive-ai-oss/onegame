[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# SlotEntry record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SlotEntry`](SlotEntry.md)&gt;

```csharp
public sealed record SlotEntry : IEquatable<SlotEntry>
```

One save slot in the `saves` report.

## Constructors

| Member | Description |
| --- | --- |
| [`SlotEntry(string, string, string, string, string, long, string, int)`](#m-ctor-string-string-string-string-string-long-string-int) | One save slot in the `saves` report. |

### <a id="m-ctor-string-string-string-string-string-long-string-int"></a>SlotEntry(string, string, string, string, string, long, string, int)

```csharp
public SlotEntry(string Slot, string Label, string Kind, string SavedAt, string GameVersion, long Tick, string WorldHash, int Bytes);
```

One save slot in the `saves` report.

**Parameters**

- `Slot` (`string`): The slot name.
- `Label` (`string`): The game's label.
- `Kind` (`string`): `manual`, `auto` or `quick`.
- `SavedAt` (`string`): When it was written, ISO 8601 UTC.
- `GameVersion` (`string`): The game version that wrote it.
- `Tick` (`long`): The world tick when saved.
- `WorldHash` (`string`): The world hash when saved, 16 hex digits.
- `Bytes` (`int`): The slot file's size.

## Properties

| Member | Description |
| --- | --- |
| [`Bytes`](#m-bytes) | The slot file's size. |
| [`GameVersion`](#m-gameversion) | The game version that wrote it. |
| [`Kind`](#m-kind) | `manual`, `auto` or `quick`. |
| [`Label`](#m-label) | The game's label. |
| [`SavedAt`](#m-savedat) | When it was written, ISO 8601 UTC. |
| [`Slot`](#m-slot) | The slot name. |
| [`Tick`](#m-tick) | The world tick when saved. |
| [`WorldHash`](#m-worldhash) | The world hash when saved, 16 hex digits. |

### <a id="m-bytes"></a>Bytes

```csharp
public int Bytes { get; init; }
```

The slot file's size.

**Type:** `int`

### <a id="m-gameversion"></a>GameVersion

```csharp
public string GameVersion { get; init; }
```

The game version that wrote it.

**Type:** `string`

### <a id="m-kind"></a>Kind

```csharp
public string Kind { get; init; }
```

`manual`, `auto` or `quick`.

**Type:** `string`

### <a id="m-label"></a>Label

```csharp
public string Label { get; init; }
```

The game's label.

**Type:** `string`

### <a id="m-savedat"></a>SavedAt

```csharp
public string SavedAt { get; init; }
```

When it was written, ISO 8601 UTC.

**Type:** `string`

### <a id="m-slot"></a>Slot

```csharp
public string Slot { get; init; }
```

The slot name.

**Type:** `string`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The world tick when saved.

**Type:** `long`

### <a id="m-worldhash"></a>WorldHash

```csharp
public string WorldHash { get; init; }
```

The world hash when saved, 16 hex digits.

**Type:** `string`
