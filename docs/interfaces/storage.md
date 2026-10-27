# Storage

What a game keeps for a player: save slots, typed settings and plain files. Three levels, each built on the one below, none hidden:

| Level | Type | Assembly | What it is |
|---|---|---|---|
| Files | `IByteStore` | `OneGame.Core` (`src/OneGame.Core/Storage/`) | named byte blobs: `TryRead(name, maxBytes)`, `Write` (atomic replace), `Delete`, `List(prefix)` |
| Slots, settings | `SaveSlots` (`src/OneGame.Sync/Storage/`), `SettingsSchema<T>` / `SettingsFile<T>` (Core) | Sync, Core | a world save with a description in a file; a typed settings object in a file |
| Happy path | `GameInstance.Storage` → `GameStorage` (`src/OneGame.Host/Storage/`) | Host | the store chosen for the platform, slots on the running world, `OpenSettings`, the harness lists what it opened |

User guide: [saves.md](../saves.md).

## Stores

- Names: ASCII letters, digits, `.`, `_`, `-`; 1 to 96 characters; no leading `.`; not ending in `.tmp` (`StoreNames`). An invalid name throws `ArgumentException` (a programming error); everything else answers `false`.
- `TryRead` checks the size against `maxBytes` before copying and answers `false` for missing, unreadable and oversize alike.
- `Write` is all-or-nothing. `FolderByteStore` writes `<name>.tmp` in the same folder, flushes it to the device, then renames over the target; a leftover `.tmp` is never read or listed. `MemoryByteStore` is a map. `BrowserByteStore` (Host, browser only) reads and writes an in-memory copy that the page loaded from IndexedDB before the program started and queues one IndexedDB transaction per write (`BrowserFiles`, `onegame-platform.js`: `storageOpen`, `kv*`). `BrowserByteStore.Pending` counts uncommitted writes; the web control channel's `flush` operation waits for 0.
- Default choice (`GameStorage.Create`): `GameOptions.Storage`, then `GameDefinition.CreateStore(game)`, then `--data`/`GameOptions.DataDirectory`/`ONEGAME_DATA_DIR` (a folder), then memory for a headless or `--no-present` run unless `--persist-data`, IndexedDB in a browser, else a folder under `Environment.SpecialFolder.LocalApplicationData` named after the game. On Android the platform host sets `HOME` to the app's files directory and on iOS `HOME` is the app container, so the same folder is app storage.

## Slot file

`slot-<name>.sav`: magic `GOSL`, container format (1), kind, saved-at (Unix ms), game version, label, tick, world hash, then the `SaveFile` bytes (header with seed, tick, hashes and per-type schema versions, then the snapshot). `SaveSlots.TryParse` reads it from untrusted bytes: every length is checked against the bytes present, label ≤ 128 bytes, version ≤ 64, the inner save's header must parse. `Load` runs `SaveFile.Load` (payload hash, seed check, migrations for older schemas, hash check when nothing migrated). `MaxSaveBytes` (default 64 MiB, `GameDefinition.MaxSaveBytes`) is applied on read and write.

Results are typed: `SlotStatus` = `Ok`, `NoSuchSlot`, `InvalidName`, `WriteFailed`, `TooLarge`, `BadSlotFile`, `UnsupportedFormat`, `SaveRejected` (with the loader's `LoadResult`). `GameStorage.Load` builds a fresh world through `GameInstance.TryLoadSnapshot` and swaps it in only on success. `SaveSlots.Load(slot, World)` loads in place and leaves the world unspecified on failure. `Autosave` rotates `auto-0..auto-(n-1)` (default 3): first free, else oldest.

## Settings file

magic `OGST`, format (1), your schema version, up to 512 fields of (key, kind, value) with kinds whole (varint), flag, number (float bits), text (UTF-8); ≤ 64 KiB. Fields are declared with `Whole/Flag/Number/Text/Choice<TEnum>` (ranges, text limit; no reflection). `Load` never throws: a damaged file gives defaults and a `Reason` (`Status = Defaulted`); a bad field gives its default and a note; unknown keys are noted and ignored; no file is `Missing`. `ISettingsFile` lets tools list the settings a game opened.

## Harness

- Operation `saves` (`ControlSurface.Saves.cs`): `list`, `save`, `load`, `quick-save`, `quick-load`, `autosave`, `load-latest`, `delete`, `settings-load`, `settings-save`, `write-file`, `delete-file`. The report carries slots, files, settings (status, reason, notes, text), `pending`, tick and hash. Failures are errors coded by `SlotStatus` (`no_such_slot`, `bad_slot_file`, `save_rejected`, ...).
- Scenarios: step `saves:` (an action, or a map with `action`, `slot`, `label`, `file`, `name`, `data`) and expectation `saves:` (`slots`, `has`, `lacks`, `latest`, `files`, `settings`, `slot: {name, label, kind, tick, game-version}`).
- `TestOptions.Storage` hands a test cluster a shared store to model a restart. `onegame web test --profile <folder> --port N` keeps the browser profile and origin between runs (IndexedDB belongs to an origin, port included) and waits for pending writes before closing the page.

## Fuzz

`storage-slot` (slot file through `List`, `TryParse` and `Load`) and `storage-settings` (`SettingsFile.Apply`) in `tests/OneGame.Sync.Tests/Fuzz/SyncFuzzTargets.cs`; seeds in `testdata/fuzz-sync/storage-*`.

## Not built

Cloud sync as a store beyond the sample's Steam Cloud adapter; encryption or signing of saves; save thumbnails; a world save that includes view state; per-field settings migration beyond `SettingsLoad.FileVersion`; Android and iOS device runs of the folder store (the abstraction is tested; the folder is the app's storage by the platform hosts' `HOME`).
