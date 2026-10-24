using OneGame.Control;

namespace OneGame.Control.Tests;

public class ControlModuleTests
{
    [Fact]
    public void ModuleIsNamedAfterItsAssembly() => Assert.Equal("OneGame.Control", ControlModule.Name);
}
