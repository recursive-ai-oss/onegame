using System.Text.Json;

namespace OneGame.Gpu.WebGpu;

/// <summary>Creates WebGPU devices in the browser.</summary>
public static class WebGpu
{
    /// <summary>True when the page has a WebGPU implementation (navigator.gpu exists).</summary>
    public static bool IsAvailable => OperatingSystem.IsBrowser();

    /// <summary>Runs everything recorded so far and waits until the GPU has finished it, so errors it caused have been reported.</summary>
    public static async Task SettleAsync(GpuDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        device.WaitIdle();
        await Js.WhenIdle().ConfigureAwait(true);
        await Task.Delay(5).ConfigureAwait(true);
    }

    /// <summary>
    /// Asks the browser for an adapter and a device. The browser answers asynchronously, so a game awaits this before it creates its
    /// instance and hands the device to it.
    /// </summary>
    /// <exception cref="GpuUnsupportedException">The browser has no WebGPU, or no adapter.</exception>
    public static async Task<GpuDevice> CreateDeviceAsync(GpuDeviceOptions? options = null)
    {
        if (!OperatingSystem.IsBrowser())
        {
            throw new GpuUnsupportedException("The WebGPU backend runs inside a browser.");
        }

        options ??= new GpuDeviceOptions();
        string json;
        try
        {
            json = await Js.Init("{\"fallback\":" + (options.Adapter == "software" ? "true" : "false") + "}").ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.JavaScript.JSException)
        {
            throw new GpuUnsupportedException(ex.Message);
        }

        using var doc = JsonDocument.Parse(json);
        var r = doc.RootElement;
        var features = new HashSet<string>(StringComparer.Ordinal);
        foreach (var f in r.GetProperty("features").EnumerateArray())
        {
            features.Add(f.GetString() ?? "");
        }

        var info = new GpuAdapterInfo(
            r.GetProperty("name").GetString() ?? "WebGPU",
            r.GetProperty("software").GetBoolean() ? AdapterKind.Software : AdapterKind.Other,
            0,
            0,
            "WebGPU");
        var caps = new GpuCapabilities(
            Timestamps: false,
            MultiDrawIndirect: false,
            IndirectFirstInstance: features.Contains("indirect-first-instance"),
            DepthClamp: features.Contains("depth-clip-control"),
            DepthBiasClamp: true,
            NonUniformArrayIndexing: false,
            MaxSampleCount: 4,
            MaxTextureSize: (uint)r.GetProperty("maxTexture2D").GetInt32(),
            MaxTextureSize3D: (uint)r.GetProperty("maxTexture3D").GetInt32(),
            MaxArrayLayers: (uint)r.GetProperty("maxLayers").GetInt32(),
            MaxResourceArrayCount: 1);
        return new WebGpuDevice(info, caps);
    }
}
