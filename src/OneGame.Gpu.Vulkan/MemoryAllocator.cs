using OneGame.Gpu.Vulkan.Interop;

namespace OneGame.Gpu.Vulkan;

/// <summary>A sub-range of a device memory block.</summary>
internal unsafe struct MemoryAllocation
{
    public VkDeviceMemory Memory;
    public ulong Offset;
    public ulong Size;
    public byte* Mapped;
    public int Block;
    public int Pool;
}

internal enum MemoryKind
{
    /// <summary>Fast GPU-only memory.</summary>
    DeviceLocal,

    /// <summary>CPU-writable memory for uploads.</summary>
    Upload,

    /// <summary>CPU-readable memory (cached when available) for readback.</summary>
    Readback,
}

/// <summary>
/// First-fit sub-allocator over large memory blocks, one set of blocks per Vulkan memory type.
/// Vulkan limits the number of allocations (maxMemoryAllocationCount), so resources share blocks.
/// </summary>
internal sealed unsafe class MemoryAllocator
{
    const ulong BlockSize = 64UL << 20;
    const ulong DedicatedThreshold = 16UL << 20;

    sealed class Block
    {
        public VkDeviceMemory Memory;
        public ulong Size;
        public byte* Mapped;
        public List<(ulong Offset, ulong Size)> Free = [];
        public bool InUse;
    }

    readonly VulkanDevice _device;
    readonly List<Block>[] _pools;
    readonly VkPhysicalDeviceMemoryProperties _props;
    readonly ulong _granularity;

    public MemoryAllocator(VulkanDevice device, in VkPhysicalDeviceMemoryProperties props, ulong bufferImageGranularity)
    {
        _device = device;
        _props = props;
        _granularity = Math.Max(bufferImageGranularity, 1);
        _pools = new List<Block>[props.memoryTypeCount];
        for (var i = 0; i < _pools.Length; i++)
        {
            _pools[i] = [];
        }
    }

    VkMemoryPropertyFlagBits Flags(uint i) => _props.memoryTypes[(int)i].propertyFlags;

    uint PickType(uint typeBits, MemoryKind kind)
    {
        // Preference lists, most to least wanted. Upload and readback memory must be host-visible and coherent so no flushes are needed.
        var coherent = VkMemoryPropertyFlagBits.HostVisible | VkMemoryPropertyFlagBits.HostCoherent;
        VkMemoryPropertyFlagBits[] wants = kind switch
        {
            MemoryKind.DeviceLocal => [VkMemoryPropertyFlagBits.DeviceLocal, 0],
            MemoryKind.Upload => [coherent, coherent | VkMemoryPropertyFlagBits.DeviceLocal],
            _ => [coherent | VkMemoryPropertyFlagBits.HostCached, coherent],
        };
        foreach (var want in wants)
        {
            for (uint i = 0; i < _props.memoryTypeCount; i++)
            {
                if ((typeBits & (1u << (int)i)) != 0 && (Flags(i) & want) == want)
                {
                    return i;
                }
            }
        }

        throw new GpuUnsupportedException("no suitable memory type for the requested resource");
    }

    static ulong AlignUp(ulong v, ulong a) => (v + a - 1) / a * a;

    public MemoryAllocation Allocate(in VkMemoryRequirements req, MemoryKind kind)
    {
        var type = PickType(req.memoryTypeBits, kind);
        var pool = _pools[type];
        var align = Math.Max(req.alignment, _granularity);
        var size = AlignUp(req.size, align);
        var host = (Flags(type) & VkMemoryPropertyFlagBits.HostVisible) != 0;

        if (size < DedicatedThreshold)
        {
            for (var b = 0; b < pool.Count; b++)
            {
                if (pool[b].InUse && TryCarve(pool[b], size, align, out var off))
                {
                    return Make(pool[b], b, (int)type, off, size);
                }
            }
        }

        var blk = NewBlock(type, size < DedicatedThreshold ? BlockSize : size, host);
        var slot = pool.FindIndex(b => !b.InUse);
        if (slot < 0)
        {
            pool.Add(blk);
            slot = pool.Count - 1;
        }
        else
        {
            pool[slot] = blk;
        }

        TryCarve(blk, size, align, out var offset);
        return Make(blk, slot, (int)type, offset, size);
    }

