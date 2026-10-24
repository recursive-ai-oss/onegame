using OneGame.Steam;

namespace OneGame.Steam.Tests;

public class SteamModuleTests
{
    [Fact]
    public void ModuleIsNamedAfterItsAssembly() => Assert.Equal("OneGame.Steam", SteamModule.Name);
}
