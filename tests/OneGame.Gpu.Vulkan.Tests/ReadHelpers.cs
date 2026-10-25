namespace OneGame.Gpu.Vulkan.Tests;

/// <summary>Reads a buffer into a typed array.</summary>
internal static class TestRead
{
    public static async Task Buffer<T>(GpuDevice d, GpuBuffer buffer, T[] destination)
        where T : unmanaged
    {
        var bytes = new byte[destination.Length * System.Runtime.CompilerServices.Unsafe.SizeOf<T>()];
        await d.ReadBufferAsync(buffer, 0, bytes);
        System.Runtime.InteropServices.MemoryMarshal.Cast<byte, T>(bytes).CopyTo(destination);
    }
}
