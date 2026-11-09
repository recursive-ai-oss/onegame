using System.Text.Json;
using OneGame.Core;
using OneGame.Host;
using OneGame.UI;

namespace OneGame.Control.Tests;

[Collection("AudioService.Active")]
public class WindowOpTests
{
    private sealed class QuietGame : GameDefinition
    {
        public override string Name => "QuietGame";

        public override void RegisterEntities(EntityRegistry registry)
        {
        }

        public override void Setup(World world)
        {
        }

        public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column(Ui.Label("x")));
    }

    private static JsonElement Call(ControlSurface surface, string args = "{}") => JsonDocument.Parse(surface.Call("window", args)).RootElement;

    [Fact]
    public void TheWindowOpReadsAndSetsThePlacementAndTextRestoresIt()
    {
        using var game = GameInstance.Create(new QuietGame(), new GameOptions { Headless = true, Gpu = GpuMode.Off });
        var surface = new ControlSurface(game);

        var read = Call(surface);
        Assert.Equal("normal", read.GetProperty("state").GetString());
        Assert.True(read.GetProperty("canPlace").GetBoolean());

        var set = Call(surface, """{"x":120,"y":80,"width":900,"height":500,"state":"maximized"}""");
        Assert.Equal("maximized", set.GetProperty("state").GetString());
        Assert.Equal("120,80,900x500,maximized", set.GetProperty("text").GetString());
        Assert.Equal(120, set.GetProperty("x").GetInt32());

        // One field changes only that field.
        var moved = Call(surface, """{"x":10}""");
        Assert.Equal("10,80,900x500,maximized", moved.GetProperty("text").GetString());

        var restored = Call(surface, """{"text":"5,6,640x360,normal"}""");
        Assert.Equal("normal", restored.GetProperty("state").GetString());
        Assert.Equal((640, 360), (restored.GetProperty("width").GetInt32(), restored.GetProperty("height").GetInt32()));
        Assert.Equal(640, game.Window.Width);
    }

    [Fact]
    public void BadInputIsRefusedWithBadArgs()
    {
        using var game = GameInstance.Create(new QuietGame(), new GameOptions { Headless = true, Gpu = GpuMode.Off });
        var surface = new ControlSurface(game);
        Assert.Equal("bad_args", Assert.Throws<ControlException>(() => surface.Call("window", """{"state":"sideways"}""")).Code);
        Assert.Equal("bad_args", Assert.Throws<ControlException>(() => surface.Call("window", """{"text":"nope"}""")).Code);
        Assert.Equal("bad_args", Assert.Throws<ControlException>(() => surface.Call("window", """{"width":0}""")).Code);
    }
}
