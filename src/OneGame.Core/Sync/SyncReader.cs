using System.Text;
using System.Text.Unicode;

namespace OneGame.Core;

/// <summary>
/// Reads the generated binary format from untrusted bytes. Nothing here throws on bad input: the first problem is kept in
/// <see cref="Error"/>, every later read returns a default value, and the caller checks <see cref="Ok"/> once at the end.
/// Every length is checked against its limit and against the bytes that are left before anything is allocated.
/// </summary>
public ref struct SyncReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _position;
    private SyncError _error;

    /// <summary>Creates a reader over <paramref name="data"/>.</summary>
    public SyncReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _position = 0;
        _error = SyncError.None;
    }

    /// <summary>The first error met, or <see cref="SyncError.None"/>.</summary>
    public readonly SyncError Error => _error;

    /// <summary>True while no error has been met.</summary>
    public readonly bool Ok => _error == SyncError.None;

    /// <summary>Bytes consumed so far.</summary>
    public readonly int Position => _position;

    /// <summary>Bytes left to read.</summary>
    public readonly int Remaining => _data.Length - _position;

    /// <summary>True when every byte has been consumed.</summary>
    public readonly bool AtEnd => _position >= _data.Length;

    /// <summary>Records an error unless one is already recorded.</summary>
    public void Fail(SyncError error)
    {
        if (_error == SyncError.None)
        {
            _error = error;
        }
    }

    /// <summary>Fails with <see cref="SyncError.TrailingBytes"/> if any bytes are left.</summary>
    public void ExpectEnd()
    {
        if (Ok && !AtEnd)
        {
            Fail(SyncError.TrailingBytes);
        }
    }

    /// <summary>Reads one raw byte.</summary>
    public byte ReadByte()
    {
        if (!Ok)
        {
            return 0;
        }

        if (_position >= _data.Length)
        {
            Fail(SyncError.Truncated);
            return 0;
        }

        return _data[_position++];
    }

    /// <summary>Reads a bool; only 0 and 1 are accepted.</summary>
    public bool ReadBool()
    {
        var b = ReadByte();
        if (b > 1)
        {
            Fail(SyncError.InvalidValue);
            return false;
        }

        return b == 1;
    }

    /// <summary>Reads an unsigned LEB128 varint of at most ten bytes.</summary>
    public ulong ReadVarUInt()
    {
        ulong result = 0;
        for (var shift = 0; shift < 70; shift += 7)
        {
            if (!Ok)
            {
                return 0;
            }

            if (_position >= _data.Length)
            {
                Fail(SyncError.Truncated);
                return 0;
            }

            var b = _data[_position++];
            if (shift == 63 && b > 1)
            {
                Fail(SyncError.VarIntOverflow);
                return 0;
            }

            result |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                return result;
            }
        }

        Fail(SyncError.VarIntOverflow);
        return 0;
    }

    /// <summary>Reads a zigzag varint.</summary>
    public long ReadVarInt()
    {
        var u = ReadVarUInt();
        return (long)(u >> 1) ^ -(long)(u & 1);
    }

    private long ReadRanged(long min, long max)
    {
        var v = ReadVarInt();
        if (v < min || v > max)
        {
            Fail(SyncError.InvalidValue);
            return 0;
        }

        return v;
    }

    /// <summary>Reads a raw signed byte.</summary>
    public sbyte ReadSByte() => (sbyte)ReadByte();

    /// <summary>Reads a 16-bit signed integer.</summary>
    public short ReadInt16() => (short)ReadRanged(short.MinValue, short.MaxValue);

    /// <summary>Reads a 16-bit unsigned integer.</summary>
    public ushort ReadUInt16() => (ushort)ReadRangedU(ushort.MaxValue);

    /// <summary>Reads a 32-bit signed integer.</summary>
    public int ReadInt32() => (int)ReadRanged(int.MinValue, int.MaxValue);

    /// <summary>Reads a 32-bit unsigned integer.</summary>
    public uint ReadUInt32() => (uint)ReadRangedU(uint.MaxValue);

    /// <summary>Reads a 64-bit signed integer.</summary>
    public long ReadInt64() => ReadVarInt();

    /// <summary>Reads a 64-bit unsigned integer.</summary>
    public ulong ReadUInt64() => ReadVarUInt();

    private ulong ReadRangedU(ulong max)
    {
        var v = ReadVarUInt();
        if (v > max)
        {
            Fail(SyncError.InvalidValue);
            return 0;
        }

        return v;
    }

    /// <summary>Reads four little-endian bytes.</summary>
    public uint ReadFixed32()
    {
        if (!Ok)
        {
            return 0;
        }

        if (Remaining < 4)
        {
            Fail(SyncError.Truncated);
            return 0;
        }

        uint v = 0;
        for (var i = 0; i < 4; i++)
        {
            v |= (uint)_data[_position++] << (8 * i);
        }

        return v;
    }

    /// <summary>Reads eight little-endian bytes.</summary>
    public ulong ReadFixed64()
    {
        if (!Ok)
        {
            return 0;
        }

        if (Remaining < 8)
        {
            Fail(SyncError.Truncated);
            return 0;
        }

        ulong v = 0;
        for (var i = 0; i < 8; i++)
        {
            v |= (ulong)_data[_position++] << (8 * i);
        }

        return v;
    }

    /// <summary>Reads a count and checks it against <paramref name="limit"/> and the bytes left (every element takes at least one byte).</summary>
    /// <returns>The count, or 0 after a failure.</returns>
    public int ReadCount(int limit)
    {
        var n = ReadVarUInt();
        if (!Ok)
        {
            return 0;
        }

        if (n > (ulong)limit || n > (ulong)Remaining)
        {
            Fail(SyncError.LimitExceeded);
            return 0;
        }

        return (int)n;
    }

    /// <summary>Reads a UTF-8 string written by <see cref="SyncWriter.WriteString"/>.</summary>
    public string ReadString()
    {
        var n = ReadCount(SyncLimits.MaxStringBytes);
        if (!Ok || n == 0)
        {
            return string.Empty;
        }

        var bytes = _data.Slice(_position, n);
        if (!Utf8.IsValid(bytes))
        {
            Fail(SyncError.InvalidValue);
            return string.Empty;
        }

        _position += n;
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>Reads a length-prefixed byte block and returns a view of it without copying.</summary>
    public ReadOnlySpan<byte> ReadBytes(int limit)
    {
        var n = ReadCount(limit);
        if (!Ok)
        {
            return default;
        }

        var bytes = _data.Slice(_position, n);
        _position += n;
        return bytes;
    }

    /// <summary>Starts a sub-reader over the next <paramref name="length"/> bytes and skips them here.</summary>
    public SyncReader Slice(int length)
    {
        if (!Ok || length < 0 || length > Remaining)
        {
            Fail(length < 0 ? SyncError.InvalidValue : SyncError.Truncated);
            return new SyncReader(default);
        }

        var sub = new SyncReader(_data.Slice(_position, length));
        _position += length;
        return sub;
    }
}
