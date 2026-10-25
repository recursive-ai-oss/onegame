using OneGame.Platform;

namespace OneGame.Windowed.Tests;

/// <summary>The windowed suite on macOS: a Cocoa window with a Metal layer swapchain, driven through the control layer.</summary>
[Collection("cocoa")]
public sealed class CocoaWindowedTests : WindowedTests
{
    private protected override bool IsX11 => false;

    private protected override string BackendName => "cocoa";

    private protected override bool HonorsRequestedSize => true;

    private protected override Session Start()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "needs macOS");
        return new Session(PlatformBackendKind.Cocoa, "");
    }
}
