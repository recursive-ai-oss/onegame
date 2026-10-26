using OneGame.Platform;
using Xunit;

namespace OneGame.Platform.Tests;

public class RunningOnTests
{
    [Fact]
    public void ADesktopComputerIsADesktop()
    {
        Assert.False(RunningOn.Android);
        Assert.False(RunningOn.IOS);
        Assert.False(RunningOn.Browser);
        Assert.False(RunningOn.Phone);
        Assert.False(RunningOn.Mobile);
        Assert.True(RunningOn.Desktop);
    }
}
