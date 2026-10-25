using System.Buffers.Binary;
using System.Text;

namespace OneGame.Gpu.WebGpu;

/// <summary>
/// A growable byte stream of 32-bit words: the commands the managed side wants the browser to run. The format is private to this
/// backend and its script; both ends are built together.
/// </summary>
internal sealed class OpStream
{
    private byte[] _buffer = new byte[4096];
    private int _length;

    public int Length => _length;

    public ReadOnlySpan<byte> Span => _buffer.AsSpan(0, _length);

    public Span<byte> Mutable => _buffer.AsSpan(0, _length);

    public void Clear() => _length = 0;

    private Span<byte> Reserve(int n)
    {
        if (_length + n > _buffer.Length)
        {
            Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, checked(_length + n)));
        }

        var s = _buffer.AsSpan(_length, n);
        _length += n;
        return s;
    }

    public OpStream U(uint v)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(Reserve(4), v);
        return this;
    }

    public OpStream U(int v) => U(unchecked((uint)v));

    public OpStream U(ulong v)
    {
        var s = Reserve(8);
        BinaryPrimitives.WriteUInt32LittleEndian(s, (uint)v);
        BinaryPrimitives.WriteUInt32LittleEndian(s[4..], (uint)(v >> 32));
        return this;
    }

    public OpStream F(float v)
    {
        BinaryPrimitives.WriteSingleLittleEndian(Reserve(4), v);
        return this;
    }

    public OpStream Bytes(ReadOnlySpan<byte> data)
    {
        U((uint)data.Length);
        var padded = (data.Length + 3) & ~3;
        var s = Reserve(padded);
        data.CopyTo(s);
        s[data.Length..].Clear();
        return this;
    }

    public OpStream Str(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return U(0u);
        }

        var n = Encoding.UTF8.GetByteCount(text);
        U((uint)n);
        var s = Reserve((n + 3) & ~3);
        Encoding.UTF8.GetBytes(text, s);
        s[n..].Clear();
        return this;
    }

    public OpStream Append(OpStream other)
    {
        other.Span.CopyTo(Reserve(other.Length));
        return this;
    }
}

/// <summary>The numbers of the stream's operations; the script has the same table.</summary>
internal static class Op
{
    public const uint CreateBuffer = 1, CreateTexture = 2, CreateSampler = 3, CreateShader = 4, CreateLayout = 5, CreateBindGroup = 6,
        CreateGraphicsPipeline = 7, CreateComputePipeline = 8, Destroy = 9, WriteBuffer = 10, WriteTexture = 11,
        BeginEncoder = 20, Submit = 21, BeginRenderPass = 22, EndRenderPass = 23, BeginComputePass = 24, EndComputePass = 25,
        SetPipeline = 26, SetBindGroup = 27, SetVertexBuffer = 28, SetIndexBuffer = 29, SetViewport = 30, SetScissor = 31,
        Draw = 32, DrawIndexed = 33, DrawIndirect = 34, DrawIndexedIndirect = 35, Dispatch = 36, DispatchIndirect = 37,
        CopyBuffer = 40, CopyTextureToBuffer = 41, CopyBufferToTexture = 42, CopyTexture = 43, GenerateMips = 44, SetBackbuffer = 45, PushBlock = 46;
}
