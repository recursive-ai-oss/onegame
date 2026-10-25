using System.Text.Json;
using OneGame.Control;
using OneGame.Host;

namespace OneGame.Testing.Tests;

/// <summary>Operations that write files put them under the output folder, and refuse every way out of it.</summary>
public class OutputPathTests
{
    private static TestOptions Options() => new() { Width = 320, Height = 200, Gpu = GpuMode.Off };

    [Fact]
    public void ARelativePathLandsUnderTheOutputFolderWithItsSubfolders()
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options());
        var c = cluster.Client;
        using var result = JsonDocument.Parse(c.Call("capture_frame", """{"size":"240x160","path":"shots/a.png"}"""));
        var expected = Path.Combine(cluster.OutputDirectory, "shots", "a.png");
        Assert.Equal(expected, result.RootElement.GetProperty("path").GetString());
        Assert.True(File.Exists(expected));
        Assert.False(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "shots", "a.png")), "nothing may be written next to the game");

        using var snap = JsonDocument.Parse(c.Call("snapshot_save", """{"path":"saves/one.snap"}"""));
        Assert.True(File.Exists(Path.Combine(cluster.OutputDirectory, "saves", "one.snap")));
        using var seq = JsonDocument.Parse(c.Call("capture_sequence", """{"dir":"seq/a","count":2,"size":"160x100"}"""));
        Assert.True(File.Exists(Path.Combine(cluster.OutputDirectory, "seq", "a", "frame-0001.png")));
        Assert.True(File.Exists(Path.Combine(cluster.OutputDirectory, "seq", "a", "manifest.json")));
    }

    [Theory]
    [InlineData("capture_frame", "../x.png")]
    [InlineData("capture_frame", "shots/../../x.png")]
    [InlineData("snapshot_save", "../../x.snap")]
    [InlineData("snapshot_load", "../x.snap")]
    public void APathThatClimbsOutIsRefusedWithAClearMessage(string op, string path)
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options());
        var ex = Assert.Throws<ControlException>(() => cluster.Client.Call(op, JsonSerializer.Serialize(new Dictionary<string, string> { ["path"] = path })));
        Assert.Equal("path_outside", ex.Code);
        Assert.Contains("output folder", ex.Message, StringComparison.Ordinal);
        Assert.Contains("..", ex.Message, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.GetFullPath(path, cluster.OutputDirectory)));
    }

    [Fact]
    public void SequenceFoldersAreCheckedToo()
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options());
        var ex = Assert.Throws<ControlException>(() => cluster.Client.Call("capture_sequence", """{"dir":"../seq","count":1}"""));
        Assert.Equal("path_outside", ex.Code);
    }

    [Fact]
    public void AnAbsolutePathIsRefusedEvenInsideTheOutputFolder()
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options());
        var inside = Path.Combine(cluster.OutputDirectory, "a.png");
        foreach (var path in new[] { inside, "/etc/onegame-test.png" })
        {
            var ex = Assert.Throws<ControlException>(() => cluster.Client.Call("capture_frame", JsonSerializer.Serialize(new Dictionary<string, string> { ["path"] = path })));
            Assert.Equal("path_outside", ex.Code);
            Assert.Contains("absolute", ex.Message, StringComparison.Ordinal);
        }

        Assert.False(File.Exists(inside));
    }

    [Fact]
    public void EmptyPathsAreBadArguments()
    {
        using var cluster = TestCluster.Start(new ShopGame(), Options());
        Assert.Equal("bad_args", Assert.Throws<ControlException>(() => cluster.Client.Call("capture_frame", """{"path":""}""")).Code);
        Assert.Equal("bad_args", Assert.Throws<ControlException>(() => cluster.Client.Call("capture_frame", """{"path":"a\u0000b.png"}""")).Code);
    }

    [Fact]
    public void ALinkInsideTheFolderCannotLeadOut()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        using var cluster = TestCluster.Start(new ShopGame(), Options());
        var outside = Path.Combine(Path.GetTempPath(), "onegame-outside-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(outside);
        try
        {
            Directory.CreateSymbolicLink(Path.Combine(cluster.OutputDirectory, "link"), outside);
            var ex = Assert.Throws<ControlException>(() => cluster.Client.Call("capture_frame", """{"path":"link/a.png"}"""));
            Assert.Equal("path_outside", ex.Code);
            Assert.Contains("link", ex.Message, StringComparison.Ordinal);
            Assert.Empty(Directory.GetFiles(outside));

            // A link that stays inside is fine.
            Directory.CreateDirectory(Path.Combine(cluster.OutputDirectory, "real"));
            Directory.CreateSymbolicLink(Path.Combine(cluster.OutputDirectory, "alias"), Path.Combine(cluster.OutputDirectory, "real"));
            cluster.Client.Call("capture_frame", """{"path":"alias/a.png","size":"160x100"}""");
            Assert.True(File.Exists(Path.Combine(cluster.OutputDirectory, "real", "a.png")));
        }
        finally
        {
            Directory.Delete(outside, true);
        }
    }
}
