using System.Runtime.InteropServices;
using OneGame.Gpu.Vulkan.Interop;

namespace OneGame.Gpu.Vulkan;

/// <summary>An object destroyed only after the GPU has finished with the submissions that might use it.</summary>
internal interface IRetirable
{
    void DestroyNow();
}

/// <summary>Collects validation-layer messages reported through the debug messenger.</summary>
internal sealed class ValidationSink
{
    readonly List<string> _messages = [];

    public void Add(string text)
    {
        lock (_messages)
        {
            _messages.Add(text);
        }
    }

    public string[] Take()
    {
        lock (_messages)
        {
            var r = _messages.ToArray();
            _messages.Clear();
            return r;
        }
    }
}

/// <summary>Vulkan 1.3 device: owns the instance, queue, memory, deferred destruction and submission.</summary>
internal sealed unsafe partial class VulkanDevice : GpuDevice
{
    public const uint QueueFamilyIgnored = ~0u;
    public const ulong WholeSize = ~0UL;

    VkGlobalFns _global = null!;
    VkInstanceFns _instanceFns = null!;
    readonly VkDeviceFns _deviceFns = new();
    readonly GpuAdapterInfo _adapter;
    readonly ValidationSink? _sink;
    GCHandle _sinkHandle;
    VkDebugUtilsMessengerEXT _messenger;
    VkInstance _instance;
    VkPhysicalDevice _physical;
    VkDevice _device;
    VkQueue _queue;
    VkSemaphore _timeline;
    MemoryAllocator _memory = null!;
    bool _disposed;
    bool _anisotropy;
    float _maxAnisotropy;
    internal bool HasSwapchainExtension;
    internal uint QueueFamily;
    internal bool SurfaceKhr, XlibKhr, WaylandKhr, Win32Khr;

    ulong _serial;
    readonly List<(ulong Serial, IRetirable Item)> _retired = [];
    readonly List<(ulong Serial, VulkanEncoder Encoder)> _inflight = [];
    readonly Stack<VulkanEncoder> _freeEncoders = new();
    readonly List<(ulong Serial, Staging Item)> _stagingBusy = [];
    readonly List<Staging> _stagingPending = [];
    readonly Stack<Staging>[] _stagingFree = new Stack<Staging>[2 * 48];
    readonly List<VulkanSwapchain> _acquired = [];
    readonly List<VulkanSwapchain> _swapchains = [];
    VulkanEncoder? _upload;
    nint _submitScratch;
    int _submitScratchBytes;

    internal VkDeviceFns Fns => _deviceFns;
    internal VkInstanceFns InstanceFns => _instanceFns;
    internal VkGlobalFns GlobalFns => _global;
    internal VkDevice Handle => _device;
    internal VkInstance Instance => _instance;
    internal VkPhysicalDevice Physical => _physical;
    internal VkQueue Queue => _queue;
    internal MemoryAllocator Memory => _memory;
    internal ulong CompletedSerial { get; private set; }

    public override GpuAdapterInfo Adapter => _adapter;

    VulkanDevice(GpuAdapterInfo adapter, ValidationSink? sink)
    {
        _adapter = adapter;
        _sink = sink;
    }

    internal static VulkanDevice Create(GpuDeviceOptions options, VulkanInstance inst, int adapterIndex)
    {
        var info = inst.Adapters[adapterIndex];
        var dev = new VulkanDevice(info.Info, inst.Sink)
        {
            _instance = inst.Handle,
            _physical = info.Physical,
        };
        dev.AdoptInstance(inst);
        try
        {
            dev.CreateLogicalDevice(info);
        }
        catch
        {
            dev.Dispose();
            throw;
        }

        return dev;
    }

    VulkanInstance? _owner;

    void AdoptInstance(VulkanInstance inst)
    {
        _owner = inst;
        _global = inst.Global;
        _instanceFns = inst.Fns;
        _messenger = inst.Messenger;
        _sinkHandle = inst.SinkHandle;
        SurfaceKhr = inst.SurfaceKhr;
        XlibKhr = inst.XlibKhr;
        WaylandKhr = inst.WaylandKhr;
        Win32Khr = inst.Win32Khr;
    }

