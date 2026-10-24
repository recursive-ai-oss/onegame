using OneGame.Gpu;

namespace OneGame.Gpu.Tests;

public class GpuModuleTests
{
    [Fact]
    public void ModuleIsNamedAfterItsAssembly() => Assert.Equal("OneGame.Gpu", GpuModule.Name);
}
