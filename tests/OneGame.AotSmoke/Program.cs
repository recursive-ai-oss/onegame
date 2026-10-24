using OneGame.AotSmoke;
using OneGame.Audio;
using OneGame.Control;
using OneGame.Core;
using OneGame.Gpu;
using OneGame.Gpu.Vulkan;
using OneGame.Platform;
using OneGame.Render2D;
using OneGame.Steam;
using OneGame.Sync;
using OneGame.Testing;
using OneGame.UI;

// Touches every runtime assembly so the native publish has to compile and trim all of them.
string[] names =
[
    CoreModule.Name,
    SyncModule.Name,
    GpuModule.Name,
    VulkanModule.Name,
    PlatformModule.Name,
    Render2DModule.Name,
    UIModule.Name,
    AudioModule.Name,
    SteamModule.Name,
    ControlModule.Name,
    TestingModule.Name,
];

foreach (var name in names)
{
    if (!name.StartsWith("OneGame.", StringComparison.Ordinal))
    {
        Console.Error.WriteLine($"unexpected assembly name: {name}");
        return 1;
    }
}

// Runs generated entity code under NativeAOT: state, commands, snapshot, restore, delta and hash.
var registry = new EntityRegistry();
OneGame.Generated.OneGame_AotSmoke.EntityTypes.Register(registry);
var world = new World(7, registry);
var sparks = world.AddTable(new SparkTable());
sparks.Add(new Spark { X = Fixed.FromRatio(1, 3), Life = 5 });
var purse = world.Add(new Purse { Owner = "smoke" });
purse.Send.Add(3, Fixed.FromRatio(3, 2));
for (var i = 0; i < 10; i++)
{
    world.Step();
}

var snapshot = new SyncWriter();
world.WriteSnapshot(snapshot);
var restored = new World(7, registry);
restored.AddTable(new SparkTable());
if (restored.ReadSnapshot(snapshot.Written) != SyncError.None || restored.ComputeHash() != world.ComputeHash())
{
    Console.Error.WriteLine("snapshot restore did not reproduce the world hash");
    return 1;
}

var changed = (Purse)restored.Entities[0];
changed.Owner = "changed";
var delta = new SyncWriter();
changed.WriteDeltaFrom(purse, delta);
var deltaReader = new SyncReader(delta.Written);
purse.ApplyDelta(ref deltaReader);
if (!deltaReader.Ok || purse.ComputeHash() != changed.ComputeHash())
{
    Console.Error.WriteLine("delta did not reproduce the changed entity");
    return 1;
}

var angle = Fixed.Sin(Fixed.One);
Console.WriteLine($"ok: {names.Length} assemblies; hash {world.ComputeHash():x16}; sin(1)={angle}; json {world.DumpJson().Length} chars");
return 0;
