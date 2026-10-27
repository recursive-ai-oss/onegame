using System.Numerics;
using OneGame.Atlas;
using OneGame.Core;
using OneGame.Gpu;
using OneGame.Host;
using OneGame.Particles;
using OneGame.Render2D;
using OneGame.Render3D;
using OneGame.Text;
using OneGame.UI;

namespace OneGame.AotSmoke;

/// <summary>An entity whose view shows an additive spray through <c>Draw2D.Particles</c>.</summary>
[Entity]
public partial class Sparker : Entity, IView2D
{
    private ParticleEmitter? _sparks;

    [View]
    public void Draw(Draw2D d)
    {
        _sparks ??= new ParticleEmitter(new ParticleSettings
        {
            Rate = 200,
            Prewarm = 1f,
            Lifetime = 1f,
            Speed = new FloatRange(20, 60),
            Spread = MathF.PI,
            Size = 12f,
            Color = ParticleGradient.FadeOut(ParticleGradient.Rgb(0xffa040)),
            Blend = ParticleBlend.Additive,
        }, seed: 5, name: "sparks");
        d.Particles(_sparks, new Vector2(48, 32));
    }
}

/// <summary>Particles under the native publish: the emitter simulation, the 2D draw path with the additive material, and billboards in the 3D renderer.</summary>
internal static class ParticleSmoke
{
    public static string? Run(GpuDevice device, byte[] font)
    {
        var registry = new EntityRegistry();
        OneGame.Generated.OneGame_AotSmoke.EntityTypes.Register(registry);
        var world = new World(1, registry);
        world.Add(new Sparker());
        var fonts = new FontLibrary();
        fonts.Add(font);
        using var stage = new Render2DStage(new HostResources(fonts, new FontTextMeasurer(fonts), new ArtAtlas(), GeneratedShaders.Instance));
        using var target = device.CreateTexture(new TextureDesc(96, 64, GpuFormat.Rgba8UnormSrgb, TextureUsage.RenderTarget | TextureUsage.CopySource));
        world.Step();
        stage.Render(device, target, new FrameInfo(96, 64, 1, world.Tick, new DisplayList(), world, Fixed.Zero));
        var px = new byte[96 * 64 * 4];
        device.ReadTexture(target, 0, 0, px);
        var lit = 0;
        for (var i = 0; i < px.Length; i += 4)
        {
            lit += px[i] > 60 ? 1 : 0;
        }

        var emitters = stage.Draw.ParticleEmitters;
        if (emitters.Count != 1 || emitters[0].Count < 100 || lit < 100)
        {
            return $"2D: {emitters.Count} emitters, {(emitters.Count > 0 ? emitters[0].Count : 0)} particles, {lit} lit pixels";
        }

        using var renderer = new Renderer3D(device);
        using var target3 = new Offscreen3D(renderer, 96, 72, GpuFormat.Rgba8UnormSrgb);
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 0, 6), Vector3.Zero, Vector3.UnitY) };
        var batch = new BillboardBatch3D();
        scene.Billboards.Add(batch);
        var e = emitters[0];
        for (var i = 0; i < e.Count; i++)
        {
            var p = e.Sample(i);
            batch.Add(new Billboard3D { Position = new Vector3((p.Position.X - 48) / 20f, (p.Position.Y - 32) / 20f, 0), Size = new Vector2(0.5f), Color = new Vector4(1, 0.6f, 0.2f, p.Color.W), Additive = i % 2 == 0 });
        }

        var pixels = target3.Capture(scene);
        var different = 0;
        for (var i = 4; i < pixels.Length; i += 4)
        {
            different += pixels[i] != pixels[0] || pixels[i + 1] != pixels[1] ? 1 : 0;
        }

        return different < 200 || renderer.LastStats.Billboards != e.Count ? $"3D: {different} pixels differ, {renderer.LastStats.Billboards} quads" : null;
    }
}
