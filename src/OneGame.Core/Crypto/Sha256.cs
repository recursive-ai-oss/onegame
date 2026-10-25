using System.Buffers.Binary;
using System.Numerics;

namespace OneGame.Core;

/// <summary>
/// SHA-256 as specified in FIPS 180-4 §6.2. It is written out here because the platform hash is not available everywhere the
/// engine runs: on Android the runtime's hash provider needs a system crypto library the app does not have, and the process
/// aborts on first use. Tests compare it with the platform hash on hosts that have one.
/// </summary>
internal struct Sha256
{
    /// <summary>Digest size in bytes.</summary>
    public const int DigestSize = 32;

    /// <summary>Block size in bytes.</summary>
    public const int BlockSize = 64;

    private static readonly uint[] K =
    [
        0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
        0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
        0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
        0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
        0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
        0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
        0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
        0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2,
    ];

    private uint _h0, _h1, _h2, _h3, _h4, _h5, _h6, _h7;
    private ulong _length;
    private int _buffered;
    private InlineBlock _buffer;

    [System.Runtime.CompilerServices.InlineArray(BlockSize)]
    private struct InlineBlock
    {
        private byte _e;
    }

    /// <summary>A hash state ready for <see cref="Update"/>.</summary>
    public static Sha256 Create()
    {
        var s = default(Sha256);
        s._h0 = 0x6a09e667; s._h1 = 0xbb67ae85; s._h2 = 0x3c6ef372; s._h3 = 0xa54ff53a;
        s._h4 = 0x510e527f; s._h5 = 0x9b05688c; s._h6 = 0x1f83d9ab; s._h7 = 0x5be0cd19;
        return s;
    }

    /// <summary>One-shot hash of <paramref name="data"/> into the 32-byte <paramref name="digest"/>.</summary>
    public static void Hash(ReadOnlySpan<byte> data, Span<byte> digest)
    {
        var s = Create();
        s.Update(data);
        s.Final(digest);
    }

    /// <summary>The lower-case hex digest of <paramref name="data"/>.</summary>
    public static string HashHex(ReadOnlySpan<byte> data)
    {
        Span<byte> d = stackalloc byte[DigestSize];
        Hash(data, d);
        return Convert.ToHexStringLower(d);
    }

    /// <summary>Feeds more bytes.</summary>
    public void Update(ReadOnlySpan<byte> data)
    {
        _length += (ulong)data.Length;
        Span<byte> buf = _buffer;
        if (_buffered > 0)
        {
            var take = Math.Min(BlockSize - _buffered, data.Length);
            data[..take].CopyTo(buf[_buffered..]);
            _buffered += take;
            data = data[take..];
            if (_buffered < BlockSize)
            {
                return;
            }

            Block(buf);
            _buffered = 0;
        }

        while (data.Length >= BlockSize)
        {
            Block(data[..BlockSize]);
            data = data[BlockSize..];
        }

        data.CopyTo(buf);
        _buffered = data.Length;
    }

    /// <summary>Finishes the hash (padding per §5.1.1) and writes 32 bytes.</summary>
    public void Final(Span<byte> digest)
    {
        var bits = _length * 8;
        Span<byte> pad = stackalloc byte[BlockSize * 2];
        pad.Clear();
        pad[0] = 0x80;
        var padLength = _buffered < 56 ? 56 - _buffered : 120 - _buffered;
        BinaryPrimitives.WriteUInt64BigEndian(pad[padLength..], bits);
        Update(pad[..(padLength + 8)]);
        BinaryPrimitives.WriteUInt32BigEndian(digest, _h0);
        BinaryPrimitives.WriteUInt32BigEndian(digest[4..], _h1);
        BinaryPrimitives.WriteUInt32BigEndian(digest[8..], _h2);
        BinaryPrimitives.WriteUInt32BigEndian(digest[12..], _h3);
        BinaryPrimitives.WriteUInt32BigEndian(digest[16..], _h4);
        BinaryPrimitives.WriteUInt32BigEndian(digest[20..], _h5);
        BinaryPrimitives.WriteUInt32BigEndian(digest[24..], _h6);
        BinaryPrimitives.WriteUInt32BigEndian(digest[28..], _h7);
    }

    private void Block(ReadOnlySpan<byte> block)
    {
        Span<uint> w = stackalloc uint[64];
        for (var t = 0; t < 16; t++)
        {
            w[t] = BinaryPrimitives.ReadUInt32BigEndian(block[(t * 4)..]);
        }

        for (var t = 16; t < 64; t++)
        {
            var s0 = BitOperations.RotateRight(w[t - 15], 7) ^ BitOperations.RotateRight(w[t - 15], 18) ^ (w[t - 15] >> 3);
            var s1 = BitOperations.RotateRight(w[t - 2], 17) ^ BitOperations.RotateRight(w[t - 2], 19) ^ (w[t - 2] >> 10);
            w[t] = w[t - 16] + s0 + w[t - 7] + s1;
        }

        uint a = _h0, b = _h1, c = _h2, d = _h3, e = _h4, f = _h5, g = _h6, h = _h7;
        for (var t = 0; t < 64; t++)
        {
            var big1 = BitOperations.RotateRight(e, 6) ^ BitOperations.RotateRight(e, 11) ^ BitOperations.RotateRight(e, 25);
            var ch = (e & f) ^ (~e & g);
            var t1 = h + big1 + ch + K[t] + w[t];
            var big0 = BitOperations.RotateRight(a, 2) ^ BitOperations.RotateRight(a, 13) ^ BitOperations.RotateRight(a, 22);
            var maj = (a & b) ^ (a & c) ^ (b & c);
            var t2 = big0 + maj;
            h = g; g = f; f = e; e = d + t1; d = c; c = b; b = a; a = t1 + t2;
        }

        _h0 += a; _h1 += b; _h2 += c; _h3 += d; _h4 += e; _h5 += f; _h6 += g; _h7 += h;
    }
}

/// <summary>HMAC-SHA-256 as specified in RFC 2104 over <see cref="Sha256"/>.</summary>
internal static class HmacSha256
{
    /// <summary>Compares two MACs without stopping at the first difference, so timing does not reveal how much of a guess was right.</summary>
    public static bool FixedTimeEquals(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        var diff = 0;
        for (var i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }

        return diff == 0;
    }

    /// <summary>Writes the 32-byte MAC of <paramref name="data"/> under <paramref name="key"/>.</summary>
    public static void Hash(ReadOnlySpan<byte> key, ReadOnlySpan<byte> data, Span<byte> mac)
    {
        // RFC 2104 §2: keys longer than the block are hashed first, shorter ones zero-padded.
        Span<byte> k = stackalloc byte[Sha256.BlockSize];
        k.Clear();
        if (key.Length > Sha256.BlockSize)
        {
            Sha256.Hash(key, k);
        }
        else
        {
            key.CopyTo(k);
        }

        Span<byte> pad = stackalloc byte[Sha256.BlockSize];
        for (var i = 0; i < pad.Length; i++)
        {
            pad[i] = (byte)(k[i] ^ 0x36);
        }

        var inner = Sha256.Create();
        inner.Update(pad);
        inner.Update(data);
        Span<byte> innerDigest = stackalloc byte[Sha256.DigestSize];
        inner.Final(innerDigest);

        for (var i = 0; i < pad.Length; i++)
        {
            pad[i] = (byte)(k[i] ^ 0x5c);
        }

        var outer = Sha256.Create();
        outer.Update(pad);
        outer.Update(innerDigest);
        outer.Final(mac);
    }
}
