using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace OneGame.Build.Tests;

/// <summary>
/// Proves the "only our code ships" guard: a throwaway project that imports the guard is given package references, and the
/// guard's check target must accept or reject them. The check runs before any package is downloaded, so no network is needed.
/// </summary>
public class PackageGuardTests
{
    private static readonly string RepoRoot = FindRoot();

    private static string FindRoot([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", ".."));

    private static (int Code, string Output) Check(string references, string extraProps = "")
    {
        var dir = Directory.CreateTempSubdirectory("og-guard-").FullName;
        try
        {
            var guard = Path.Combine(RepoRoot, "build", "PackageGuard.targets");
            File.WriteAllText(Path.Combine(dir, "Probe.csproj"), $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <OneGameShips>true</OneGameShips>
                    {extraProps}
                  </PropertyGroup>
                  <ItemGroup>
                    {references}
                  </ItemGroup>
                  <Import Project="{guard}" />
                </Project>
                """);
            var host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
            var psi = new ProcessStartInfo(host, "msbuild Probe.csproj -t:OneGameCheckPackages -nologo -v:m")
            {
                WorkingDirectory = dir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            psi.Environment["MSBUILDDISABLENODEREUSE"] = "1";
            using var process = Process.Start(psi)!;
            var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            process.WaitForExit();
            return (process.ExitCode, output);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ForbiddenPackageFailsTheBuild()
    {
        var (code, output) = Check("""<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />""");
        Assert.NotEqual(0, code);
        Assert.Contains("OGBUILD001", output, StringComparison.Ordinal);
        Assert.Contains("Newtonsoft.Json", output, StringComparison.Ordinal);
    }

    [Fact]
    public void OneFamilyPackagesPass()
    {
        var (code, output) = Check("""
            <PackageReference Include="RecursiveAI.OneFont" Version="1.0.0" />
            <PackageReference Include="OneYaml" Version="1.0.0" />
            """);
        Assert.True(code == 0, output);
    }

    [Fact]
    public void RoslynComponentMayUseCodeAnalysisPrivately()
    {
        var (code, output) = Check(
            """<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.14.0" PrivateAssets="all" />""",
            "<IsRoslynComponent>true</IsRoslynComponent>");
        Assert.True(code == 0, output);
    }

    [Fact]
    public void CodeAnalysisThatFlowsToConsumersFails()
    {
        var (code, output) = Check(
            """<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.14.0" />""",
            "<IsRoslynComponent>true</IsRoslynComponent>");
        Assert.NotEqual(0, code);
        Assert.Contains("OGBUILD001", output, StringComparison.Ordinal);
    }

    [Fact]
    public void CodeAnalysisInAnOrdinaryProjectFails()
    {
        var (code, _) = Check("""<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.14.0" PrivateAssets="all" />""");
        Assert.NotEqual(0, code);
    }
}
