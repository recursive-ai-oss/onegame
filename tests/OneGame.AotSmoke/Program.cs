using OneGame.Audio;
using OneGame.Control;
using OneGame.Core;
using OneGame.Gpu;
using OneGame.Gpu.Vulkan;
using OneGame.Platform;
using OneGame.Render2D;
using OneGame.Steam;
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

// The platform layer, headless: a window, a synthetic key through the shared event path, and a controller mapping line.
using (var app = PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Headless, EnableGamepadDevices = false }))
{
    var window = app.CreateWindow(new WindowOptions { Width = 320, Height = 200 });
    app.Injector.Key(window.Id, Key.A, true);
    app.PumpEvents();
    var mapped = GamepadMapping.TryParse("030000005e0400008e02000014010000,Xbox 360 Controller,a:b0,leftx:a0,dpup:h0.1,platform:Linux,", out _);
    if (!app.TryGetEvent(out var e) || e.Key != Key.A || !app.Input.IsKeyDown(Key.A) || !mapped)
    {
        Console.Error.WriteLine("platform smoke failed");
        return 1;
    }
}

Console.WriteLine($"ok: {names.Length} assemblies");
return 0;
