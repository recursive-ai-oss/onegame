using System.Runtime.InteropServices;
using OneGame.Gpu.Metal.Interop;

namespace OneGame.Gpu.Metal;

/// <summary>Entry point of the Metal backend.</summary>
public static unsafe class MetalGpu
{
    /// <summary>Whether this machine can run the backend: an Apple arm64 platform with a Metal device.</summary>
    public static bool IsAvailable
    {
        get
        {
            if (!Objc.Load())
            {
                return false;
            }

            using var pool = new Pool();
            var device = Objc.MTLCreateSystemDefaultDevice();
            if (device == 0)
            {
                return false;
            }

            Objc.objc_release(device);
            return true;
        }
    }

    /// <summary>Lists the Metal devices of this machine.</summary>
    public static IReadOnlyList<GpuAdapterInfo> EnumerateAdapters()
    {
        using var pool = new Pool();
        var devices = Devices();
        try
        {
            return devices.Select(Describe).ToArray();
        }
        finally
        {
            foreach (var d in devices)
            {
                Objc.objc_release(d);
            }
        }
    }

    /// <summary>
    /// Creates a device on the best Metal device (discrete first, then the one driving the display), or the one chosen by
    /// <see cref="GpuDeviceOptions.Adapter"/> or the ONEGAME_GPU_ADAPTER environment variable.
    /// </summary>
    /// <remarks>
    /// <see cref="GpuDeviceOptions.Validation"/> turns on Metal's API validation (the debug layer), which only takes effect when it is
    /// enabled before the first Metal call of the process; set <c>MTL_DEBUG_LAYER=1</c> in the environment to be sure.
    /// With validation on, a Metal error ends the process with the message, so tests cannot pass over one.
    /// </remarks>
    /// <exception cref="GpuUnsupportedException">No Metal device, not an Apple arm64 platform, or validation was required but is off.</exception>
    public static GpuDevice CreateDevice(GpuDeviceOptions? options = null)
    {
        options ??= new GpuDeviceOptions();
        if (options.Validation || options.RequireValidation)
        {
            // Read when the Metal framework creates its first device, so it has to be set before anything loads the framework.
            Objc.setenv("MTL_DEBUG_LAYER", "1", 0);
            Objc.setenv("MTL_DEBUG_LAYER_ERROR_MODE", "assert", 0);
        }

        if (!Objc.Load())
        {
            throw new GpuUnsupportedException("the Metal backend needs macOS or iOS on arm64");
        }

        using var pool = new Pool();
        var devices = Devices();
        var chosen = Pick(devices, options.Adapter ?? Environment.GetEnvironmentVariable("ONEGAME_GPU_ADAPTER"));
        foreach (var d in devices)
        {
            if (d != chosen)
            {
                Objc.objc_release(d);
            }
        }

        // The debug layer wraps the device in a class whose name says so.
        var debug = Objc.ClassName(chosen).Contains("Debug", StringComparison.OrdinalIgnoreCase);
        if (options.RequireValidation && !debug)
        {
            Objc.objc_release(chosen);
            throw new GpuUnsupportedException("Metal API validation is not active; set MTL_DEBUG_LAYER=1 before the process starts");
        }

        return new MetalDevice(chosen, Describe(chosen), debug);
    }

    /// <summary>A <c>CAMetalLayer</c> that belongs to no view, with one reference for the caller to release. Lets tests and tools present without a window.</summary>
    internal static nint CreateDetachedLayer() => Objc.objc_retain(Msg.Obj(Cls.CAMetalLayer, Sel.layer));

    /// <summary>Whether Metal's API validation wraps this device.</summary>
    internal static bool IsValidationActive(GpuDevice device) => device is MetalDevice { DebugLayer: true };

    // Every device, each with one reference the caller releases.
    static nint[] Devices()
    {
        var copyAll = Objc.MetalExport("MTLCopyAllDevices");
        if (copyAll != 0)
        {
            var array = ((delegate* unmanaged<nint>)copyAll)();
            var count = (int)Msg.UL(array, Sel.count);
            var result = new nint[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = Objc.objc_retain(Msg.Obj(array, Sel.objectAtIndex_, (nuint)i));
            }

            Objc.objc_release(array);
            if (count > 0)
            {
                return result;
            }
        }

        var one = Objc.MTLCreateSystemDefaultDevice();
        return one == 0 ? [] : [one];
    }

    static nint Pick(nint[] devices, string? wanted)
    {
        if (devices.Length == 0)
        {
            throw new GpuUnsupportedException("no Metal device found");
        }

        if (!string.IsNullOrEmpty(wanted))
        {
            foreach (var d in devices)
            {
                var info = Describe(d);
                if (info.Name.Contains(wanted, StringComparison.OrdinalIgnoreCase) || info.Kind.ToString().Equals(wanted, StringComparison.OrdinalIgnoreCase))
                {
                    return d;
                }
            }

            throw new GpuUnsupportedException($"no Metal device matches '{wanted}'");
        }

        foreach (var d in devices)
        {
            if (Describe(d).Kind == AdapterKind.Discrete)
            {
                return d;
            }
        }

        return devices[0];
    }

    static GpuAdapterInfo Describe(nint device)
    {
        var name = Objc.ToManaged(Msg.Obj(device, Sel.name));
        var unified = Msg.Flag(device, Sel.hasUnifiedMemory);
        var kind = unified ? AdapterKind.Integrated : AdapterKind.Discrete;
        // MTLGPUFamilyMetal3 is 5001 and MTLGPUFamilyMetal4 is 5002.
        var api = Msg.Flag(device, Sel.supportsFamily_, (nint)5002) ? "Metal 4" : Msg.Flag(device, Sel.supportsFamily_, (nint)5001) ? "Metal 3" : "Metal 2";
        return new GpuAdapterInfo(name, kind, unified ? 0x106Bu : 0u, 0, api);
    }
}
