using OneGame.Gpu.Vulkan.Interop;

namespace OneGame.Gpu.Vulkan;

/// <summary>A swapchain on a native window surface, rebuilt transparently when the window changes size.</summary>
internal sealed unsafe class VulkanSwapchain : GpuSwapchain, IRetirable
{
    const int AcquireRing = 3;

    readonly VulkanDevice _dev;
    readonly SwapchainDesc _desc;
    VkSurfaceKHR _surface;
    VkSwapchainKHR _swapchain;
    VulkanTexture[] _images = [];
    VkSemaphore[] _renderDone = [];
    readonly VkSemaphore[] _acquire = new VkSemaphore[AcquireRing];
    int _acquireIndex;
    uint _imageIndex;
    bool _acquired;
    bool _submitted;
    bool _needsRebuild = true;
    uint _width, _height;
    uint _requestedWidth, _requestedHeight;
    GpuFormat _format;
    VkFormat _vkFormat;
    VkColorSpaceKHR _colorSpace;
    bool _disposed;

    // Android reports Suboptimal for as long as the display is rotated against the swapchain's pre-transform; the compositor does the
    // rotation (Vulkan 1.3 §30.8: preTransform), so only a change of size makes a rebuild worthwhile there.
    readonly bool _compositorRotates;

    public VulkanSwapchain(VulkanDevice dev, in SurfaceSource source, in SwapchainDesc desc)
    {
        _compositorRotates = source.Kind == SurfaceKind.Android;
        _dev = dev;
        _desc = desc;
        _requestedWidth = desc.Width;
        _requestedHeight = desc.Height;
        if (!dev.HasSwapchainExtension || !dev.SurfaceKhr)
        {
            throw new GpuUnsupportedException("the adapter or instance does not support presenting to windows (VK_KHR_swapchain / VK_KHR_surface)");
        }

        _surface = CreateSurface(source);
        try
        {
            
            uint ok;
            dev.Check(dev.InstanceFns.vkGetPhysicalDeviceSurfaceSupportKHR(dev.Physical, dev.QueueFamily, _surface, &ok), "vkGetPhysicalDeviceSurfaceSupportKHR");
            
            if (ok == 0)
            {
                throw new GpuUnsupportedException("the graphics queue cannot present to this surface");
            }

            for (var i = 0; i < AcquireRing; i++)
            {
                var sci = new VkSemaphoreCreateInfo();
                VkSemaphore s;
                dev.Check(dev.Fns.vkCreateSemaphore(dev.Handle, &sci, null, &s), "vkCreateSemaphore");
                _acquire[i] = s;
            }

            Rebuild();
        }
        catch
        {
            Dispose();
            throw;
        }

        dev.TrackSwapchain(this);
    }

    VkSurfaceKHR CreateSurface(in SurfaceSource s)
    {
        VkSurfaceKHR surface;
        switch (s.Kind)
        {
            case SurfaceKind.X11:
                if (!_dev.XlibKhr)
                {
                    throw new GpuUnsupportedException("VK_KHR_xlib_surface is not available");
                }

                var xi = new VkXlibSurfaceCreateInfoKHR { dpy = (void*)s.Display, window = (nuint)s.Window };
                _dev.Check(_dev.InstanceFns.vkCreateXlibSurfaceKHR(_dev.Instance, &xi, null, &surface), "vkCreateXlibSurfaceKHR");
                break;
            case SurfaceKind.Wayland:
                if (!_dev.WaylandKhr)
                {
                    throw new GpuUnsupportedException("VK_KHR_wayland_surface is not available");
                }

                var wi = new VkWaylandSurfaceCreateInfoKHR { display = (void*)s.Display, surface = (void*)s.Window };
                _dev.Check(_dev.InstanceFns.vkCreateWaylandSurfaceKHR(_dev.Instance, &wi, null, &surface), "vkCreateWaylandSurfaceKHR");
                break;
            case SurfaceKind.Win32:
                if (!_dev.Win32Khr)
                {
                    throw new GpuUnsupportedException("VK_KHR_win32_surface is not available");
                }

                var win = new VkWin32SurfaceCreateInfoKHR { hinstance = s.Display, hwnd = s.Window };
                _dev.Check(_dev.InstanceFns.vkCreateWin32SurfaceKHR(_dev.Instance, &win, null, &surface), "vkCreateWin32SurfaceKHR");
                break;
            case SurfaceKind.Android:
                if (!_dev.AndroidKhr)
                {
                    throw new GpuUnsupportedException("VK_KHR_android_surface is not available");
                }

                var droid = new VkAndroidSurfaceCreateInfoKHR { window = (void*)s.Window };
                _dev.Check(_dev.InstanceFns.vkCreateAndroidSurfaceKHR(_dev.Instance, &droid, null, &surface), "vkCreateAndroidSurfaceKHR");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(s));
        }

