using System.Text;

namespace OneGame.Core;

/// <summary>
/// Growable byte buffer that the generated codecs write into. Integers are LEB128 varints (signed values zigzag-encoded), so
/// small values stay small and the encoding is the same on every platform.
/// </summary>
public sealed class SyncWriter
{
    private byte[] _buffer;
    private int _length;

    /// <summary>Creates a writer with the given starting capacity.</summary>
    public SyncWriter(int capacity = 256)
    {
        _buffer = new byte[Math.Max(capacity, 16)];
    }

    /// <summary>Number of bytes written so far.</summary>
    public int Length => _length;

    /// <summary>The bytes written so far. Valid until the next write or <see cref="Reset"/>.</summary>
    public ReadOnlySpan<byte> Written => new(_buffer, 0, _length);

    /// <summary>Discards the written bytes and keeps the buffer, so a reused writer allocates nothing.</summary>
    public void Reset() => _length = 0;

    /// <summary>Copies the written bytes into a new array.</summary>
    public byte[] ToArray() => Written.ToArray();

    private void Ensure(int extra)
    {
        if (_buffer.Length - _length >= extra)
        {
            return;
        }

        var size = _buffer.Length;
        while (size - _length < extra)
        {
            size = checked(size * 2);
        }

        Array.Resize(ref _buffer, size);
    }

    /// <summary>Writes one raw byte.</summary>
    public void WriteByte(byte value)
    {
        Ensure(1);
        _buffer[_length++] = value;
    }

    /// <summary>Writes one raw signed byte.</summary>
    public void WriteSByte(sbyte value) => WriteByte((byte)value);

    /// <summary>Writes a bool as one byte, 0 or 1.</summary>
    public void WriteBool(bool value) => WriteByte(value ? (byte)1 : (byte)0);

    /// <summary>Writes an unsigned LEB128 varint.</summary>
    public void WriteVarUInt(ulong value)
    {
        Ensure(10);
        while (value >= 0x80)
        {
            _buffer[_length++] = (byte)(value | 0x80);
            value >>= 7;
        }

        _buffer[_length++] = (byte)value;
    }

    /// <summary>Writes a signed varint (zigzag, then LEB128).</summary>
    public void WriteVarInt(long value) => WriteVarUInt(((ulong)value << 1) ^ (ulong)(value >> 63));

    /// <summary>Writes a 16-bit signed integer.</summary>
    public void WriteInt16(short value) => WriteVarInt(value);

    /// <summary>Writes a 16-bit unsigned integer.</summary>
    public void WriteUInt16(ushort value) => WriteVarUInt(value);

    /// <summary>Writes a 32-bit signed integer.</summary>
    public void WriteInt32(int value) => WriteVarInt(value);

    /// <summary>Writes a 32-bit unsigned integer.</summary>
    public void WriteUInt32(uint value) => WriteVarUInt(value);

    /// <summary>Writes a 64-bit signed integer.</summary>
    public void WriteInt64(long value) => WriteVarInt(value);

    /// <summary>Writes a 64-bit unsigned integer.</summary>
    public void WriteUInt64(ulong value) => WriteVarUInt(value);

    /// <summary>Writes a 32-bit value as exactly four little-endian bytes.</summary>
    public void WriteFixed32(uint value)
    {
        Ensure(4);
        for (var i = 0; i < 4; i++)
        {
            _buffer[_length++] = (byte)(value >> (8 * i));
        }
    }

    /// <summary>Writes a 64-bit value as exactly eight little-endian bytes.</summary>
    public void WriteFixed64(ulong value)
    {
        Ensure(8);
        for (var i = 0; i < 8; i++)
        {
            _buffer[_length++] = (byte)(value >> (8 * i));
        }
    }

    /// <summary>Writes raw bytes with no length prefix.</summary>
    public void WriteRaw(ReadOnlySpan<byte> bytes)
    {
        Ensure(bytes.Length);
        bytes.CopyTo(_buffer.AsSpan(_length));
        _length += bytes.Length;
    }

    /// <summary>Writes a byte count followed by the bytes.</summary>
    public void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        WriteVarUInt((ulong)bytes.Length);
        WriteRaw(bytes);
    }

    /// <summary>Writes a UTF-8 string with a length prefix. A null string is written as empty.</summary>
    /// <exception cref="SyncLimitException">The string is longer than <see cref="SyncLimits.MaxStringBytes"/> in UTF-8.</exception>
    public void WriteString(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            WriteByte(0);
            return;
        }

        var byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount > SyncLimits.MaxStringBytes)
        {
            throw new SyncLimitException($"A synced string is {byteCount} UTF-8 bytes; the limit is {SyncLimits.MaxStringBytes}.");
        }

        WriteVarUInt((ulong)byteCount);
        Ensure(byteCount);
        _length += Encoding.UTF8.GetBytes(value, 0, value.Length, _buffer, _length);
    }

    /// <summary>Writes an element or row count and checks it against <paramref name="limit"/>.</summary>
    /// <exception cref="SyncLimitException"><paramref name="count"/> is above <paramref name="limit"/>.</exception>
    public void WriteCount(int count, int limit)
    {
        if (count > limit)
        {
            throw new SyncLimitException($"A synced collection has {count} elements; the limit is {limit}.");
        }

        WriteVarUInt((ulong)count);
    }
}

/// <summary>Thrown when state about to be written would be refused by the decoder's limits.</summary>
public sealed class SyncLimitException : InvalidOperationException
{
    /// <summary>Creates the exception.</summary>
    public SyncLimitException(string message)
        : base(message)
    {
    }
}
