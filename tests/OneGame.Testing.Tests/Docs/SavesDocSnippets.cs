using OneGame.Core;
using OneGame.Host;
using OneGame.Storage;
using OneGame.Sync;

namespace OneGame.Testing.Tests.Docs;

// The code blocks of docs/saves.md are copies of the lines below (SavesDocTests checks they match), so they compile and run.

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

/// <summary>A tiny custom store: the low level is one interface.</summary>
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

public static class SavesDocSnippets
{
    public static void SaveAndLoad(GameInstance game)
    {
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
    }

    public static void LoadScreen(GameInstance game)
    {
        foreach (var slot in game.Storage.Slots.List())      // newest first; damaged slot files are left out
        {
            Console.WriteLine($"{slot.Slot}: {slot.Label} ({slot.Kind}, {slot.SavedAt:g}, game {slot.GameVersion}, tick {slot.Tick})");
        }
    }

    public static void QuickAndAuto(GameInstance game)
    {
        game.Storage.QuickSave();                // the "quick" slot
        game.Storage.Autosave("Level 3");        // rotates over auto-0, auto-1, auto-2: a crash costs at most one
        game.Storage.QuickLoad();
        game.Storage.LoadLatest();               // what a Continue button does
    }

    public static void Settings(GameInstance game)
    {
        var settings = game.Storage.OpenSettings(Options.Schema);   // loads now; defaults if missing or damaged
        if (!settings.LastLoad!.Clean)
        {
            Console.WriteLine(settings.LastLoad.Reason);            // why, and settings.LastLoad.Notes for single fields
        }

        settings.Value.Volume = 4;
        settings.Save();                                            // false if the store could not write
    }

    public static void LowLevel(GameInstance game, ReadOnlySpan<byte> bytes)
    {
        var files = game.Storage.Files;                             // an IByteStore
        files.Write("profile.bin", bytes);                          // atomic: the old file or the new one, never half
        if (files.TryRead("profile.bin", maxBytes: 16 * 1024, out var data))
        {
            Console.WriteLine(data.Length);
        }
    }

    public static void OtherWorlds(SaveSlots slots, World world, AuthorityScope scope)
    {
        slots.Save("a", world, "a plain world");
        slots.Save("b", scope, "an authority scope");     // also lets Durability.AfterSave release held answers
        slots.Load("b", scope);                          // subscribers get a fresh snapshot
    }
}
