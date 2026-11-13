using System.Text.Json;
using OneGame.Control;
using OneGame.Host;
using Scene3DGame;

namespace OneGame.Scene3D.Tests;

/// <summary>The debug panel's GPU figures come from the render graph's timestamps and show in the <c>overlay</c> operation.</summary>
public class GpuTimingTests
{
    [Fact]
    public void OverlayReportsGpuPassTimesWhereTheBackendHasTimestamps()
    {
        Environment.SetEnvironmentVariable("ONEGPU_VALIDATION", "1");
        using var game = GameInstance.Create(new Scene3DGameDefinition(), new GameOptions { Headless = true, Gpu = GpuMode.Required });
        var surface = new ControlSurface(game, Environment.GetEnvironmentVariable("ONEGAME_CAPTURE_DIR"));
        surface.Call("overlay", """{"visible":true,"masked":false}""");
        for (var i = 0; i < 6; i++)
        {
            game.Step(1);
            game.Capture(320, 180);   // headless runs draw only on request
        }

        if (Environment.GetEnvironmentVariable("ONEGAME_CAPTURE_DIR") is { Length: > 0 })
        {
            surface.Call("capture_frame", """{"size":"640x360","path":"overlay-timing.png"}""");
        }

        var report = JsonDocument.Parse(surface.Call("overlay")).RootElement;
        var timing = report.GetProperty("timing");
        var status = timing.GetProperty("gpu").GetString();
        var lines = report.GetProperty("lines").EnumerateArray().Select(l => l.GetString()!).ToArray();
        if (status == "ready")
        {
            Assert.Contains(timing.GetProperty("gpuPasses").EnumerateArray(), p => p.GetProperty("name").GetString()!.StartsWith("forward", StringComparison.Ordinal));
            Assert.True(timing.GetProperty("gpuMs").GetDouble() > 0);
            Assert.Contains(lines, l => l.StartsWith("gpu ", StringComparison.Ordinal) && l.Contains("forward", StringComparison.Ordinal));
        }
        else
        {
            Assert.Equal("unsupported", status);
            Assert.Contains("gpu n/a (no timestamps)", lines);
        }

        Console.WriteLine("gpu timing: " + status + " " + string.Join(" | ", lines.Where(l => l.StartsWith("gpu", StringComparison.Ordinal) || l.StartsWith("cpu", StringComparison.Ordinal))));
    }
}
