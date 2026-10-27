using OneGame.AotSmoke;
using OneGame.Atlas;
using OneGame.Audio;
using OneGame.Control;
using OneGame.Core;
using OneGame.Gpu;
using OneGame.Gpu.Vulkan;
using OneGame.Net;
using OneGame.Platform;
using OneGame.Render2D;
using OneGame.Steam;
using OneGame.Stores;
using OneGame.Sync;
using OneGame.Text;
using OneGame.Testing;
using OneGame.UI;
using OneGame.UI.Kit;

// Touches every runtime assembly so the native publish has to compile and trim all of them.
string[] names =
[
    CoreModule.Name,
    SyncModule.Name,
    NetModule.Name,
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

// Touch: two fingers through the shared event path; the first one also drives the pointer.
using (var app = PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Headless, EnableGamepadDevices = false }))
{
    var window = app.CreateWindow();
    app.Injector.TouchDown(window.Id, 0, 30, 40);
    app.Injector.TouchDown(window.Id, 1, 90, 40);
    var pressed = app.Input.IsButtonDown(MouseButton.Left) && app.Input.TouchCount == 2 && app.Input.PointerX == 30;
    app.Injector.TouchUp(window.Id, 0, 30, 40);
    if (!pressed || app.Input.IsButtonDown(MouseButton.Left) || app.Input.TouchCount != 1)
    {
        Console.Error.WriteLine("touch smoke failed");
        return 1;
    }

    Console.WriteLine("ok: touch drives the pointer with the first finger only");
}

// Audio: the capture path (no device, exact frame counts) and the no-device path through the real OS backend, which must stay silent and alive.
using (var audio = AudioService.Create(new AudioServiceOptions { Capture = true }))
{
    // Decodes Vorbis (44.1 kHz into the 48 kHz output) and Opus through the real mixer, with a pitch change and a stream.
    audio.Play(audio.LoadSound(ReadAsset("tone44k.ogg")));
    audio.Play(audio.LoadSound(ReadAsset("loop48k.opus")), new PlayOptions { Pitch = 1.5f });
    audio.PlayMusic(ReadAsset("tone44k.ogg"));
    audio.Advance(4800);
    audio.Update();
    if (audio.Capture is not { FramesCaptured: 4800 } || !(audio.Capture.Peak > 0.1f) || audio.Capture.Samples.ToArray().Any(v => !float.IsFinite(v)))
    {
        Console.Error.WriteLine("audio capture smoke failed");
        return 1;
    }

    Console.WriteLine($"ok: audio vorbis 44.1 kHz and opus decoded and mixed at {audio.Format.SampleRate} Hz, peak {audio.Capture.Peak:F3}");
}

// FLAC and MP3 decoding, bus filter, reverb, the master limiter and the loudness meter under NativeAOT.
using (var audio = AudioService.Create(new AudioServiceOptions { Capture = true, MasterLimiter = new LimiterOptions() }))
{
    audio.SetBusFilter(AudioBus.Sfx, AudioFilterKind.LowPass, 6000f);
    audio.SetBusReverb(AudioBus.Ui, new ReverbOptions());
    audio.Play(audio.LoadSound(ReadAsset("tone44k.flac")));
    audio.Play(audio.LoadSound(ReadAsset("tone44k.mp3")), new PlayOptions { Bus = AudioBus.Ui });
    audio.Advance(24_000);
    var status = audio.Describe();
    if (audio.GetBusLoudness(AudioBus.Ui) is not { } busLoud || !(busLoud.IntegratedLufs > -60) || status.Buses[2].Loudness?.IntegratedLufs is null || audio.TryEnableBusMeter(new AudioBus(5)) != BusMeterResult.NoFreeSlot || !audio.IsBusMetered(AudioBus.Ui))
    {
        Console.Error.WriteLine("audio bus loudness smoke failed");
        return 1;
    }

    // The device section and the lifecycle switch, which the phone and web backends are driven through.
    audio.SetSuspended(true);
    audio.SetSuspended(false);
    if (status.Device is not { Backend: "capture", FramesPlayed: 24_000 } || audio.Output.Suspensions != 1 || audio.Output.IsPumped)
    {
        Console.Error.WriteLine("audio device report smoke failed");
        return 1;
    }

    if (!(audio.Capture!.Peak > 0.1f) || status.Loudness.IntegratedLufs is not { } lufs || lufs > 0 || lufs < -40 || status.Limiter is null)
    {
        Console.Error.WriteLine($"audio effects smoke failed: peak {audio.Capture.Peak}, loudness {status.Loudness.IntegratedLufs}");
        return 1;
    }

    Console.WriteLine($"ok: audio flac and mp3 decoded, filter, reverb and limiter ran, {lufs:F1} LUFS");
}

