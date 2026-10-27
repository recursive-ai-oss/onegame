using System.Numerics;
using System.Runtime.CompilerServices;
using OneGame.Control;
using OneGame.Gpu;
using OneGame.Host;
using OneGame.Render2D.Tests.Shaders;
using OneGame.UI;

namespace OneGame.Render2D.Tests;

public class HotReloadTests
{
    private static string RippleSource([CallerFilePath] string here = "") =>
        File.ReadAllText(Path.Combine(Path.GetDirectoryName(here)!, "Shaders", "Ripple.hlsl"));

    private static void Write(string path, string text, int bump)
    {
        File.WriteAllText(path, text);
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(bump));
    }

    private static byte[] Draw(Rig rig, Texture2D tex, Material material)
    {
        var scene = new Scene2D();
        scene.Draw(Sprite.Create(tex, Vector2.Zero) with { Scale = new Vector2(2, 2), Material = material });
        return rig.Render(() => rig.Renderer.DrawScene(scene, Camera2D.Default), new Color(20, 20, 20, 255));
    }

    [Fact]
    public void EditedShaderIsSwappedInIncompatibleAndBrokenOnesAreRefused()
    {
        // Reloading recompiles to SPIR-V; the Metal path would recompile to Metal Shading Language and is not built yet.
        Assert.SkipUnless(GpuBackends.Selected == GpuBackend.Vulkan, "hot reload is wired to the Vulkan backend");
        var dir = Path.Combine(Path.GetTempPath(), "onegame-hot-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "Ripple.hlsl");
            var original = RippleSource();
            Write(path, original, 0);

            using var rig = new Rig(64, 64);
            var tex = rig.Own(rig.Renderer.CreateTexture(16, 16, Pixels.Solid(16, 16, new Color(200, 200, 200, 255)), TextureFilter.Linear));
            var material = rig.Own(rig.Renderer.CreateMaterial(new ShaderCode(OneGame.Gpu.ShaderStage.Pixel, Ripple.PSMain.EntryPoint, Ripple.PSMain.SpirV.ToArray(), Ripple.PSMain.Msl.ToArray(), Ripple.PSMain.MslFunctionName, Ripple.PSMain.Layout), "ripple"));
            material.SetParameter(0, new Vector4(0, 1, 0, 0));
            material.SetParameter(1, new Vector4(0, 0, 0, 0));

            var registry = new HotShaders();
            registry.Register(new HotShader("ripple", path, "PSMain", Ripple.Reflection, spirv =>
                rig.Renderer.ReplaceMaterialShader(material, new ShaderCode(OneGame.Gpu.ShaderStage.Pixel, "PSMain", spirv))));
            var log = new StringWriter();
            var reloader = new ShaderReloader(registry, log);

            var before = Draw(rig, tex, material);
            Assert.Empty(reloader.Check());
            Assert.Equal(before, Draw(rig, tex, material));
            Assert.Equal((200, 200, 200, 255), Pixels.At(before, 64, 32, 32));

            // A body edit: the sprite turns green. The renderer keeps running and the next frame shows it.
            Write(path, original.Replace("return float4(mixed, t.a);", "return float4(0, t.a, 0, t.a);"), 2);
            Assert.Equal(["ripple"], reloader.Check());
            var green = Draw(rig, tex, material);
            Assert.Equal((0, 255, 0, 255), Pixels.At(green, 64, 32, 32));
            Assert.Equal("ok", reloader.Status()[0].State);
            Assert.Equal(1, reloader.Status()[0].Reloads);

            // An interface change (a bigger constant buffer) is refused and the green shader keeps drawing.
            Write(path, original.Replace("float4 P[2];", "float4 P[3];"), 4);
            Assert.Equal(["ripple"], reloader.Check());
            var status = reloader.Status()[0];
            Assert.Equal("incompatible", status.State);
            Assert.Contains("Rebuild", status.Message);
            Assert.Equal(green, Draw(rig, tex, material));

            // A syntax error is reported with file and line; nothing throws and the green shader keeps drawing.
            Write(path, original.Replace("float4 t = Tex.Sample(Samp, uv) * i.Color;", "float4 t = Tex.Sample(Samp, uv) * i.Color"), 6);
            Assert.Equal(["ripple"], reloader.Check());
            status = reloader.Status()[0];
            Assert.Equal("compile_error", status.State);
            Assert.NotEmpty(status.Problems);
            Assert.Equal(path, status.Problems[0].File);
            Assert.True(status.Problems[0].Line > 0);
            Assert.Equal(green, Draw(rig, tex, material));
            Assert.Contains("kept its old version", log.ToString());

            // Fixing the file recovers; an unchanged file is not compiled again.
            Write(path, original, 8);
            Assert.Equal(["ripple"], reloader.Check());
            Assert.Equal("ok", reloader.Status()[0].State);
            Assert.Equal(before, Draw(rig, tex, material));
            Assert.Empty(reloader.Check());
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
