using System.Text.Json;
using OneGame.Control;
using OneGame.Host;
using OneGame.Live;
using OneGame.UI;

namespace OneGame.Testing.Tests;

/// <summary>A shop whose screen has an id with a slash (the kit makes ids like that) and a label that reads another element's bounds.</summary>
public sealed class SlashGame : ShopGame
{
    public override void DefineScreens(ScreenSet screens) =>
        screens.Add("main", game =>
        {
            Element? box = null;
            return Ui.Column(
                Ui.Label("track").Named("qty/track"),
                Ui.Label("odd id").Named("a.b c"),
                (box = Ui.Column().Named("box").Size(Length.Ui(40), Length.Ui(10))),
                Ui.Column().Named("mirror").Background(Color.Rgb(0xC03030)).OnUpdate(el => el.Style(st => st.Size(Length.Px(box!.AbsoluteBounds.Width), Length.Px(10)))))
                .Padding(Length.Px(8)).Background(Color.Rgb(0x203040)).TextColor(Color.White);
        });
}

public class HarnessRegressionTests
{
    private static TestOptions Options() => new() { Width = 320, Height = 200, Gpu = GpuMode.Off };

    [Fact]
    public void IdsWithSlashesAndOddCharactersAreAddressable()
    {
        using var cluster = TestCluster.Start(new SlashGame(), Options());
        var scenario = ScenarioParser.Parse(
            "steps:\n  - click: {id: qty/track}\n  - click: {selector: '#qty/track'}\nexpect:\n  - ui: {id: qty/track, count: 1, text: track}\n  - ui: {id: 'a.b c', count: 1}\n", "inline.yaml", "slash");
        var result = cluster.Run(scenario);
        Assert.True(result.Passed, JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult));
        Assert.Equal("track", JsonDocument.Parse(cluster.Client.Call("ui_query", """{"selector":"#qty/track"}""")).RootElement[0].GetProperty("text").GetString());
    }

    [Fact]
    public void MalformedSelectorsAreReportedFailuresNotCrashes()
    {
        using var cluster = TestCluster.Start(new SlashGame(), Options());
        var step = cluster.Run(ScenarioParser.Parse("steps:\n  - step: 1\n  - click: {selector: '#'}\n", "inline.yaml", "bad"));
        Assert.False(step.Passed);
        Assert.Equal(3, step.Steps[^1].Line);
        Assert.Contains("selector", step.Steps[^1].Error, StringComparison.Ordinal);

        var expect = cluster.Run(ScenarioParser.Parse("expect:\n  - ui: {selector: 'a >'}\n  - ui: {selector: ':bogus'}\n", "inline.yaml", "bad2"));
        Assert.False(expect.Passed);
        Assert.All(expect.Expectations, e => Assert.False(e.Passed));
        Assert.Equal(2, expect.Expectations[0].Line);
        Assert.Equal("bad_args", Assert.Throws<ControlException>(() => cluster.Client.Call("ui_query", """{"selector":"#"}""")).Code);
    }

    // The red bar mirrors the width of the 40-unit box, which is 100 px at scale 2.5. A binding that reads the previous
    // layout would draw it 40 px wide.
    private static (int Lo, int Hi) RedBar(CapturedFrame f)
    {
        int lo = int.MaxValue, hi = -1;
        for (var i = 0; i < f.Rgba.Length; i += 4)
        {
            if (f.Rgba[i] == 0xC0 && f.Rgba[i + 1] == 0x30)
            {
                var x = (i / 4) % f.Width;
                lo = Math.Min(lo, x);
                hi = Math.Max(hi, x);
            }
        }

        return (lo, hi);
    }

    [Fact]
    public void CaptureAtANewSizeLaysOutBeforeDrawing()
    {
        using var cluster = TestCluster.Start(new SlashGame(), Options());
        var c = cluster.Client;
        c.Capture("320x200");
        var first = c.Capture("1920x1080", 2.5f);
        Assert.Equal((8, 107), RedBar(first));

        using var fresh = TestCluster.Start(new SlashGame(), Options());
        Assert.Equal(0, ImageCompare.Compare(first, fresh.Client.Capture("1920x1080", 2.5f)).DifferingPixels);
        Assert.Equal(0, ImageCompare.Compare(first, c.Capture("1920x1080", 2.5f)).DifferingPixels);
    }

    [Fact]
    public void RecordingAFixtureScenarioKeepsTheTicksAndReplaysIdentically()
    {
        var dir = Path.Combine(Path.GetTempPath(), "og-rec-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var file = Path.Combine(dir, "s.yaml");
            File.WriteAllText(file, "name: rec\nfixture: rich\nsteps:\n  - command: {entity: Shop, name: Buy, args: [2]}\n  - step: 6\n");
            Dev.Enable();
            var rec = Path.Combine(dir, "r.ogrp");
            var outText = new StringWriter();
            var code = GameApp.Run(new ShopGame(), ["--headless", "--gpu", "off", "--json", "--scenario", file, "--record", rec, "--artifacts", dir], outText, TextWriter.Null);
            Assert.True(code == 0, outText.ToString());

            var replayOut = new StringWriter();
            Assert.Equal(0, GameApp.Run(new ShopGame(), ["--headless", "--gpu", "off", "--json", "--replay", rec], replayOut, TextWriter.Null));
            using var doc = JsonDocument.Parse(replayOut.ToString());
            Assert.True(doc.RootElement.GetProperty("identical").GetBoolean());
            Assert.Equal(6, doc.RootElement.GetProperty("ticks").GetInt32());
            Assert.Equal(1, doc.RootElement.GetProperty("commands").GetInt32());
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void ReplayRefusesToCallAnEmptyRecordingIdentical()
    {
        var dir = Path.Combine(Path.GetTempPath(), "og-rec-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            using var game = GameInstance.Create(new ShopGame(), new GameOptions { Headless = true, Gpu = GpuMode.Off });
            var path = Path.Combine(dir, "empty.ogrp");
            RecordingFile.Save(game.StartRecording().Stop(), path);
            Dev.Enable();
            var o = new StringWriter();
            Assert.Equal(1, GameApp.Run(new ShopGame(), ["--headless", "--gpu", "off", "--json", "--replay", path], o, TextWriter.Null));
            using var doc = JsonDocument.Parse(o.ToString());
            Assert.False(doc.RootElement.GetProperty("identical").GetBoolean());
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