using (var audio = AudioService.Create(new AudioServiceOptions { Device = new AudioDeviceOptions { DeviceName = "onegame_no_such_device" } }))
{
    audio.Update();
    if (audio.Output.Status != AudioOutputStatus.NoDevice || AudioService.DefaultBackend().Name.Length == 0)
    {
        Console.Error.WriteLine("audio no-device smoke failed");
        return 1;
    }
}

// Runs generated entity code under NativeAOT: state, commands, snapshot, restore, delta and hash.
var registry = new EntityRegistry();
OneGame.Generated.OneGame_AotSmoke.EntityTypes.Register(registry);
var world = new World(7, registry);
var sparks = world.AddTable(new SparkTable());
sparks.Add(new Spark { X = Fixed.FromRatio(1, 3), Life = 5 });
var purse = world.Add(new Purse { Owner = "smoke" });
purse.Send.Add(3, Fixed.FromRatio(3, 2));
for (var i = 0; i < 10; i++)
{
    world.Step();
}

var snapshot = new SyncWriter();
world.WriteSnapshot(snapshot);
var restored = new World(7, registry);
restored.AddTable(new SparkTable());
if (restored.ReadSnapshot(snapshot.Written) != SyncError.None || restored.ComputeHash() != world.ComputeHash())
{
    Console.Error.WriteLine("snapshot restore did not reproduce the world hash");
    return 1;
}

var changed = (Purse)restored.Entities[0];
changed.Owner = "changed";
var delta = new SyncWriter();
changed.WriteDeltaFrom(purse, delta);
var deltaReader = new SyncReader(delta.Written);
purse.ApplyDelta(ref deltaReader);
if (!deltaReader.Ok || purse.ComputeHash() != changed.ComputeHash())
{
    Console.Error.WriteLine("delta did not reproduce the changed entity");
    return 1;
}

var syncProblem = SyncSmoke.Run();
if (syncProblem is not null)
{
    Console.Error.WriteLine("sync smoke failed: " + syncProblem);
    return 1;
}

var transferProblem = TransferSmoke.Run();
if (transferProblem is not null)
{
    Console.Error.WriteLine("transfer smoke failed: " + transferProblem);
    return 1;
}

Console.WriteLine("ok: authority hand-off with replay, relay with a filtered client, escrow trade settled");

var netProblem = NetSmoke.Run();
if (netProblem is not null)
{
    Console.Error.WriteLine("net smoke failed: " + netProblem);
    return 1;
}

var purchasesProblem = PurchasesSmoke.Run();
if (purchasesProblem is not null)
{
    Console.Error.WriteLine("purchases smoke failed: " + purchasesProblem);
    return 1;
}

Console.WriteLine("ok: purchases over the fake store, and the platform without a store");

var serverProblem = ServerSmoke.Run();
if (serverProblem is not null)
{
    Console.Error.WriteLine("server smoke failed: " + serverProblem);
    return 1;
}

Console.WriteLine("ok: headless server host with a loopback client, zero drift, control surface; plugins refused without a loader");

var angle = Fixed.Sin(Fixed.One);
Console.WriteLine($"ok: {names.Length} assemblies; hash {world.ComputeHash():x16}; sin(1)={angle}; json {world.DumpJson().Length} chars");

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

// The harness: host, control layer, scenarios, capture, recording and MCP messages.
var harness = HarnessSmoke.Run();
if (harness != 0)
{
    return harness;
}


// Windows, clipboard and input-method text.
var windowed = WindowedSmoke.Run();
if (windowed != 0)
{
    return windowed;
}

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
    var steamInputProblem = SteamInputSmoke.Run(store);
    if (steamInputProblem is not null)
    {
        Console.Error.WriteLine("steam input: " + steamInputProblem);
        return 1;
    }

    Console.WriteLine("steam input: action sets, actions file, stand-in controller, glyph names and the disabled path");
}

var actionProblem = ActionSmoke.Run();
if (actionProblem is not null)
{
    Console.Error.WriteLine("actions: " + actionProblem);
    return 1;
}

Console.WriteLine("actions: keyboard, pad, harness and Steam Input sources, rebinding and the bindings text");