    void CreateLogicalDevice(VulkanInstance.AdapterEntry info)
    {
        using var arena = new Arena();

        // Features the backend relies on: dynamic rendering and synchronization2 (Vulkan 1.3), timeline semaphores (1.2).
        var f13 = new VkPhysicalDeviceVulkan13Features();
        var f12 = new VkPhysicalDeviceVulkan12Features { pNext = &f13 };
        var f2 = new VkPhysicalDeviceFeatures2 { pNext = &f12 };
        _instanceFns.vkGetPhysicalDeviceFeatures2(_physical, &f2);
        if (f13.dynamicRendering == 0 || f13.synchronization2 == 0 || f12.timelineSemaphore == 0)
        {
            throw new GpuUnsupportedException($"adapter '{_adapter.Name}' lacks dynamic rendering, synchronization2 or timeline semaphores (Vulkan 1.3 core features)");
        }

        _anisotropy = f2.features.samplerAnisotropy != 0;
        QueueFamily = info.QueueFamily;

        var e13 = new VkPhysicalDeviceVulkan13Features { dynamicRendering = 1, synchronization2 = 1 };
        var e12 = new VkPhysicalDeviceVulkan12Features { pNext = &e13, timelineSemaphore = 1 };
        var e2 = new VkPhysicalDeviceFeatures2 { pNext = &e12 };
        e2.features.samplerAnisotropy = _anisotropy ? 1u : 0u;

        float priority = 1f;
        var qci = new VkDeviceQueueCreateInfo { queueFamilyIndex = QueueFamily, queueCount = 1, pQueuePriorities = &priority };

        var exts = new List<string>();
        HasSwapchainExtension = info.HasSwapchain;
        if (HasSwapchainExtension)
        {
            exts.Add("VK_KHR_swapchain");
        }

        var extPtrs = arena.Alloc<nint>(exts.Count);
        for (var i = 0; i < exts.Count; i++)
        {
            extPtrs[i] = (nint)arena.Utf8(exts[i]);
        }

        var dci = new VkDeviceCreateInfo
        {
            pNext = &e2,
            queueCreateInfoCount = 1,
            pQueueCreateInfos = &qci,
            enabledExtensionCount = (uint)exts.Count,
            ppEnabledExtensionNames = (byte**)extPtrs,
        };
        VkDevice dev;
        Check(_instanceFns.vkCreateDevice(_physical, &dci, null, &dev), "vkCreateDevice");
        _device = dev;

        var getDeviceProc = (delegate* unmanaged<nint, byte*, nint>)VulkanLoader.GetInstanceProcAddr(_instance.Value, "vkGetDeviceProcAddr\0"u8.GetPointer());
        _deviceFns.Load(_device.Value, getDeviceProc);

        VkQueue q;
        _deviceFns.vkGetDeviceQueue(_device, QueueFamily, 0, &q);
        _queue = q;

        var tci = new VkSemaphoreTypeCreateInfo { semaphoreType = VkSemaphoreType.Timeline, initialValue = 0 };
        var sci = new VkSemaphoreCreateInfo { pNext = &tci };
        VkSemaphore sem;
        Check(_deviceFns.vkCreateSemaphore(_device, &sci, null, &sem), "vkCreateSemaphore");
        _timeline = sem;

        VkPhysicalDeviceProperties props;
        _instanceFns.vkGetPhysicalDeviceProperties(_physical, &props);
        _maxAnisotropy = props.limits.maxSamplerAnisotropy;
        VkPhysicalDeviceMemoryProperties mem;
        _instanceFns.vkGetPhysicalDeviceMemoryProperties(_physical, &mem);
        _memory = new MemoryAllocator(this, mem, props.limits.bufferImageGranularity);
    }

    internal void Check(VkResult r, string call)
    {
        if (r >= 0)
        {
            return;
        }

        throw r switch
        {
            VkResult.ErrorOutOfDeviceMemory or VkResult.ErrorOutOfHostMemory => new GpuOutOfMemoryException($"{call} ran out of memory"),
            VkResult.ErrorDeviceLost => new GpuDeviceLostException($"{call}: the device was lost"),
            _ => new GpuException($"{call} failed on {_adapter.Name}: {r}"),
        };
    }

    public override string[] TakeValidationMessages() => _sink?.Take() ?? [];

    // ---- deferred destruction and submission ----

    /// <summary>Queues an object for destruction once every submission made so far (and the next one) has finished.</summary>
    internal void Retire(IRetirable item)
    {
        if (_disposed)
        {
            return;
        }

        _retired.Add((_serial + 1, item));
    }

