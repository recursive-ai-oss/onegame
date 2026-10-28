using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace OneGame.Core;

/// <summary>
/// The ChaCha20-Poly1305 AEAD of RFC 8439 §2.8, written out here because the platform's implementation needs a system crypto
/// library that phones lack. Nothing here allocates: working state lives on the stack. The cipher is add-rotate-xor only and
/// Poly1305 is plain multiplication, so no branch or memory index depends on key or data; the only data-dependent decision is the
/// final accept/reject of a tag, which is made on a constant-time comparison.
/// </summary>
internal static class ChaCha20Poly1305
{
    /// <summary>Key size in bytes.</summary>
    public const int KeySize = 32;

    /// <summary>Nonce size in bytes.</summary>
    public const int NonceSize = 12;

    /// <summary>Authentication tag size in bytes.</summary>
    public const int TagSize = 16;

    /// <summary>
    /// Encrypts <paramref name="plaintext"/> and writes the ciphertext followed by the 16-byte tag to <paramref name="output"/>,
    /// which must be exactly <c>plaintext.Length + 16</c> bytes. The ciphertext part may be the same memory as the plaintext
    /// (in place); any other overlap is not supported. A (key, nonce) pair must never be used for two messages.
    /// </summary>
    public static void Seal(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> aad, ReadOnlySpan<byte> plaintext, Span<byte> output)
    {
        Check(key, nonce);
        if (output.Length != plaintext.Length + TagSize)
        {
            throw new ArgumentException("output must be plaintext length plus 16 bytes", nameof(output));
        }

        Span<byte> polyKey = stackalloc byte[32];
        Span<uint> state = stackalloc uint[16];
        Init(state, key, nonce);
        Block0(state, polyKey);
        var ciphertext = output[..plaintext.Length];
        Xor(state, 1, plaintext, ciphertext);
        Span<byte> tag = stackalloc byte[TagSize];
        Mac(polyKey, aad, ciphertext, tag);
        tag.CopyTo(output[plaintext.Length..]);
        Wipe(polyKey);
        Wipe(MemoryMarshalAsBytes(state));
    }

    /// <summary>
    /// Checks the tag at the end of <paramref name="input"/> (ciphertext then 16-byte tag) and, only if it is right, writes the
    /// plaintext to <paramref name="output"/> (exactly <c>input.Length - 16</c> bytes; in place is allowed). Returns false, and
    /// leaves <paramref name="output"/> zeroed, for a forged or damaged message.
    /// </summary>
    public static bool Open(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> aad, ReadOnlySpan<byte> input, Span<byte> output)
    {
        Check(key, nonce);
        if (input.Length < TagSize || output.Length != input.Length - TagSize)
        {
            return false;
        }

        var ciphertext = input[..output.Length];
        Span<byte> polyKey = stackalloc byte[32];
        Span<uint> state = stackalloc uint[16];
        Init(state, key, nonce);
        Block0(state, polyKey);
        Span<byte> expected = stackalloc byte[TagSize];
        Mac(polyKey, aad, ciphertext, expected);
        var ok = HmacSha256.FixedTimeEquals(expected, input[output.Length..]);
        if (ok)
        {
            Xor(state, 1, ciphertext, output);
        }
        else
        {
            output.Clear();
        }

        Wipe(polyKey);
        Wipe(MemoryMarshalAsBytes(state));
        return ok;
    }

