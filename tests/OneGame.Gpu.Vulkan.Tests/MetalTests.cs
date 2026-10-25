using OneGame.Gpu.Metal;
using OneGame.Gpu.Metal.Interop;

namespace OneGame.Gpu.Vulkan.Tests;

/// <summary>Checks that only exist on the Metal backend: presenting to a layer, and that API validation really is on.</summary>
public sealed class MetalTests
{
    [Fact]
    public void PresentsToALayerWithoutAWindow()
    {
        Assert.SkipUnless(GpuBackends.Selected == GpuBackend.Metal && GpuBackends.IsAvailable, "needs the Metal backend");
        using var gpu = new GpuFixture();
        var d = gpu.Device;
        var layer = MetalGpu.CreateDetachedLayer();
        try
        {
            using var swapchain = d.CreateSwapchain(SurfaceSource.MetalLayer(layer), new SwapchainDesc(32, 16));
            Assert.Equal(GpuFormat.Bgra8Unorm, swapchain.Format);
            for (var frame = 0; frame < 4; frame++)
            {
                Assert.Equal(AcquireStatus.Ready, swapchain.TryAcquire(out var back));
                var enc = d.BeginCommands();
                enc.BeginRenderPass(ColorAttachment.Clear(back, 1, 0, 0, 1)).EndPass();
                d.Submit(enc);
                var pixels = new byte[32 * 16 * 4];
                d.ReadTexture(back, 0, 0, pixels);

                // Bgra8: blue, green, red, alpha.
                Assert.Equal(new byte[] { 0, 0, 255, 255 }, pixels[..4]);
                swapchain.Present();
            }

            swapchain.Resize(16, 8);
            Assert.Equal(AcquireStatus.Ready, swapchain.TryAcquire(out var small));
            Assert.Equal((16u, 8u), (small.Width, small.Height));
            swapchain.Present();
            swapchain.Resize(0, 0);
            Assert.Equal(AcquireStatus.Skipped, swapchain.TryAcquire(out _));
        }
        finally
        {
            Objc.objc_release(layer);
        }
    }

    [Fact]
    public void ValidationIsActive()
    {
        Assert.SkipUnless(GpuBackends.Selected == GpuBackend.Metal && GpuBackends.IsAvailable, "needs the Metal backend");
        using var gpu = new GpuFixture();
        Assert.True(MetalGpu.IsValidationActive(gpu.Device));
    }
}