    /// <summary>Reads the timeline, then releases what the GPU has finished with.</summary>
    internal void Poll()
    {
        ulong v;
        Check(_deviceFns.vkGetSemaphoreCounterValue(_device, _timeline, &v), "vkGetSemaphoreCounterValue");
        CompletedSerial = v;

        var n = 0;
        while (n < _retired.Count && _retired[n].Serial <= v)
        {
            _retired[n].Item.DestroyNow();
            n++;
        }

        _retired.RemoveRange(0, n);

        n = 0;
        while (n < _inflight.Count && _inflight[n].Serial <= v)
        {
            _inflight[n].Encoder.Reset();
            _freeEncoders.Push(_inflight[n].Encoder);
            n++;
        }

        _inflight.RemoveRange(0, n);

        n = 0;
        while (n < _stagingBusy.Count && _stagingBusy[n].Serial <= v)
        {
            ReleaseStaging(_stagingBusy[n].Item);
            n++;
        }

        _stagingBusy.RemoveRange(0, n);
    }

    internal void WaitSerial(ulong serial)
    {
        if (CompletedSerial >= serial)
        {
            return;
        }

        var sem = _timeline;
        var wi = new VkSemaphoreWaitInfo { semaphoreCount = 1, pSemaphores = &sem, pValues = &serial };
        Check(_deviceFns.vkWaitSemaphores(_device, &wi, ulong.MaxValue), "vkWaitSemaphores");
        Poll();
    }

    public override void WaitIdle()
    {
        if (_upload is not null)
        {
            Submit(BeginCommands());
        }

        WaitSerial(_serial);

        // Presentation waits are not tracked by the timeline; an idle queue is the only sign they are done with their semaphores.
        Check(_deviceFns.vkQueueWaitIdle(_queue), "vkQueueWaitIdle");
    }

    public override GpuCommandEncoder BeginCommands()
    {
        Poll();
        return _freeEncoders.Count > 0 ? _freeEncoders.Pop() : new VulkanEncoder(this);
    }

    internal VulkanEncoder UploadEncoder => _upload ??= (VulkanEncoder)BeginCommands();

    internal void RegisterAcquire(VulkanSwapchain sc) => _acquired.Add(sc);

    public override void Submit(GpuCommandEncoder commands)
    {
        var enc = (VulkanEncoder)commands;
        if (enc.Device != this)
        {
            throw new ArgumentException("encoder belongs to another device", nameof(commands));
        }

        enc.Finish();
        var upload = _upload;
        _upload = null;
        upload?.Finish();

        // Throttle: at most MaxFramesInFlight submissions run ahead of the CPU.
        Poll();
        if (_serial - CompletedSerial >= MaxFramesInFlight)
        {
            WaitSerial(_serial - MaxFramesInFlight + 1);
        }

        var cbCount = enc.Done.Count + (upload?.Done.Count ?? 0);
        var needBytes = Math.Max(cbCount, 1) * sizeof(VkCommandBufferSubmitInfo);
        if (needBytes > _submitScratchBytes)
        {
            if (_submitScratch != 0)
            {
                NativeMemory.Free((void*)_submitScratch);
            }

            _submitScratchBytes = Math.Max(needBytes, 4096);
            _submitScratch = (nint)NativeMemory.Alloc((nuint)_submitScratchBytes);
        }

        var cbInfos = (VkCommandBufferSubmitInfo*)_submitScratch;
        var k = 0;
        if (upload is not null)
        {
            foreach (var cb in upload.Done)
            {
                cbInfos[k++] = new VkCommandBufferSubmitInfo { commandBuffer = cb };
            }
        }

        foreach (var cb in enc.Done)
        {
            cbInfos[k++] = new VkCommandBufferSubmitInfo { commandBuffer = cb };
        }

        var serial = ++_serial;
        const int MaxSwap = 8;
        var waits = stackalloc VkSemaphoreSubmitInfo[MaxSwap];
        var signals = stackalloc VkSemaphoreSubmitInfo[MaxSwap + 1];
        var nw = 0;
        var ns = 0;
        signals[ns++] = new VkSemaphoreSubmitInfo { semaphore = _timeline, value = serial, stageMask = VkPipelineStageFlagBits2.AllCommands };
        foreach (var sc in _acquired)
        {
            if (nw == MaxSwap)
            {
                break;
            }

            waits[nw++] = new VkSemaphoreSubmitInfo { semaphore = sc.AcquireSemaphore, stageMask = VkPipelineStageFlagBits2.AllCommands };
            signals[ns++] = new VkSemaphoreSubmitInfo { semaphore = sc.RenderDoneSemaphore, stageMask = VkPipelineStageFlagBits2.AllCommands };
            sc.MarkSubmitted();
        }

        _acquired.Clear();

        var submit = new VkSubmitInfo2
        {
            waitSemaphoreInfoCount = (uint)nw,
            pWaitSemaphoreInfos = waits,
            commandBufferInfoCount = (uint)k,
            pCommandBufferInfos = cbInfos,
            signalSemaphoreInfoCount = (uint)ns,
            pSignalSemaphoreInfos = signals,
        };
        Check(_deviceFns.vkQueueSubmit2(_queue, 1, &submit, default), "vkQueueSubmit2");

        if (upload is not null)
        {
            _inflight.Add((serial, upload));
        }

        _inflight.Add((serial, enc));
        foreach (var s in _stagingPending)
        {
            _stagingBusy.Add((serial, s));
        }

        _stagingPending.Clear();
    }

