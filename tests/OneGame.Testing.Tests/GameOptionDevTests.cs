using System.Text.Json;
using OneGame.Host;
using OneGame.Live;

namespace OneGame.Testing.Tests;

/// <summary>A shop that takes a <c>--level</c> option of its own.</summary>
public sealed class LevelShopGame : ShopGame
{
    public override IReadOnlyList<string> OptionNames => ["level"];
}

public class GameOptionDevTests
{
    [Fact]
    public void AGameOptionWorksBesideDevelopmentOptionsAndAMisspeltOneIsStillRefused()
    {
        Dev.Enable();
        var dir = Path.Combine(Path.GetTempPath(), "og-opt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var file = Path.Combine(dir, "s.yaml");
            File.WriteAllText(file, "name: opt\nsteps:\n  - step: 1\n");
            var ok = new StringWriter();
            Assert.Equal(0, GameApp.Run(new LevelShopGame(), ["--headless", "--gpu", "off", "--json", "--level", "3", "--scenario", file, "--artifacts", dir], ok, TextWriter.Null));

            var bad = new StringWriter();
            Assert.Equal(ExitCodes.Usage, GameApp.Run(new LevelShopGame(), ["--headless", "--gpu", "off", "--json", "--level", "3", "--scenaro", file], bad, TextWriter.Null));
            using var doc = JsonDocument.Parse(bad.ToString());
            var message = doc.RootElement.GetProperty("error").GetProperty("message").GetString()!;
            Assert.Contains("Unknown option --scenaro", message, StringComparison.Ordinal);
            Assert.Contains("--level", message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
