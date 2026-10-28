[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Storage

# SaveSlots class

**Namespace:** `OneGame.Storage`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class SaveSlots
```

Named save slots over a [`IByteStore`](../../OneGame.Core/OneGame.Storage/IByteStore.md): each slot is one file holding a small description and a world save ([`SaveFile`](../OneGame.Sync/SaveFile.md), so older saves load through the entity migrations). Writes are atomic through the store, so a slot is always either the old save or the new one.

Call these from the host or from view code, between ticks. They do I/O and read the wall clock, which sim code must never do.

## Constructors

| Member | Description |
| --- | --- |
| [`SaveSlots(IByteStore, SaveSlotOptions?)`](#m-ctor-ibytestore-saveslotoptions) | Creates slots in `store`. |

### <a id="m-ctor-ibytestore-saveslotoptions"></a>SaveSlots(IByteStore, SaveSlotOptions?)

```csharp
public SaveSlots(IByteStore store, SaveSlotOptions? options = null);
```

Creates slots in `store`.

**Parameters**

- `store` ([`IByteStore`](../../OneGame.Core/OneGame.Storage/IByteStore.md))
- `options` ([`SaveSlotOptions`](SaveSlotOptions.md)?)

## Fields

| Member | Description |
| --- | --- |
| [`MaxLabelBytes`](#m-maxlabelbytes) | The longest label, in UTF-8 bytes. |
| [`MaxSlotNameLength`](#m-maxslotnamelength) | The longest slot name. |
| [`QuickSlot`](#m-quickslot) | The slot [`SaveSlots.QuickSave()`](SaveSlots.md#m-quicksave-world-string) writes. |

### <a id="m-maxlabelbytes"></a>MaxLabelBytes

```csharp
public const int MaxLabelBytes = 128;
```

The longest label, in UTF-8 bytes. Longer labels are cut.

**Type:** `int`

### <a id="m-maxslotnamelength"></a>MaxSlotNameLength

```csharp
public const int MaxSlotNameLength = 48;
```

The longest slot name.

**Type:** `int`

### <a id="m-quickslot"></a>QuickSlot

```csharp
public const string QuickSlot = "quick";
```

The slot [`SaveSlots.QuickSave()`](SaveSlots.md#m-quicksave-world-string) writes.

**Type:** `string`

## Properties

| Member | Description |
| --- | --- |
| [`Store`](#m-store) | The store the slots live in. |

### <a id="m-store"></a>Store

```csharp
public IByteStore Store { get; }
```

The store the slots live in.

**Type:** [`IByteStore`](../../OneGame.Core/OneGame.Storage/IByteStore.md)

## Methods

| Member | Description |
| --- | --- |
| [`Autosave(World, string)`](#m-autosave-world-string) | Writes an autosave into the rotating slots `auto-0`, `auto-1`…: the first free one, else the oldest. |
| [`Autosave(AuthorityScope, string)`](#m-autosave-authorityscope-string) | Writes an autosave of an authority scope (see [`SaveSlots.Autosave()`](SaveSlots.md#m-autosave-world-string)). |
| [`AutosaveName(int)`](#m-autosavename-int) | The slot name of autosave number `index`. |
| [`Delete(string)`](#m-delete-string) | Deletes a slot. |
| [`Exists(string)`](#m-exists-string) | True when the slot exists and can be read. |
| [`Import(string, byte[], string, SlotKind)`](#m-import-string-byte-string-slotkind) | Puts a save you already hold (for example one read from another store, or written by an earlier version of the game) into a slot. |
| [`IsValidSlotName(string?)`](#m-isvalidslotname-string) | True when `slot` is a usable name: 1 to 48 ASCII letters, digits, `_` or `-`. |
| [`Latest()`](#m-latest) | The newest slot of any kind, or null when there are none (what a "Continue" button loads). |
| [`List()`](#m-list) | Describes every slot that can be read, newest first (ties by name). |
| [`Load(string, World)`](#m-load-string-world) | Replaces the contents of `world` with the slot's save, migrating entity types saved at an older schema. |
| [`Load(string, AuthorityScope)`](#m-load-string-authorityscope) | Replaces an authority scope's state with the slot's save; subscribers get a fresh snapshot (see [`AuthorityScope.Load()`](../OneGame.Sync/AuthorityScope.md#m-load-readonlyspan-byte)). |
| [`QuickSave(World, string)`](#m-quicksave-world-string) | Saves `world` into the [`SaveSlots.QuickSlot`](SaveSlots.md#m-quickslot). |
| [`QuickSave(AuthorityScope, string)`](#m-quicksave-authorityscope-string) | Saves an authority scope into the [`SaveSlots.QuickSlot`](SaveSlots.md#m-quickslot). |
| [`ReadSave(string, out byte[])`](#m-readsave-string-byte) | Reads a slot's save bytes (for `GameInstance.LoadSnapshot`, which builds a fresh world and swaps it in only when the load works). |
| [`Save(string, World, string, SlotKind)`](#m-save-string-world-string-slotkind) | Saves `world` into `slot`, replacing what was there. |
| [`Save(string, AuthorityScope, string, SlotKind)`](#m-save-string-authorityscope-string-slotkind) | Saves an authority scope's world into `slot` (this also lets `Durability.AfterSave` release held answers). |
| [`TryGetInfo(string, out SlotInfo?)`](#m-trygetinfo-string-slotinfo) | Reads one slot's description. |
| [`TryParse(ReadOnlySpan<byte>, string, out SlotInfo?, out ReadOnlySpan<byte>)`](#m-tryparse-readonlyspan-byte-string-slotinfo-readonlyspan-byte) | Reads a slot file from untrusted bytes: the description plus a view of the save inside. |

### <a id="m-autosave-world-string"></a>Autosave(World, string)

```csharp
public SlotResult Autosave(World world, string label = "");
```

Writes an autosave into the rotating slots `auto-0`, `auto-1`…: the first free one, else the oldest. A crash during the write costs at most that one slot; the newer autosaves are untouched.

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md))
- `label` (`string`)

**Returns:** [`SlotResult`](SlotResult.md)

### <a id="m-autosave-authorityscope-string"></a>Autosave(AuthorityScope, string)

```csharp
public SlotResult Autosave(AuthorityScope scope, string label = "");
```

Writes an autosave of an authority scope (see [`SaveSlots.Autosave()`](SaveSlots.md#m-autosave-world-string)).

**Parameters**

- `scope` ([`AuthorityScope`](../OneGame.Sync/AuthorityScope.md))
- `label` (`string`)

**Returns:** [`SlotResult`](SlotResult.md)

### <a id="m-autosavename-int"></a>AutosaveName(int)

```csharp
public static string AutosaveName(int index);
```

The slot name of autosave number `index`.

**Parameters**

- `index` (`int`)

**Returns:** `string`

### <a id="m-delete-string"></a>Delete(string)

```csharp
public bool Delete(string slot);
```

Deletes a slot. Returns true when it is gone afterwards.

**Parameters**

- `slot` (`string`)

**Returns:** `bool`

### <a id="m-exists-string"></a>Exists(string)

```csharp
public bool Exists(string slot);
```

True when the slot exists and can be read.

**Parameters**

- `slot` (`string`)

**Returns:** `bool`

### <a id="m-import-string-byte-string-slotkind"></a>Import(string, byte\[\], string, SlotKind)

```csharp
public SlotResult Import(string slot, byte[] save, string label = "", SlotKind kind = 0);
```

Puts a save you already hold (for example one read from another store, or written by an earlier version of the game) into a slot. The bytes must be a valid [`SaveFile`](../OneGame.Sync/SaveFile.md); they are not loaded.

**Parameters**

- `slot` (`string`)
- `save` (`byte[]`)
- `label` (`string`)
- `kind` ([`SlotKind`](SlotKind.md))

**Returns:** [`SlotResult`](SlotResult.md)

### <a id="m-isvalidslotname-string"></a>IsValidSlotName(string?)

```csharp
public static bool IsValidSlotName(string? slot);
```

True when `slot` is a usable name: 1 to 48 ASCII letters, digits, `_` or `-`.

**Parameters**

- `slot` (`string?`)

**Returns:** `bool`

### <a id="m-latest"></a>Latest()

```csharp
public SlotInfo? Latest();
```

The newest slot of any kind, or null when there are none (what a "Continue" button loads).

**Returns:** [`SlotInfo`](SlotInfo.md)?

### <a id="m-list"></a>List()

```csharp
public IReadOnlyList<SlotInfo> List();
```

Describes every slot that can be read, newest first (ties by name). Damaged slot files are left out; see [`SaveSlots.TryGetInfo()`](SaveSlots.md#m-trygetinfo-string-slotinfo) for why one is.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`SlotInfo`](SlotInfo.md)&gt;

### <a id="m-load-string-world"></a>Load(string, World)

```csharp
public SlotResult Load(string slot, World world);
```

Replaces the contents of `world` with the slot's save, migrating entity types saved at an older schema. If the save is refused part-way the world is unspecified: build it fresh and discard it.

**Parameters**

- `slot` (`string`)
- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md))

**Returns:** [`SlotResult`](SlotResult.md)

### <a id="m-load-string-authorityscope"></a>Load(string, AuthorityScope)

```csharp
public SlotResult Load(string slot, AuthorityScope scope);
```

Replaces an authority scope's state with the slot's save; subscribers get a fresh snapshot (see [`AuthorityScope.Load()`](../OneGame.Sync/AuthorityScope.md#m-load-readonlyspan-byte)).

**Parameters**

- `slot` (`string`)
- `scope` ([`AuthorityScope`](../OneGame.Sync/AuthorityScope.md))

**Returns:** [`SlotResult`](SlotResult.md)

### <a id="m-quicksave-world-string"></a>QuickSave(World, string)

```csharp
public SlotResult QuickSave(World world, string label = "");
```

Saves `world` into the [`SaveSlots.QuickSlot`](SaveSlots.md#m-quickslot).

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md))
- `label` (`string`)

**Returns:** [`SlotResult`](SlotResult.md)

### <a id="m-quicksave-authorityscope-string"></a>QuickSave(AuthorityScope, string)

```csharp
public SlotResult QuickSave(AuthorityScope scope, string label = "");
```

Saves an authority scope into the [`SaveSlots.QuickSlot`](SaveSlots.md#m-quickslot).

**Parameters**

- `scope` ([`AuthorityScope`](../OneGame.Sync/AuthorityScope.md))
- `label` (`string`)

**Returns:** [`SlotResult`](SlotResult.md)

### <a id="m-readsave-string-byte"></a>ReadSave(string, out byte\[\])

```csharp
public SlotResult ReadSave(string slot, out byte[] save);
```

Reads a slot's save bytes (for `GameInstance.LoadSnapshot`, which builds a fresh world and swaps it in only when the load works). Nothing is restored here.

**Parameters**

- `slot` (`string`)
- `save` (`byte[]`)

**Returns:** [`SlotResult`](SlotResult.md)

### <a id="m-save-string-world-string-slotkind"></a>Save(string, World, string, SlotKind)

```csharp
public SlotResult Save(string slot, World world, string label = "", SlotKind kind = 0);
```

Saves `world` into `slot`, replacing what was there.

**Parameters**

- `slot` (`string`)
- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md))
- `label` (`string`)
- `kind` ([`SlotKind`](SlotKind.md))

**Returns:** [`SlotResult`](SlotResult.md)

### <a id="m-save-string-authorityscope-string-slotkind"></a>Save(string, AuthorityScope, string, SlotKind)

```csharp
public SlotResult Save(string slot, AuthorityScope scope, string label = "", SlotKind kind = 0);
```

Saves an authority scope's world into `slot` (this also lets `Durability.AfterSave` release held answers).

**Parameters**

- `slot` (`string`)
- `scope` ([`AuthorityScope`](../OneGame.Sync/AuthorityScope.md))
- `label` (`string`)
- `kind` ([`SlotKind`](SlotKind.md))

**Returns:** [`SlotResult`](SlotResult.md)

### <a id="m-trygetinfo-string-slotinfo"></a>TryGetInfo(string, out SlotInfo?)

```csharp
public SlotResult TryGetInfo(string slot, out SlotInfo? info);
```

Reads one slot's description.

**Parameters**

- `slot` (`string`)
- `info` ([`SlotInfo`](SlotInfo.md)?)

**Returns:** [`SlotResult`](SlotResult.md)

### <a id="m-tryparse-readonlyspan-byte-string-slotinfo-readonlyspan-byte"></a>TryParse(ReadOnlySpan&lt;byte&gt;, string, out SlotInfo?, out ReadOnlySpan&lt;byte&gt;)

```csharp
public static SlotStatus TryParse(ReadOnlySpan<byte> file, string slot, out SlotInfo? info, out ReadOnlySpan<byte> save);
```

Reads a slot file from untrusted bytes: the description plus a view of the save inside. Lengths are checked against the bytes present before anything is allocated, and the inner save's header is checked too.

**Parameters**

- `file` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): The slot file.
- `slot` (`string`): The slot's name, copied into the description.
- `info` ([`SlotInfo`](SlotInfo.md)?): The description, or null.
- `save` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): The save bytes (a view into `file`).

**Returns:** [`SlotStatus`](SlotStatus.md)
