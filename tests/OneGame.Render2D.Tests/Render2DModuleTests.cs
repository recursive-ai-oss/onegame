using OneGame.Render2D;

namespace OneGame.Render2D.Tests;

public class Render2DModuleTests
{
    [Fact]
    public void ModuleIsNamedAfterItsAssembly() => Assert.Equal("OneGame.Render2D", Render2DModule.Name);
}