        return surface;
    }

    public override uint Width => _width;

    public override uint Height => _height;

    public override GpuFormat Format => _format;

    public override int ImageCount => _images.Length;

    public override void Resize(uint width, uint height)
    {
        if (width != _requestedWidth || height != _requestedHeight)
        {
            _requestedWidth = width;
            _requestedHeight = height;
            _needsRebuild = true;
        }
    }

    VkSurfaceCapabilitiesKHR QueryCaps()
    {
        VkSurfaceCapabilitiesKHR caps;
        _dev.Check(_dev.InstanceFns.vkGetPhysicalDeviceSurfaceCapabilitiesKHR(_dev.Physical, _surface, &caps), "vkGetPhysicalDeviceSurfaceCapabilitiesKHR");
        return caps;
    }

    (uint W, uint H) ChooseExtent(in VkSurfaceCapabilitiesKHR caps)
    {
        if (caps.currentExtent.width != uint.MaxValue)
        {
            return (caps.currentExtent.width, caps.currentExtent.height);
        }

        return (Math.Clamp(_requestedWidth, caps.minImageExtent.width, caps.maxImageExtent.width),
            Math.Clamp(_requestedHeight, caps.minImageExtent.height, caps.maxImageExtent.height));
    }

    void Rebuild()
    {
        var caps = QueryCaps();
        var (w, h) = ChooseExtent(caps);
        _needsRebuild = false;
        if (w == 0 || h == 0)
        {
            // Minimized: nothing to draw into until the window has an area again.
            _width = w;
            _height = h;
            DestroyImages(waitIdle: true);
            if (_swapchain.Value != 0)
            {
                _dev.Fns.vkDestroySwapchainKHR(_dev.Handle, _swapchain, null);
                _swapchain = default;
            }

            return;
        }

        uint fc = 0;
        _dev.InstanceFns.vkGetPhysicalDeviceSurfaceFormatsKHR(_dev.Physical, _surface, &fc, null);
        var formats = new VkSurfaceFormatKHR[fc];
        fixed (VkSurfaceFormatKHR* fp = formats)
        {
            _dev.InstanceFns.vkGetPhysicalDeviceSurfaceFormatsKHR(_dev.Physical, _surface, &fc, fp);
        }

        var wantFormat = _desc.Srgb ? VkFormat.B8G8R8A8Srgb : VkFormat.B8G8R8A8Unorm;
        var altFormat = _desc.Srgb ? VkFormat.R8G8B8A8Srgb : VkFormat.R8G8B8A8Unorm;
        var chosen = -1;
        for (var pass = 0; pass < 2 && chosen < 0; pass++)
        {
            for (var i = 0; i < formats.Length; i++)
            {
                if (formats[i].format == (pass == 0 ? wantFormat : altFormat) && formats[i].colorSpace == VkColorSpaceKHR.SrgbNonlinear)
                {
                    chosen = i;
                    break;
                }
            }
        }

        if (chosen < 0)
        {
            throw new GpuUnsupportedException("the surface offers no 8-bit BGRA/RGBA format in the sRGB color space");
        }

        _vkFormat = formats[chosen].format;
        _colorSpace = formats[chosen].colorSpace;
        _format = VkMap.FromVk(_vkFormat);

        uint pc = 0;
        _dev.InstanceFns.vkGetPhysicalDeviceSurfacePresentModesKHR(_dev.Physical, _surface, &pc, null);
        var modes = new VkPresentModeKHR[pc];
        fixed (VkPresentModeKHR* mp = modes)
        {
            _dev.InstanceFns.vkGetPhysicalDeviceSurfacePresentModesKHR(_dev.Physical, _surface, &pc, mp);
        }

        var mode = VkPresentModeKHR.Fifo;
        var preferred = _desc.PresentMode switch
        {
            PresentMode.LowLatency => VkPresentModeKHR.Mailbox,
            PresentMode.Immediate => VkPresentModeKHR.Immediate,
            _ => VkPresentModeKHR.Fifo,
        };
        if (Array.IndexOf(modes, preferred) >= 0)
        {
            mode = preferred;
        }

        var usage = VkImageUsageFlagBits.ColorAttachment | VkImageUsageFlagBits.TransferSrc;
        if ((_desc.ExtraUsage & TextureUsage.Sampled) != 0)
        {
            usage |= VkImageUsageFlagBits.Sampled;
        }

        if ((_desc.ExtraUsage & TextureUsage.CopyDestination) != 0)
        {
            usage |= VkImageUsageFlagBits.TransferDst;
        }

        if ((caps.supportedUsageFlags & usage) != usage)
        {
            throw new GpuUnsupportedException("the surface does not support rendering into and copying from its images");
        }

        var count = Math.Max(caps.minImageCount + 1, 3u);
        if (caps.maxImageCount != 0)
        {
            count = Math.Min(count, caps.maxImageCount);
        }

        var alpha = (caps.supportedCompositeAlpha & VkCompositeAlphaFlagBitsKHR.Opaque) != 0
            ? VkCompositeAlphaFlagBitsKHR.Opaque
            : (caps.supportedCompositeAlpha & VkCompositeAlphaFlagBitsKHR.Inherit) != 0 ? VkCompositeAlphaFlagBitsKHR.Inherit : VkCompositeAlphaFlagBitsKHR.PreMultiplied;

        // Images may still be in flight on the queue; drain before replacing them.
        _dev.WaitIdle();
        var old = _swapchain;
        var sci = new VkSwapchainCreateInfoKHR
        {
            surface = _surface,
            minImageCount = count,
            imageFormat = _vkFormat,
            imageColorSpace = _colorSpace,
            imageExtent = new VkExtent2D { width = w, height = h },
            imageArrayLayers = 1,
            imageUsage = usage,
            imageSharingMode = VkSharingMode.Exclusive,
            preTransform = _compositorRotates && (caps.supportedTransforms & VkSurfaceTransformFlagBitsKHR.Identity) != 0 ? VkSurfaceTransformFlagBitsKHR.Identity : caps.currentTransform,
            compositeAlpha = alpha,
            presentMode = mode,
            clipped = 1,
            oldSwapchain = old,
        };
        VkSwapchainKHR sc;
        _dev.Check(_dev.Fns.vkCreateSwapchainKHR(_dev.Handle, &sci, null, &sc), "vkCreateSwapchainKHR");
        DestroyImages(waitIdle: false);
        if (old.Value != 0)
        {
            _dev.Fns.vkDestroySwapchainKHR(_dev.Handle, old, null);
        }

        _swapchain = sc;
        _width = w;
        _height = h;

        uint n = 0;
        _dev.Fns.vkGetSwapchainImagesKHR(_dev.Handle, sc, &n, null);
        var handles = new VkImage[n];
        fixed (VkImage* hp = handles)
        {
            _dev.Fns.vkGetSwapchainImagesKHR(_dev.Handle, sc, &n, hp);
        }

        var tex = new TextureDesc(w, h, _format, TextureUsage.RenderTarget | TextureUsage.CopySource | _desc.ExtraUsage) { Label = "swapchain image" };
        _images = new VulkanTexture[n];
        _renderDone = new VkSemaphore[n];
        for (var i = 0; i < n; i++)
        {
            var view = _dev.CreateView(handles[i], _format, VkImageAspectFlagBits.Color, 1, 1);
            _images[i] = new VulkanTexture(_dev, handles[i], view, default, tex, true);
            var si = new VkSemaphoreCreateInfo();
            VkSemaphore s;
            _dev.Check(_dev.Fns.vkCreateSemaphore(_dev.Handle, &si, null, &s), "vkCreateSemaphore");
            _renderDone[i] = s;
        }
    }

    void DestroyImages(bool waitIdle)
    {
        if (waitIdle && _images.Length > 0)
        {
            _dev.WaitIdle();
        }

        foreach (var img in _images)
        {
            img.DestroyView();
        }

        foreach (var s in _renderDone)
        {
            _dev.Fns.vkDestroySemaphore(_dev.Handle, s, null);
        }

        _images = [];
        _renderDone = [];
    }

    internal VkSemaphore AcquireSemaphore => _acquire[_acquireIndex];

    internal VkSemaphore RenderDoneSemaphore => _renderDone[_imageIndex];

    internal void MarkSubmitted() => _submitted = true;

    public override AcquireStatus TryAcquire(out GpuTexture backbuffer)
    {
        if (_acquired)
        {
            throw new InvalidOperationException("the previous backbuffer was not presented");
        }

        backbuffer = null!;
        for (var attempt = 0; attempt < 4; attempt++)
        {
            // Window systems that report a size (X11, Win32) are polled so a resize shows up without an explicit call.
            var caps = QueryCaps();
            var (w, h) = ChooseExtent(caps);
            if (_needsRebuild || w != _width || h != _height)
            {
                Rebuild();
            }

            if (_swapchain.Value == 0)
            {
                return AcquireStatus.Skipped;
            }

            _acquireIndex = (_acquireIndex + 1) % AcquireRing;
            uint index;
            var r = _dev.Fns.vkAcquireNextImageKHR(_dev.Handle, _swapchain, ulong.MaxValue, _acquire[_acquireIndex], default, &index);
            if (r == VkResult.ErrorOutOfDate)
            {
                _needsRebuild = true;
                continue;
            }

            if (r == VkResult.Suboptimal)
            {
                _needsRebuild = !_compositorRotates;
            }
            else if (r != VkResult.Success)
            {
                if (r == VkResult.ErrorSurfaceLost)
                {
                    throw new GpuSurfaceLostException();
                }

                _dev.Check(r, "vkAcquireNextImageKHR");
            }

            _imageIndex = index;
            _acquired = true;
            _submitted = false;
            _dev.RegisterAcquire(this);
            backbuffer = _images[index];
            return AcquireStatus.Ready;
        }

        return AcquireStatus.Skipped;
    }

    public override void Present()
    {
        if (!_acquired)
        {
            throw new InvalidOperationException("no backbuffer is acquired; call TryAcquire first");
        }

        if (!_submitted)
        {
            throw new InvalidOperationException("submit the commands that render into the backbuffer before presenting it");
        }

        var wait = _renderDone[_imageIndex];
        var sc = _swapchain;
        var index = _imageIndex;
        var pi = new VkPresentInfoKHR { waitSemaphoreCount = 1, pWaitSemaphores = &wait, swapchainCount = 1, pSwapchains = &sc, pImageIndices = &index };
        var queue = _dev.Queue;
        var r = _dev.Fns.vkQueuePresentKHR(queue, &pi);
        _acquired = false;
        if (r == VkResult.ErrorOutOfDate || (r == VkResult.Suboptimal && !_compositorRotates))
        {
            _needsRebuild = true;
        }
        else if (r == VkResult.Suboptimal)
        {
            // Nothing to do: see _compositorRotates.
        }
        else if (r == VkResult.ErrorSurfaceLost)
        {
            throw new GpuSurfaceLostException();
        }
        else
        {
            _dev.Check(r, "vkQueuePresentKHR");
        }
    }

    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _dev.ForgetSwapchain(this);
        DestroyNow();
    }

    public void DestroyNow()
    {
        if (_dev.Handle.Value == 0)
        {
            return;
        }

        if (_images.Length > 0 || _swapchain.Value != 0)
        {
            _dev.WaitIdle();
        }

        DestroyImages(waitIdle: false);
        if (_swapchain.Value != 0)
        {
            _dev.Fns.vkDestroySwapchainKHR(_dev.Handle, _swapchain, null);
            _swapchain = default;
        }

        foreach (var s in _acquire)
        {
            if (s.Value != 0)
            {
                _dev.Fns.vkDestroySemaphore(_dev.Handle, s, null);
            }
        }

        if (_surface.Value != 0)
        {
            _dev.InstanceFns.vkDestroySurfaceKHR(_dev.Instance, _surface, null);
            _surface = default;
        }
    }
}