    private static void Check(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce)
    {
        if (key.Length != KeySize)
        {
            throw new ArgumentException("key must be 32 bytes", nameof(key));
        }

        if (nonce.Length != NonceSize)
        {
            throw new ArgumentException("nonce must be 12 bytes", nameof(nonce));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Wipe(Span<byte> secret) => secret.Clear();

    private static Span<byte> MemoryMarshalAsBytes(Span<uint> words) => System.Runtime.InteropServices.MemoryMarshal.AsBytes(words);

    // RFC 8439 §2.3: "expand 32-byte k", the key, a block counter and the nonce.
    private static void Init(Span<uint> s, ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce)
    {
        s[0] = 0x61707865; s[1] = 0x3320646e; s[2] = 0x79622d32; s[3] = 0x6b206574;
        for (var i = 0; i < 8; i++)
        {
            s[4 + i] = BinaryPrimitives.ReadUInt32LittleEndian(key[(4 * i)..]);
        }

        s[12] = 0;
        s[13] = BinaryPrimitives.ReadUInt32LittleEndian(nonce);
        s[14] = BinaryPrimitives.ReadUInt32LittleEndian(nonce[4..]);
        s[15] = BinaryPrimitives.ReadUInt32LittleEndian(nonce[8..]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Quarter(ref uint a, ref uint b, ref uint c, ref uint d)
    {
        a += b; d = BitOperations.RotateLeft(d ^ a, 16);
        c += d; b = BitOperations.RotateLeft(b ^ c, 12);
        a += b; d = BitOperations.RotateLeft(d ^ a, 8);
        c += d; b = BitOperations.RotateLeft(b ^ c, 7);
    }

    // RFC 8439 §2.3: 20 rounds (10 column + diagonal pairs), then add the input state. The words live in locals so the rounds
    // run in registers.
    private static void BlockFunction(ReadOnlySpan<uint> state, uint counter, Span<byte> output)
    {
        uint x0 = state[0], x1 = state[1], x2 = state[2], x3 = state[3];
        uint x4 = state[4], x5 = state[5], x6 = state[6], x7 = state[7];
        uint x8 = state[8], x9 = state[9], x10 = state[10], x11 = state[11];
        uint x12 = counter, x13 = state[13], x14 = state[14], x15 = state[15];
        for (var i = 0; i < 10; i++)
        {
            Quarter(ref x0, ref x4, ref x8, ref x12);
            Quarter(ref x1, ref x5, ref x9, ref x13);
            Quarter(ref x2, ref x6, ref x10, ref x14);
            Quarter(ref x3, ref x7, ref x11, ref x15);
            Quarter(ref x0, ref x5, ref x10, ref x15);
            Quarter(ref x1, ref x6, ref x11, ref x12);
            Quarter(ref x2, ref x7, ref x8, ref x13);
            Quarter(ref x3, ref x4, ref x9, ref x14);
        }

        BinaryPrimitives.WriteUInt32LittleEndian(output, x0 + state[0]);
        BinaryPrimitives.WriteUInt32LittleEndian(output[4..], x1 + state[1]);
        BinaryPrimitives.WriteUInt32LittleEndian(output[8..], x2 + state[2]);
        BinaryPrimitives.WriteUInt32LittleEndian(output[12..], x3 + state[3]);
        BinaryPrimitives.WriteUInt32LittleEndian(output[16..], x4 + state[4]);
        BinaryPrimitives.WriteUInt32LittleEndian(output[20..], x5 + state[5]);
        BinaryPrimitives.WriteUInt32LittleEndian(output[24..], x6 + state[6]);
        BinaryPrimitives.WriteUInt32LittleEndian(output[28..], x7 + state[7]);
        BinaryPrimitives.WriteUInt32LittleEndian(output[32..], x8 + state[8]);
        BinaryPrimitives.WriteUInt32LittleEndian(output[36..], x9 + state[9]);
        BinaryPrimitives.WriteUInt32LittleEndian(output[40..], x10 + state[10]);
        BinaryPrimitives.WriteUInt32LittleEndian(output[44..], x11 + state[11]);
        BinaryPrimitives.WriteUInt32LittleEndian(output[48..], x12 + counter);
        BinaryPrimitives.WriteUInt32LittleEndian(output[52..], x13 + state[13]);
        BinaryPrimitives.WriteUInt32LittleEndian(output[56..], x14 + state[14]);
        BinaryPrimitives.WriteUInt32LittleEndian(output[60..], x15 + state[15]);
    }

    // RFC 8439 §2.6: the one-time Poly1305 key is the first 32 bytes of the block at counter 0.
    private static void Block0(ReadOnlySpan<uint> state, Span<byte> polyKey)
    {
        Span<byte> block = stackalloc byte[64];
        BlockFunction(state, 0, block);
        block[..32].CopyTo(polyKey);
        Wipe(block);
    }

    private static void Xor(ReadOnlySpan<uint> state, uint counter, ReadOnlySpan<byte> input, Span<byte> output)
    {
        Span<byte> block = stackalloc byte[64];
        var offset = 0;
        while (offset < input.Length)
        {
            BlockFunction(state, counter++, block);
            var n = Math.Min(64, input.Length - offset);
            if (n == 64)
            {
                for (var i = 0; i < 64; i += 8)
                {
                    BinaryPrimitives.WriteUInt64LittleEndian(output[(offset + i)..], BinaryPrimitives.ReadUInt64LittleEndian(input[(offset + i)..]) ^ BinaryPrimitives.ReadUInt64LittleEndian(block[i..]));
                }
            }
            else
            {
                for (var i = 0; i < n; i++)
                {
                    output[offset + i] = (byte)(input[offset + i] ^ block[i]);
                }
            }

            offset += n;
        }

        Wipe(block);
    }

    // RFC 8439 §2.8: MAC over aad, padding to 16, ciphertext, padding to 16, then both lengths as 64-bit little endian.
    private static void Mac(ReadOnlySpan<byte> polyKey, ReadOnlySpan<byte> aad, ReadOnlySpan<byte> ciphertext, Span<byte> tag)
    {
        var poly = new Poly1305(polyKey);
        poly.Update(aad);
        poly.Pad();
        poly.Update(ciphertext);
        poly.Pad();
        Span<byte> lengths = stackalloc byte[16];
        BinaryPrimitives.WriteUInt64LittleEndian(lengths, (ulong)aad.Length);
        BinaryPrimitives.WriteUInt64LittleEndian(lengths[8..], (ulong)ciphertext.Length);
        poly.Update(lengths);
        poly.Finish(tag);
    }

    /// <summary>ChaCha20 keystream XOR on its own (RFC 8439 §2.4), for the RFC's cipher vectors.</summary>
    internal static void ChaCha20Xor(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, uint counter, ReadOnlySpan<byte> input, Span<byte> output)
    {
        Check(key, nonce);
        Span<uint> state = stackalloc uint[16];
        Init(state, key, nonce);
        Xor(state, counter, input, output);
        Wipe(MemoryMarshalAsBytes(state));
    }

    /// <summary>One-time authenticator of RFC 8439 §2.5, in 26-bit limbs so every partial product fits 64 bits.</summary>
    internal struct Poly1305
    {
        private uint _r0, _r1, _r2, _r3, _r4;
        private uint _s1, _s2, _s3, _s4; // 5 * r1..r4, for the reduction mod 2^130 - 5
        private uint _h0, _h1, _h2, _h3, _h4;
        private uint _pad0, _pad1, _pad2, _pad3;
        private int _buffered;
        private Block16 _buffer;

        [InlineArray(16)]
        private struct Block16
        {
            private byte _e;
        }

        public Poly1305(ReadOnlySpan<byte> key)
        {
            // RFC 8439 §2.5: r is the first 16 bytes with the bits clamped; s the last 16.
            _r0 = BinaryPrimitives.ReadUInt32LittleEndian(key) & 0x3ffffff;
            _r1 = (BinaryPrimitives.ReadUInt32LittleEndian(key[3..]) >> 2) & 0x3ffff03;
            _r2 = (BinaryPrimitives.ReadUInt32LittleEndian(key[6..]) >> 4) & 0x3ffc0ff;
            _r3 = (BinaryPrimitives.ReadUInt32LittleEndian(key[9..]) >> 6) & 0x3f03fff;
            _r4 = (BinaryPrimitives.ReadUInt32LittleEndian(key[12..]) >> 8) & 0x00fffff;
            _s1 = _r1 * 5; _s2 = _r2 * 5; _s3 = _r3 * 5; _s4 = _r4 * 5;
            _pad0 = BinaryPrimitives.ReadUInt32LittleEndian(key[16..]);
            _pad1 = BinaryPrimitives.ReadUInt32LittleEndian(key[20..]);
            _pad2 = BinaryPrimitives.ReadUInt32LittleEndian(key[24..]);
            _pad3 = BinaryPrimitives.ReadUInt32LittleEndian(key[28..]);
            _h0 = _h1 = _h2 = _h3 = _h4 = 0;
            _buffered = 0;
            _buffer = default;
        }

        public void Update(ReadOnlySpan<byte> data)
        {
            if (_buffered > 0)
            {
                var take = Math.Min(16 - _buffered, data.Length);
                data[..take].CopyTo(((Span<byte>)_buffer)[_buffered..]);
                _buffered += take;
                data = data[take..];
                if (_buffered < 16)
                {
                    return;
                }

                Blocks(_buffer, 1 << 24);
                _buffered = 0;
            }

            while (data.Length >= 16)
            {
                Blocks(data[..16], 1 << 24);
                data = data[16..];
            }

            if (data.Length > 0)
            {
                data.CopyTo((Span<byte>)_buffer);
                _buffered = data.Length;
            }
        }

        /// <summary>Zero-pads the data so far to a multiple of 16 bytes (RFC 8439 §2.8).</summary>
        public void Pad()
        {
            if (_buffered > 0)
            {
                ((Span<byte>)_buffer)[_buffered..].Clear();
                Blocks(_buffer, 1 << 24);
                _buffered = 0;
            }
        }

        public void Finish(Span<byte> tag)
        {
            if (_buffered > 0)
            {
                // A final partial block carries its own 1 bit instead of the 2^128 bit.
                Span<byte> last = stackalloc byte[16];
                last.Clear();
                ((ReadOnlySpan<byte>)_buffer)[.._buffered].CopyTo(last);
                last[_buffered] = 1;
                Blocks(last, 0);
                _buffered = 0;
            }

            // Full carry, then h - p computed branch-free and selected by the sign of the borrow.
            var h0 = _h0; var h1 = _h1; var h2 = _h2; var h3 = _h3; var h4 = _h4;
            var c = h1 >> 26; h1 &= 0x3ffffff; h2 += c;
            c = h2 >> 26; h2 &= 0x3ffffff; h3 += c;
            c = h3 >> 26; h3 &= 0x3ffffff; h4 += c;
            c = h4 >> 26; h4 &= 0x3ffffff; h0 += c * 5;
            c = h0 >> 26; h0 &= 0x3ffffff; h1 += c;

            var g0 = h0 + 5; c = g0 >> 26; g0 &= 0x3ffffff;
            var g1 = h1 + c; c = g1 >> 26; g1 &= 0x3ffffff;
            var g2 = h2 + c; c = g2 >> 26; g2 &= 0x3ffffff;
            var g3 = h3 + c; c = g3 >> 26; g3 &= 0x3ffffff;
            var g4 = h4 + c - (1u << 26);

            var mask = (g4 >> 31) - 1; // all ones when h >= p (no borrow), so take g
            g0 &= mask; g1 &= mask; g2 &= mask; g3 &= mask; g4 &= mask;
            mask = ~mask;
            h0 = (h0 & mask) | g0; h1 = (h1 & mask) | g1; h2 = (h2 & mask) | g2; h3 = (h3 & mask) | g3; h4 = (h4 & mask) | g4;

            // Pack to 128 bits and add s modulo 2^128.
            var t0 = h0 | (h1 << 26);
            var t1 = (h1 >> 6) | (h2 << 20);
            var t2 = (h2 >> 12) | (h3 << 14);
            var t3 = (h3 >> 18) | (h4 << 8);
            var f = (ulong)t0 + _pad0;
            BinaryPrimitives.WriteUInt32LittleEndian(tag, (uint)f);
            f = (ulong)t1 + _pad1 + (f >> 32);
            BinaryPrimitives.WriteUInt32LittleEndian(tag[4..], (uint)f);
            f = (ulong)t2 + _pad2 + (f >> 32);
            BinaryPrimitives.WriteUInt32LittleEndian(tag[8..], (uint)f);
            f = (ulong)t3 + _pad3 + (f >> 32);
            BinaryPrimitives.WriteUInt32LittleEndian(tag[12..], (uint)f);
            this = default;
        }

        // h = (h + block + hibit) * r mod 2^130 - 5.
        private void Blocks(ReadOnlySpan<byte> m, uint hibit)
        {
            var h0 = _h0 + (BinaryPrimitives.ReadUInt32LittleEndian(m) & 0x3ffffff);
            var h1 = _h1 + ((BinaryPrimitives.ReadUInt32LittleEndian(m[3..]) >> 2) & 0x3ffffff);
            var h2 = _h2 + ((BinaryPrimitives.ReadUInt32LittleEndian(m[6..]) >> 4) & 0x3ffffff);
            var h3 = _h3 + ((BinaryPrimitives.ReadUInt32LittleEndian(m[9..]) >> 6) & 0x3ffffff);
            var h4 = _h4 + ((BinaryPrimitives.ReadUInt32LittleEndian(m[12..]) >> 8) | hibit);

            var d0 = ((ulong)h0 * _r0) + ((ulong)h1 * _s4) + ((ulong)h2 * _s3) + ((ulong)h3 * _s2) + ((ulong)h4 * _s1);
            var d1 = ((ulong)h0 * _r1) + ((ulong)h1 * _r0) + ((ulong)h2 * _s4) + ((ulong)h3 * _s3) + ((ulong)h4 * _s2);
            var d2 = ((ulong)h0 * _r2) + ((ulong)h1 * _r1) + ((ulong)h2 * _r0) + ((ulong)h3 * _s4) + ((ulong)h4 * _s3);
            var d3 = ((ulong)h0 * _r3) + ((ulong)h1 * _r2) + ((ulong)h2 * _r1) + ((ulong)h3 * _r0) + ((ulong)h4 * _s4);
            var d4 = ((ulong)h0 * _r4) + ((ulong)h1 * _r3) + ((ulong)h2 * _r2) + ((ulong)h3 * _r1) + ((ulong)h4 * _r0);

            var c = (uint)(d0 >> 26); h0 = (uint)d0 & 0x3ffffff;
            d1 += c; c = (uint)(d1 >> 26); h1 = (uint)d1 & 0x3ffffff;
            d2 += c; c = (uint)(d2 >> 26); h2 = (uint)d2 & 0x3ffffff;
            d3 += c; c = (uint)(d3 >> 26); h3 = (uint)d3 & 0x3ffffff;
            d4 += c; c = (uint)(d4 >> 26); h4 = (uint)d4 & 0x3ffffff;
            h0 += c * 5; c = h0 >> 26; h0 &= 0x3ffffff;
            h1 += c;

            _h0 = h0; _h1 = h1; _h2 = h2; _h3 = h3; _h4 = h4;
        }
    }
}
