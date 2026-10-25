using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using OneGame.Control;
using OneGame.Host;
using OneGame.Render2D;
using OneGame.Testing.Tests.Shaders;
using OneGame.Text;

namespace OneGame.Testing.Tests;

/// <summary>A game whose render stage draws a sprite with a material and registers the material's shader file for hot reload.</summary>
public sealed class GlowGame : ShopGame
{
    public static string ShaderPath { get; set; } = "";

    public override string Name => "GlowGame";

    public override void DefineFonts(FontLibrary fonts) =>
        fonts.Add(File.ReadAllBytes(Path.Combine(GpuPaths.RepoRoot, "testdata", "text", "fonts", "NotoSans-Subset-Regular.ttf")));

    public override IRenderStage CreateRenderStage(HostResources resources) => new Stage(resources);

    private sealed class Stage(HostResources resources) : Render2DStage(resources)
    {
        private Texture2D? _texture;
        private Material? _material;

        protected override void RendererCreated(Renderer2D renderer)
        {
            var pixels = new byte[8 * 8 * 4];
            Array.Fill(pixels, (byte)255);
            _texture = renderer.CreateTexture(8, 8, pixels);
            _material = renderer.CreateMaterial(new ShaderCode(OneGame.Gpu.ShaderStage.Pixel, Glow.PSMain.EntryPoint, Glow.PSMain.SpirV.ToArray(), Glow.PSMain.Msl.ToArray(), Glow.PSMain.MslFunctionName, Glow.Bindings.All.ToArray()), "glow");
            _material.SetParameter(0, new Vector4(1, 0, 0, 1));
            var material = _material;
            HotShaders.Register(new HotShader("glow", ShaderPath, Glow.PSMain.EntryPoint, Glow.Reflection, spirv =>
                renderer.ReplaceMaterialShader(material, new ShaderCode(OneGame.Gpu.ShaderStage.Pixel, Glow.PSMain.EntryPoint, spirv))));
        }

        protected override void DrawOverUi(Renderer2D renderer, in FrameInfo frame)
        {
            var scene = new Scene2D();
            scene.Draw(Sprite.Create(_texture!, new Vector2(0, 0)) with { Scale = new Vector2(10, 10), Material = _material });
            renderer.DrawScene(scene, Camera2D.Default);
        }
    }
}

/// <summary>Shader hot reload through the control layer, as an agent drives it.</summary>
public class HotReloadTests
{
    private static string GlowSource([CallerFilePath] string here = "") =>
        File.ReadAllText(Path.Combine(Path.GetDirectoryName(here)!, "Shaders", "Glow.hlsl"));

    private static void Write(string path, string text, int bump)
    {
        File.WriteAllText(path, text);
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(bump));
    }

    private static (byte R, byte G, byte B) Center(CapturedFrame f) =>
        (f.Rgba[(((f.Height / 2) * f.Width) + (f.Width / 2)) * 4], f.Rgba[(((f.Height / 2) * f.Width) + (f.Width / 2)) * 4 + 1], f.Rgba[(((f.Height / 2) * f.Width) + (f.Width / 2)) * 4 + 2]);

    private static void Save(string name, CapturedFrame frame)
    {
        if (Environment.GetEnvironmentVariable("ONEGAME_TEST_SAVE") is { Length: > 0 } dir)
        {
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), Png.Encode(frame));
        }
    }

    private static string State(string json) => JsonDocument.Parse(json).RootElement.GetProperty("shaders")[0].GetProperty("state").GetString()!;

    [Fact]
    public void ShaderEditsReachTheNextCaptureWithoutARestart()
    {
        var dir = Path.Combine(Path.GetTempPath(), "onegame-hot-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "Glow.hlsl");
            var original = GlowSource();
            Write(path, original, 0);
            GlowGame.ShaderPath = path;
            Environment.SetEnvironmentVariable("ONEGAME_GPU_VALIDATION", "1");
            using var cluster = TestCluster.Start(new GlowGame(), new TestOptions { Width = 160, Height = 120, Gpu = GpuMode.Required });
            var c = cluster.Client;
            c.Step(3);
            var first = c.Capture("160x120");
            Assert.Equal((255, 0, 0), Center(first));
            Save("hot-1-red", first);
            Assert.Equal("ok", State(c.Call("shaders")));
            var tick = c.Game.World.Tick;

            // Edit the pixel shader: the next capture shows it and the tick carries on.
            Write(path, original.Replace("t.rgb * P[0].rgb", "t.rgb * float3(0, 1, 0)"), 2);
            var reload = c.Call("shaders");
            Assert.Contains("\"changed\":[\"glow\"]", reload, StringComparison.Ordinal);
            c.Step(2);
            var green = c.Capture("160x120");
            Assert.Equal((0, 255, 0), Center(green));
            Save("hot-2-green", green);
            Assert.Equal(tick + 2, c.Game.World.Tick);

            // The capture operation also picks up an edit made since the last check.
            Write(path, original.Replace("t.rgb * P[0].rgb", "t.rgb * float3(0, 0, 1)"), 4);
            var shot = JsonDocument.Parse(c.Call("capture_frame", "{\"size\":\"160x120\"}")).RootElement.GetProperty("sha256").GetString();
            Assert.NotEmpty(shot!);
            Assert.Equal((0, 0, 255), Center(c.Capture("160x120")));

            // A changed constant buffer is refused with a reason; the blue shader keeps drawing.
            Write(path, original.Replace("float4 P[2];", "float4 P[3];"), 6);
            var refused = c.Call("shaders");
            Assert.Equal("incompatible", State(refused));
            Assert.Contains("Rebuild", refused, StringComparison.Ordinal);
            Assert.Equal((0, 0, 255), Center(c.Capture("160x120")));

            // A syntax error is reported with the file and line.
            Write(path, original.Replace("float4 t = Tex.Sample(Samp, i.Uv) * i.Color;", "float4 t = Tex.Sample(Samp, i.Uv) * i.Color"), 8);
            var broken = c.Call("shaders");
            Assert.Equal("compile_error", State(broken));
            var problem = JsonDocument.Parse(broken).RootElement.GetProperty("shaders")[0].GetProperty("problems")[0];
            Assert.Equal(path, problem.GetProperty("file").GetString());
            Assert.True(problem.GetProperty("line").GetInt32() > 0);
            Assert.Equal((0, 0, 255), Center(c.Capture("160x120")));

            Assert.Empty(c.Game.TakeGpuValidationMessages());
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
