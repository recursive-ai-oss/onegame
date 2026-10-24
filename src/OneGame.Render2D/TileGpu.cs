using System.Runtime.InteropServices;
using OneGame.Gpu;

namespace OneGame.Render2D;

/// <summary>
/// The GPU side of one tilemap: two pools of chunk-sized array layers (tile ids for near zoom, colours for far zoom) with least-recently-used
/// replacement, plus the bind group the tile shaders read them through.
/// </summary>
internal sealed class TileGpu : IDisposable
{
    private const int ChunkTiles = TileMap.ChunkSize * TileMap.ChunkSize;

    private readonly GpuTexture _ids;
    private readonly GpuTexture _colors;
    private readonly GpuBuffer _info;
    private readonly long[] _keys;
    private readonly int[] _versions;
    private readonly int[] _lastFrame;
    private readonly Dictionary<long, int> _slotOf = [];
    private readonly uint[] _scratch = new uint[ChunkTiles];
    private int _used;
    private bool _disposed;

    public TileGpu(GpuDevice device, GpuPipelineLayout layout, TileMap map)
    {
        var n = Math.Max(map.PoolChunks, 2);
        _keys = new long[n];
        _versions = new int[n];
        _lastFrame = new int[n];
        _ids = device.CreateTexture(new TextureDesc(TileMap.ChunkSize, TileMap.ChunkSize, GpuFormat.R32Uint, TextureUsage.Sampled | TextureUsage.CopyDestination) { ArrayLayers = (uint)n, Label = "tile ids" });
        _colors = device.CreateTexture(new TextureDesc(TileMap.ChunkSize, TileMap.ChunkSize, GpuFormat.Rgba8UnormSrgb, TextureUsage.Sampled | TextureUsage.CopyDestination) { ArrayLayers = (uint)n, Label = "tile colours" });
        _info = device.CreateBuffer(new BufferDesc(16, BufferUsage.Uniform | BufferUsage.CopyDestination) { Label = "tile info" });
        var ts = map.TileSet;
        device.WriteBuffer(_info, 0, MemoryMarshal.AsBytes<float>([ts.Columns, ts.Rows, ts.TileWidth, ts.TileHeight]));
        Group = device.CreateBindGroup(layout, 2, [BindGroupEntry.ForBuffer(0, _info), BindGroupEntry.ForTexture(1, _ids), BindGroupEntry.ForTexture(2, _colors)]);
    }

    public GpuBindGroup Group { get; }

    /// <summary>Finds or assigns a layer for a chunk. Returns -1 when every layer was used this frame. <paramref name="upload"/> says the layer needs the chunk's data.</summary>
    public int Acquire(long key, int version, int frame, out bool upload)
    {
        if (_slotOf.TryGetValue(key, out var slot))
        {
            _lastFrame[slot] = frame;
            upload = _versions[slot] != version;
            _versions[slot] = version;
            return slot;
        }

        if (_used < _keys.Length)
        {
            slot = _used++;
        }
        else
        {
            slot = -1;
            var oldest = int.MaxValue;
            for (var i = 0; i < _keys.Length; i++)
            {
                if (_lastFrame[i] < frame && _lastFrame[i] < oldest)
                {
                    oldest = _lastFrame[i];
                    slot = i;
                }
            }

            if (slot < 0)
            {
                upload = false;
                return -1;
            }

            _slotOf.Remove(_keys[slot]);
        }

        _keys[slot] = key;
        _versions[slot] = version;
        _lastFrame[slot] = frame;
        _slotOf[key] = slot;
        upload = true;
        return slot;
    }

    public void Upload(GpuDevice device, int level, int slot, ushort[]? ids, uint[]? colors)
    {
        if (level == 0)
        {
            for (var i = 0; i < ChunkTiles; i++)
            {
                _scratch[i] = ids![i];
            }

            device.WriteTexture(_ids, 0, (uint)slot, MemoryMarshal.AsBytes<uint>(_scratch));
        }
        else
        {
            device.WriteTexture(_colors, 0, (uint)slot, MemoryMarshal.AsBytes<uint>(colors!));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Group.Dispose();
        _info.Dispose();
        _ids.Dispose();
        _colors.Dispose();
    }
}