    // ---- staging memory for uploads and readback ----

    internal sealed class Staging
    {
        public VkBuffer Buffer;
        public MemoryAllocation Alloc;
        public ulong Size;
        public int Bucket;
    }

    Staging AcquireStaging(ulong size, bool readback)
    {
        var bits = 64 - (int)ulong.LeadingZeroCount(Math.Max(size, 65536) - 1);
        var bucket = bits + (readback ? 48 : 0);
        var stack = _stagingFree[bucket] ??= new Stack<Staging>();
        if (stack.Count > 0)
        {
            return stack.Pop();
        }

        var cap = 1UL << bits;
        var bci = new VkBufferCreateInfo
        {
            size = cap,
            usage = readback ? VkBufferUsageFlagBits.TransferDst : VkBufferUsageFlagBits.TransferSrc,
            sharingMode = VkSharingMode.Exclusive,
        };
        VkBuffer buf;
        Check(_deviceFns.vkCreateBuffer(_device, &bci, null, &buf), "vkCreateBuffer");
        VkMemoryRequirements req;
        _deviceFns.vkGetBufferMemoryRequirements(_device, buf, &req);
        var alloc = _memory.Allocate(req, readback ? MemoryKind.Readback : MemoryKind.Upload);
        Check(_deviceFns.vkBindBufferMemory(_device, buf, alloc.Memory, alloc.Offset), "vkBindBufferMemory");
        return new Staging { Buffer = buf, Alloc = alloc, Size = cap, Bucket = bucket };
    }

    void ReleaseStaging(Staging s) => (_stagingFree[s.Bucket] ??= new Stack<Staging>()).Push(s);

    public override void WriteBuffer(GpuBuffer buffer, ulong offset, ReadOnlySpan<byte> data)
    {
        var b = (VulkanBuffer)buffer;
        if ((b.Usage & BufferUsage.CopyDestination) == 0)
        {
            throw new ArgumentException("buffer was not created with BufferUsage.CopyDestination", nameof(buffer));
        }

        if (offset + (ulong)data.Length > b.Size)
        {
            throw new ArgumentOutOfRangeException(nameof(data), "write extends past the end of the buffer");
        }

        if (data.IsEmpty)
        {
            return;
        }

        var st = AcquireStaging((ulong)data.Length, false);
        data.CopyTo(new Span<byte>(st.Alloc.Mapped, data.Length));
        _stagingPending.Add(st);
        UploadEncoder.CopyRawToBuffer(st.Buffer, b, offset, (ulong)data.Length);
    }

    public override void WriteTexture(GpuTexture texture, uint mipLevel, uint layer, ReadOnlySpan<byte> data)
    {
        var t = (VulkanTexture)texture;
        if ((t.Usage & TextureUsage.CopyDestination) == 0)
        {
            throw new ArgumentException("texture was not created with TextureUsage.CopyDestination", nameof(texture));
        }

        ValidateSubresource(t, mipLevel, layer);
        var expected = LevelByteSize(t.Format, t.Width, t.Height, mipLevel);
        if ((ulong)data.Length != expected)
        {
            throw new ArgumentException($"expected {expected} bytes of tightly packed texel rows for mip {mipLevel}, got {data.Length}", nameof(data));
        }

        var st = AcquireStaging(expected, false);
        data.CopyTo(new Span<byte>(st.Alloc.Mapped, data.Length));
        _stagingPending.Add(st);
        UploadEncoder.CopyRawToTexture(st.Buffer, t, mipLevel, layer);
    }

    static void ValidateSubresource(VulkanTexture t, uint mip, uint layer)
    {
        if (mip >= t.MipLevels || layer >= t.ArrayLayers)
        {
            throw new ArgumentOutOfRangeException(nameof(mip), "mip level or layer out of range");
        }
    }

