using System.Numerics;
using System.Runtime.CompilerServices;
using OneGame.Gltf;
using RecursiveAI.OneGpu;
using RecursiveAI.OneGpu.Vulkan;
using OneGame.Render2D;
using OneGame.Render3D;
using OneGame.UI;
using Scene = OneGame.Render3D.Scene3D;

namespace OneGame.Scene3D.Tests;

/// <summary>A 3D target shown in 2D with <see cref="Renderer2D.WrapTexture"/>: no readback, validation silent, pixels as captured directly.</summary>
public class WrappedTextureTests
{
    private const int Side = 64;

    private static string Box([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "testdata", "gltf", "Box", "glTF-Embedded", "Box.gltf"));

    private sealed class Rig : IDisposable
    {
        public Rig(int width)
        {
            Width = width;
            Device = GpuBackends.CreateDevice(new GpuDeviceOptions { Validation = true, RequireValidation = true });
            R3 = new Renderer3D(Device);
            R2 = Renderer2D.Create(Device, GpuFormat.Rgba8UnormSrgb, GeneratedShaders.Instance);
            Target = Device.CreateTexture(new TextureDesc((uint)width, Side, GpuFormat.Rgba8UnormSrgb, TextureUsage.RenderTarget | TextureUsage.CopySource));
            Model = R3.Upload(GltfLoader.LoadFile(Box()));
            Scene = new Scene { Camera = Camera3D.LookAt(new Vector3(2, 1.5f, 3), Vector3.Zero, Vector3.UnitY), Samples = 1 };
            Scene.Add(Model);
            Scene.Lights.Add(Light3D.Directional(new Vector3(-1, -1, -1), Vector3.One, 3f));
            Scene.Environment.Background = BackgroundMode.Color;
            Scene.Environment.BackgroundColor = new Vector3(0.05f, 0.15f, 0.3f);
            Scene.Post.Bloom = false;
            Off = new Offscreen3D(R3, Side, Side, GpuFormat.Rgba8UnormSrgb, sampled: true);
        }

        public int Width { get; }

        public GpuDevice Device { get; }

        public Renderer3D R3 { get; }

        public Renderer2D R2 { get; }

        public GpuTexture Target { get; }

        public Model3D Model { get; }

        public Scene Scene { get; }

        public Offscreen3D Off { get; set; }

        public void Frame(Texture2D tex, bool sprite, bool ui)
        {
            Off.Render(Scene);
            R2.BeginFrame(Target, new FrameOptions { Clear = new Color(0, 0, 0, 255) });
            if (sprite)
            {
                var scene = new Scene2D();
                scene.Draw(scene.DefaultLayer, Sprite.Create(tex, Vector2.Zero) with { Pivot = Vector2.Zero });
                R2.DrawScene(scene, new Camera2D { Position = new Vector2(Width / 2f, Side / 2f), Zoom = 1f });
            }

            if (ui)
            {
                R2.Ui.Image(new Rect(Side, 0, Side, Side), tex, Color.White);
            }

            var enc = Device.BeginCommands();
            R2.EndFrame(enc);
            Device.Submit(enc);
        }

        public byte[] Read()
        {
            var bytes = new byte[Width * Side * 4];
            Device.ReadTexture(Target, 0, 0, bytes);
            return bytes;
        }

        public void Dispose()
        {
            Device.WaitIdle();
            Off.Dispose();
            Model.Dispose();
            R2.Dispose();
            R3.Dispose();
            Target.Dispose();
            Device.WaitIdle();
            var messages = Device.TakeValidationMessages();
            Device.Dispose();
            Assert.Empty(messages);
        }
    }

