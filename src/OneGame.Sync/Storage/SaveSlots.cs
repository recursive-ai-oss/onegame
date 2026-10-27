using System.Text;
using OneGame.Core;
using OneGame.Sync;

namespace OneGame.Storage;

/// <summary>Why a slot was written.</summary>
public enum SlotKind : byte
{
    /// <summary>The player chose to save.</summary>
    Manual = 0,

    /// <summary>The game saved by itself (see <see cref="SaveSlots.Autosave(World, string)"/>).</summary>
    Auto = 1,

    /// <summary>A quick save (see <see cref="SaveSlots.QuickSave(World, string)"/>).</summary>
    Quick = 2,
}

/// <summary>The small description of a slot a load screen shows, read without restoring the world.</summary>
/// <param name="Slot">The slot's name.</param>
/// <param name="Label">The text the game gave it (a level name, "Day 12"), up to <see cref="SaveSlots.MaxLabelBytes"/> UTF-8 bytes.</param>
/// <param name="Kind">Why it was written.</param>
/// <param name="SavedAt">When it was written, by the clock of the <see cref="SaveSlotOptions.Time"/> provider (UTC).</param>
/// <param name="GameVersion">The <see cref="SaveSlotOptions.GameVersion"/> of the build that wrote it.</param>
/// <param name="Tick">The world's completed tick count at that moment.</param>
/// <param name="WorldHash">The world hash at that moment.</param>
/// <param name="Bytes">The size of the slot file.</param>
public sealed record SlotInfo(string Slot, string Label, SlotKind Kind, DateTimeOffset SavedAt, string GameVersion, long Tick, ulong WorldHash, int Bytes);

/// <summary>How a slot operation ended.</summary>
public enum SlotStatus
{
    /// <summary>It worked.</summary>
    Ok = 0,

    /// <summary>There is no slot of that name.</summary>
    NoSuchSlot,

    /// <summary>The slot name is not allowed (see <see cref="SaveSlots.IsValidSlotName"/>).</summary>
    InvalidName,

    /// <summary>The store could not write the file. The slot's earlier content, if any, is intact.</summary>
    WriteFailed,

    /// <summary>The slot file is larger than <see cref="SaveSlotOptions.MaxSaveBytes"/>.</summary>
    TooLarge,

    /// <summary>The file is not a slot, or is cut short or damaged.</summary>
    BadSlotFile,

    /// <summary>The slot file's format is newer than this build reads.</summary>
    UnsupportedFormat,

    /// <summary>The save inside was refused by the world (see <see cref="SlotResult.Load"/>). The world is unspecified: discard it.</summary>
    SaveRejected,
}

/// <summary>The outcome of a slot operation.</summary>
/// <param name="Status">How it ended.</param>
/// <param name="Info">The slot's description, when it could be read or was written.</param>
/// <param name="Load">What the save loader found, for loads that reached it.</param>
/// <param name="Detail">A sentence for logs when something went wrong.</param>
public sealed record SlotResult(SlotStatus Status, SlotInfo? Info, LoadResult? Load, string Detail)
{
    /// <summary>True when <see cref="Status"/> is <see cref="SlotStatus.Ok"/>.</summary>
    public bool Ok => Status == SlotStatus.Ok;
}

/// <summary>Options for <see cref="SaveSlots"/>.</summary>
public sealed class SaveSlotOptions
{
    /// <summary>Your game's version text, recorded in each slot (at most 64 characters). Informational: migrations key off entity schema versions.</summary>
    public string GameVersion { get; init; } = string.Empty;

    /// <summary>The clock that stamps slots. Replace it in tests to get the same bytes every run.</summary>
    public TimeProvider Time { get; init; } = TimeProvider.System;

    /// <summary>The largest slot file read or written, in bytes (default 64 MiB). Checked before anything is allocated.</summary>
    public int MaxSaveBytes { get; init; } = 64 * 1024 * 1024;

    /// <summary>How many rotating autosave slots there are (default 3).</summary>
    public int AutosaveSlots { get; init; } = 3;
}

/// <summary>
/// Named save slots over a <see cref="IByteStore"/>: each slot is one file holding a small description and a world save
/// (<see cref="SaveFile"/>, so older saves load through the entity migrations). Writes are atomic through the store, so a slot is
/// always either the old save or the new one.
/// </summary>
/// <remarks>
/// Call these from the host or from view code, between ticks. They do I/O and read the wall clock, which sim code must never do.
/// </remarks>
public sealed class SaveSlots
{
    /// <summary>The longest label, in UTF-8 bytes. Longer labels are cut.</summary>
    public const int MaxLabelBytes = 128;

    /// <summary>The longest slot name.</summary>
    public const int MaxSlotNameLength = 48;

