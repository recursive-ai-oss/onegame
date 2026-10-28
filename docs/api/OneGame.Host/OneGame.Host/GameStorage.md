[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# GameStorage class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public sealed class GameStorage
```

A game's saves and settings: the happy path over [`IByteStore`](../../OneGame.Core/OneGame.Storage/IByteStore.md), [`SaveSlots`](../../OneGame.Sync/OneGame.Storage/SaveSlots.md) and [`SettingsFile<T>`](../../OneGame.Core/OneGame.Storage/SettingsFile-1.md). Reach it as [`GameInstance.Storage`](GameInstance.md#m-storage). Call it between ticks, from the host or from view code (a button handler), never from sim code: it reads the clock and does I/O.

## Properties

| Member | Description |
| --- | --- |
| [`Files`](#m-files) | The store the game's files live in (the low level: named byte blobs). |
| [`SettingsFiles`](#m-settingsfiles) | The settings files opened through [`GameStorage.OpenSettings()`](GameStorage.md#m-opensettings-t-settingsschema-t), in the order they were opened (the harness lists them). |
| [`Slots`](#m-slots) | The slot manager over [`GameStorage.Files`](GameStorage.md#m-files) (the middle level: list, read and write slots of any world). |

### <a id="m-files"></a>Files

```csharp
public IByteStore Files { get; }
```

The store the game's files live in (the low level: named byte blobs).

**Type:** [`IByteStore`](../../OneGame.Core/OneGame.Storage/IByteStore.md)

### <a id="m-settingsfiles"></a>SettingsFiles

```csharp
public IReadOnlyList<ISettingsFile> SettingsFiles { get; }
```

The settings files opened through [`GameStorage.OpenSettings()`](GameStorage.md#m-opensettings-t-settingsschema-t), in the order they were opened (the harness lists them).

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ISettingsFile`](../../OneGame.Core/OneGame.Storage/ISettingsFile.md)&gt;

### <a id="m-slots"></a>Slots

```csharp
public SaveSlots Slots { get; }
```

The slot manager over [`GameStorage.Files`](GameStorage.md#m-files) (the middle level: list, read and write slots of any world).

**Type:** [`SaveSlots`](../../OneGame.Sync/OneGame.Storage/SaveSlots.md)

## Methods

| Member | Description |
| --- | --- |
| [`Autosave(string)`](#m-autosave-string) | Saves into the next rotating autosave slot. |
| [`Create(string, GameOptions)`](#m-create-string-gameoptions) | Chooses the default store for a game. |
| [`Load(string)`](#m-load-string) | Replaces the running world with a slot's save. |
| [`LoadLatest()`](#m-loadlatest) | Loads the newest slot of any kind (a "Continue" button). |
| [`OpenSettings<T>(SettingsSchema<T>)`](#m-opensettings-t-settingsschema-t) | Opens a typed settings file in this game's store and loads it. |
| [`QuickLoad()`](#m-quickload) | Loads the quick-save slot. |
| [`QuickSave(string)`](#m-quicksave-string) | Saves into the quick-save slot. |
| [`Save(string, string, SlotKind)`](#m-save-string-string-slotkind) | Saves the running world into `slot`. |

### <a id="m-autosave-string"></a>Autosave(string)

```csharp
public SlotResult Autosave(string label = "");
```

Saves into the next rotating autosave slot.

**Parameters**

- `label` (`string`)

**Returns:** [`SlotResult`](../../OneGame.Sync/OneGame.Storage/SlotResult.md)

### <a id="m-create-string-gameoptions"></a>Create(string, GameOptions)

```csharp
public static IByteStore Create(string appName, GameOptions options);
```

Chooses the default store for a game. `ONEGAME_DATA_DIR` or [`GameOptions.DataDirectory`](GameOptions.md#m-datadirectory) picks a folder; otherwise a headless or unpresented run uses memory (unless [`GameOptions.PersistData`](GameOptions.md#m-persistdata)), a browser uses IndexedDB, and the rest use a folder under the user's local application data.

**Parameters**

- `appName` (`string`)
- `options` ([`GameOptions`](GameOptions.md))

**Returns:** [`IByteStore`](../../OneGame.Core/OneGame.Storage/IByteStore.md)

### <a id="m-load-string"></a>Load(string)

```csharp
public SlotResult Load(string slot);
```

Replaces the running world with a slot's save. The world is built fresh and swapped in only when the load works, so a damaged or refused slot leaves the running game as it was. Older saves go through the entity migrations ([`LoadResult.Migrated`](../../OneGame.Sync/OneGame.Sync/LoadResult.md#m-migrated)).

**Parameters**

- `slot` (`string`)

**Returns:** [`SlotResult`](../../OneGame.Sync/OneGame.Storage/SlotResult.md)

### <a id="m-loadlatest"></a>LoadLatest()

```csharp
public SlotResult LoadLatest();
```

Loads the newest slot of any kind (a "Continue" button). [`SlotStatus.NoSuchSlot`](../../OneGame.Sync/OneGame.Storage/SlotStatus.md#m-nosuchslot) when there are none.

**Returns:** [`SlotResult`](../../OneGame.Sync/OneGame.Storage/SlotResult.md)

### <a id="m-opensettings-t-settingsschema-t"></a>OpenSettings&lt;T&gt;(SettingsSchema&lt;T&gt;)

```csharp
public SettingsFile<T> OpenSettings<T>(SettingsSchema<T> schema) where T : class, new();
```

Opens a typed settings file in this game's store and loads it. [`SettingsFile<T>.Value`](../../OneGame.Core/OneGame.Storage/SettingsFile-1.md#m-value) holds the player's choices, or the defaults when the file is missing or damaged; the returned load result says which and why. Call [`SettingsFile<T>.Save()`](../../OneGame.Core/OneGame.Storage/SettingsFile-1.md#m-save) after changes.

**Parameters**

- `schema` ([`SettingsSchema`](../../OneGame.Core/OneGame.Storage/SettingsSchema-1.md)`<T>`)

**Returns:** [`SettingsFile`](../../OneGame.Core/OneGame.Storage/SettingsFile-1.md)`<T>`

### <a id="m-quickload"></a>QuickLoad()

```csharp
public SlotResult QuickLoad();
```

Loads the quick-save slot.

**Returns:** [`SlotResult`](../../OneGame.Sync/OneGame.Storage/SlotResult.md)

### <a id="m-quicksave-string"></a>QuickSave(string)

```csharp
public SlotResult QuickSave(string label = "");
```

Saves into the quick-save slot.

**Parameters**

- `label` (`string`)

**Returns:** [`SlotResult`](../../OneGame.Sync/OneGame.Storage/SlotResult.md)

### <a id="m-save-string-string-slotkind"></a>Save(string, string, SlotKind)

```csharp
public SlotResult Save(string slot, string label = "", SlotKind kind = 0);
```

Saves the running world into `slot`.

**Parameters**

- `slot` (`string`)
- `label` (`string`)
- `kind` ([`SlotKind`](../../OneGame.Sync/OneGame.Storage/SlotKind.md))

**Returns:** [`SlotResult`](../../OneGame.Sync/OneGame.Storage/SlotResult.md)
