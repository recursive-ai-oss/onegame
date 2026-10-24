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

// The UI: build a tree, style and lay it out, produce a display list, query, click and dump.
var ui = new UiTree();
ui.SetViewport(320, 200);
ui.AddStyleSheet(new StyleSheet()
    .Rule(":root", s => s.FontSize(10).Token("--accent", Color.Hex("#d9a441")))
    .Rule(".go", s => s.Var("background-color", "--accent").Padding(4, 10))
    .Rule(".go:hover", s => s.Opacity(0.5f)));
int[] rowIds = [1, 2, 3];
var clicks = 0;
var score = 0;
ui.Root.With(Ui.Column(
    Ui.Label("Score").BindText(() => score, v => "Score " + v.ToString(System.Globalization.CultureInfo.InvariantCulture)).Named("score"),
    Ui.Button("Go", () => clicks++).Named("go").Class("go"),
    Ui.Each(() => rowIds, i => Ui.Label("row").Named("r" + i))).Gap(4).Padding(8));
var list = ui.Frame();
var go = ui.Find("go")!;
var c = go.AbsoluteBounds;
ui.Dispatch(UiEvent.PointerDown(c.X + 2, c.Y + 2));
ui.Dispatch(UiEvent.PointerUp(c.X + 2, c.Y + 2));
score = 7;
ui.Frame();
if (clicks != 1 || ui.Find("score")!.Text != "Score 7" || list.Commands.Length == 0 || !ui.DumpJson().Contains("\"go\"", StringComparison.Ordinal)
    || ui.Query(".go:hover").Count != 1 || ui.Focused != go)
{
    Console.Error.WriteLine($"ui smoke failed: clicks={clicks} score={ui.Find("score")!.Text} commands={list.Commands.Length} hover={ui.Query(".go:hover").Count}");
    return 1;
}

Console.WriteLine($"ok: ui {list.Commands.Length} commands, go at {go.AbsoluteBounds}");

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