    /// <summary>The slot <see cref="QuickSave(World, string)"/> writes.</summary>
    public const string QuickSlot = "quick";

    private const uint Magic = 0x4C534F47; // "GOSL"
    private const uint Format = 1;
    private const string Prefix = "slot-";
    private const string Suffix = ".sav";
    private const string AutoPrefix = "auto-";

    private readonly IByteStore _store;
    private readonly SaveSlotOptions _options;

    /// <summary>Creates slots in <paramref name="store"/>.</summary>
    public SaveSlots(IByteStore store, SaveSlotOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
        _options = options ?? new SaveSlotOptions();
        if (_options.GameVersion.Length > 64)
        {
            throw new ArgumentException("The game version is at most 64 characters.", nameof(options));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(_options.AutosaveSlots, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(_options.MaxSaveBytes, 1024);
    }

    /// <summary>The store the slots live in.</summary>
    public IByteStore Store => _store;

    /// <summary>True when <paramref name="slot"/> is a usable name: 1 to 48 ASCII letters, digits, <c>_</c> or <c>-</c>.</summary>
    public static bool IsValidSlotName(string? slot)
    {
        if (string.IsNullOrEmpty(slot) || slot.Length > MaxSlotNameLength)
        {
            return false;
        }

        foreach (var c in slot)
        {
            if (!(char.IsAsciiLetterOrDigit(c) || c is '_' or '-'))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The slot name of autosave number <paramref name="index"/>.</summary>
    public static string AutosaveName(int index) => AutoPrefix + index.ToString(System.Globalization.CultureInfo.InvariantCulture);

    // ---- writing ----

    /// <summary>Saves <paramref name="world"/> into <paramref name="slot"/>, replacing what was there.</summary>
    public SlotResult Save(string slot, World world, string label = "", SlotKind kind = SlotKind.Manual)
    {
        ArgumentNullException.ThrowIfNull(world);
        return Write(slot, label, kind, () => SaveFile.Write(world));
    }

    /// <summary>Saves an authority scope's world into <paramref name="slot"/> (this also lets <c>Durability.AfterSave</c> release held answers).</summary>
    public SlotResult Save(string slot, AuthorityScope scope, string label = "", SlotKind kind = SlotKind.Manual)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return Write(slot, label, kind, scope.Save);
    }

    /// <summary>
    /// Puts a save you already hold (for example one read from another store, or written by an earlier version of the game) into a slot.
    /// The bytes must be a valid <see cref="SaveFile"/>; they are not loaded.
    /// </summary>
    public SlotResult Import(string slot, byte[] save, string label = "", SlotKind kind = SlotKind.Manual)
    {
        ArgumentNullException.ThrowIfNull(save);
        return Write(slot, label, kind, () => save);
    }

    /// <summary>Saves <paramref name="world"/> into the <see cref="QuickSlot"/>.</summary>
    public SlotResult QuickSave(World world, string label = "") => Save(QuickSlot, world, label, SlotKind.Quick);

    /// <summary>Saves an authority scope into the <see cref="QuickSlot"/>.</summary>
    public SlotResult QuickSave(AuthorityScope scope, string label = "") => Save(QuickSlot, scope, label, SlotKind.Quick);

    /// <summary>
    /// Writes an autosave into the rotating slots <c>auto-0</c>, <c>auto-1</c>…: the first free one, else the oldest. A crash during
    /// the write costs at most that one slot; the newer autosaves are untouched.
    /// </summary>
    public SlotResult Autosave(World world, string label = "")
    {
        ArgumentNullException.ThrowIfNull(world);
        return Save(NextAutosaveSlot(), world, label, SlotKind.Auto);
    }

    /// <summary>Writes an autosave of an authority scope (see <see cref="Autosave(World, string)"/>).</summary>
    public SlotResult Autosave(AuthorityScope scope, string label = "")
    {
        ArgumentNullException.ThrowIfNull(scope);
        return Save(NextAutosaveSlot(), scope, label, SlotKind.Auto);
    }

    /// <summary>The newest slot of any kind, or null when there are none (what a "Continue" button loads).</summary>
    public SlotInfo? Latest()
    {
        var all = List();
        return all.Count == 0 ? null : all[0];
    }

    // ---- reading ----

    /// <summary>Describes every slot that can be read, newest first (ties by name). Damaged slot files are left out; see <see cref="TryGetInfo"/> for why one is.</summary>
    public IReadOnlyList<SlotInfo> List()
    {
        var infos = new List<SlotInfo>();
        foreach (var file in _store.List(Prefix))
        {
            if (!file.EndsWith(Suffix, StringComparison.Ordinal))
            {
                continue;
            }

            var slot = file.Substring(Prefix.Length, file.Length - Prefix.Length - Suffix.Length);
            if (IsValidSlotName(slot) && TryGetInfo(slot, out var info).Ok)
            {
                infos.Add(info!);
            }
        }

        infos.Sort(static (a, b) =>
        {
            var c = b.SavedAt.CompareTo(a.SavedAt);
            return c != 0 ? c : string.CompareOrdinal(a.Slot, b.Slot);
        });
        return infos;
    }

    /// <summary>Reads one slot's description.</summary>
    public SlotResult TryGetInfo(string slot, out SlotInfo? info)
    {
        var read = ReadFile(slot, out var file);
        info = null;
        if (!read.Ok)
        {
            return read;
        }

        var status = TryParse(file, slot, out info, out _);
        return status == SlotStatus.Ok ? new SlotResult(SlotStatus.Ok, info, null, string.Empty) : new SlotResult(status, null, null, Describe(status, slot));
    }

    /// <summary>True when the slot exists and can be read.</summary>
    public bool Exists(string slot) => TryGetInfo(slot, out _).Ok;

    /// <summary>
    /// Reads a slot's save bytes (for <c>GameInstance.LoadSnapshot</c>, which builds a fresh world and swaps it in only when the load
    /// works). Nothing is restored here.
    /// </summary>
    public SlotResult ReadSave(string slot, out byte[] save)
    {
        save = [];
        var read = ReadFile(slot, out var file);
        if (!read.Ok)
        {
            return read;
        }

        var status = TryParse(file, slot, out var info, out var payload);
        if (status != SlotStatus.Ok)
        {
            return new SlotResult(status, null, null, Describe(status, slot));
        }

        save = payload.ToArray();
        return new SlotResult(SlotStatus.Ok, info, null, string.Empty);
    }

    /// <summary>
    /// Replaces the contents of <paramref name="world"/> with the slot's save, migrating entity types saved at an older schema. If the
    /// save is refused part-way the world is unspecified: build it fresh and discard it.
    /// </summary>
    public SlotResult Load(string slot, World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var read = ReadSave(slot, out var save);
        if (!read.Ok)
        {
            return read;
        }

        return Finish(read.Info!, SaveFile.Load(save, world));
    }

    /// <summary>Replaces an authority scope's state with the slot's save; subscribers get a fresh snapshot (see <see cref="AuthorityScope.Load"/>).</summary>
    public SlotResult Load(string slot, AuthorityScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var read = ReadSave(slot, out var save);
        if (!read.Ok)
        {
            return read;
        }

        return Finish(read.Info!, scope.Load(save));
    }

    /// <summary>Deletes a slot. Returns true when it is gone afterwards.</summary>
    public bool Delete(string slot) => IsValidSlotName(slot) && _store.Delete(FileOf(slot));

    // ---- the file ----

    /// <summary>
    /// Reads a slot file from untrusted bytes: the description plus a view of the save inside. Lengths are checked against the bytes
    /// present before anything is allocated, and the inner save's header is checked too.
    /// </summary>
    /// <param name="file">The slot file.</param>
    /// <param name="slot">The slot's name, copied into the description.</param>
    /// <param name="info">The description, or null.</param>
    /// <param name="save">The save bytes (a view into <paramref name="file"/>).</param>
    public static SlotStatus TryParse(ReadOnlySpan<byte> file, string slot, out SlotInfo? info, out ReadOnlySpan<byte> save)
    {
        info = null;
        save = default;
        var r = new SyncReader(file);
        if (r.ReadFixed32() != Magic || !r.Ok)
        {
            return SlotStatus.BadSlotFile;
        }

        if (r.ReadVarUInt() != Format)
        {
            return r.Ok ? SlotStatus.UnsupportedFormat : SlotStatus.BadSlotFile;
        }

        var kind = r.ReadByte();
        var savedMs = r.ReadVarInt();
        var version = r.ReadString();
        var label = r.ReadString();
        var tick = r.ReadVarInt();
        var hash = r.ReadFixed64();
        var body = r.ReadBytes(int.MaxValue);
        r.ExpectEnd();
        if (!r.Ok || kind > (byte)SlotKind.Quick || tick < 0 || savedMs is < -62_135_596_800_000L or > 253_402_300_799_999L
            || version.Length > 64 || Encoding.UTF8.GetByteCount(label) > MaxLabelBytes)
        {
            return SlotStatus.BadSlotFile;
        }

        if (SaveFile.TryReadHeader(body, out _, out _, out _) != SaveStatus.Ok)
        {
            return SlotStatus.BadSlotFile;
        }

        info = new SlotInfo(slot, label, (SlotKind)kind, DateTimeOffset.FromUnixTimeMilliseconds(savedMs), version, tick, hash, file.Length);
        save = body;
        return SlotStatus.Ok;
    }

    private static string FileOf(string slot) => Prefix + slot + Suffix;

    private SlotResult Write(string slot, string label, SlotKind kind, Func<byte[]> makeSave)
    {
        if (!IsValidSlotName(slot))
        {
            return new SlotResult(SlotStatus.InvalidName, null, null, $"'{slot}' is not a slot name: use 1 to {MaxSlotNameLength} letters, digits, '_' or '-'.");
        }

        var save = makeSave();
        if (SaveFile.TryReadHeader(save, out var header, out _, out _) != SaveStatus.Ok || header is null)
        {
            return new SlotResult(SlotStatus.BadSlotFile, null, null, "The world produced a save that cannot be read back.");
        }

        var cut = CutLabel(label ?? string.Empty);
        var savedAt = _options.Time.GetUtcNow();
        var w = new SyncWriter(save.Length + 128);
        w.WriteFixed32(Magic);
        w.WriteVarUInt(Format);
        w.WriteByte((byte)kind);
        w.WriteVarInt(savedAt.ToUnixTimeMilliseconds());
        w.WriteString(_options.GameVersion);
        w.WriteString(cut);
        w.WriteVarInt(header.Tick);
        w.WriteFixed64(header.WorldHash);
        w.WriteBytes(save);
        if (w.Length > _options.MaxSaveBytes)
        {
            return new SlotResult(SlotStatus.TooLarge, null, null, $"The save is {w.Length} bytes; the limit is {_options.MaxSaveBytes}.");
        }

        if (!_store.Write(FileOf(slot), w.Written))
        {
            return new SlotResult(SlotStatus.WriteFailed, null, null, $"The store ({_store.Description}) could not write slot '{slot}'; any earlier save in it is unchanged.");
        }

        var info = new SlotInfo(slot, cut, kind, DateTimeOffset.FromUnixTimeMilliseconds(savedAt.ToUnixTimeMilliseconds()), _options.GameVersion, header.Tick, header.WorldHash, w.Length);
        return new SlotResult(SlotStatus.Ok, info, null, string.Empty);
    }

    private SlotResult ReadFile(string slot, out byte[] file)
    {
        file = [];
        if (!IsValidSlotName(slot))
        {
            return new SlotResult(SlotStatus.InvalidName, null, null, $"'{slot}' is not a slot name.");
        }

        if (!_store.TryRead(FileOf(slot), _options.MaxSaveBytes, out file))
        {
            // A store answers false for "missing", "unreadable" and "too big"; the listing tells the first from the others.
            foreach (var name in _store.List(FileOf(slot)))
            {
                if (name == FileOf(slot))
                {
                    return new SlotResult(SlotStatus.TooLarge, null, null, $"Slot '{slot}' could not be read (unreadable or larger than {_options.MaxSaveBytes} bytes).");
                }
            }

            return new SlotResult(SlotStatus.NoSuchSlot, null, null, $"There is no slot named '{slot}'.");
        }

        return new SlotResult(SlotStatus.Ok, null, null, string.Empty);
    }

    private static SlotResult Finish(SlotInfo info, LoadResult load) =>
        load.Status == SaveStatus.Ok
            ? new SlotResult(SlotStatus.Ok, info, load, string.Empty)
            : new SlotResult(SlotStatus.SaveRejected, info, load, $"The save in slot '{info.Slot}' was refused: {load.Status} ({load.Detail}).");

    private static string Describe(SlotStatus status, string slot) => status switch
    {
        SlotStatus.UnsupportedFormat => $"Slot '{slot}' was written by a newer version of the game.",
        _ => $"Slot '{slot}' is not a readable save file.",
    };

    private string NextAutosaveSlot()
    {
        SlotInfo?[] found = new SlotInfo?[_options.AutosaveSlots];
        for (var i = 0; i < found.Length; i++)
        {
            TryGetInfo(AutosaveName(i), out found[i]);
        }

        var pick = 0;
        for (var i = 0; i < found.Length; i++)
        {
            if (found[i] is null)
            {
                return AutosaveName(i);
            }

            if (found[i]!.SavedAt < found[pick]!.SavedAt)
            {
                pick = i;
            }
        }

        return AutosaveName(pick);
    }

    private static string CutLabel(string label)
    {
        if (Encoding.UTF8.GetByteCount(label) <= MaxLabelBytes)
        {
            return label;
        }

        // Cut on a character boundary so the label stays valid UTF-8.
        var end = Math.Min(label.Length, MaxLabelBytes);
        while (end > 0 && Encoding.UTF8.GetByteCount(label.AsSpan(0, end)) > MaxLabelBytes)
        {
            end--;
        }

        if (end > 0 && char.IsHighSurrogate(label[end - 1]))
        {
            end--;
        }

        return label[..end];
    }
}
