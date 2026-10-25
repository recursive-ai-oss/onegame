using System.Runtime.InteropServices;
using OneGame.Gpu.Metal.Interop;

namespace OneGame.Gpu.Metal;

/// <summary>A device on the Metal API. One device is used from one thread.</summary>
internal sealed unsafe partial class MetalDevice : GpuDevice
{
    /// <summary>Metal buffer index of vertex buffer slot 0; slot n is bound at this minus n. Matches the shader compiler's vertex buffer index.</summary>
    internal const uint VertexBufferBase = 30;

    nint _device;
    nint _queue;
    nint _noDepthState;
    readonly GpuAdapterInfo _info;
    GpuCapabilities _caps;
    bool _disposed;
    readonly bool _debugLayer;
    readonly List<string> _messages = [];

    // Submissions that may still be running: (serial, command buffer, counts toward the frame limit).
    readonly Queue<(ulong Serial, nint Buffer, bool Frame)> _inflight = new();
    ulong _serial;
    int _frames;

    // Writes waiting to be copied into their destinations before the next submitted commands.
    readonly List<PendingUpload> _uploads = [];
    readonly Stack<MetalEncoder> _freeEncoders = new();

    internal MetalDevice(nint device, GpuAdapterInfo info, bool debugLayer)
    {
        _device = device;
        _info = info;
        _debugLayer = debugLayer;
        using var pool = new Pool();
        _queue = Msg.Obj(_device, Sel.newCommandQueue);
        if (_queue == 0)
        {
            throw new GpuException("Metal could not create a command queue");
        }

        _caps = QueryCapabilities();
        var dsd = Msg.Obj(Cls.MTLDepthStencilDescriptor, Sel.@new);
        _noDepthState = Msg.Obj(_device, Sel.newDepthStencilStateWithDescriptor_, dsd);
        Objc.objc_release(dsd);
    }

    internal nint Handle => _device;

    internal nint NoDepthState => _noDepthState;

    /// <inheritdoc />
    public override GpuAdapterInfo Adapter => _info;

    /// <inheritdoc />
    public override GpuCapabilities Capabilities => _caps;

    /// <inheritdoc />
    public override GpuBackend Backend => GpuBackend.Metal;

    GpuCapabilities QueryCapabilities()
    {
        uint maxSamples = 1;
        foreach (var n in new nuint[] { 8, 4, 2 })
        {
            if (Msg.Flag(_device, Sel.supportsTextureSampleCount_, n))
            {
                maxSamples = (uint)n;
                break;
            }
        }

        // Metal 3 limits for Apple silicon; the largest 2D texture is 16384 texels on every GPU family this backend runs on.
        return new GpuCapabilities(
            Timestamps: false,
            MultiDrawIndirect: false,
            IndirectFirstInstance: true,
            DepthClamp: true,
            DepthBiasClamp: true,
            NonUniformArrayIndexing: true,
            MaxSampleCount: maxSamples,
            MaxTextureSize: 16384,
            MaxTextureSize3D: 2048,
            MaxArrayLayers: 2048,
            MaxResourceArrayCount: 64)
        {
            PushConstants = false,
        };
    }

    // ---- submission ----

    /// <summary>Releases command buffers the GPU has finished and notes the ones that failed.</summary>
    void Poll()
    {
        while (_inflight.Count > 0)
        {
            var (_, cb, _) = _inflight.Peek();
            var status = Msg.UL(cb, Sel.status);
            if (status < MT.StatusCompleted)
            {
                break;
            }

            Retire(_inflight.Dequeue());
        }
    }

    void Retire((ulong Serial, nint Buffer, bool Frame) entry)
    {
        if (Msg.UL(entry.Buffer, Sel.status) == MT.StatusError)
        {
            var err = Msg.Obj(entry.Buffer, Sel.error);
            _messages.Add("command buffer failed: " + (err == 0 ? "unknown error" : Objc.ErrorText(err)));
        }

        if (entry.Frame)
        {
            _frames--;
        }

        Objc.objc_release(entry.Buffer);
    }

