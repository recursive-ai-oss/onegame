using OneGame.Gpu.Metal;
using OneGame.Gpu.Vulkan;

namespace OneGame.Gpu;

/// <summary>
/// Chooses and creates a graphics device. Metal is the default on macOS and iOS and Vulkan everywhere else; the
/// <c>ONEGAME_GPU_BACKEND</c> environment variable (<c>vulkan</c>, <c>metal</c> or <c>auto</c>) overrides it, for example to run the
/// Vulkan backend through a loader on a Mac.
/// </summary>
public static class GpuBackends
{
    /// <summary>The backend this process uses: <c>ONEGAME_GPU_BACKEND</c> when set, otherwise the platform's own.</summary>
    public static GpuBackend Selected
    {
        get
        {
            var value = Environment.GetEnvironmentVariable("ONEGAME_GPU_BACKEND");
            if (value is not null && value.Equals("metal", StringComparison.OrdinalIgnoreCase))
            {
                return GpuBackend.Metal;
            }

            if (value is not null && value.Equals("vulkan", StringComparison.OrdinalIgnoreCase))
            {
                return GpuBackend.Vulkan;
            }

            return OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() ? GpuBackend.Metal : GpuBackend.Vulkan;
        }
    }

    /// <summary>Whether the selected backend can run on this machine.</summary>
    public static bool IsAvailable => Selected == GpuBackend.Metal ? MetalGpu.IsAvailable : VulkanGpu.IsAvailable;

    /// <summary>Why the selected backend is unavailable, for an error message.</summary>
    public static string UnavailableReason => Selected == GpuBackend.Metal ? "No Metal device was found (the Metal backend needs macOS or iOS on arm64)." : "No Vulkan loader library was found.";

    /// <summary>Lists the adapters of the selected backend.</summary>
    public static IReadOnlyList<GpuAdapterInfo> EnumerateAdapters() =>
        Selected == GpuBackend.Metal ? MetalGpu.EnumerateAdapters() : VulkanGpu.EnumerateAdapters();

    /// <summary>Creates a device on the selected backend.</summary>
    /// <param name="options">Device options; <see cref="GpuDeviceOptions.Adapter"/> and <c>ONEGAME_GPU_ADAPTER</c> choose the adapter.</param>
    /// <exception cref="GpuUnsupportedException">The backend is not available on this machine.</exception>
    public static GpuDevice CreateDevice(GpuDeviceOptions? options = null) =>
        Selected == GpuBackend.Metal ? MetalGpu.CreateDevice(options) : VulkanGpu.CreateDevice(options);
}