// Text and vector art: shape a string with the real measurer, rasterize one glyph and one SVG into atlas pages.
var fonts = new FontLibrary();
fonts.Add(ReadResource("font.ttf"));
var measurer = new FontTextMeasurer(fonts);
var textStyle = new TextStyle("Noto Sans", 18, 400, false, 0, 0, WhiteSpace.Normal);
var textLines = new List<TextLine>();
measurer.Layout("Silk 95g and iron", textStyle, 120, textLines);
var placed = new List<PositionedGlyph>();
measurer.Place("Silk 95g and iron", textStyle, textLines[0].Start, textLines[0].Length, textLines[0].Height, placed);
var glyphAtlas = new GlyphAtlas(fonts);
var glyphStatus = glyphAtlas.TryGet(placed[0].FaceId, placed[0].GlyphId, 18, 0, out var glyph);
var art = new ArtAtlas();
art.AddSvg("coin", ReadResource("coin.svg"));
var svgStatus = art.TryGetScaled("coin", 2, out var coin);
if (textLines.Count < 2 || placed.Count == 0 || glyphStatus != AtlasStatus.Ok || glyph.Width == 0 || svgStatus != AtlasStatus.Ok || coin.Width != 48)
{
    Console.Error.WriteLine($"text smoke failed: lines={textLines.Count} glyphs={placed.Count} glyph={glyphStatus} svg={svgStatus}");
    return 1;
}

Console.WriteLine($"ok: text {textLines.Count} lines, {placed.Count} glyphs, glyph {glyph.Width}x{glyph.Height}, svg {coin.Width}x{coin.Height}");

// Large art and tiled backgrounds: a standalone texture for an SVG over the page threshold, tile geometry from the atlas's sizes, and a toggle button's state.
var backdropStatus = art.TryGetFitted("coin", 1920, 1080, 1.5f, out var backdrop);
var tileTree = new UiTree(measurer) { ArtSizes = new ArtAtlasSizes(art) };
tileTree.SetViewport(200, 100);
tileTree.Root.With(Ui.Box().Style(s => s.Size(100, 50).BackgroundImage("coin").BackgroundRepeat(BackgroundRepeat.Repeat).BackgroundSize(BackgroundSizing.Auto)));
var tiled = tileTree.Frame().Commands.ToArray().FirstOrDefault(c => c.Kind == DrawKind.Image);
var fastOn = false;
var toggle = UiKit.ToggleButton("FAST", () => fastOn, v => fastOn = v);
tileTree.Root.With(toggle);
tileTree.Frame();
if (backdropStatus != AtlasStatus.Ok || backdrop.Standalone == 0 || !art.TryGetStandalone(backdrop.Standalone, out _) || tiled.TileSize != new System.Numerics.Vector2(24, 24) || toggle.Pressed != false)
{
    Console.Error.WriteLine($"art smoke failed: backdrop={backdropStatus} standalone={backdrop.Standalone} tile={tiled.TileSize} pressed={toggle.Pressed}");
    return 1;
}

Console.WriteLine($"ok: standalone art {backdrop.Width}x{backdrop.Height}, tiled background {tiled.TileSize.X}x{tiled.TileSize.Y}, toggle button state");

// The component kit: a table, a filter field, a dialog and a theme swap, driven through input events.
var kitProblem = KitSmoke.Run(measurer);
if (kitProblem is not null)
{
    Console.Error.WriteLine("kit smoke failed: " + kitProblem);
    return 1;
}

Console.WriteLine("ok: ui kit table, filter, sort, dialog and theme swap");

// glTF import: a skinned, animated binary model with a PNG texture, an embedded text model, and two hostile files.
var gltfProblem = GltfSmoke.Run(ReadResource("fox.glb"), ReadResource("box.gltf"));
if (gltfProblem is not null)
{
    Console.Error.WriteLine("gltf smoke failed: " + gltfProblem);
    return 1;
}

Console.WriteLine("ok: gltf skinned model with PNG texture, animation pose, joint matrices, embedded model, truncated and escaping files refused");

// Entities in 3D: transforms, camera, projection, picking, world-anchored UI and the 3D control operations.
var scene3d = Scene3DSmoke.Run(ReadResource("font.ttf"), ReadResource("box.gltf"));
if (scene3d is not null)
{
    Console.Error.WriteLine("scene3d smoke failed: " + scene3d);
    return 1;
}