    void WaitOldest()
    {
        var entry = _inflight.Dequeue();
        Msg.Void(entry.Buffer, Sel.waitUntilCompleted);
        Retire(entry);
    }

    /// <summary>Commits a command buffer the backend now owns one reference to.</summary>
    internal void Commit(nint commandBuffer, bool frame)
    {
        Msg.Void(commandBuffer, Sel.commit);
        _inflight.Enqueue((++_serial, commandBuffer, frame));
        if (frame)
        {
            _frames++;
        }
    }

    /// <inheritdoc />
    public override GpuCommandEncoder BeginCommands()
    {
        ThrowIfDisposed();
        using var pool = new Pool();
        var cb = Msg.Obj(_queue, Sel.commandBuffer);
        Objc.objc_retain(cb);
        var enc = _freeEncoders.Count > 0 ? _freeEncoders.Pop() : new MetalEncoder(this);
        enc.Begin(cb);
        return enc;
    }

    /// <inheritdoc />
    public override void Submit(GpuCommandEncoder commands)
    {
        var enc = commands as MetalEncoder ?? throw new ArgumentException("encoder belongs to another backend", nameof(commands));
        if (enc.Device != this)
        {
            throw new ArgumentException("encoder belongs to another device", nameof(commands));
        }

        using var pool = new Pool();
        var cb = enc.Finish();
        FlushUploads();
        Commit(cb, frame: true);
        _freeEncoders.Push(enc);

        // Throttle: at most MaxFramesInFlight submissions run ahead of the CPU.
        Poll();
        while (_frames >= MaxFramesInFlight && _inflight.Count > 0)
        {
            WaitOldest();
        }
    }

    internal void Present(nint drawable)
    {
        using var pool = new Pool();
        FlushUploads();
        var cb = Msg.Obj(_queue, Sel.commandBuffer);
        Objc.objc_retain(cb);
        Msg.Void(cb, Sel.presentDrawable_, drawable);
        Commit(cb, frame: false);
    }

    /// <inheritdoc />
    public override void WaitIdle()
    {
        ThrowIfDisposed();
        using var pool = new Pool();
        FlushUploads();
        while (_inflight.Count > 0)
        {
            WaitOldest();
        }
    }

    /// <inheritdoc />
    public override string[] TakeValidationMessages()
    {
        Poll();
        var result = _messages.ToArray();
        _messages.Clear();
        return result;
    }

    internal bool DebugLayer => _debugLayer;

    // ---- uploads ----

    struct PendingUpload
    {
        public nint Source;
        public nint Destination;
        public ulong DestinationOffset;
        public ulong Size;
        public bool IsTexture;
        public uint Mip, Layer, X, Y, Z, Width, Height, Depth;
        public nuint BytesPerRow, BytesPerImage;
    }

    nint NewStaging(ReadOnlySpan<byte> data)
    {
        fixed (byte* p = data)
        {
            var buffer = Msg.Obj(_device, Sel.newBufferWithBytes_length_options_, (nint)p, (nuint)data.Length, MT.StorageShared);
            if (buffer == 0)
            {
                throw new GpuOutOfMemoryException("could not allocate a staging buffer of " + data.Length + " bytes");
            }

            return buffer;
        }
    }

    /// <inheritdoc />
    public override void WriteBuffer(GpuBuffer buffer, ulong offset, ReadOnlySpan<byte> data)
    {
        var b = buffer as MetalBuffer ?? throw new ArgumentException("buffer belongs to another backend", nameof(buffer));
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

        using var pool = new Pool();
        _uploads.Add(new PendingUpload
        {
            Source = NewStaging(data),
            Destination = Objc.objc_retain(b.Handle),
            DestinationOffset = offset,
            Size = (ulong)data.Length,
        });
    }

