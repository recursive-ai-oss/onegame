using OneGame.Core;
using OneGame.Storage;
using OneGame.Sync;

namespace OneGame.Host;

/// <summary>
/// A game's saves and settings: the happy path over <see cref="IByteStore"/>, <see cref="SaveSlots"/> and <see cref="SettingsFile{T}"/>.
/// Reach it as <see cref="GameInstance.Storage"/>. Call it between ticks, from the host or from view code (a button handler), never
/// from sim code: it reads the clock and does I/O.
/// </summary>
public sealed class GameStorage
{
    private readonly GameInstance _game;

    internal GameStorage(GameInstance game)
    {
        _game = game;
        Files = game.Options.Storage ?? game.Definition.CreateStore(game) ?? Create(game.Definition.Name, game.Options);
        Slots = new SaveSlots(Files, new SaveSlotOptions { GameVersion = game.Definition.Version, MaxSaveBytes = game.Definition.MaxSaveBytes });
    }

    private readonly List<ISettingsFile> _settings = [];

    /// <summary>The store the game's files live in (the low level: named byte blobs).</summary>
    public IByteStore Files { get; }

    /// <summary>The slot manager over <see cref="Files"/> (the middle level: list, read and write slots of any world).</summary>
    public SaveSlots Slots { get; }

    /// <summary>
    /// Chooses the default store for a game. <c>ONEGAME_DATA_DIR</c> or <see cref="GameOptions.DataDirectory"/> picks a folder; otherwise
    /// a headless or unpresented run uses memory (unless <see cref="GameOptions.PersistData"/>), a browser uses IndexedDB, and the rest use a
    /// folder under the user's local application data.
    /// </summary>
    public static IByteStore Create(string appName, GameOptions options)
    {
        ArgumentException.ThrowIfNullOrEmpty(appName);
        ArgumentNullException.ThrowIfNull(options);
        var dir = options.DataDirectory is { Length: > 0 } given ? given : Environment.GetEnvironmentVariable("ONEGAME_DATA_DIR");
        if (!string.IsNullOrEmpty(dir))
        {
            return new FolderByteStore(dir);
        }

        if (OperatingSystem.IsBrowser())
        {
            return !options.PersistData && (options.Headless || !options.PresentFrames) ? new MemoryByteStore() : new BrowserByteStore(appName);
        }

        return !options.PersistData && (options.Headless || !options.PresentFrames) ? new MemoryByteStore() : new FolderByteStore(FolderByteStore.DefaultDirectory(appName));
    }

    // ---- save slots on the running game ----

    /// <summary>Saves the running world into <paramref name="slot"/>.</summary>
    public SlotResult Save(string slot, string label = "", SlotKind kind = SlotKind.Manual) => Slots.Save(slot, _game.World, label, kind);

    /// <summary>Saves into the quick-save slot.</summary>
    public SlotResult QuickSave(string label = "") => Slots.QuickSave(_game.World, label);

    /// <summary>Saves into the next rotating autosave slot.</summary>
    public SlotResult Autosave(string label = "") => Slots.Autosave(_game.World, label);

    /// <summary>
    /// Replaces the running world with a slot's save. The world is built fresh and swapped in only when the load works, so a damaged or
    /// refused slot leaves the running game as it was. Older saves go through the entity migrations (<see cref="LoadResult.Migrated"/>).
    /// </summary>
    public SlotResult Load(string slot)
    {
        var read = Slots.ReadSave(slot, out var save);
        if (!read.Ok)
        {
            return read;
        }

        var load = _game.TryLoadSnapshot(save);
        return load.Status == SaveStatus.Ok
            ? new SlotResult(SlotStatus.Ok, read.Info, load, string.Empty)
            : new SlotResult(SlotStatus.SaveRejected, read.Info, load, $"The save in slot '{slot}' was refused: {load.Status} ({load.Detail}).");
    }

    /// <summary>Loads the quick-save slot.</summary>
    public SlotResult QuickLoad() => Load(SaveSlots.QuickSlot);

    /// <summary>Loads the newest slot of any kind (a "Continue" button). <see cref="SlotStatus.NoSuchSlot"/> when there are none.</summary>
    public SlotResult LoadLatest() =>
        Slots.Latest() is { } latest ? Load(latest.Slot) : new SlotResult(SlotStatus.NoSuchSlot, null, null, "There are no saved games.");

    // ---- settings ----

    /// <summary>The settings files opened through <see cref="OpenSettings{T}"/>, in the order they were opened (the harness lists them).</summary>
    public IReadOnlyList<ISettingsFile> SettingsFiles => _settings;

    /// <summary>
    /// Opens a typed settings file in this game's store and loads it. <see cref="SettingsFile{T}.Value"/> holds the player's choices, or
    /// the defaults when the file is missing or damaged; the returned load result says which and why. Call <see cref="SettingsFile{T}.Save"/> after changes.
    /// </summary>
    public SettingsFile<T> OpenSettings<T>(SettingsSchema<T> schema)
        where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(schema);
        var file = new SettingsFile<T>(Files, schema);
        file.Load();
        _settings.Add(file);
        return file;
    }
}
