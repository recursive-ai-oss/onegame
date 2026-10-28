using System.Text.Json;
using OneGame.Core;
using OneGame.Host;
using OneGame.UI;

namespace OneGame.Control.Tests;

// The handler and status are process-wide, like the runtime's own notice, so these tests run one at a time.
[Collection("HotReload")]
public class HotReloadTests
{
    private sealed class EditableGame : GameDefinition
    {
        // Stands in for a method body that an applied edit replaced.
        internal static string Text = "before";

        public override string Name => "EditableGame";

        public override void RegisterEntities(EntityRegistry registry)
        {
        }

        public override void Setup(World world)
        {
        }

        public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Label(Text).Named("t"));
    }

    private static string LabelText(ControlSurface surface)
    {
        using var doc = JsonDocument.Parse(surface.Call("ui_query", "{\"selector\":\"#t\"}"));
        return doc.RootElement[0].GetProperty("text").GetString()!;
    }

    [Fact]
    public void AnAppliedEditRebuildsTheScreensAndLeavesTheWorldAlone()
    {
        EditableGame.Text = "before";
        using var game = GameInstance.Create(new EditableGame(), new GameOptions { Headless = true, Gpu = GpuMode.Off });
        var surface = new ControlSurface(game);
        game.Step(5);
        var tick = game.World.Tick;
        var hash = game.World.ComputeHash();
        Assert.Equal("before", LabelText(surface));

        var applied = HotReloadHandler.Applied;
        EditableGame.Text = "after";
        Assert.Equal("before", LabelText(surface));
        HotReloadHandler.UpdateApplication([typeof(EditableGame)]);
        surface.PollHotReload();

        Assert.Equal("after", LabelText(surface));
        Assert.Equal(tick, game.World.Tick);
        Assert.Equal(hash, game.World.ComputeHash());
        using var doc = JsonDocument.Parse(surface.Call("hot_reload"));
        var status = doc.RootElement;
        Assert.Equal(applied + 1, status.GetProperty("applied").GetInt32());
        Assert.False(status.GetProperty("pending").GetBoolean());
        Assert.False(status.GetProperty("replaySafe").GetBoolean());
        Assert.Contains(typeof(EditableGame).FullName!, status.GetProperty("lastTypes").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public void AStatusFileFromTheRunnerShowsInTheOperation()
    {
        var file = Path.Combine(Path.GetTempPath(), "onegame-hot-status-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(file, "{\"state\":\"error\",\"message\":\"CS1002 at Game.cs(3)\",\"restarts\":2}");
        var previous = Environment.GetEnvironmentVariable("ONEGAME_HOT_STATUS");
        Environment.SetEnvironmentVariable("ONEGAME_HOT_STATUS", file);
        try
        {
            using var game = GameInstance.Create(new EditableGame(), new GameOptions { Headless = true, Gpu = GpuMode.Off });
            using var doc = JsonDocument.Parse(new ControlSurface(game).Call("hot_reload"));
            Assert.Equal("error", doc.RootElement.GetProperty("state").GetString());
            Assert.Equal("CS1002 at Game.cs(3)", doc.RootElement.GetProperty("message").GetString());
            Assert.Equal(2, doc.RootElement.GetProperty("restarts").GetInt32());
        }
        finally
        {
            Environment.SetEnvironmentVariable("ONEGAME_HOT_STATUS", previous);
            File.Delete(file);
        }
    }

    [Fact]
    public void ASavedWorldIsLoadedOnceAfterARestart()
    {
        using var source = GameInstance.Create(new EditableGame(), new GameOptions { Headless = true, Gpu = GpuMode.Off });
        source.Step(7);
        var file = Path.Combine(Path.GetTempPath(), "onegame-hot-resume-" + Guid.NewGuid().ToString("N") + ".ogsv");
        File.WriteAllBytes(file, source.SaveSnapshot());
        var previous = Environment.GetEnvironmentVariable("ONEGAME_HOT_RESUME");
        Environment.SetEnvironmentVariable("ONEGAME_HOT_RESUME", file);
        try
        {
            using var game = GameInstance.Create(new EditableGame(), new GameOptions { Headless = true, Gpu = GpuMode.Off });
            var surface = new ControlSurface(game);
            Assert.True(surface.ResumeFromHotRestart());
            Assert.Equal(source.World.Tick, game.World.Tick);
            Assert.False(File.Exists(file));
            Assert.False(surface.ResumeFromHotRestart());
            using var doc = JsonDocument.Parse(surface.Call("hot_reload"));
            Assert.True(doc.RootElement.GetProperty("resumed").GetBoolean());
        }
        finally
        {
            Environment.SetEnvironmentVariable("ONEGAME_HOT_RESUME", previous);
            File.Delete(file);
        }
    }
}
