using System.Runtime.CompilerServices;
using System.Text.Json;
using OneGame.Control;
using OneGame.Host;
using OneGame.Storage;
using OneGame.Testing.Tests.Docs;

namespace OneGame.Testing.Tests;

/// <summary>The saves guide's code is code that compiles, and the saves control operation and scenario steps do what the guide says.</summary>
public sealed class SavesTests
{
    private static string Root([CallerFilePath] string here = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    private static IEnumerable<string> Names(params string[] names) => names.Order();

    private static TestCluster Start(IByteStore store) => TestCluster.Start(new ShopGame(), new TestOptions { Gpu = GpuMode.Off, Storage = store });

    private static JsonElement Saves(TestClient c, string args = "{}") => JsonDocument.Parse(c.Call("saves", args)).RootElement.Clone();

    // ---- the guide ----

    [Fact]
    public void EveryCodeBlockInTheGuideIsCompiledCode()
    {
        var guide = File.ReadAllText(Path.Combine(Root(), "docs", "saves.md")).Replace("\r\n", "\n", StringComparison.Ordinal);
        var source = File.ReadAllText(Path.Combine(Root(), "tests", "OneGame.Testing.Tests", "Docs", "SavesDocSnippets.cs")).Replace("\r\n", "\n", StringComparison.Ordinal);
        var plain = source;
        var indented = string.Join('\n', source.Split('\n').Select(l => l.StartsWith("        ", StringComparison.Ordinal) ? l[8..] : l));
        var blocks = System.Text.RegularExpressions.Regex.Matches(guide, "```csharp\n(.*?)\n```", System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.True(blocks.Count >= 8);
        foreach (System.Text.RegularExpressions.Match block in blocks)
        {
            Assert.True(plain.Contains(block.Groups[1].Value, StringComparison.Ordinal) || indented.Contains(block.Groups[1].Value, StringComparison.Ordinal),
                "A csharp block of docs/saves.md is not in tests/OneGame.Testing.Tests/Docs/SavesDocSnippets.cs:\n" + block.Groups[1].Value);
        }
    }

    [Fact]
    public void TheGuidesScenarioParses()
    {
        var guide = File.ReadAllText(Path.Combine(Root(), "docs", "saves.md")).Replace("\r\n", "\n", StringComparison.Ordinal);
        var yaml = System.Text.RegularExpressions.Regex.Match(guide, "```yaml\n(.*?)\n```", System.Text.RegularExpressions.RegexOptions.Singleline).Groups[1].Value;
        var scenario = ScenarioParser.Parse("name: guide\n" + yaml);
        Assert.Contains(scenario.Steps, s => s.Kind == StepKind.Saves);
        Assert.Contains(scenario.Steps, s => s.Kind == StepKind.Expect);
    }

    [Fact]
    public void TheGuidesSnippetsRunAgainstARealGame()
    {
        var store = new MemoryByteStore();
        using var cluster = Start(store);
        var game = cluster.Client.Game;
        SavesDocSnippets.SaveAndLoad(game);
        SavesDocSnippets.LoadScreen(game);
        SavesDocSnippets.QuickAndAuto(game);
        SavesDocSnippets.Settings(game);
        SavesDocSnippets.LowLevel(game, [1, 2, 3]);
        Assert.Equal(Names("options.bin", "profile.bin", "slot-auto-0.sav", "slot-quick.sav", "slot-slot1.sav"), store.List().Order());
        Assert.Equal(4, game.Storage.OpenSettings(Options.Schema).Value.Volume);
    }

    [Fact]
    public void ACustomStoreIsUsedForEverythingTheGameKeeps()
    {
        var log = new List<string>();
        using var cluster = Start(new LoggingStore(new MemoryByteStore(), log));
        cluster.Client.Game.Storage.Save("x", "custom");
        cluster.Client.Game.Storage.OpenSettings(Options.Schema).Save();
        Assert.Equal(Names("write slot-x.sav", "write options.bin"), log.Order());
        Assert.StartsWith("logging memory", cluster.Client.Game.Storage.Files.Description, StringComparison.Ordinal);
    }

    // ---- save and load through the running game ----

    [Fact]
    public void ASlotSavedInOneRunLoadsInAFreshRunWithTheSameHashAndMetadata()
    {
        var store = new MemoryByteStore();
        ulong saved;
        using (var first = Start(store))
        {
            var c = first.Client;
            c.Step(12);
            c.Call("command_send", """{"entity":"Shop#1","command":"Restock","args":["7"]}""");
            c.Step(3);
            saved = c.Hash;
            var r = Saves(c, """{"action":"save","slot":"day-1","label":"Day 1"}""");
            Assert.Equal("day-1", r.GetProperty("loaded").GetString());
        }

        using var second = Start(store);
        var d = second.Client;
        Assert.NotEqual(saved, d.Hash);
        var list = Saves(d);
        var slot = Assert.Single(list.GetProperty("slots").EnumerateArray());
        Assert.Equal(("day-1", "Day 1", "manual"), (slot.GetProperty("slot").GetString(), slot.GetProperty("label").GetString(), slot.GetProperty("kind").GetString()));
        Assert.Equal(saved.ToString("x16", System.Globalization.CultureInfo.InvariantCulture), slot.GetProperty("worldHash").GetString());

        var loaded = Saves(d, """{"action":"load","slot":"day-1"}""");
        Assert.False(loaded.GetProperty("migrated").GetBoolean());
        Assert.Equal(saved, d.Hash);
        Assert.Equal(15, d.Tick);
    }

    [Fact]
    public void FailuresComeBackAsTypedErrors()
    {
        using var cluster = Start(new MemoryByteStore());
        var c = cluster.Client;
        Assert.Equal("no_such_slot", Assert.Throws<ControlException>(() => c.Call("saves", """{"action":"load","slot":"nope"}""")).Code);
        Assert.Equal("invalid_name", Assert.Throws<ControlException>(() => c.Call("saves", """{"action":"save","slot":"../x"}""")).Code);
        Assert.Equal("no_such_slot", Assert.Throws<ControlException>(() => c.Call("saves", """{"action":"quick-load"}""")).Code);
        c.Call("saves", """{"action":"write-file","name":"slot-bad.sav","data":"AAECAwQF"}""");
        Assert.Equal("bad_slot_file", Assert.Throws<ControlException>(() => c.Call("saves", """{"action":"load","slot":"bad"}""")).Code);
        Assert.Equal("bad_args", Assert.Throws<ControlException>(() => c.Call("saves", """{"action":"sing"}""")).Code);

        // A damaged slot did not touch the running game.
        var hash = c.Hash;
        Assert.Throws<ControlException>(() => c.Call("saves", """{"action":"load","slot":"bad"}"""));
        Assert.Equal(hash, c.Hash);
    }

    [Fact]
    public void SettingsAppearInTheReportAndAPlantedBadFileFallsBackWithAReason()
    {
        var store = new MemoryByteStore();
        using var cluster = Start(store);
        var c = cluster.Client;
        var file = c.Game.Storage.OpenSettings(Options.Schema);
        file.Value.Volume = 2;
        file.Save();

        var report = Saves(c).GetProperty("settings").EnumerateArray().Single();
        Assert.Equal(("options.bin", "missing"), (report.GetProperty("file").GetString(), report.GetProperty("status").GetString()));
        Assert.Contains("volume = 2", report.GetProperty("text").GetString(), StringComparison.Ordinal);

        Saves(c, """{"action":"settings-load"}""");
        Assert.Equal("loaded", Saves(c).GetProperty("settings")[0].GetProperty("status").GetString());

        Saves(c, """{"action":"write-file","name":"options.bin","data":"AQIDBAU="}""");
        Saves(c, """{"action":"settings-load"}""");
        var bad = Saves(c).GetProperty("settings")[0];
        Assert.Equal("defaulted", bad.GetProperty("status").GetString());
        Assert.Contains("not a settings file", bad.GetProperty("reason").GetString(), StringComparison.Ordinal);
        Assert.Contains("volume = 7", bad.GetProperty("text").GetString(), StringComparison.Ordinal);
        Assert.Equal("not_found", Assert.Throws<ControlException>(() => c.Call("saves", """{"action":"settings-load","file":"nope.bin"}""")).Code);
    }

    [Fact]
    public void ScenarioStepsAndExpectationsDriveSlots()
    {
        using var cluster = Start(new MemoryByteStore());
        var scenario = ScenarioParser.Parse("""
            name: slots
            steps:
              - command: { entity: Shop, name: Restock, args: [5] }
              - step: 2
              - saves: { action: save, slot: before, label: "Before the sale" }
              - saves: quick-save
              - command: { entity: Shop, name: Restock, args: [50] }
              - step: 2
              - saves: { action: load, slot: before }
              - expect:
                  saves:
                    has: before
                    lacks: after
                    slots: 2
                    files: 2
                    slot: { name: before, label: "Before the sale", kind: manual, tick: 2 }
            expect:
              - tick: 2
            """);
        var result = cluster.Run(scenario);
        Assert.True(result.Passed, string.Join("; ", result.Steps.Where(s => !s.Ok).Select(s => s.Error)));

        var wrong = cluster.Run(ScenarioParser.Parse("""
            name: wrong
            steps:
              - expect:
                  saves: { has: nothing-here }
            """));
        Assert.False(wrong.Passed);
    }

    [Fact]
    public void AutosaveRotatesAndLoadLatestFindsTheNewest()
    {
        using var cluster = Start(new MemoryByteStore());
        var c = cluster.Client;
        for (var i = 0; i < 5; i++)
        {
            c.Step(2);
            Saves(c, "{\"action\":\"autosave\",\"label\":\"n" + i + "\"}");
            Thread.Sleep(3);
        }

        var slots = Saves(c).GetProperty("slots").EnumerateArray().ToList();
        Assert.Equal(3, slots.Count);
        Assert.Equal("n4", slots[0].GetProperty("label").GetString());
        c.Step(5);
        var r = Saves(c, """{"action":"load-latest"}""");
        Assert.Equal(10, c.Tick);
        Assert.StartsWith("auto-", r.GetProperty("loaded").GetString(), StringComparison.Ordinal);
    }
}
