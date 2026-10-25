using System.Text.Json;
using OneGame.Testing;

namespace Lanterns.Tests;

/// <summary>A bot client in the process cluster draws the 3D view from the scopes it follows.</summary>
public class ClusterViewTests
{
    [Fact]
    public void ABotsCaptureShowsTheThreeDimensionalViewDrawnFromItsReplicas()
    {
        var spec = ClusterTests.Spec("lanterns-3d");
        spec = spec with { Nodes = [.. spec.Nodes.Select(n => n.Role == NodeRole.Client ? n with { Args = [.. n.Args, "--gpu", "auto"] } : n)] };
        using var cluster = TestCluster.Launch(spec);
        Assert.True(cluster.WaitForTick("bot1", 600));
        Assert.True(cluster.WaitForTick("bot2", 600));
        var dir = Path.Combine(Path.GetTempPath(), "lanterns-3d");
        Directory.CreateDirectory(dir);
        foreach (var bot in new[] { "bot1", "bot2" })
        {
            using var doc = JsonDocument.Parse(cluster.Call(bot, "capture_frame", "{\"size\":\"720p\",\"path\":\"" + bot + "-3d.png\"}"));
            var renderer = doc.RootElement.GetProperty("renderer").GetString()!;
            Assert.NotEqual("cpu", renderer);
            var path = doc.RootElement.GetProperty("path").GetString()!;
            var frame = OneGame.Host.Png.Load(path);

            // Lit islands, sea and lanterns: far more than the flat panels of the UI alone.
            var colours = new HashSet<int>();
            for (var i = 0; i < frame.Rgba.Length; i += 4 * 13)
            {
                colours.Add((frame.Rgba[i] >> 3) | ((frame.Rgba[i + 1] >> 3) << 5) | ((frame.Rgba[i + 2] >> 3) << 10));
            }

            Assert.True(colours.Count > 300, $"{bot}: {colours.Count} colours");
            File.Copy(path, Path.Combine(dir, bot + "-3d.png"), true);
        }

        var report = cluster.Hashes();
        Assert.True(report.Ok, string.Join("; ", report.Problems));
    }
}
