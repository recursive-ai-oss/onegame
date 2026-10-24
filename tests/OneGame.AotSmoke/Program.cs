using OneGame.Audio;
using OneGame.Control;
using OneGame.Core;
using OneGame.Gpu;
using OneGame.Gpu.Vulkan;
using OneGame.Platform;
using OneGame.Render2D;
using OneGame.Steam;
using OneGame.Stores;
using OneGame.Sync;
using OneGame.Testing;
using OneGame.UI;

// Touches every runtime assembly so the native publish has to compile and trim all of them.
string[] names =
[
    CoreModule.Name,
    SyncModule.Name,
    GpuModule.Name,
    VulkanModule.Name,
    PlatformModule.Name,
    Render2DModule.Name,
    UIModule.Name,
    AudioModule.Name,
    SteamModule.Name,
    ControlModule.Name,
    TestingModule.Name,
];

foreach (var name in names)
{
    if (!name.StartsWith("OneGame.", StringComparison.Ordinal))
    {
        Console.Error.WriteLine($"unexpected assembly name: {name}");
        return 1;
    }
}

Console.WriteLine($"ok: {names.Length} assemblies");

// Store: connect to Steam. Without the library or a running client this must fall back to no-op services, never throw.
using (var store = SteamPlatform.Start())
{
    store.Pump();
    var unlock = store.Achievements.Unlock("SMOKE");
    var wrote = store.Cloud.Write("smoke.bin", [1, 2, 3]);
    if (!store.IsAvailable && (unlock != StoreResult.Unavailable || wrote != StoreResult.Unavailable))
    {
        Console.Error.WriteLine("store: unavailable store returned a result other than Unavailable");
        return 1;
    }

    Console.WriteLine(store.IsAvailable ? $"store: {store.Name} connected as {store.Identity.DisplayName}" : $"store: none ({store.UnavailableReason})");
}

// Graphics: create a device, clear an offscreen target and read it back. "--require-gpu" turns a missing Vulkan driver into a failure.
var requireGpu = args.Contains("--require-gpu");
if (!VulkanGpu.IsAvailable)
{
    Console.WriteLine("gpu: no Vulkan loader on this machine");
    return requireGpu ? 1 : 0;
}

using var device = VulkanGpu.CreateDevice();
using var target = device.CreateTexture(new TextureDesc(8, 8, GpuFormat.Rgba8Unorm, TextureUsage.RenderTarget | TextureUsage.CopySource));
var encoder = device.BeginCommands();
encoder.BeginRenderPass(ColorAttachment.Clear(target, 1f, 0.5f, 0f, 1f)).EndPass();
device.Submit(encoder);
var pixels = new byte[8 * 8 * 4];
device.ReadTexture(target, 0, 0, pixels);
if (pixels[0] != 255 || pixels[1] is < 127 or > 128 || pixels[2] != 0 || pixels[3] != 255)
{
    Console.Error.WriteLine($"gpu: unexpected pixel {pixels[0]},{pixels[1]},{pixels[2]},{pixels[3]}");
    return 1;
}

Console.WriteLine($"gpu: {device.Adapter.Name} ({device.Adapter.Kind}) cleared and read back 8x8");
return 0;
