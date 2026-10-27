namespace OneGame.Control;

public static partial class ControlOps
{
    private static ControlOp[] SaveOps =>
    [
        new("saves",
            "The game's save slots, files and settings as JSON, after an optional action: slots [{slot, label, kind, savedAt, gameVersion, tick, worldHash, bytes}] newest first, files (every file in the store), settings [{file, status, reason, notes, text}] for the settings files the game opened, store (where the files live), pending (browser writes not yet committed to IndexedDB), plus the world's tick and hash. action: list (default); save (slot, label); load (slot; swaps the running world for the slot's save, running entity migrations for older saves); quick-save; quick-load; autosave (next rotating slot); load-latest; delete (slot); settings-load and settings-save (file, default every file: read it again or write the current values); write-file (name, data as base64, at most 1 MiB; to plant a damaged file); delete-file (name). A failed action returns its status as the error code (no_such_slot, bad_slot_file, save_rejected, write_failed, invalid_name, too_large, unsupported_format) with the reason.",
            """{"type":"object","properties":{"action":{"type":"string","enum":["list","save","load","quick-save","quick-load","autosave","load-latest","delete","settings-load","settings-save","write-file","delete-file"]},"slot":{"type":"string"},"label":{"type":"string"},"file":{"type":"string"},"name":{"type":"string"},"data":{"type":"string"}}}""", true),
    ];
}
