using OneGame.Platform.Tests;

namespace OneGame.Platform.Tests;

/// <summary>The test app's entry point. xUnit's generated one would run tests off the main thread, which Cocoa cannot use.</summary>
internal static class Program
{
    public static int Main(string[] args) => MainThread.Host(args, Run);

    private static int Run(string[] args) =>
        args.Any(a => a == "--server" || a == "--internal-msbuild-node")
            ? Xunit.MicrosoftTestingPlatform.TestPlatformTestFramework.RunAsync(args, SelfRegisteredExtensions.AddSelfRegisteredExtensions).GetAwaiter().GetResult()
            : Xunit.Runner.InProc.SystemConsole.ConsoleRunner.Run(args).GetAwaiter().GetResult();
}
