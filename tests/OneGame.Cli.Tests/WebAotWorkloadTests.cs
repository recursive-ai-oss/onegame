using OneGame.Cli.Verbs;

namespace OneGame.Cli.Tests;

public class WebAotWorkloadTests
{
    [Fact]
    public void AMissingWorkloadNamesTheInstallCommand()
    {
        var ex = Assert.Throws<CliException>(() => WebExport.RequireAotWorkload("Installed Workload Id      Manifest Version      Installation Source\n----\nandroid   1.0   SDK 10.0.400\n"));
        Assert.Equal("missing_workload", ex.Code);
        Assert.Contains("dotnet workload install wasm-tools", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnInstalledWorkloadPasses() =>
        WebExport.RequireAotWorkload("Installed Workload Id      Manifest Version      Installation Source\n----\nwasm-tools                 10.0.114/10.0.100      SDK 10.0.400\n");
}
