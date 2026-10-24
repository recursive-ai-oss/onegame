using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: DisableRuntimeMarshalling]

namespace OneGame.Gpu.Vulkan.Interop;

/// <summary>The single native entry into the Vulkan loader; every other function is reached through it by pointer.</summary>
internal static unsafe partial class VulkanLoader
{
    const string LibraryAlias = "onegame-vulkan-loader";

    static VulkanLoader()
    {
        NativeLibrary.SetDllImportResolver(typeof(VulkanLoader).Assembly, Resolve);
    }

    /// <summary>Whether a Vulkan loader library can be found on this machine.</summary>
    public static bool IsAvailable()
    {
        foreach (var name in CandidateNames())
        {
            if (NativeLibrary.TryLoad(name, out var handle))
            {
                NativeLibrary.Free(handle);
                return true;
            }
        }

        return false;
    }

    static string[] CandidateNames() =>
        OperatingSystem.IsWindows() ? ["vulkan-1.dll"]
        : OperatingSystem.IsMacOS() ? ["libvulkan.1.dylib", "libMoltenVK.dylib"]
        : ["libvulkan.so.1", "libvulkan.so"];

    static nint Resolve(string name, Assembly assembly, DllImportSearchPath? path)
    {
        if (name != LibraryAlias)
        {
            return 0;
        }

        foreach (var candidate in CandidateNames())
        {
            if (NativeLibrary.TryLoad(candidate, out var handle))
            {
                return handle;
            }
        }

        return 0;
    }

    /// <summary>Finds an instance-level or global entry point (Vulkan 1.3 §4.1: vkGetInstanceProcAddr).</summary>
    [LibraryImport(LibraryAlias, EntryPoint = "vkGetInstanceProcAddr")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nint GetInstanceProcAddr(nint instance, byte* name);
}
