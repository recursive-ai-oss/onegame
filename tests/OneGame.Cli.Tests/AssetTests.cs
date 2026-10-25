using System.Text.Json;

namespace OneGame.Cli.Tests;

public class AssetTests
{
    private static string Model(string relative) => Path.Combine(AppContext.BaseDirectory, "testdata", "gltf", relative);

    private static (int Code, string Out, string Err) Run(params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var code = Program.Run(args, output, error);
        return (code, output.ToString(), error.ToString());
    }

    [Fact]
    public void InspectPrintsOneJsonObjectForASkinnedAnimatedModel()
    {
        var (code, text, _) = Run("asset", "inspect", Model("Fox/glTF-Binary/Fox.glb"), "--json");
        Assert.Equal(0, code);
        using var doc = JsonDocument.Parse(text);
        var r = doc.RootElement;
        Assert.True(r.GetProperty("ok").GetBoolean());
        Assert.Equal("glb", r.GetProperty("format").GetString());
        var counts = r.GetProperty("counts");
        Assert.Equal(26, counts.GetProperty("nodes").GetInt32());
        Assert.Equal(1, counts.GetProperty("meshes").GetInt32());
        Assert.Equal(1728, counts.GetProperty("vertices").GetInt64());
        Assert.Equal(576, counts.GetProperty("triangles").GetInt64());
        Assert.Equal(3, counts.GetProperty("animations").GetInt32());
        Assert.Equal(1, counts.GetProperty("skins").GetInt32());
        Assert.Equal("Run", r.GetProperty("animations")[2].GetProperty("name").GetString());
        Assert.True(r.GetProperty("animations")[2].GetProperty("duration").GetSingle() > 0.5f);
        Assert.True(r.GetProperty("meshes")[0].GetProperty("skinned").GetBoolean());
        Assert.Equal("HeaderOnly", r.GetProperty("images")[0].GetProperty("status").GetString());
        Assert.Equal("png", r.GetProperty("images")[0].GetProperty("format").GetString());
        Assert.Equal(3, r.GetProperty("bounds").GetProperty("min").GetArrayLength());
        Assert.Equal(0, r.GetProperty("warnings").GetArrayLength());
        Assert.False(r.GetProperty("truncated").GetBoolean());
        Assert.Single(text.Split('\n', StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void InspectListsAreCappedAndSaySo()
    {
        var (code, text, _) = Run("asset", "inspect", Model("Fox/glTF/Fox.gltf"), "--max-items", "1", "--json");
        Assert.Equal(0, code);
        using var doc = JsonDocument.Parse(text);
        Assert.True(doc.RootElement.GetProperty("truncated").GetBoolean());
        Assert.Equal(1, doc.RootElement.GetProperty("animations").GetArrayLength());
        Assert.Equal(3, doc.RootElement.GetProperty("counts").GetProperty("animations").GetInt32());
    }

    [Fact]
    public void InspectWithoutJsonPrintsReadableText()
    {
        var (code, text, _) = Run("asset", "inspect", Model("Box/glTF/Box.gltf"));
        Assert.Equal(0, code);
        Assert.Contains("vertices 24", text, StringComparison.Ordinal);
        Assert.Contains("triangles 12", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ADamagedFileIsReportedWithItsCode()
    {
        var dir = Path.Combine(Path.GetTempPath(), "inspect-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var bad = Path.Combine(dir, "bad.gltf");
            File.WriteAllText(bad, "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"byteLength\":1000000000000,\"uri\":\"x.bin\"}]}");
            var (code, text, _) = Run("asset", "inspect", bad, "--json");
            Assert.Equal(1, code);
            using var doc = JsonDocument.Parse(text);
            Assert.False(doc.RootElement.GetProperty("ok").GetBoolean());
            Assert.Equal("gltf_limit_exceeded", doc.RootElement.GetProperty("error").GetProperty("code").GetString());

            var (missing, _, _) = Run("asset", "inspect", Path.Combine(dir, "none.glb"), "--json");
            Assert.Equal(4, missing);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void BadArgumentsAreUsageErrors()
    {
        Assert.Equal(2, Run("asset").Code);
        Assert.Equal(2, Run("asset", "inspect").Code);
        Assert.Equal(2, Run("asset", "inspect", "a.glb", "b.glb").Code);
        Assert.Equal(2, Run("asset", "convert", "a.glb").Code);
        Assert.Equal(2, Run("asset", "inspect", "a.glb", "--nope").Code);
    }
}
