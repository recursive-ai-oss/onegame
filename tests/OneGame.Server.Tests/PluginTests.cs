using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using OneGame.Generators;
using OneGame.Server.Plugins;
using Xunit.Sdk;

namespace OneGame.Server.Tests;

/// <summary>
/// Runs on its own: the unload test measures the whole process's managed heap, so another class
/// allocating at the same time (a plugin compile, a network test) would show up as growth.
/// </summary>
[CollectionDefinition(nameof(HeapMeasuring), DisableParallelization = true)]
public sealed class HeapMeasuring;

[Collection(nameof(HeapMeasuring))]
public class PluginTests : IDisposable
{
    private static readonly ScopeId Scope1 = new(1);
    private static readonly EntityId First = new(1);
    private readonly List<string> _folders = [];

    private string Folder(int variant = 1, Func<string, string>? edit = null)
    {
        var dir = PluginBuilds.NewFolder();
        _folders.Add(dir);
        return PluginBuilds.Build(dir, variant, edit);
    }

    private static ServerConfig OneScope() => new() { Name = "t", Scopes = [new ScopeConfig(1, 7)] };

    private static PluginConfig Spec(string dir, params (string, string)[] settings) =>
        new("counter", dir, [], [.. settings.Select(s => new KeyValuePair<string, string>(s.Item1, s.Item2))]);

