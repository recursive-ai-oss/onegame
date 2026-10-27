using System.Numerics;
using System.Text;
using System.Text.Json;
using OneGame.Audio;
using OneGame.Control;
using OneGame.Host;
using OneGame.Render2D;
using OneGame.Text;
using OneYaml;

namespace OneGame.Testing.Tests;

/// <summary>The game's data, as a game would declare it: a plain type and a generated context that rejects unknown keys.</summary>
public sealed record Palette(string Name, int Gold);

[YamlSerializable(typeof(Palette))]
internal sealed partial class AssetYaml : YamlSerializerContext
{
    internal static AssetYaml Strict { get; } = new(new YamlSerializerOptions { UnmappedMemberHandling = YamlUnmappedMemberHandling.Disallow });
}

/// <summary>A game that draws a sprite from <c>art/ship.png</c> and reads <c>data/palette.yaml</c>, each loaded with one line.</summary>
public sealed class AssetsGame : ShopGame
{
    public static string Content { get; set; } = "";

    public static Asset<Palette>? Palette { get; private set; }

    public override string Name => "AssetsGame";

    public static void Reset() => Palette = null;

    public override void DefineFonts(FontLibrary fonts) =>
        fonts.Add(File.ReadAllBytes(Path.Combine(GpuPaths.RepoRoot, "testdata", "text", "fonts", "NotoSans-Subset-Regular.ttf")));

    public override IAssetSource CreateAssetSource() => new FileAssetSource([Content]);

    public override IRenderStage CreateRenderStage(HostResources resources) => new Stage(resources);

    private sealed class Stage : Render2DStage
    {
        private readonly Assets _assets;
        private Asset<Texture2D>? _ship;

        public Stage(HostResources resources)
            : base(resources) => _assets = resources.Assets!;

        protected override void DrawOverUi(Renderer2D renderer, in FrameInfo frame)
        {
            var assets = _assets;
            _ship ??= assets.LoadTexture("art/ship.png");
            Palette ??= assets.LoadData("data/palette.yaml", AssetYaml.Strict.Palette);
            var scene = new Scene2D();
            scene.Draw(Sprite.Create(_ship.Value, new Vector2(0, 0)) with { Scale = new Vector2(10, 10) });
            renderer.DrawScene(scene, Camera2D.Default);
        }
    }
}

/// <summary>Asset loading and development hot reload, driven through the control layer and captures.</summary>
public class AssetTests
{
    private static int s_shots;

    private static byte[] Png8(int side, byte r, byte g, byte b)
    {
        var px = new byte[side * side * 4];
        for (var i = 0; i < px.Length; i += 4)
        {
            (px[i], px[i + 1], px[i + 2], px[i + 3]) = (r, g, b, 255);
        }

        return Png.Encode(new CapturedFrame(side, side, px, "test"));
    }

