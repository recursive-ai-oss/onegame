using OneGame.Gpu.Vulkan.Interop;

namespace OneGame.Gpu.Vulkan;

/// <summary>Entry point of the Vulkan backend.</summary>
public static class VulkanGpu
{
    /// <summary>Whether a Vulkan loader library is present on this machine.</summary>
    public static bool IsAvailable => VulkanLoader.IsAvailable();

    /// <summary>Lists the adapters that can run the backend (Vulkan 1.3 with a graphics and compute queue).</summary>
    public static IReadOnlyList<GpuAdapterInfo> EnumerateAdapters()
    {
        var inst = VulkanInstance.Create(new GpuDeviceOptions());
        try
        {
            return inst.Adapters.Select(a => a.Info).ToArray();
        }
        finally
        {
            inst.Release();
        }
    }

    /// <summary>
    /// Creates a device on the best adapter, or the one chosen by <see cref="GpuDeviceOptions.Adapter"/> or the
    /// ONEGAME_GPU_ADAPTER environment variable.
    /// </summary>
    /// <exception cref="GpuUnsupportedException">No Vulkan loader, no suitable adapter, or the validation layer was required but is missing.</exception>
    public static GpuDevice CreateDevice(GpuDeviceOptions? options = null)
    {
        options ??= new GpuDeviceOptions();
        var inst = VulkanInstance.Create(options);
        try
        {
            return VulkanDevice.Create(options, inst, inst.Pick(options.Adapter));
        }
        catch
        {
            inst.Release();
            throw;
        }
    }
}