    /// <inheritdoc />
    public override void WriteTexture(GpuTexture texture, uint mipLevel, uint layer, ReadOnlySpan<byte> data)
    {
        var t = texture as MetalTexture ?? throw new ArgumentException("texture belongs to another backend", nameof(texture));
        ValidateSubresource(t, mipLevel, layer);
        var region = new TextureRegion(mipLevel, layer, 0, 0, 0, Math.Max(t.Width >> (int)mipLevel, 1), Math.Max(t.Height >> (int)mipLevel, 1), Math.Max(t.Depth >> (int)mipLevel, 1));
        WriteTexture(texture, region, data);
    }

    /// <inheritdoc />
    public override void WriteTexture(GpuTexture texture, in TextureRegion region, ReadOnlySpan<byte> data)
    {
        var t = texture as MetalTexture ?? throw new ArgumentException("texture belongs to another backend", nameof(texture));
        if ((t.Usage & TextureUsage.CopyDestination) == 0)
        {
            throw new ArgumentException("texture was not created with TextureUsage.CopyDestination", nameof(texture));
        }

        ValidateSubresource(t, region.MipLevel, region.Layer);
        var lw = Math.Max(t.Width >> (int)region.MipLevel, 1);
        var lh = Math.Max(t.Height >> (int)region.MipLevel, 1);
        var ld = Math.Max(t.Depth >> (int)region.MipLevel, 1);
        if (region.Width == 0 || region.Height == 0 || region.Depth == 0
            || (ulong)region.X + region.Width > lw || (ulong)region.Y + region.Height > lh || (ulong)region.Z + region.Depth > ld)
        {
            throw new ArgumentOutOfRangeException(nameof(region), $"region lies outside mip {region.MipLevel} ({lw}x{lh}x{ld})");
        }

        if (t.Dimension != TextureDimension.D3 && (region.Z != 0 || region.Depth != 1))
        {
            throw new ArgumentOutOfRangeException(nameof(region), "only 3D textures have slices; use Layer for arrays and cube faces");
        }

        var bpt = BytesPerTexel(t.Format);
        var expected = (ulong)region.Width * region.Height * region.Depth * bpt;
        if ((ulong)data.Length != expected)
        {
            throw new ArgumentException($"expected {expected} bytes of tightly packed texel rows for a {region.Width}x{region.Height}x{region.Depth} region, got {data.Length}", nameof(data));
        }

        using var pool = new Pool();
        _uploads.Add(new PendingUpload
        {
            Source = NewStaging(data),
            Destination = Objc.objc_retain(t.Handle),
            IsTexture = true,
            Size = expected,
            Mip = region.MipLevel,
            Layer = region.Layer,
            X = region.X,
            Y = region.Y,
            Z = region.Z,
            Width = region.Width,
            Height = region.Height,
            Depth = region.Depth,
            BytesPerRow = (nuint)(region.Width * bpt),
            BytesPerImage = (nuint)((ulong)region.Width * region.Height * bpt),
        });
    }

    static void ValidateSubresource(MetalTexture t, uint mip, uint layer)
    {
        if (mip >= t.MipLevels || layer >= t.ArrayLayers)
        {
            throw new ArgumentOutOfRangeException(nameof(mip), "mip level or layer out of range");
        }
    }

