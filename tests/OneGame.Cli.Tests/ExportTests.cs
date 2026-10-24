using System.Text.Json;
using OneGame.Cli.Verbs;

namespace OneGame.Cli.Tests;

public sealed class ExportTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "onegame-export-" + Guid.NewGuid().ToString("N"));

    public ExportTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, true);

    private string Make(string relative, string text = "x")
    {
        var path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
        return path;
    }

    [Fact]
    public void ContentFoldersShipAndTheRestIsReported()
    {
        Make("proj/fonts/A.ttf");
        Make("proj/fonts/OFL.txt");
        Make("proj/fonts/notes.docx");
        Make("proj/art/icon.svg");
        Make("proj/art/raw.exr");
        Make("proj/data/items.yaml");
        Make("proj/scenarios/click.yaml");
        Make("proj/shaders/Ui.VSMain.spv");
        Make("proj/saves/slot1.bin");
        Make("proj/bin/Debug/x.dll");
        Directory.CreateDirectory(Path.Combine(_root, "out"));
        var skipped = new List<ExportSkipped>();
        var warnings = new List<ExportWarning>();

        ExportAssets.Copy(Path.Combine(_root, "proj"), Path.Combine(_root, "out"), skipped, warnings);

        var shipped = Directory.EnumerateFiles(Path.Combine(_root, "out"), "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(Path.Combine(_root, "out"), f).Replace('\\', '/')).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(["art/icon.svg", "data/items.yaml", "fonts/A.ttf", "fonts/OFL.txt"], shipped);
        var reasons = skipped.ToDictionary(s => s.Path, s => s.Reason);
        Assert.Contains("fonts/notes.docx", reasons.Keys);
        Assert.Contains("art/raw.exr", reasons.Keys);
        Assert.Contains("shaders/", reasons.Keys);
        Assert.Contains("saves/", reasons.Keys);
        Assert.DoesNotContain("scenarios/", reasons.Keys);
        Assert.DoesNotContain("bin/", reasons.Keys);
        Assert.Empty(warnings);
    }

    [Fact]
    public void NoticesCarryTheGeneratedCodeNoticesAndTheFontLicence()
    {
        Make("out/fonts/A.ttf");
        Make("out/fonts/OFL.txt", "SIL OPEN FONT LICENSE Version 1.1");
        var warnings = new List<ExportWarning>();

        var text = ExportAssets.Notices("demo", "2.0.1", "0.1.0", Path.Combine(_root, "out"), steam: true, warnings);

        Assert.Contains("demo 2.0.1", text, StringComparison.Ordinal);
        Assert.Contains("Kristian Høgsberg", text, StringComparison.Ordinal);
        Assert.Contains("Khronos Group", text, StringComparison.Ordinal);
        Assert.Contains("SIL OPEN FONT LICENSE Version 1.1", text, StringComparison.Ordinal);
        Assert.Contains("Steam API library", text, StringComparison.Ordinal);
        Assert.Empty(warnings);
    }

    [Fact]
    public void FontsWithoutALicenceAreReported()
    {
        Make("out/fonts/A.ttf");
        var warnings = new List<ExportWarning>();

        ExportAssets.Notices("demo", "1.0.0", "0.1.0", Path.Combine(_root, "out"), steam: false, warnings);

        Assert.Equal("font_licence_missing", Assert.Single(warnings).Code);
    }

    [Fact]
    public void SteamLibraryIsFoundInTheSuppliedFolderOnly()
    {
        var file = Make("redist/linux-x64/libsteam_api.so");
        Assert.Equal(file, ExportVerb.FindSteamLibrary("linux-x64", Path.Combine(_root, "redist")));
        var saved = Environment.GetEnvironmentVariable("ONEGAME_STEAM_REDIST");
        Environment.SetEnvironmentVariable("ONEGAME_STEAM_REDIST", null);
        try
        {
            var ex = Assert.Throws<CliException>(() => ExportVerb.FindSteamLibrary("win-x64", Path.Combine(_root, "redist")));
            Assert.Equal("steam_redist_missing", ex.Code);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ONEGAME_STEAM_REDIST", saved);
        }
    }

    [Fact]
    public void NewGameStartsWithTheDefaultFontAndItsLicence()
    {
        var output = new StringWriter();
        var code = Program.Run(["new", "fontgame", "--dir", Path.Combine(_root, "g"), "--engine", FakeEngine(), "--json"], output, new StringWriter());
        Assert.Equal(0, code);
        Assert.Contains("fonts/OFL.txt", output.ToString(), StringComparison.Ordinal);
        Assert.True(new FileInfo(Path.Combine(_root, "g", "fonts", "NotoSans-Regular.ttf")).Length > 100_000);
        Assert.Contains("SIL OPEN FONT LICENSE", File.ReadAllText(Path.Combine(_root, "g", "fonts", "OFL.txt")), StringComparison.Ordinal);
        using var doc = JsonDocument.Parse(output.ToString());
        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
    }

    [Fact]
    public void NewestGlibcIsReadFromTheVersionNames()
    {
        var file = Make("elf.bin", "\0GLIBC_2.5\0GLIBC_2.38\0GLIBC_2.31\0GLIBC_PRIVATE\0");
        Assert.Equal(new Version(2, 38), ExportVerb.NewestGlibc(file));
        Assert.Null(ExportVerb.NewestGlibc(Make("plain.bin", "nothing here")));
    }

    [Fact]
    public void UnknownTargetIsAUsageError()
    {
        var output = new StringWriter();
        var code = Program.Run(["export", "--target", "plan9", "--json"], output, new StringWriter());
        Assert.Equal(2, code);
        Assert.Contains("bad_target", output.ToString(), StringComparison.Ordinal);
    }

    // 'new' only needs a folder that looks like an engine checkout.
    private string FakeEngine()
    {
        Make("engine/OneGame.slnx", "<Solution />");
        return Path.Combine(_root, "engine");
    }
}