    public override void ReadBuffer(GpuBuffer buffer, ulong offset, Span<byte> destination)
    {
        var b = (VulkanBuffer)buffer;
        if ((b.Usage & BufferUsage.CopySource) == 0)
        {
            throw new ArgumentException("buffer was not created with BufferUsage.CopySource", nameof(buffer));
        }

        if (offset + (ulong)destination.Length > b.Size)
        {
            throw new ArgumentOutOfRangeException(nameof(destination), "read extends past the end of the buffer");
        }

        if (destination.IsEmpty)
        {
            return;
        }

        var st = AcquireStaging((ulong)destination.Length, true);
        var enc = (VulkanEncoder)BeginCommands();
        enc.CopyBufferToRaw(b, offset, st.Buffer, (ulong)destination.Length);
        FinishReadback(enc, st, destination);
    }

    public override void ReadTexture(GpuTexture texture, uint mipLevel, uint layer, Span<byte> destination)
    {
        var t = (VulkanTexture)texture;
        if ((t.Usage & TextureUsage.CopySource) == 0)
        {
            throw new ArgumentException("texture was not created with TextureUsage.CopySource", nameof(texture));
        }

        ValidateSubresource(t, mipLevel, layer);
        var expected = LevelByteSize(t.Format, t.Width, t.Height, mipLevel);
        if ((ulong)destination.Length != expected)
        {
            throw new ArgumentException($"expected a destination of {expected} bytes for mip {mipLevel}, got {destination.Length}", nameof(destination));
        }

        var st = AcquireStaging(expected, true);
        var enc = (VulkanEncoder)BeginCommands();
        enc.CopyTextureToRaw(t, mipLevel, layer, st.Buffer);
        FinishReadback(enc, st, destination);
    }

    void FinishReadback(VulkanEncoder enc, Staging st, Span<byte> destination)
    {
        Submit(enc);
        WaitSerial(_serial);

        // Older validation layers only treat a queue-idle wait as proof that the staging buffer is free again.
        Check(_deviceFns.vkQueueWaitIdle(_queue), "vkQueueWaitIdle");
        new ReadOnlySpan<byte>(st.Alloc.Mapped, destination.Length).CopyTo(destination);
        ReleaseStaging(st);
    }

    // ---- teardown ----

    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_device.Value != 0)
        {
            try
            {
                if (_upload is not null)
                {
                    Submit(BeginCommands());
                }

                _deviceFns.vkDeviceWaitIdle(_device);
            }
            catch (GpuException)
            {
                // Tearing down after a lost device: destroy what we can.
            }
        }

        foreach (var sc in _swapchains.ToArray())
        {
            sc.Dispose();
        }

        _disposed = true;
        foreach (var (_, item) in _retired)
        {
            item.DestroyNow();
        }

        _retired.Clear();
        DestroyDescriptorPools();
        foreach (var (_, e) in _inflight)
        {
            e.Destroy();
        }

        _inflight.Clear();
        while (_freeEncoders.Count > 0)
        {
            _freeEncoders.Pop().Destroy();
        }

        _upload?.Destroy();
        foreach (var stack in _stagingFree)
        {
            if (stack is null)
            {
                continue;
            }

            foreach (var s in stack)
            {
                DestroyStaging(s);
            }
        }

        foreach (var (_, s) in _stagingBusy)
        {
            DestroyStaging(s);
        }

        foreach (var s in _stagingPending)
        {
            DestroyStaging(s);
        }

        if (_submitScratch != 0)
        {
            NativeMemory.Free((void*)_submitScratch);
            _submitScratch = 0;
        }

        if (_device.Value != 0)
        {
            if (_timeline.Value != 0)
            {
                _deviceFns.vkDestroySemaphore(_device, _timeline, null);
            }

            _memory?.Destroy();
            _deviceFns.vkDestroyDevice(_device, null);
            _device = default;
        }

        _owner?.Release();
        _owner = null;
    }

    void DestroyStaging(Staging s)
    {
        _deviceFns.vkDestroyBuffer(_device, s.Buffer, null);
        _memory.Free(s.Alloc);
    }

    internal void TrackSwapchain(VulkanSwapchain sc) => _swapchains.Add(sc);

    internal void ForgetSwapchain(VulkanSwapchain sc)
    {
        _swapchains.Remove(sc);
        _acquired.Remove(sc);
    }

    internal float MaxAnisotropy => _anisotropy ? _maxAnisotropy : 1f;
}
