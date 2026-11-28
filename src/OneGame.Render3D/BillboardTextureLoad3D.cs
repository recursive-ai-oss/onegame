namespace OneGame.Render3D;

/// <summary>
/// A billboard texture whose pixels are being premultiplied on pool threads (see <see cref="Renderer3D.BeginCreateBillboardTexture"/>).
/// Poll <see cref="IsReady"/> from the frame loop and call <see cref="Complete"/> on the renderer's thread.
/// </summary>
public sealed class BillboardTextureLoad3D
{
    private const int RowsPerChunk = 64;

    private readonly Renderer3D _renderer;
    private readonly int _width;
    private readonly int _height;
    private readonly string? _label;
    private readonly byte[] _data;
    private readonly Task _work;
    private BillboardTexture3D? _result;

    internal BillboardTextureLoad3D(Renderer3D renderer, int width, int height, ReadOnlyMemory<byte> rgba, string? label)
    {
        _renderer = renderer;
        _width = width;
        _height = height;
        _label = label;
        _data = new byte[rgba.Length];
        var data = _data;
        var stride = width * 4;
        var chunks = (height + RowsPerChunk - 1) / RowsPerChunk;
        if (OperatingSystem.IsBrowser())
        {
            // One thread only: a pool task could never run while Complete waits for it, so the work is done here.
            Renderer3D.PremultiplyRows(rgba.Span, data, 0, data.Length);
            _work = Task.CompletedTask;
            return;
        }

        _work = Task.Run(() => Parallel.For(0, chunks, chunk =>
        {
            var firstRow = chunk * RowsPerChunk;
            var lastRow = Math.Min(height, firstRow + RowsPerChunk);
            Renderer3D.PremultiplyRows(rgba.Span, data, firstRow * stride, lastRow * stride);
        }));
    }

    /// <summary>True once the pixels are ready and <see cref="Complete"/> will not wait.</summary>
    public bool IsReady => _work.IsCompleted;

    /// <summary>Uploads the pixels and returns the texture, waiting for the premultiply if it has not finished. Call it on the thread that owns the renderer; later calls return the same texture.</summary>
    /// <returns>The texture. Dispose it before the renderer.</returns>
    public BillboardTexture3D Complete()
    {
        if (_result is { } done)
        {
            return done;
        }

        _work.GetAwaiter().GetResult();
        return _result = _renderer.UploadBillboard(_width, _height, _data, _label);
    }
}
