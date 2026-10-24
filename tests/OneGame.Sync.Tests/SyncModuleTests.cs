using OneGame.Sync;

namespace OneGame.Sync.Tests;

public class SyncModuleTests
{
    [Fact]
    public void ModuleIsNamedAfterItsAssembly() => Assert.Equal("OneGame.Sync", SyncModule.Name);
}
