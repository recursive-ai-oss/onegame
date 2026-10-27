using OneGame.Control;
using OneGame.Host;
using OneGame.Storage;

namespace OneGame.AotSmoke;

/// <summary>Settings, a save slot and a folder store, through the game's storage service and the <c>saves</c> operation, so the native publish compiles them.</summary>
internal static class StorageSmoke
{
    private sealed class Prefs
    {
        public int Volume { get; set; } = 5;

        public bool Loud { get; set; }

        public float Gain { get; set; } = 1f;

        public string Name { get; set; } = "me";

        public Mode Mode { get; set; } = Mode.Calm;
    }

    private enum Mode
    {
        Calm = 0,
        Wild = 1,
    }

    private static readonly SettingsSchema<Prefs> Schema = new SettingsSchema<Prefs>("prefs.bin")
        .Whole("volume", p => p.Volume, (p, v) => p.Volume = v, 0, 10)
        .Flag("loud", p => p.Loud, (p, v) => p.Loud = v)
        .Number("gain", p => p.Gain, (p, v) => p.Gain = v, 0f, 2f)
        .Text("name", p => p.Name, (p, v) => p.Name = v, 16)
        .Choice("mode", p => p.Mode, (p, v) => p.Mode = v);

    public static string? Run()
    {
        var dir = Path.Combine(Path.GetTempPath(), "aot-storage-" + Environment.ProcessId);
        try
        {
            using var game = GameInstance.Create(new SmokeGame(), new GameOptions { Headless = true, Gpu = GpuMode.Off, Storage = new FolderByteStore(dir) });
            var prefs = game.Storage.OpenSettings(Schema);
            prefs.Value.Volume = 9;
            prefs.Value.Mode = Mode.Wild;
            if (!prefs.Save() || !game.Storage.Save("smoke", "aot").Ok)
            {
                return "storage write failed";
            }

            var hash = game.World.ComputeHash();
            game.Step(3);
            var loaded = game.Storage.Load("smoke");
            if (!loaded.Ok || game.World.ComputeHash() != hash || game.Storage.Slots.List().Count != 1)
            {
                return "slot did not round-trip: " + loaded.Status;
            }

            var again = new SettingsFile<Prefs>(new FolderByteStore(dir), Schema);
            if (!again.Load().Clean || again.Value.Volume != 9 || again.Value.Mode != Mode.Wild)
            {
                return "settings did not round-trip";
            }

            var surface = new ControlSurface(game, dir);
            return surface.Call("saves", """{"action":"list"}""").Contains("\"smoke\"", StringComparison.Ordinal) ? null : "saves operation did not list the slot";
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
