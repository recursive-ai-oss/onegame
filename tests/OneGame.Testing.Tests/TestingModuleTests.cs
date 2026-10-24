using OneGame.Testing;

namespace OneGame.Testing.Tests;

public class TestingModuleTests
{
    [Fact]
    public void ModuleIsNamedAfterItsAssembly() => Assert.Equal("OneGame.Testing", TestingModule.Name);
}