    static MemoryAllocation Make(Block blk, int index, int pool, ulong offset, ulong size) => new()
    {
        Memory = blk.Memory,
        Offset = offset,
        Size = size,
        Mapped = blk.Mapped == null ? null : blk.Mapped + offset,
        Block = index,
        Pool = pool,
    };

    static bool TryCarve(Block blk, ulong size, ulong align, out ulong offset)
    {
        for (var i = 0; i < blk.Free.Count; i++)
        {
            var (fo, fs) = blk.Free[i];
            var start = AlignUp(fo, align);
            var pad = start - fo;
            if (fs < pad + size)
            {
                continue;
            }

            var tail = fs - pad - size;
            if (pad > 0 && tail > 0)
            {
                blk.Free[i] = (fo, pad);
                blk.Free.Insert(i + 1, (start + size, tail));
            }
            else if (pad > 0)
            {
                blk.Free[i] = (fo, pad);
            }
            else if (tail > 0)
            {
                blk.Free[i] = (start + size, tail);
            }
            else
            {
                blk.Free.RemoveAt(i);
            }

            offset = start;
            return true;
        }

        offset = 0;
        return false;
    }

    Block NewBlock(uint type, ulong size, bool host)
    {
        var info = new VkMemoryAllocateInfo { allocationSize = size, memoryTypeIndex = type };
        VkDeviceMemory mem;
        var r = _device.Fns.vkAllocateMemory(_device.Handle, &info, null, &mem);
        if (r is VkResult.ErrorOutOfDeviceMemory or VkResult.ErrorOutOfHostMemory)
        {
            throw new GpuOutOfMemoryException($"device memory allocation of {size} bytes failed");
        }

        _device.Check(r, "vkAllocateMemory");
        byte* mapped = null;
        if (host)
        {
            void* p;
            _device.Check(_device.Fns.vkMapMemory(_device.Handle, mem, 0, ulong.MaxValue, 0, &p), "vkMapMemory");
            mapped = (byte*)p;
        }

        var blk = new Block { Memory = mem, Size = size, Mapped = mapped, InUse = true };
        blk.Free.Add((0, size));
        return blk;
    }

    public void Free(in MemoryAllocation a)
    {
        var blk = _pools[a.Pool][a.Block];
        var list = blk.Free;
        var i = 0;
        while (i < list.Count && list[i].Offset < a.Offset)
        {
            i++;
        }

        list.Insert(i, (a.Offset, a.Size));
        if (i + 1 < list.Count && list[i].Offset + list[i].Size == list[i + 1].Offset)
        {
            list[i] = (list[i].Offset, list[i].Size + list[i + 1].Size);
            list.RemoveAt(i + 1);
        }

        if (i > 0 && list[i - 1].Offset + list[i - 1].Size == list[i].Offset)
        {
            list[i - 1] = (list[i - 1].Offset, list[i - 1].Size + list[i].Size);
            list.RemoveAt(i);
        }

        // A block made just for one large resource goes back to the driver when it empties; shared blocks stay for reuse.
        if (blk.Size != BlockSize && list.Count == 1 && list[0].Size == blk.Size)
        {
            _device.Fns.vkFreeMemory(_device.Handle, blk.Memory, null);
            blk.InUse = false;
            blk.Free.Clear();
        }
    }

    public void Destroy()
    {
        foreach (var pool in _pools)
        {
            foreach (var b in pool)
            {
                if (b.InUse)
                {
                    _device.Fns.vkFreeMemory(_device.Handle, b.Memory, null);
                }
            }

            pool.Clear();
        }
    }
}
