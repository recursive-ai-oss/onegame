# Saves and settings

`game.Storage` keeps what a player wants back: **save slots** (a whole world, with a label for the load screen), **settings** (a typed object of the player's choices) and plain **files**. Writes are atomic, a damaged file is refused with a reason instead of a crash, and the same code runs on desktop, Android, iOS and in a browser. The types are in `OneGame.Storage` (plus `OneGame.Sync` for `SaveSlots` on other worlds).

## Save and load a slot

```csharp
var saved = game.Storage.Save("slot1", "Day 12: the market");
if (!saved.Ok)
{
    Console.WriteLine(saved.Detail);          // "The store ... could not write slot 'slot1'; any earlier save in it is unchanged."
}

var loaded = game.Storage.Load("slot1");     // swaps the running world; a damaged slot changes nothing
if (loaded.Status == SlotStatus.NoSuchSlot)
{
    Console.WriteLine("Nothing saved yet");
}
```

A slot is a whole world save plus a small description. `Load` builds a fresh world and swaps it in only when the save is good, so a damaged or refused slot leaves the running game untouched. A slot written by an older version of your game loads through your entity `[Migrate]` methods; `SlotResult.Load.Migrated` tells you it did.

Slot names are 1 to 48 ASCII letters, digits, `_` or `-`. Labels are cut at 128 UTF-8 bytes.

## A load screen

```csharp
foreach (var slot in game.Storage.Slots.List())      // newest first; damaged slot files are left out
{
    Console.WriteLine($"{slot.Slot}: {slot.Label} ({slot.Kind}, {slot.SavedAt:g}, game {slot.GameVersion}, tick {slot.Tick})");
}
```

`SlotInfo` holds `Slot`, `Label`, `Kind` (`Manual`, `Auto`, `Quick`), `SavedAt` (UTC), `GameVersion` (override `GameDefinition.Version`), `Tick`, `WorldHash` and `Bytes`. It is read from the slot file's header, without restoring the world.

## Quick save and autosave

```csharp
game.Storage.QuickSave();                // the "quick" slot
game.Storage.Autosave("Level 3");        // rotates over auto-0, auto-1, auto-2: a crash costs at most one
game.Storage.QuickLoad();
game.Storage.LoadLatest();               // what a Continue button does
```

## Settings

Declare the settings once as a class plus a schema. The defaults are whatever a new object holds. There is no reflection, so it works in AOT builds and on iOS.

```csharp
public enum Quality { Low, Medium, High }

public sealed class Options
{
    public int Volume { get; set; } = 7;
    public bool Fullscreen { get; set; }
    public string PlayerName { get; set; } = "player";
    public Quality Quality { get; set; } = Quality.Medium;

    public static SettingsSchema<Options> Schema { get; } = new SettingsSchema<Options>("options.bin")
        .Whole("volume", o => o.Volume, (o, v) => o.Volume = v, min: 0, max: 10)
        .Flag("fullscreen", o => o.Fullscreen, (o, v) => o.Fullscreen = v)
        .Text("player-name", o => o.PlayerName, (o, v) => o.PlayerName = v, maxLength: 24)
        .Choice("quality", o => o.Quality, (o, v) => o.Quality = v);
}
```

Open them when the game starts and save after a change:

```csharp
var settings = game.Storage.OpenSettings(Options.Schema);   // loads now; defaults if missing or damaged
if (!settings.LastLoad!.Clean)
{
    Console.WriteLine(settings.LastLoad.Reason);            // why, and settings.LastLoad.Notes for single fields
}

settings.Value.Volume = 4;
settings.Save();                                            // false if the store could not write
```

A missing file gives the defaults (`LastLoad.Status == Missing`). A file that is damaged, from another program or too large gives the defaults and a `Reason`, and is left alone until you save. A single field that is out of range, of the wrong type or too long takes its default and gets a line in `Notes`; the other fields keep what the player chose. A key the schema does not know (a setting you removed) is ignored and noted. Fields are `Whole`, `Flag`, `Number`, `Text` and `Choice<TEnum>`.

## Files

Under slots and settings is `IByteStore`: named blobs with four calls. Use it for anything else you keep (statistics, a profile).

```csharp
var files = game.Storage.Files;                             // an IByteStore
files.Write("profile.bin", bytes);                          // atomic: the old file or the new one, never half
if (files.TryRead("profile.bin", maxBytes: 16 * 1024, out var data))
{
    Console.WriteLine(data.Length);
}
```

Names are up to 96 ASCII letters, digits, `.`, `_` or `-`, and may not start with `.`. A read larger than `maxBytes` is refused before anything is copied. To keep files somewhere else (a cloud, a test), implement the interface and return it from `GameDefinition.CreateStore(game)` or set `GameOptions.Storage`:

```csharp
public sealed class LoggingStore(IByteStore inner, List<string> log) : IByteStore
{
    public string Description => "logging " + inner.Description;

    public bool TryRead(string name, int maxBytes, out byte[] data) => inner.TryRead(name, maxBytes, out data);

    public bool Write(string name, ReadOnlySpan<byte> data)
    {
        log.Add("write " + name);
        return inner.Write(name, data);
    }

    public bool Delete(string name) => inner.Delete(name);

    public IReadOnlyList<string> List(string prefix = "") => inner.List(prefix);
}
```

## Other worlds

`SaveSlots` works on any world or authority scope, not only the running game's:

```csharp
slots.Save("a", world, "a plain world");
slots.Save("b", scope, "an authority scope");     // also lets Durability.AfterSave release held answers
slots.Load("b", scope);                          // subscribers get a fresh snapshot
```

## Where the files go

| Where | Store |
|---|---|
| Desktop | a folder named after the game under the user's local application data (`~/.local/share/<game>`, `%LocalAppData%\<game>`, `~/Library/Application Support/<game>`) |
| Android, iOS | the same folder, inside the app's own storage |
| Browser | IndexedDB, loaded into memory before the program starts; a write is visible at once and committed in one transaction |
| Headless, `--no-present`, tests | memory, so a test never touches a player's files |

`--data <folder>` (or `ONEGAME_DATA_DIR`) points any run at a folder; `--persist-data` keeps a headless run on the platform store. A game can still pick its own store (`GameDefinition.CreateStore`).

## Rules

- Call it from the host or from view code (a button handler, a `TickCompleted` handler), **between ticks**. Saving reads the clock and does I/O, which sim code must not. Loading replaces the world, so never load from inside a command handler.
- A write that fails returns `false` / `SlotStatus.WriteFailed` and leaves the earlier file whole. In a browser a write is accepted at once and reaches IndexedDB a moment later; `BrowserByteStore.Pending` is the number still on their way.
- Everything read back is untrusted: lengths are checked against the bytes present and against limits before anything is allocated (slot files default to 64 MiB, `GameDefinition.MaxSaveBytes`; settings to 64 KiB).
- A save keeps the world, not your UI. Screens and view-only state are rebuilt after a load.

## From the harness

The `saves` control operation lists slots, files and settings, and saves, loads, deletes and plants files. Scenarios use the same:

```yaml
steps:
  - saves: { action: save, slot: before-boss, label: "Before the boss" }
  - command: { entity: Table, name: Draw }
  - step: 1
  - saves: { action: load, slot: before-boss }          # back to the saved world
  - saves: quick-save                                    # also: quick-load, autosave, load-latest, list
  - saves: { action: write-file, name: options.bin, data: AAEC }   # plant a damaged settings file (base64)
  - saves: settings-load
  - expect:
      saves:
        has: before-boss
        lacks: before-bos
        slots: 3
        latest: before-boss
        files: 2
        settings: "volume = 7"
        slot: { name: before-boss, label: "Before the boss", kind: manual, tick: 12 }
```

To prove a save outlives the process, run one scenario that saves and another that loads against the same folder (`SOLITAIRE_DATA=/tmp/x onegame test --scenario save` then `--scenario load`), or in a browser against one profile and port: `onegame web test --scenario save --profile /tmp/p --port 38417`, then again with `--scenario load`. `samples/solitaire/scenarios-persist` does both.