    private static void Write(string path, byte[] bytes, int bump)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(bump));
    }

    // A development loop polls the files between frames; here the `assets` operation does the polling before the capture.
    private static CapturedFrame Shot(TestClient c)
    {
        c.Call("assets");
        var frame = c.Capture("160x120");
        if (Environment.GetEnvironmentVariable("ONEGAME_TEST_SAVE") is { Length: > 0 } dir)
        {
            Png.Save(frame, Path.Combine(dir, $"assets-{Interlocked.Increment(ref s_shots):00}.png"));
        }

        return frame;
    }

    private static void Write(string path, string text, int bump) => Write(path, Encoding.UTF8.GetBytes(text), bump);

    private static (byte R, byte G, byte B) Center(CapturedFrame f) =>
        (f.Rgba[(((f.Height / 2) * f.Width) + (f.Width / 2)) * 4], f.Rgba[(((f.Height / 2) * f.Width) + (f.Width / 2)) * 4 + 1], f.Rgba[(((f.Height / 2) * f.Width) + (f.Width / 2)) * 4 + 2]);

    private static JsonElement Entry(string json, string path) =>
        JsonDocument.Parse(json).RootElement.GetProperty("assets").EnumerateArray().First(e => e.GetProperty("path").GetString() == path).Clone();

    private static string NewDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "onegame-assets-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void AnEditedPngShowsInTheNextCaptureAndABrokenOneKeepsTheOldPixels()
    {
        var dir = NewDir();
        try
        {
            AssetsGame.Content = dir;
            AssetsGame.Reset();
            var png = Path.Combine(dir, "art", "ship.png");
            Write(png, Png8(8, 255, 0, 0), 0);
            Write(Path.Combine(dir, "data", "palette.yaml"), "name: night\ngold: 5\n", 0);
            Environment.SetEnvironmentVariable("ONEGAME_GPU_VALIDATION", "1");
            using var cluster = TestCluster.Start(new AssetsGame(), new TestOptions { Width = 160, Height = 120, Gpu = GpuMode.Required });
            var c = cluster.Client;
            c.Step(3);
            Assert.Equal((255, 0, 0), Center(Shot(c)));
            Assert.Equal("ready", Entry(c.Call("assets"), "art/ship.png").GetProperty("state").GetString());
            var since = JsonDocument.Parse(c.Call("assets")).RootElement.GetProperty("lastSequence").GetInt64();

            // Edit the file on disk: the next capture shows the new pixels, with no restart and the same tick count going on.
            var tick = c.Game.World.Tick;
            Write(png, Png8(8, 0, 255, 0), 2);
            Assert.Equal((0, 255, 0), Center(Shot(c)));
            var after = c.Call("assets", $"{{\"since\":{since}}}");
            Assert.Equal(2, Entry(after, "art/ship.png").GetProperty("version").GetInt32());
            Assert.Contains("\"type\":\"reloaded\"", after, StringComparison.Ordinal);

            // The capture operation also picks up an edit made since the last check.
            Write(png, Png8(8, 0, 255, 255), 3);
            Assert.NotEmpty(JsonDocument.Parse(c.Call("capture_frame", "{\"size\":\"160x120\"}")).RootElement.GetProperty("sha256").GetString()!);
            Assert.Equal((0, 255, 255), Center(c.Capture("160x120")));

            // The size may change too; the same texture object keeps drawing.
            Write(png, Png8(4, 0, 0, 255), 4);
            c.Step(1);
            Assert.Equal((0, 0, 255), Center(Shot(c)));

            // A damaged file is reported; the blue pixels stay.
            Write(png, new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3 }, 6);
            var broken = c.Call("assets", $"{{\"since\":{since}}}");
            var e = Entry(broken, "art/ship.png");
            Assert.Equal("error", e.GetProperty("state").GetString());
            Assert.False(string.IsNullOrEmpty(e.GetProperty("error").GetString()));
            Assert.Contains("\"type\":\"failed\"", broken, StringComparison.Ordinal);
            Assert.Equal((0, 0, 255), Center(Shot(c)));

            // Fixing the file recovers it.
            Write(png, Png8(8, 255, 255, 0), 8);
            Assert.Equal((255, 255, 0), Center(Shot(c)));
            Assert.Equal("ready", Entry(c.Call("assets"), "art/ship.png").GetProperty("state").GetString());
            Assert.True(c.Game.World.Tick >= tick);
            Assert.Empty(c.Game.TakeGpuValidationMessages());
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void DataEditsAreCheckedAtOnceButOnlyAppliedOnRequest()
    {
        var dir = NewDir();
        try
        {
            AssetsGame.Content = dir;
            AssetsGame.Reset();
            Write(Path.Combine(dir, "art", "ship.png"), Png8(8, 255, 0, 0), 0);
            var yaml = Path.Combine(dir, "data", "palette.yaml");
            Write(yaml, "name: night\ngold: 5\n", 0);
            using var cluster = TestCluster.Start(new AssetsGame(), new TestOptions { Width = 160, Height = 120, Gpu = GpuMode.Required });
            var c = cluster.Client;
            c.Step(2);
            Shot(c);
            var palette = AssetsGame.Palette!;
            Assert.Equal(new Palette("night", 5), palette.Value);
            var digest = JsonDocument.Parse(c.Call("assets")).RootElement.GetProperty("dataDigest").GetString();

            // A valid edit is staged: the sim keeps reading the old data, so replays and drift hashes stay honest.
            Write(yaml, "name: dawn\ngold: 9\n", 2);
            var staged = c.Call("assets");
            Assert.Equal("pending", Entry(staged, "data/palette.yaml").GetProperty("state").GetString());
            Assert.Equal(new Palette("night", 5), palette.Value);
            Assert.Equal(digest, JsonDocument.Parse(staged).RootElement.GetProperty("dataDigest").GetString());

            // Applying it is the explicit boundary.
            var applied = c.Call("assets", "{\"apply_data\":true}");
            Assert.Equal(1, JsonDocument.Parse(applied).RootElement.GetProperty("applied").GetInt32());
            Assert.Equal(new Palette("dawn", 9), palette.Value);
            Assert.NotEqual(digest, JsonDocument.Parse(applied).RootElement.GetProperty("dataDigest").GetString());

            // Malformed YAML and unknown keys keep the data and say why.
            Write(yaml, "name: [unclosed\n", 4);
            var bad = c.Call("assets");
            Assert.Equal("error", Entry(bad, "data/palette.yaml").GetProperty("state").GetString());
            Write(yaml, "name: dusk\ngold: 1\nsilver: 2\n", 6);
            var unknown = Entry(c.Call("assets"), "data/palette.yaml");
            Assert.Equal("error", unknown.GetProperty("state").GetString());
            Assert.Contains("silver", unknown.GetProperty("error").GetString(), StringComparison.Ordinal);
            c.Call("assets", "{\"apply_data\":true}");
            Assert.Equal(new Palette("dawn", 9), palette.Value);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void AScenarioTriggersAndChecksReloads()
    {
        var dir = NewDir();
        try
        {
            AssetsGame.Content = dir;
            AssetsGame.Reset();
            var png = Path.Combine(dir, "art", "ship.png");
            var yaml = Path.Combine(dir, "data", "palette.yaml");
            Write(png, Png8(8, 255, 0, 0), 0);
            Write(yaml, "name: night\ngold: 5\n", 0);
            using var cluster = TestCluster.Start(new AssetsGame(), new TestOptions { Width = 160, Height = 120, Gpu = GpuMode.Required });
            var c = cluster.Client;
            c.Step(2);
            Shot(c);
            Write(png, new byte[] { 1, 2, 3 }, 2);
            Write(yaml, "name: dawn\ngold: 9\n", 2);
            var scenario = ScenarioParser.Parse("""
                steps:
                  - assets: check
                  - expect: {asset: {path: art/ship.png, state: error, version: "1"}}
                  - expect: {asset: {path: data/palette.yaml, state: pending, version: "1"}}
                  - assets: apply-data
                  - expect: {asset: {path: data/palette.yaml, state: ready, version: "2"}}
                  - expect: {asset: {path: data/missing.yaml, state: ready}}
                """, "inline.yaml", "assets");
            var result = cluster.Run(scenario);
            Assert.Equal(3, result.Expectations.Count(e => e.Passed));
            Assert.Single(result.Expectations, e => !e.Passed);
            Assert.Equal(new Palette("dawn", 9), AssetsGame.Palette!.Value);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void LoadingIsCachedCountedBoundedAndTyped()
    {
        var dir = NewDir();
        try
        {
            Write(Path.Combine(dir, "art", "a.png"), Png8(4, 1, 2, 3), 0);
            Write(Path.Combine(dir, "data", "p.yaml"), "name: x\ngold: 1\n", 0);
            using var assets = new Assets(new FileAssetSource([dir]));
            var a = assets.LoadData("data/p.yaml", AssetYaml.Strict.Palette);
            Assert.Same(a, assets.LoadData("data/p.yaml", AssetYaml.Strict.Palette));
            Assert.Equal(2, a.References);
            a.Release();
            Assert.Single(assets.Items);
            a.Release();
            Assert.Empty(assets.Items);
            Assert.Contains(assets.Events, e => e.Type == "released");

            Assert.Equal("not_found", Assert.Throws<AssetException>(() => assets.LoadData("data/none.yaml", AssetYaml.Strict.Palette)).Code);
            Assert.Equal("bad_path", Assert.Throws<AssetException>(() => assets.LoadTexture("../outside.png")).Code);
            Assert.Equal("bad_path", Assert.Throws<AssetException>(() => assets.LoadTexture(Path.Combine(dir, "art", "a.png"))).Code);
            assets.MaxDataBytes = 4;
            Assert.Equal("too_large", Assert.Throws<AssetException>(() => assets.LoadData("data/p.yaml", AssetYaml.Strict.Palette)).Code);

            // A texture with no renderer is decoded and checked, and says what is missing when read.
            var t = assets.LoadTexture("art/a.png");
            Assert.True(t.IsReady);
            Assert.Equal("no_renderer", Assert.Throws<AssetException>(() => t.Value).Code);

            // Background loads finish on Update or on first read; failures come through the handle.
            var bg = assets.LoadTexture("art/a.png", TextureFilter.Nearest, background: true);
            Write(Path.Combine(dir, "data", "q.yaml"), "name: [", 0);
            var bad = assets.LoadData("data/q.yaml", AssetYaml.Strict.Palette, background: true);
            var deadline = Environment.TickCount64 + 10_000;
            while ((!bg.Ready.IsCompleted || !bad.Ready.IsCompleted) && Environment.TickCount64 < deadline)
            {
                assets.Update();
                Thread.Sleep(5);
            }

            Assert.True(bg.IsReady);
            Assert.True(bad.Ready.IsFaulted);
            Assert.Equal("bad_format", Assert.Throws<AssetException>(() => bad.Value).Code);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void SoundsAndModelsLoadAndReloadUnderTheSameHandle()
    {
        var dir = NewDir();
        try
        {
            var testdata = Path.Combine(GpuPaths.RepoRoot, "testdata");
            Directory.CreateDirectory(Path.Combine(dir, "data"));
            Directory.CreateDirectory(Path.Combine(dir, "art"));
            File.Copy(Path.Combine(testdata, "audio", "burst-silence.ogg"), Path.Combine(dir, "data", "hit.ogg"));
            File.Copy(Path.Combine(testdata, "gltf", "Box", "glTF-Binary", "Box.glb"), Path.Combine(dir, "art", "box.glb"));
            File.Copy(Path.Combine(testdata, "gltf", "Box", "glTF", "Box.gltf"), Path.Combine(dir, "art", "boxsplit.gltf"));
            File.Copy(Path.Combine(testdata, "gltf", "Box", "glTF", "Box0.bin"), Path.Combine(dir, "art", "Box0.bin"));
            using var audio = AudioService.Create(new AudioServiceOptions { Capture = true });
            using var assets = new Assets(new FileAssetSource([dir]));
            var hit = assets.LoadSound("data/hit.ogg");
            Assert.True(hit.Value.DurationSeconds > 0);
            var box = assets.LoadModel("art/box.glb");
            Assert.NotEmpty(box.Value.Meshes);
            var split = assets.LoadModel("art/boxsplit.gltf");
            Assert.NotEmpty(split.Value.Meshes);

            // Reloading swaps what the handle returns; a damaged sound keeps the old clip.
            var old = hit.Value;
            File.Copy(Path.Combine(testdata, "audio", "tone44k.ogg"), Path.Combine(dir, "data", "hit.ogg"), true);
            Assert.True(assets.Reload(hit));
            Assert.NotSame(old, hit.Value);
            Assert.Equal(2, hit.Version);
            var good = hit.Value;
            File.WriteAllBytes(Path.Combine(dir, "data", "hit.ogg"), [1, 2, 3, 4]);
            Assert.False(assets.Reload(hit));
            Assert.Same(good, hit.Value);
            Assert.NotNull(hit.Error);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void DamagedFilesOfEveryKindOnlyEverRaiseAnAssetException()
    {
        var dir = NewDir();
        try
        {
            var testdata = Path.Combine(GpuPaths.RepoRoot, "testdata");
            var seeds = new Dictionary<string, byte[]>
            {
                ["a.png"] = Png8(8, 9, 9, 9),
                ["m.glb"] = File.ReadAllBytes(Path.Combine(testdata, "gltf", "Box", "glTF-Binary", "Box.glb")),
                ["d.yaml"] = Encoding.UTF8.GetBytes("name: night\ngold: 5\n"),
            };
            using var assets = new Assets(new FileAssetSource([dir]));
            var rng = new Random(42);
            var tried = 0;
            foreach (var (name, seed) in seeds)
            {
                for (var i = 0; i < 150; i++)
                {
                    var bytes = (byte[])seed.Clone();
                    if (i % 3 == 0)
                    {
                        bytes = bytes[..rng.Next(0, bytes.Length)];
                    }
                    else
                    {
                        for (var k = 0; k < 1 + (i % 5); k++)
                        {
                            bytes[rng.Next(bytes.Length)] = (byte)rng.Next(256);
                        }
                    }

                    File.WriteAllBytes(Path.Combine(dir, name), bytes);
                    tried++;
                    try
                    {
                        _ = name switch
                        {
                            "a.png" => (object)assets.LoadTexture(name),
                            "m.glb" => assets.LoadModel(name),
                            _ => assets.LoadData(name, AssetYaml.Strict.Palette),
                        };
                    }
                    catch (AssetException)
                    {
                    }

                    foreach (var a in assets.Items.ToArray())
                    {
                        a.Release();
                    }
                }
            }

            Assert.Equal(450, tried);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
