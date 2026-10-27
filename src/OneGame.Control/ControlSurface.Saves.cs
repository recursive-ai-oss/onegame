using System.Globalization;
using OneGame.Host;
using OneGame.Storage;

namespace OneGame.Control;

/// <summary>One save slot in the <c>saves</c> report.</summary>
/// <param name="Slot">The slot name.</param>
/// <param name="Label">The game's label.</param>
/// <param name="Kind"><c>manual</c>, <c>auto</c> or <c>quick</c>.</param>
/// <param name="SavedAt">When it was written, ISO 8601 UTC.</param>
/// <param name="GameVersion">The game version that wrote it.</param>
/// <param name="Tick">The world tick when saved.</param>
/// <param name="WorldHash">The world hash when saved, 16 hex digits.</param>
/// <param name="Bytes">The slot file's size.</param>
public sealed record SlotEntry(string Slot, string Label, string Kind, string SavedAt, string GameVersion, long Tick, string WorldHash, int Bytes);

/// <summary>One settings file in the <c>saves</c> report.</summary>
/// <param name="File">The file name in the store.</param>
/// <param name="Status"><c>loaded</c>, <c>missing</c> or <c>defaulted</c>.</param>
/// <param name="Reason">Why everything is at its default, or empty.</param>
/// <param name="Notes">One line per field that took its default.</param>
/// <param name="Text">The current values, one <c>key = value</c> line each.</param>
public sealed record SettingsEntry(string File, string Status, string Reason, string[] Notes, string Text);

/// <summary>Result of the <c>saves</c> operation.</summary>
/// <param name="Store">Where the files live.</param>
/// <param name="Slots">Every readable slot, newest first.</param>
/// <param name="Files">Every file in the store.</param>
/// <param name="Settings">The settings files the game opened.</param>
/// <param name="Pending">Browser writes not yet committed to IndexedDB (0 elsewhere).</param>
/// <param name="Tick">The world's tick.</param>
/// <param name="Hash">The world's hash.</param>
/// <param name="Loaded">The slot the action loaded or saved, when it did.</param>
/// <param name="Migrated">True when the load ran a migration (so the hash cannot equal the saved one).</param>
public sealed record SavesResult(string Store, SlotEntry[] Slots, string[] Files, SettingsEntry[] Settings, int Pending, long Tick, string Hash, string? Loaded, bool Migrated);

public sealed partial class ControlSurface
{
    private SavesResult SavesOp(Args a)
    {
        var storage = Game.Storage;
        var action = a.Str("action") ?? "list";
        string? slot = null;
        var migrated = false;
        switch (action)
        {
            case "list":
                break;
            case "save":
                slot = a.Required("slot");
                Check(storage.Save(slot, a.Str("label") ?? string.Empty));
                break;
            case "quick-save":
                slot = SaveSlots.QuickSlot;
                Check(storage.QuickSave(a.Str("label") ?? string.Empty));
                break;
            case "autosave":
                slot = Check(storage.Autosave(a.Str("label") ?? string.Empty)).Info!.Slot;
                break;
            case "load":
                slot = a.Required("slot");
                migrated = Check(storage.Load(slot)).Load!.Migrated;
                break;
            case "quick-load":
                slot = SaveSlots.QuickSlot;
                migrated = Check(storage.QuickLoad()).Load!.Migrated;
                break;
            case "load-latest":
                var latest = Check(storage.LoadLatest());
                slot = latest.Info!.Slot;
                migrated = latest.Load!.Migrated;
                break;
            case "delete":
                if (!storage.Slots.Delete(a.Required("slot")))
                {
                    throw new ControlException("write_failed", "The store could not delete the slot.");
                }

                break;
            case "settings-load":
                foreach (var s in SettingsNamed(a))
                {
                    s.Load();
                }

                break;
            case "settings-save":
                foreach (var s in SettingsNamed(a))
                {
                    if (!s.Save())
                    {
                        throw new ControlException("write_failed", $"The store could not write {s.FileName}.");
                    }
                }

                break;
            case "write-file":
                var bytes = Convert.FromBase64String(a.Required("data"));
                if (bytes.Length > 1024 * 1024)
                {
                    throw new ControlException("too_large", "write-file takes at most 1 MiB.");
                }

                if (!StoreNames.IsValid(a.Required("name")))
                {
                    throw new ControlException("invalid_name", "The name is not a valid store file name.");
                }

                if (!storage.Files.Write(a.Required("name"), bytes))
                {
                    throw new ControlException("write_failed", "The store could not write the file.");
                }

                break;
            case "delete-file":
                if (!StoreNames.IsValid(a.Required("name")) || !storage.Files.Delete(a.Required("name")))
                {
                    throw new ControlException("write_failed", "The store could not delete the file.");
                }

                break;
            default:
                throw new ControlException("bad_args", $"Unknown saves action '{action}'. Actions: list, save, load, quick-save, quick-load, autosave, load-latest, delete, settings-load, settings-save, write-file, delete-file.");
        }

        var slots = storage.Slots.List().Select(i => new SlotEntry(i.Slot, i.Label, i.Kind.ToString().ToLowerInvariant(), i.SavedAt.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture), i.GameVersion, i.Tick, i.WorldHash.ToString("x16", CultureInfo.InvariantCulture), i.Bytes)).ToArray();
        var settings = storage.SettingsFiles.Select(s =>
        {
            var l = s.LastLoad;
            return new SettingsEntry(s.FileName, (l?.Status ?? SettingsStatus.Missing).ToString().ToLowerInvariant(), l?.Reason ?? string.Empty, [.. l?.Notes ?? []], s.ToText());
        }).ToArray();
        var pending = OperatingSystem.IsBrowser() ? BrowserPending() : 0;
        return new SavesResult(storage.Files.Description, slots, [.. storage.Files.List()], settings, pending, Game.World.Tick, HashText(), slot, migrated);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("browser")]
    private static int BrowserPending() => BrowserByteStore.Pending;

    private List<ISettingsFile> SettingsNamed(Args a)
    {
        var file = a.Str("file");
        var all = Game.Storage.SettingsFiles.Where(s => file is null || s.FileName == file).ToList();
        return all.Count > 0 || file is null ? all : throw new ControlException("not_found", $"The game opened no settings file named '{file}'. Opened: {string.Join(", ", Game.Storage.SettingsFiles.Select(s => s.FileName))}.");
    }

    private static SlotResult Check(SlotResult result)
    {
        if (result.Ok)
        {
            return result;
        }

        var code = result.Status switch
        {
            SlotStatus.NoSuchSlot => "no_such_slot",
            SlotStatus.InvalidName => "invalid_name",
            SlotStatus.WriteFailed => "write_failed",
            SlotStatus.TooLarge => "too_large",
            SlotStatus.BadSlotFile => "bad_slot_file",
            SlotStatus.UnsupportedFormat => "unsupported_format",
            _ => "save_rejected",
        };
        throw new ControlException(code, result.Detail);
    }
}
