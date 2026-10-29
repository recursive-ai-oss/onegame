using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace OneGame.Build.Tests;

/// <summary>The release package check allows one preview package (Physics) and refuses every other prerelease.</summary>
public class CheckPackagesTests
{
    private static string Script([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "tools", "ci", "check-packages.py"));

    private static (int Code, string Output) Run(string engine, params (string Id, string Version, string? Dependency)[] packages)
    {
        var dir = Directory.CreateTempSubdirectory("og-pkgs-").FullName;
        try
        {
            foreach (var (id, version, dependency) in packages)
            {
                var deps = dependency is null ? "" : $"<dependencies><dependency id=\"Other\" version=\"{dependency}\" /></dependencies>";
                using var zip = ZipFile.Open(Path.Combine(dir, $"{id}.{version}.nupkg"), ZipArchiveMode.Create);
                using var writer = new StreamWriter(zip.CreateEntry($"{id}.nuspec").Open());
                writer.Write($"<package><metadata><id>{id}</id><version>{version}</version>{deps}</metadata></package>");
            }

            var psi = new ProcessStartInfo("python3") { RedirectStandardOutput = true, RedirectStandardError = true };
            psi.ArgumentList.Add(Script());
            psi.ArgumentList.Add(dir);
            psi.ArgumentList.Add(engine);
            using var process = Process.Start(psi)!;
            var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            process.WaitForExit();
            return (process.ExitCode, output);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void StablePackagesPlusThePhysicsPreviewPass()
    {
        var (code, output) = Run("1.15.0", ("RecursiveAI.OneGame.Core", "1.15.0", null), ("RecursiveAI.OneGame.Physics", "1.15.0-preview.1", "0.1.0-preview.5"));
        Assert.True(code == 0, output);
    }

    [Fact]
    public void AnyOtherPrereleaseIsRefused()
    {
        Assert.NotEqual(0, Run("1.15.0", ("RecursiveAI.OneGame.Core", "1.15.0-preview.1", null)).Code);
        Assert.NotEqual(0, Run("1.15.0", ("RecursiveAI.OneGame.Navigation", "1.15.0-preview.1", null)).Code);
        Assert.NotEqual(0, Run("1.15.0", ("RecursiveAI.OneGame.Core", "1.15.0", "1.0.0-rc.1")).Code);
    }

    [Fact]
    public void PhysicsMustBeAPreviewOfTheEngineVersion()
    {
        Assert.NotEqual(0, Run("1.15.0", ("RecursiveAI.OneGame.Physics", "1.15.0", null)).Code);
        Assert.NotEqual(0, Run("1.15.0", ("RecursiveAI.OneGame.Physics", "1.14.0-preview.1", null)).Code);
    }
}