Console.WriteLine("ok: entities in 3D: transform sync, camera, projection, picking, pinned label, camera/pick/project operations");

// Graphics: create a device, clear an offscreen target and read it back. "--require-gpu" turns a missing graphics driver into a failure.
var requireGpu = args.Contains("--require-gpu");
if (!GpuBackends.IsAvailable)
{
    Console.WriteLine("gpu: " + GpuBackends.UnavailableReason);
    return requireGpu ? 1 : 0;
}

using var device = GpuBackends.CreateDevice();
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

if (!GraphSmoke.Run(device))
{
    return 1;
}

// The 2D renderer: sprites, a rounded rectangle and a line of text through the atlases, offscreen, read back.
using var renderer = Renderer2D.Create(device, GpuFormat.Rgba8UnormSrgb, GeneratedShaders.Instance);
using var frameTarget = device.CreateTexture(new TextureDesc(96, 64, GpuFormat.Rgba8UnormSrgb, TextureUsage.RenderTarget | TextureUsage.CopySource));
using var sprite = renderer.CreateTexture(2, 2, [255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255, 255, 0, 0, 255], TextureFilter.Nearest);
var scene = new Scene2D();
scene.Draw(Sprite.Create(sprite, new System.Numerics.Vector2(-24, 0)) with { Scale = new System.Numerics.Vector2(8, 8) });
var labelTree = new UiTree(measurer);
labelTree.SetViewport(96, 64);
labelTree.AddStyleSheet(new StyleSheet().Rule(":root", s => s.FontSize(14).TextColor(Color.White)));
labelTree.Root.With(Ui.Label("Silk 95g").Style(s => s.MarginLeft(30).MarginTop(40)));
var labelList = labelTree.Frame();
var quads = new TextQuadBuilder(measurer, glyphAtlas);
renderer.BeginFrame(frameTarget, new FrameOptions { Clear = new Color(0, 0, 0, 255) });
renderer.DrawScene(scene, Camera2D.Default);
renderer.Ui.Fill(new Rect(60, 8, 24, 24), new Color(0, 255, 0, 255), new CornerRadii(8, 8, 8, 8));
renderer.Ui.Draw(labelList, quads, glyphAtlas, art);
var frameEncoder = device.BeginCommands();
var frameStats = renderer.EndFrame(frameEncoder);
device.Submit(frameEncoder);
var frame = new byte[96 * 64 * 4];
device.ReadTexture(frameTarget, 0, 0, frame);
var red = (((32 * 96) + 24) * 4);
var green = (((20 * 96) + 72) * 4);
var anyText = false;
for (var i = 40 * 96 * 4; i < frame.Length; i += 4)
{
    anyText |= frame[i] > 128 && frame[i + 1] > 128 && frame[i + 2] > 128;
}

if (frame[red] != 255 || frame[red + 1] != 0 || frame[green] != 0 || frame[green + 1] != 255 || !anyText)
{
    Console.Error.WriteLine($"render2d: unexpected pixels red={frame[red]},{frame[red + 1]} green={frame[green]},{frame[green + 1]} text={anyText}");
    return 1;
}

Console.WriteLine($"render2d: sprite, rounded rect and text drawn in {frameStats.DrawCalls} draws, {frameStats.Passes} pass; read back 96x64");

var draw2dProblem = Draw2DSmoke.Run(device, ReadResource("font.ttf"));
if (draw2dProblem is not null)
{
    Console.Error.WriteLine("draw2d: " + draw2dProblem);
    return 1;
}

Console.WriteLine("draw2d: IView2D entity drawn through the default stage: animated sprite clip, disc, line and text, camera follow");

var render3dProblem = Render3DSmoke.Run(device, ReadResource("fox.glb"));
if (render3dProblem is not null)
{
    Console.Error.WriteLine("render3d: " + render3dProblem);
    return 1;
}

Console.WriteLine("render3d: skinned glTF model through the PBR passes, then shadow maps (cascaded and spot), ambient occlusion, an instance batch, toon outlines and all four quality levels");
return 0;

static byte[] ReadResource(string name)
{
    using var stream = typeof(Program).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException("missing resource " + name);
    using var copy = new MemoryStream();
    stream.CopyTo(copy);
    return copy.ToArray();
}

static MemoryStream ReadAsset(string name)
{
    using var s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(name) ?? throw new InvalidOperationException(name);
    var m = new MemoryStream();
    s.CopyTo(m);
    m.Position = 0;
    return m;
}