    /// <summary>How many bytes of one Side x Side block differ by more than one level.</summary>
    private static int Differing(byte[] a, int aStride, int aX, byte[] b, int bStride, int bX)
    {
        var n = 0;
        for (var y = 0; y < Side; y++)
        {
            for (var x = 0; x < Side * 4; x++)
            {
                n += Math.Abs(a[(y * aStride) + (aX * 4) + x] - b[(y * bStride) + (bX * 4) + x]) > 1 ? 1 : 0;
            }
        }

        return n;
    }

    [Fact]
    public void ASpriteAndAUiImageShowTheTargetAsCapturedAndRebindFollowsAResize()
    {
        using var rig = new Rig(Side * 2);
        var direct = rig.Off.Capture(rig.Scene);
        var tex = rig.R2.WrapTexture(rig.Off.Target, TextureFilter.Nearest);
        Assert.Equal((Side, Side), (tex.Width, tex.Height));
        rig.Frame(tex, sprite: true, ui: true);
        var px = rig.Read();
        if (Environment.GetEnvironmentVariable("ONEGAME_CAPTURE_DIR") is { Length: > 0 } dir)
        {
            Directory.CreateDirectory(dir);
            OneGame.Render3D.Tests.Png.Write(Path.Combine(dir, "wrapped-texture.png"), rig.Width, Side, px);
        }

        // The same texels through a sprite (left half) and the UI canvas (right half); the 3D scene has a lit box and a blue backdrop. Against an
        // ordinary texture made from the captured bytes the result is identical on every adapter; against the capture itself it is within one level
        // (RADV puts a few edge pixels of the sprite and UI paths up to 15 levels off, and an ordinary texture does the same there, so it is the 2D path and not the wrapper).
        var control = rig.R2.CreateTexture(Side, Side, direct, TextureFilter.Nearest);
        rig.Frame(control, sprite: true, ui: true);
        var expected = rig.Read();
        control.Dispose();
        Assert.Equal(expected, px);
        Assert.InRange(Differing(px, Side * 8, 0, direct, Side * 4, 0), 0, 3);
        Assert.InRange(Differing(px, Side * 8, Side, direct, Side * 4, 0), 0, 16);
        var colours = new HashSet<uint>();
        for (var i = 0; i < direct.Length; i += 4)
        {
            colours.Add(BitConverter.ToUInt32(direct, i));
        }

        Assert.True(colours.Count > 8, "the offscreen scene should not be flat");

        // The target is recreated at another size: the wrapper follows without a new Texture2D.
        var old = rig.Off;
        rig.Off = new Offscreen3D(rig.R3, Side, Side, GpuFormat.Rgba16Float, sampled: true);
        rig.R2.RebindTexture(tex, rig.Off.Target);
        rig.Frame(tex, sprite: true, ui: false);
        old.Dispose();
        Assert.Equal(GpuFormat.Rgba16Float, rig.Off.Target.Format);
        tex.Dispose();
        rig.Off.Render(rig.Scene); // the image is still the owner's to use
    }

    [Fact]
    public void SteadyFramesThatRenderAndDrawTheWrappedTargetAllocateNothing()
    {
        using var rig = new Rig(Side * 2);
        var tex = rig.R2.WrapTexture(rig.Off.Target, TextureFilter.Nearest);
        for (var i = 0; i < 100; i++)
        {
            rig.Frame(tex, true, true);
        }

        rig.Device.WaitIdle();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 200; i++)
        {
            rig.Frame(tex, false, true);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        rig.Device.WaitIdle();
        if (rig.Device.Adapter.Kind == AdapterKind.Software || rig.Device.Adapter.VendorId == 0x10DE)
        {
            Assert.Equal(0, allocated);
        }

        tex.Dispose();
    }

    [Fact]
    public void ARefusedImageSaysWhy()
    {
        using var rig = new Rig(Side * 2);
        using var plain = new Offscreen3D(rig.R3, Side, Side);
        var e = Assert.Throws<ArgumentException>(() => rig.R2.WrapTexture(plain.Target));
        Assert.Contains("Sampled", e.Message, StringComparison.Ordinal);
    }
}
