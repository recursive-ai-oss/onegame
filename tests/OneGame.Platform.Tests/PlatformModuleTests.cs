using OneGame.Platform;

namespace OneGame.Platform.Tests;

public class PlatformModuleTests
{
    [Fact]
    public void ModuleIsNamedAfterItsAssembly() => Assert.Equal("OneGame.Platform", PlatformModule.Name);
}
