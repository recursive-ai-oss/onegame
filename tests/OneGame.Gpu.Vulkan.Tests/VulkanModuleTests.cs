using OneGame.Gpu.Vulkan;

namespace OneGame.Gpu.Vulkan.Tests;

public class VulkanModuleTests
{
    [Fact]
    public void ModuleIsNamedAfterItsAssembly() => Assert.Equal("OneGame.Gpu.Vulkan", VulkanModule.Name);
}
