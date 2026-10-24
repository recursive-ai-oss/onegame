using System.Runtime.InteropServices;

namespace OneGame.Gpu.Vulkan.Tests;

/// <summary>Minimal Xlib window created by the test itself (the platform layer is not involved).</summary>
internal static partial class Xlib
{
    const string Lib = "libX11.so.6";

    [LibraryImport(Lib)]
    public static partial nint XOpenDisplay(nint name);

    [LibraryImport(Lib)]
    public static partial void XCloseDisplay(nint display);

    [LibraryImport(Lib)]
    public static partial int XDefaultScreen(nint display);

    [LibraryImport(Lib)]
    public static partial nint XRootWindow(nint display, int screen);

    [LibraryImport(Lib)]
    public static partial nint XCreateSimpleWindow(nint display, nint parent, int x, int y, uint width, uint height, uint border, nuint borderColor, nuint background);

    [LibraryImport(Lib)]
    public static partial void XMapWindow(nint display, nint window);

    [LibraryImport(Lib)]
    public static partial void XResizeWindow(nint display, nint window, uint width, uint height);

    [LibraryImport(Lib)]
    public static partial void XDestroyWindow(nint display, nint window);

    [LibraryImport(Lib)]
    public static partial void XSync(nint display, int discard);
}

public sealed class SwapchainTests
{
    [Fact]
    public void PresentsToX11WindowAndFollowsResize()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("DISPLAY") is { Length: > 0 }, "needs an X server (run under Xvfb)");
        var display = Xlib.XOpenDisplay(0);
        Assert.NotEqual(0, display);
        var window = Xlib.XCreateSimpleWindow(display, Xlib.XRootWindow(display, Xlib.XDefaultScreen(display)), 0, 0, 200, 120, 0, 0, 0);
        Xlib.XMapWindow(display, window);
        Xlib.XSync(display, 0);
        try
        {
            using var gpu = new GpuFixture();
            var d = gpu.Device;
            GpuSwapchain swapchain;
            try
            {
                swapchain = d.CreateSwapchain(SurfaceSource.X11(display, window), new SwapchainDesc(200, 120));
            }
            catch (GpuUnsupportedException e)
            {
                // Some drivers cannot present to a virtual X server (RADV needs DRI3, which Xvfb lacks).
                Assert.Skip(e.Message);
                return;
            }

            using var disposeSwapchain = swapchain;
            Assert.Equal((200u, 120u), (swapchain.Width, swapchain.Height));

            void Frame(float r, float g, float b)
            {
                Assert.Equal(AcquireStatus.Ready, swapchain.TryAcquire(out var back));
                var enc = d.BeginCommands();
                var pass = enc.BeginRenderPass(ColorAttachment.Clear(back, r, g, b, 1));
                pass.EndPass();
                d.Submit(enc);
                swapchain.Present();
            }

            for (var i = 0; i < 6; i++)
            {
                Frame(1, 0, 0);
            }

            // The backbuffer really holds what was drawn: read it before presenting.
            Assert.Equal(AcquireStatus.Ready, swapchain.TryAcquire(out var image));
            var enc2 = d.BeginCommands();
            var pass2 = enc2.BeginRenderPass(ColorAttachment.Clear(image, 0, 1, 0, 1));
            pass2.EndPass();
            d.Submit(enc2);
            var pixels = new byte[200 * 120 * 4];
            d.ReadTexture(image, 0, 0, pixels);
            swapchain.Present();
            var px = (60 * 200 + 100) * 4;
            Assert.Equal(GpuFormat.Bgra8Unorm, swapchain.Format);
            Assert.Equal((0, 255, 0), (pixels[px], pixels[px + 1], pixels[px + 2]));

            // Resize the window: the next acquire rebuilds the swapchain at the new size.
            Xlib.XResizeWindow(display, window, 320, 180);
            Xlib.XSync(display, 0);
            Frame(0, 0, 1);
            Frame(0, 0, 1);
            Assert.Equal((320u, 180u), (swapchain.Width, swapchain.Height));
            Frame(1, 1, 1);
        }
        finally
        {
            Xlib.XDestroyWindow(display, window);
            Xlib.XCloseDisplay(display);
        }
    }
}
