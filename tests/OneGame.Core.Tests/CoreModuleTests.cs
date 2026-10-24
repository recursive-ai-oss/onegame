using OneGame.Core;

namespace OneGame.Core.Tests;

public class CoreModuleTests
{
    [Fact]
    public void ModuleIsNamedAfterItsAssembly() => Assert.Equal("OneGame.Core", CoreModule.Name);
}