    private static TestServer Server(string? dir = null, params (string, string)[] settings)
    {
        var t = new TestServer(OneScope(), loader: new PluginLoader());
        if (dir is not null)
        {
            t.Host.LoadPlugin(Spec(dir, settings));
        }

        return t;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var f in _folders)
        {
            try
            {
                Directory.Delete(f, true);
            }
            catch (IOException)
            {
            }
        }
    }

    [Fact]
    public void APluginAddsASystemAndACommandAndAClientSeesThemWithoutDrift()
    {
        using var t = Server(Folder());
        var client = t.AddClient("c1", Scope1);
        Assert.Single(t.Host.Scopes[0].World.Entities);
        Assert.Equal(PluginState.Loaded, t.Host.Plugins()[0].State);

        t.Step(10);
        Assert.Equal(CommandStatus.Forwarded, client.Send(First, "Add", "100"));
        t.Step(5);

        // 15 ticks of the system (+1 each) and the command's +100.
        Assert.Equal(115, GetValue(t.Host.Scopes[0].World));
        Assert.Equal(115, GetValue(client.World));
        Assert.Empty(t.CheckSync());
        Assert.Equal(0, t.DriftCount);
    }

    // The sample entity's type lives in the plugin's load context, so tests read it through the synced state, not by type.
    private static int GetValue(World w) => int.Parse(ReadProperty(w, "Value"), CultureInfo.InvariantCulture);

    private static string ReadProperty(World w, string name)
    {
        var json = new JsonOut();
        w.Find(First)!.WriteJson(json);
        using var doc = System.Text.Json.JsonDocument.Parse(json.ToString());
        return Search(doc.RootElement, name) ?? throw new InvalidOperationException("no property " + name + " in " + json);
    }

    private static string? Search(System.Text.Json.JsonElement e, string name)
    {
        if (e.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            return null;
        }

        foreach (var p in e.EnumerateObject())
        {
            if (p.Name == name)
            {
                return p.Value.ToString();
            }

            if (Search(p.Value, name) is { } inner)
            {
                return inner;
            }
        }

        return null;
    }

    [Fact]
    public void AHostWithoutALoaderReportsConfiguredPluginsAsFailuresAndStillRuns()
    {
        var config = new ServerConfig { Plugins = [new PluginConfig("counter", "/nowhere", [], [])] };
        using var t = new TestServer(config);
        Assert.Single(t.Host.LoadFailures);
        Assert.Equal("unsupported", t.Host.LoadFailures[0].Code);
        t.Step(3);
        Assert.Equal(3, t.Host.Scopes[0].World.Tick);
    }

    [Fact]
    public void AnApiVersionTheHostDoesNotSpeakIsRefusedWithAClearError()
    {
        var dir = Folder(1, s => s.Replace("[assembly: ServerPlugin(typeof(Sample.CounterPlugin))]", "[assembly: ServerPlugin(typeof(Sample.CounterPlugin), 99)]", StringComparison.Ordinal));
        using var t = Server();
        var ex = Assert.Throws<PluginException>(() => t.Host.LoadPlugin(Spec(dir)));
        Assert.Equal("api_mismatch", ex.Code);
        Assert.Contains("plugin API 99", ex.Message, StringComparison.Ordinal);
        Assert.Contains("1..1", ex.Message, StringComparison.Ordinal);
        Assert.Empty(t.Host.Plugins());
    }

    [Fact]
    public void AnAssemblyWithoutTheEntryAttributeIsRefused()
    {
        var dir = Folder(1, s => s.Replace("[assembly: ServerPlugin(typeof(Sample.CounterPlugin))]", "", StringComparison.Ordinal));
        using var t = Server();
        var ex = Assert.Throws<PluginException>(() => t.Host.LoadPlugin(Spec(dir)));
        Assert.Equal("no_entry", ex.Code);
    }

    [Fact]
    public void AMissingFolderIsNotFound()
    {
        using var t = Server();
        var ex = Assert.Throws<PluginException>(() => t.Host.LoadPlugin(Spec("/does/not/exist")));
        Assert.Equal("not_found", ex.Code);
    }

    [Fact]
    public void ReloadingAnUnchangedBuildLeavesTheHashSequenceIdenticalToARunWithoutIt()
    {
        var dir = Folder();
        using var plain = Server(dir);
        plain.AddClient("c", Scope1);
        plain.Step(60);

        using var reloaded = Server(dir);
        var client = reloaded.AddClient("c", Scope1);
        reloaded.Step(30);
        var result = reloaded.Host.ReloadPlugin("counter");
        reloaded.Step(30);

        Assert.True(result.Collected);
        Assert.Equal(plain.HashHistory(Scope1), reloaded.HashHistory(Scope1));
        Assert.Empty(reloaded.CheckSync());
        Assert.Equal(60, client.Replica.World.Tick);
    }

    [Fact]
    public void EditedLogicTakesEffectOnTheNextTickWithTheSameEntitiesAndNoReplicaGap()
    {
        var dir = Folder(1);
        using var t = Server(dir);
        var client = t.AddClient("c", Scope1);
        t.Step(20);
        Assert.Equal(20, GetValue(client.World));

        PluginBuilds.Build(dir, 2);
        var result = t.Host.ReloadPlugin("counter");
        Assert.NotEqual(result.OldBuild, result.NewBuild);
        Assert.False(result.Migrated);
        Assert.Equal(20, GetValue(t.Host.Scopes[0].World));
        Assert.Single(t.Host.Scopes[0].World.Entities);

        var framesBefore = client.Replica.FramesApplied;
        for (var i = 0; i < 10; i++)
        {
            t.Step();
            Assert.Empty(t.CheckSync());
        }

        // Variant 2 adds the step twice per tick.
        Assert.Equal(20 + (10 * 2), GetValue(t.Host.Scopes[0].World));
        Assert.Equal(GetValue(t.Host.Scopes[0].World), GetValue(client.World));
        Assert.Equal(First, t.Host.Scopes[0].World.Entities[0].Id);
        Assert.Equal(30, client.Replica.World.Tick);
        Assert.True(client.Replica.FramesApplied >= framesBefore + 10);
        Assert.Equal(0, client.Replica.Resyncs);
        Assert.Equal(0, t.DriftCount);
    }

    [Fact]
    public void ACommandFromAClientSentJustBeforeAReloadStillRuns()
    {
        var dir = Folder(1);
        using var t = Server(dir);
        var client = t.AddClient("c", Scope1);
        t.Step(5);
        client.Send(First, "Add", "50");
        PluginBuilds.Build(dir, 2);
        t.Host.ReloadPlugin("counter");
        t.Step(1);

        Assert.Equal(5 + 50 + 2, GetValue(t.Host.Scopes[0].World));
        Assert.Empty(t.CheckSync());
    }

    [Fact]
    public void ACommandQueuedByTheHostBlocksAReloadUntilTheNextTick()
    {
        using var t = Server(Folder());
        t.Step(2);
        t.Host.SendCommand(Scope1, First, "Add", "5");
        var ex = Assert.Throws<PluginException>(() => t.Host.ReloadPlugin("counter"));
        Assert.Equal("conflict", ex.Code);
        t.Step(1);
        t.Host.ReloadPlugin("counter");
        Assert.Equal(8, GetValue(t.Host.Scopes[0].World));
    }

    [Fact]
    public void ASchemaChangeInAPluginEntityGoesThroughTheGeneratedMigration()
    {
        var dir = Folder(1);
        using var t = Server(dir);
        var client = t.AddClient("c", Scope1);
        t.Step(12);

        PluginBuilds.Build(dir, 3);
        var result = t.Host.ReloadPlugin("counter");
        Assert.True(result.Migrated);
        t.Step(3);

        Assert.Equal(15, GetValue(t.Host.Scopes[0].World));
        Assert.Equal("migrated", ReadProperty(t.Host.Scopes[0].World, "Label"));
        Assert.Equal("migrated", ReadProperty(client.World, "Label"));
        Assert.Empty(t.CheckSync());
    }

    [Fact]
    public void AReloadThatCannotRestoreStatePutsTheOldBuildBackAndKeepsTicking()
    {
        var dir = Folder(1);
        using var t = Server(dir);
        var client = t.AddClient("c", Scope1);
        t.Step(10);
        var oldBuild = t.Host.Plugins()[0].Build;

        // Schema 2 with the migration removed: the saved state cannot be read.
        PluginBuilds.Build(dir, 3, s => s.Replace("[Migrate(1)]", "", StringComparison.Ordinal));
        var ex = Assert.Throws<PluginException>(() => t.Host.ReloadPlugin("counter"));
        Assert.Equal("reload_failed", ex.Code);
        Assert.Contains("Migrate", ex.Message, StringComparison.Ordinal);
        Assert.Equal(oldBuild, t.Host.Plugins()[0].Build);

        t.Step(5);
        Assert.Equal(15, GetValue(t.Host.Scopes[0].World));
        Assert.Empty(t.CheckSync());
        Assert.Equal(15, client.World.Tick);
    }

    [Fact]
    public void AReloadThatDropsATypeWithLiveEntitiesIsRefused()
    {
        var dir = Folder(1);
        using var t = Server(dir);
        t.Step(2);
        PluginBuilds.Build(dir, 1, s => s.Replace("context.AddEntity<Counter>();", "", StringComparison.Ordinal));
        var ex = Assert.Throws<PluginException>(() => t.Host.ReloadPlugin("counter"));
        Assert.Equal("conflict", ex.Code);
        t.Step(2);
        Assert.Equal(4, GetValue(t.Host.Scopes[0].World));
    }

    [Fact]
    public void UnloadingRemovesTheEntitiesStopsTheSystemsAndTheLoadContextIsCollected()
    {
        var dir = Folder();
        using var t = Server(dir);
        var client = t.AddClient("c", Scope1);
        t.Step(5);

        var result = t.Host.UnloadPlugin("counter");
        Assert.Equal(1, result.EntitiesRemoved);
        Assert.True(result.Collected);
        t.Step(2);
        Assert.Empty(t.Host.Scopes[0].World.Entities);
        Assert.Empty(client.World.Entities);
        Assert.Empty(t.Host.Plugins());
        Assert.Empty(t.CheckSync());
    }

    [Fact]
    public void TheCollectedContextIsProvenByAWeakReferenceFromOutsideTheHost()
    {
        var weak = LoadAndUnload(Folder());
        for (var i = 0; i < 10 && weak.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.False(weak.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference LoadAndUnload(string dir)
    {
        // The test's own handle on the plugin's entry object; it must not keep the context alive after the unload.
        var loader = new PluginLoader();
        var image = loader.Load("counter", dir);
        var weak = new WeakReference(image.Plugin.GetType().Assembly, trackResurrection: true);
        image.Release();
        return weak;
    }

    [Fact]
    public void FiftyCyclesOfLoadReloadAndUnloadDoNotGrowTheManagedHeap()
    {
        var dir1 = Folder(1);
        var dir2 = Folder(2);
        using var t = Server();
        t.AddClient("c", Scope1);

        long Heap()
        {
            for (var i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            return GC.GetTotalMemory(true);
        }

        var v1 = File.ReadAllBytes(Path.Combine(dir1, "counter.dll"));
        var v2 = File.ReadAllBytes(Path.Combine(dir2, "counter.dll"));
        void Cycle(int n)
        {
            for (var i = 0; i < n; i++)
            {
                File.WriteAllBytes(Path.Combine(dir1, "counter.dll"), v1);
                t.Host.LoadPlugin(Spec(dir1));
                t.Step(3);
                File.WriteAllBytes(Path.Combine(dir1, "counter.dll"), v2);
                Assert.True(t.Host.ReloadPlugin("counter").Collected);
                t.Step(3);
                Assert.True(t.Host.UnloadPlugin("counter").Collected);
            }
        }

        Cycle(5);
        var before = Heap();
        Cycle(50);
        var after = Heap();
        var growth = after - before;
        Console.WriteLine($"managed heap after 5 warm-up cycles: {before:N0} B; after 50 more: {after:N0} B; growth {growth:N0} B");
        Assert.True(growth < 1_000_000, $"managed heap grew by {growth:N0} bytes over 50 cycles");
        Assert.Empty(t.CheckSync());
    }

    [Theory]
    [InlineData("tick")]
    [InlineData("entity")]
    public void APluginThatThrowsWhileTickingIsContainedReportedAndTheServerKeepsTicking(string mode)
    {
        using var t = Server(Folder(), ("fault", mode), ("faultTick", "5"));
        var client = t.AddClient("c", Scope1);
        t.Step(4);
        t.Step(1);
        t.Step(10);

        var info = t.Host.Plugins()[0];
        Assert.Equal(PluginState.Faulted, info.State);
        Assert.Equal("tick", info.Fault!.Phase);
        Assert.Contains("exploded", info.Fault.Message, StringComparison.Ordinal);
        Assert.Empty(t.Host.Scopes[0].World.Entities);
        Assert.Equal(15 - (mode == "entity" ? 1 : 0), t.Host.Scopes[0].World.Tick);
        Assert.Empty(t.CheckSync());
        Assert.Contains(t.Host.Log.Tail(20), l => l.Level == "error" && l.Message.Contains("was stopped", StringComparison.Ordinal));
        Assert.Empty(client.World.Entities);

        // The faulted plugin can still be unloaded and its context collected.
        Assert.True(t.Host.UnloadPlugin("counter").Collected);
    }

    [Fact]
    public void ACommandThatThrowsIsContained()
    {
        using var t = Server(Folder());
        var client = t.AddClient("c", Scope1);
        t.Step(2);
        client.Send(First, "Boom");
        t.Step(3);

        Assert.Equal(PluginState.Faulted, t.Host.Plugins()[0].State);
        Assert.Contains("command exploded", t.Host.Plugins()[0].Fault!.Message, StringComparison.Ordinal);
        Assert.Empty(t.CheckSync());
    }

    [Fact]
    public void APluginThatThrowsInLoadIsRefusedAndLeavesNoTrace()
    {
        using var t = Server();
        t.Step(2);
        var ex = Assert.Throws<PluginException>(() => t.Host.LoadPlugin(Spec(Folder(), ("fault", "load"))));
        Assert.Equal("load_failed", ex.Code);
        Assert.Contains("load exploded", ex.Message, StringComparison.Ordinal);
        Assert.Empty(t.Host.Plugins());
        t.Step(2);
        Assert.Equal(4, t.Host.Scopes[0].World.Tick);

        // Nothing was registered, so the same name loads fine afterwards.
        t.Host.LoadPlugin(Spec(Folder()));
        Assert.Single(t.Host.Plugins());
    }

    [Fact]
    public void APluginThatThrowsWhileSeedingIsQuarantinedNotFatal()
    {
        using var t = Server(Folder(), ("fault", "seed"));
        t.Step(3);
        var info = t.Host.Plugins()[0];
        Assert.Equal(PluginState.Faulted, info.State);
        Assert.Equal("seed", info.Fault!.Phase);
        Assert.Equal(3, t.Host.Scopes[0].World.Tick);
    }

    [Fact]
    public void APluginThatThrowsInUnloadIsReportedAndStillUnloaded()
    {
        using var t = Server(Folder(), ("fault", "unload"));
        t.Step(2);
        var result = t.Host.UnloadPlugin("counter");
        Assert.True(result.Collected);
        Assert.Contains(t.Host.Log.Tail(20), l => l.Level == "error" && l.Message.Contains("threw in Unload", StringComparison.Ordinal));
        t.Step(2);
        Assert.Empty(t.Host.Plugins());
    }

    [Fact]
    public void DuplicateNamesAndUnknownPluginsAreTypedErrors()
    {
        using var t = Server(Folder());
        Assert.Equal("duplicate", Assert.Throws<PluginException>(() => t.Host.LoadPlugin(Spec(Folder()))).Code);
        Assert.Equal("not_found", Assert.Throws<PluginException>(() => t.Host.ReloadPlugin("nope")).Code);
        Assert.Equal("not_found", Assert.Throws<PluginException>(() => t.Host.UnloadPlugin("nope")).Code);
    }

    [Fact]
    public void StopRunsEveryPluginUnloadHook()
    {
        var t = Server(Folder(), ("fault", "unload"));
        t.Step(1);
        t.Dispose();
        Assert.Equal(ServerState.Stopped, t.Host.State);
        Assert.Contains(t.Host.Log.Tail(30), l => l.Message.Contains("threw in Unload", StringComparison.Ordinal));
    }

    [Fact]
    public void ALoadedPluginNeverAppearsInTheDefaultLoadContext()
    {
        using var t = Server(Folder());
        Assert.DoesNotContain(
            System.Runtime.Loader.AssemblyLoadContext.Default.Assemblies,
            a => a.GetName().Name == "counter");
        _ = Stopwatch.GetTimestamp();
    }

    [Fact]
    public async Task TheSimulationRulesApplyToPluginCode()
    {
        var clean = await PluginBuilds.AnalyzerErrors(s => s);
        Assert.Empty(clean);

        var clock = await PluginBuilds.AnalyzerErrors(s => s.Replace("Ticks++;", "Ticks += (int)DateTime.UtcNow.Ticks;", StringComparison.Ordinal));
        Assert.Contains("OG0004", clock);
    }
}
