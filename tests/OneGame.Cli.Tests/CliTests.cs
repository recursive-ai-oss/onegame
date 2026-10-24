using System.Text.Json;

namespace OneGame.Cli.Tests;

public class CliTests
{
    private static (int Code, string Out) Run(params string[] args)
    {
        var output = new StringWriter();
        var code = Program.Run(args, output, new StringWriter());
        return (code, output.ToString());
    }

    [Fact]
    public void VersionJsonIsOneObject()
    {
        var (code, text) = Run("--version", "--json");
        Assert.Equal(0, code);
        using var doc = JsonDocument.Parse(text);
        Assert.Equal("onegame", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal("0.1.0", doc.RootElement.GetProperty("version").GetString());
    }

    [Fact]
    public void HelpJsonListsHelp()
    {
        var (code, text) = Run("help", "--json");
        Assert.Equal(0, code);
        using var doc = JsonDocument.Parse(text);
        Assert.Equal("help", doc.RootElement.GetProperty("commands")[0].GetProperty("name").GetString());
    }

    [Fact]
    public void UnknownVerbFails()
    {
        var (code, text) = Run("nope", "--json");
        Assert.Equal(2, code);
        using var doc = JsonDocument.Parse(text);
        Assert.False(doc.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("unknown_verb", doc.RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.Contains("nope", doc.RootElement.GetProperty("error").GetProperty("message").GetString(), StringComparison.Ordinal);
    }
}
