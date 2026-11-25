using OneGame.Platform.Tests;

namespace OneGame.Windowed.Tests;

/// <summary>The test app's entry point. xUnit's generated one would run tests off the main thread, which Cocoa cannot use.</summary>
internal static class Program
{
    public static int Main(string[] args) => MainThread.Host(args, Run);

    // "--child-window <quit|close> <display>" is the child process of X11ExitTests, not a test run.
    private static int Run(string[] args) =>
        args is ["--child-window", var mode, var display]
            ? X11ExitTests.RunChild(mode, display)
            : args is ["--child-device-loop", var loops]
                ? DeviceLoopProbe.Run(int.Parse(loops, System.Globalization.CultureInfo.InvariantCulture))
                : args is ["--child-runs", var backend, var shown, var count, var definition, var extra]
                ? RunLoopTests.RunChild(backend, shown, int.Parse(count, System.Globalization.CultureInfo.InvariantCulture), definition == "same", extra == "-" ? "" : extra)
                : RunTests(args);

    private static int RunTests(string[] args) =>
        args.Any(a => a == "--server" || a == "--internal-msbuild-node")
            ? Xunit.MicrosoftTestingPlatform.TestPlatformTestFramework.RunAsync(args, SelfRegisteredExtensions.AddSelfRegisteredExtensions).GetAwaiter().GetResult()
            : Xunit.Runner.InProc.SystemConsole.ConsoleRunner.Run(args).GetAwaiter().GetResult();
}
