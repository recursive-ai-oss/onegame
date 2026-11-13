using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;
using OneGame.Control;
using OneGame.Core;
using OneGame.Gltf;
using OneGame.Host;
using OneGame.Render3D;
using RecursiveAI.OneGpu;
using OneGame.UI;

namespace OneGame.Scene3D.Tests;

/// <summary>The <c>camera</c>, <c>pick</c> and <c>project</c> operations on a Render3DStage whose game fills the scene by hand.</summary>
public class ManualSceneTests
{
    internal static string FontPath([CallerFilePath] string here = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "assets", "fonts", "NotoSans-Regular.ttf"));

    private sealed class HandStage(HostResources resources) : Render3DStage(resources)
    {
        public Model3D? Model;

        protected override void RendererCreated(Renderer3D renderer)
        {
            Model = renderer.Upload(GltfLoader.Load(Square()));
            Scene.Add(Model).Label = "crate";
        }

        public override void Dispose()
        {
            Model?.Dispose();
            base.Dispose();
        }
    }

    private sealed class HandGame : GameDefinition
    {
        public OneGame.Render3D.Scene3D? Scene;

        public override string Name => "HandGame";

        public override void RegisterEntities(EntityRegistry registry)
        {
        }

        public override void Setup(World world)
        {
        }

        public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column(Ui.Label("x")));

        public override void DefineFonts(OneGame.Text.FontLibrary fonts) => fonts.Add(File.ReadAllBytes(ManualSceneTests.FontPath()));

        public override IRenderStage CreateRenderStage(HostResources resources)
        {
            var stage = new HandStage(resources);
            Scene = stage.Scene;
            return stage;
        }
    }

    // A 2 x 2 square in the z = 0 plane, facing +Z.
    private static byte[] Square(string material = "{}")
    {
        var data = new List<byte>();
        void F(float v) => data.AddRange(BitConverter.GetBytes(v));
        foreach (var (x, y) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) })
        {
            F(x);
            F(y);
            F(0);
        }

        foreach (var (u, v) in new[] { (0f, 1f), (1f, 1f), (1f, 0f), (0f, 0f) })
        {
            F(u);
            F(v);
        }

        foreach (var i in new ushort[] { 0, 1, 2, 0, 2, 3 })
        {
            data.AddRange(BitConverter.GetBytes(i));
        }

        var uri = "data:application/octet-stream;base64," + Convert.ToBase64String([.. data]);
        return Encoding.UTF8.GetBytes($$"""
        {"asset":{"version":"2.0"},"scene":0,"scenes":[{"nodes":[0]}],"nodes":[{"mesh":0}],
         "meshes":[{"primitives":[{"attributes":{"POSITION":0,"TEXCOORD_0":1},"indices":2,"material":0}]}],
         "materials":[{{material}}],
         "buffers":[{"byteLength":{{data.Count}},"uri":"{{uri}}"}],
         "bufferViews":[{"buffer":0,"byteOffset":0,"byteLength":48},{"buffer":0,"byteOffset":48,"byteLength":32},{"buffer":0,"byteOffset":80,"byteLength":12}],
         "accessors":[{"bufferView":0,"componentType":5126,"count":4,"type":"VEC3","min":[-1,-1,0],"max":[1,1,0]},
                      {"bufferView":1,"componentType":5126,"count":4,"type":"VEC2"},
                      {"bufferView":2,"componentType":5123,"count":6,"type":"SCALAR"}]}
        """);
    }

    private static JsonElement Call(ControlSurface surface, string op, string args = "{}") => JsonDocument.Parse(surface.Call(op, args)).RootElement.Clone();

    private static double[] Numbers(JsonElement e) => [.. e.EnumerateArray().Select(v => v.GetDouble())];

    [Fact]
    public void CameraPickAndProjectWorkOnAHandFilledScene()
    {
        var definition = new HandGame();
        using var game = GameInstance.Create(definition, new GameOptions { Headless = true, Gpu = GpuMode.Required });
        var surface = new ControlSurface(game);
        game.Capture(320, 180);   // the first frame creates the renderer, which uploads the model and sets the scene
        var scene = definition.Scene!;
        scene.Camera = Camera3D.LookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.UnitY);

        var camera = Call(surface, "camera");
        Assert.Equal([0, 0, 5], Numbers(camera.GetProperty("position")));
        Assert.False(camera.GetProperty("overridden").GetBoolean());

        var centre = Call(surface, "project", """{"point":[0,0,0],"size":"320x180"}""");
        Assert.True(centre.GetProperty("onScreen").GetBoolean());
        Assert.InRange(centre.GetProperty("x").GetDouble(), 159.9, 160.1);
        Assert.InRange(centre.GetProperty("y").GetDouble(), 89.9, 90.1);

        var hit = Call(surface, "pick", """{"x":160,"y":90,"size":"320x180"}""");
        Assert.True(hit.GetProperty("hit").GetBoolean());
        Assert.Equal("crate", hit.GetProperty("model").GetString());
        Assert.InRange(hit.GetProperty("distance").GetDouble(), 4.89, 4.91);   // rays start on the near plane (0.1)
        Assert.False(Call(surface, "pick", """{"x":2,"y":2,"size":"320x180"}""").GetProperty("hit").GetBoolean());

        // An override moves the camera the stage draws with, and clearing it gives the game's own camera back.
        var moved = Call(surface, "camera", """{"position":[0,0,3],"target":[0,0,0]}""");
        Assert.True(moved.GetProperty("overridden").GetBoolean());
        game.Capture(320, 180);
        Assert.Equal(3f, scene.Camera.Position.Z, 3);
        Call(surface, "camera", """{"reset":true}""");
        game.Capture(320, 180);
        Assert.Equal(5f, scene.Camera.Position.Z, 3);
    }

    // A stage that draws another place into a sampled target from UpdateScene and shows it on a window in its own scene.
    private sealed class PortalStage(HostResources resources) : Render3DStage(resources)
    {
        private Offscreen3D? _portal;
        private readonly OneGame.Render3D.Scene3D _other = new();
        private Model3D? _window;
        private Model3D? _red;

        protected override void RendererCreated(Renderer3D renderer)
        {
            _red = renderer.Upload(GltfLoader.Load(Square("""{"pbrMetallicRoughness":{"baseColorFactor":[1,0,0,1]},"extensions":{"KHR_materials_unlit":{}}}""")));
            _other.Add(_red);
            _other.Camera = Camera3D.LookAt(new Vector3(0, 0, 3), Vector3.Zero, Vector3.UnitY);
            _other.Environment.Background = BackgroundMode.Color;
            _other.Environment.BackgroundColor = new Vector3(0, 0, 1);
            _other.Post.ToneMapper = ToneMapper.None;
            _portal = new Offscreen3D(renderer, 128, 128, GpuFormat.Rgba8UnormSrgb, sampled: true);
            _window = renderer.Upload(GltfLoader.Load(Square("""{"pbrMetallicRoughness":{"baseColorFactor":[0,0,0,1]},"emissiveFactor":[1,1,1]}""")));
            _window.Materials[0].SetTexture(MaterialSlot3D.Emissive, _portal.Target);
            Scene.Add(_window);
            Scene.Camera = Camera3D.LookAt(new Vector3(0, 0, 8), Vector3.Zero, Vector3.UnitY);
            Scene.Post.ToneMapper = ToneMapper.None;
        }

        protected override void UpdateScene(OneGame.Render3D.Scene3D scene, in FrameInfo frame) => _portal!.Render(_other);

        public override void Dispose()
        {
            _window?.Dispose();
            _red?.Dispose();
            _portal?.Dispose();
            base.Dispose();
        }
    }

    private sealed class PortalGame : GameDefinition
    {
        public override string Name => "PortalGame";

        public override void RegisterEntities(EntityRegistry registry)
        {
        }

        public override void Setup(World world)
        {
        }

        public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column(Ui.Label("x")));

        public override void DefineFonts(OneGame.Text.FontLibrary fonts) => fonts.Add(File.ReadAllBytes(ManualSceneTests.FontPath()));

        public override IRenderStage CreateRenderStage(HostResources resources) => new PortalStage(resources);
    }

    [Fact]
    public void AStageCanRenderAPortalFromUpdateScene()
    {
        using var game = GameInstance.Create(new PortalGame(), new GameOptions { Headless = true, Gpu = GpuMode.Required });
        var frame = game.Capture(320, 180);
        var at = ((90 * 320) + 160) * 4;
        // The window is a 2 x 2 square seen from 8 units away: its middle shows the portal's red square (a little pink where the default bloom spills).
        Assert.True(frame.Rgba[at] > 200 && frame.Rgba[at + 1] < 90 && frame.Rgba[at + 2] < 90, $"centre pixel {frame.Rgba[at]},{frame.Rgba[at + 1]},{frame.Rgba[at + 2]}");
        Assert.Empty(game.TakeGpuValidationMessages());
    }

    [Fact]
    public void AGameWithoutAThreeDStageIsToldWhy()
    {
        using var game = GameInstance.Create(new TwoDGame(), new GameOptions { Headless = true, Gpu = GpuMode.Off });
        var surface = new ControlSurface(game);
        var error = Assert.Throws<ControlException>(() => surface.Call("camera"));
        Assert.Equal("no_scene3d", error.Code);
    }

    private sealed class TwoDGame : GameDefinition
    {
        public override string Name => "TwoD";

        public override void RegisterEntities(EntityRegistry registry)
        {
        }

        public override void Setup(World world)
        {
        }

        public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column(Ui.Label("x")));
    }
}
