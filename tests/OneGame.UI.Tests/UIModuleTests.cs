using OneGame.UI;

namespace OneGame.UI.Tests;

public class UIModuleTests
{
    [Fact]
    public void ModuleIsNamedAfterItsAssembly() => Assert.Equal("OneGame.UI", UIModule.Name);
}
