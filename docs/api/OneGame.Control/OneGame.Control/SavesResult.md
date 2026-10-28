[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# SavesResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SavesResult`](SavesResult.md)&gt;

```csharp
public sealed record SavesResult : IEquatable<SavesResult>
```

Result of the `saves` operation.

## Constructors

| Member | Description |
| --- | --- |
| [`SavesResult(string, SlotEntry[], string[], SettingsEntry[], int, long, string, string?, bool)`](#m-ctor-string-slotentry-string-settingsentry-int-long-string-string-bool) | Result of the `saves` operation. |

### <a id="m-ctor-string-slotentry-string-settingsentry-int-long-string-string-bool"></a>SavesResult(string, SlotEntry\[\], string\[\], SettingsEntry\[\], int, long, string, string?, bool)

```csharp
public SavesResult(string Store, SlotEntry[] Slots, string[] Files, SettingsEntry[] Settings, int Pending, long Tick, string Hash, string? Loaded, bool Migrated);
```

Result of the `saves` operation.

**Parameters**

- `Store` (`string`): Where the files live.
- `Slots` ([`SlotEntry`](SlotEntry.md)\[\]): Every readable slot, newest first.
- `Files` (`string[]`): Every file in the store.
- `Settings` ([`SettingsEntry`](SettingsEntry.md)\[\]): The settings files the game opened.
- `Pending` (`int`): Browser writes not yet committed to IndexedDB (0 elsewhere).
- `Tick` (`long`): The world's tick.
- `Hash` (`string`): The world's hash.
- `Loaded` (`string?`): The slot the action loaded or saved, when it did.
- `Migrated` (`bool`): True when the load ran a migration (so the hash cannot equal the saved one).

## Properties

| Member | Description |
| --- | --- |
| [`Files`](#m-files) | Every file in the store. |
| [`Hash`](#m-hash) | The world's hash. |
| [`Loaded`](#m-loaded) | The slot the action loaded or saved, when it did. |
| [`Migrated`](#m-migrated) | True when the load ran a migration (so the hash cannot equal the saved one). |
| [`Pending`](#m-pending) | Browser writes not yet committed to IndexedDB (0 elsewhere). |
| [`Settings`](#m-settings) | The settings files the game opened. |
| [`Slots`](#m-slots) | Every readable slot, newest first. |
| [`Store`](#m-store) | Where the files live. |
| [`Tick`](#m-tick) | The world's tick. |

### <a id="m-files"></a>Files

```csharp
public string[] Files { get; init; }
```

Every file in the store.

**Type:** `string[]`

### <a id="m-hash"></a>Hash

```csharp
public string Hash { get; init; }
```

The world's hash.

**Type:** `string`

### <a id="m-loaded"></a>Loaded

```csharp
public string? Loaded { get; init; }
```

The slot the action loaded or saved, when it did.

**Type:** `string?`

### <a id="m-migrated"></a>Migrated

```csharp
public bool Migrated { get; init; }
```

True when the load ran a migration (so the hash cannot equal the saved one).

**Type:** `bool`

### <a id="m-pending"></a>Pending

```csharp
public int Pending { get; init; }
```

Browser writes not yet committed to IndexedDB (0 elsewhere).

**Type:** `int`

### <a id="m-settings"></a>Settings

```csharp
public SettingsEntry[] Settings { get; init; }
```

The settings files the game opened.

**Type:** [`SettingsEntry`](SettingsEntry.md)\[\]

### <a id="m-slots"></a>Slots

```csharp
public SlotEntry[] Slots { get; init; }
```

Every readable slot, newest first.

**Type:** [`SlotEntry`](SlotEntry.md)\[\]

### <a id="m-store"></a>Store

```csharp
public string Store { get; init; }
```

Where the files live.

**Type:** `string`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The world's tick.

**Type:** `long`