    /// <summary>Copies queued writes into their destinations, ahead of anything committed after this call.</summary>
    void FlushUploads()
    {
        if (_uploads.Count == 0)
        {
            return;
        }

        var cb = Msg.Obj(_queue, Sel.commandBuffer);
        Objc.objc_retain(cb);
        var blit = Msg.Obj(cb, Sel.blitCommandEncoder);
        foreach (var u in _uploads)
        {
            if (!u.IsTexture)
            {
                Msg.Void(blit, Sel.copyFromBuffer_sourceOffset_toBuffer_destinationOffset_size_, u.Source, (nuint)0, u.Destination, (nuint)u.DestinationOffset, (nuint)u.Size);
            }
            else
            {
                Msg.Void(blit, Sel.copyFromBuffer_sourceOffset_sourceBytesPerRow_sourceBytesPerImage_sourceSize_toTexture_destinationSlice_destinationLevel_destinationOrigin_,
                    u.Source, (nuint)0, u.BytesPerRow, u.BytesPerImage, new MTLSize(u.Width, u.Height, u.Depth),
                    u.Destination, (nuint)u.Layer, (nuint)u.Mip, new MTLOrigin(u.X, u.Y, u.Z));
            }
        }

        Msg.Void(blit, Sel.endEncoding);
        Commit(cb, frame: false);
        foreach (var u in _uploads)
        {
            // The command buffer holds what it needs from here on.
            Objc.objc_release(u.Source);
            Objc.objc_release(u.Destination);
        }

        _uploads.Clear();
    }

    // ---- readback ----

    /// <inheritdoc />
    public override void ReadBuffer(GpuBuffer buffer, ulong offset, Span<byte> destination)
    {
        var b = buffer as MetalBuffer ?? throw new ArgumentException("buffer belongs to another backend", nameof(buffer));
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

        using var pool = new Pool();
        FlushUploads();
        var staging = Msg.Obj(_device, Sel.newBufferWithLength_options_, (nuint)destination.Length, MT.StorageShared);
        var cb = Msg.Obj(_queue, Sel.commandBuffer);
        Objc.objc_retain(cb);
        var blit = Msg.Obj(cb, Sel.blitCommandEncoder);
        Msg.Void(blit, Sel.copyFromBuffer_sourceOffset_toBuffer_destinationOffset_size_, b.Handle, (nuint)offset, staging, (nuint)0, (nuint)destination.Length);
        Msg.Void(blit, Sel.endEncoding);
        FinishReadback(cb, staging, destination);
    }

    /// <inheritdoc />
    public override void ReadTexture(GpuTexture texture, uint mipLevel, uint layer, Span<byte> destination)
    {
        var t = texture as MetalTexture ?? throw new ArgumentException("texture belongs to another backend", nameof(texture));
        if ((t.Usage & TextureUsage.CopySource) == 0)
        {
            throw new ArgumentException("texture was not created with TextureUsage.CopySource", nameof(texture));
        }

        ValidateSubresource(t, mipLevel, layer);
        var expected = LevelByteSize(t.Format, t.Width, t.Height, t.Depth, mipLevel);
        if ((ulong)destination.Length != expected)
        {
            throw new ArgumentException($"expected a destination of {expected} bytes for mip {mipLevel}, got {destination.Length}", nameof(destination));
        }

        using var pool = new Pool();
        FlushUploads();
        var staging = Msg.Obj(_device, Sel.newBufferWithLength_options_, (nuint)expected, MT.StorageShared);
        var cb = Msg.Obj(_queue, Sel.commandBuffer);
        Objc.objc_retain(cb);
        var blit = Msg.Obj(cb, Sel.blitCommandEncoder);
        MetalEncoder.EncodeTextureToBuffer(blit, t, mipLevel, layer, staging, 0);
        Msg.Void(blit, Sel.endEncoding);
        FinishReadback(cb, staging, destination);
    }

    void FinishReadback(nint cb, nint staging, Span<byte> destination)
    {
        // Everything submitted earlier runs first (one queue), so waiting for this buffer waits for it all.
        Commit(cb, frame: false);
        while (_inflight.Count > 0)
        {
            WaitOldest();
        }

        new ReadOnlySpan<byte>((void*)Msg.Obj(staging, Sel.contents), destination.Length).CopyTo(destination);
        Objc.objc_release(staging);
    }

    // ---- teardown ----

    void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            WaitIdle();
        }
        catch (GpuException)
        {
            // Tearing down after a failed device: release what we can.
        }

        _disposed = true;
        Objc.Release(ref _noDepthState);
        Objc.Release(ref _queue);
        Objc.Release(ref _device);
    }
}
