using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace OneGame.Core;

/// <summary>
/// X25519 Diffie-Hellman as specified in RFC 7748 §5: the Montgomery ladder on Curve25519 over the field of 2^255 - 19.
/// <para>
/// Constant time: field elements are ten signed limbs of alternating 26 and 25 bits (radix 2^25.5) held in 64-bit integers;
/// every operation is fixed-length add, subtract, multiply and shift with no table lookups. The ladder swaps its two points with
/// a mask derived from each scalar bit (<c>cswap</c>) instead of branching, and the final inversion is a fixed addition chain.
/// Loops branch only on the public limb index.
/// </para>
/// </summary>
internal static class X25519
{
    /// <summary>Size of private keys, public keys and shared secrets in bytes.</summary>
    public const int KeySize = 32;

    private const int Limbs = 10;

    /// <summary>The public key of the standard base point (u = 9) for <paramref name="privateKey"/>.</summary>
    public static void PublicKey(ReadOnlySpan<byte> privateKey, Span<byte> publicKey)
    {
        Span<byte> nine = stackalloc byte[KeySize];
        nine.Clear();
        nine[0] = 9;
        Ladder(privateKey, nine, publicKey);
    }

    /// <summary>
    /// The shared secret of <paramref name="privateKey"/> and the peer's <paramref name="publicKey"/>. Returns false when the
    /// result is all zeros, which only a low-order peer key produces (RFC 7748 §6.1 allows rejecting it); the bytes are still
    /// written so a caller can compare with other implementations.
    /// </summary>
    public static bool Agree(ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> publicKey, Span<byte> shared)
    {
        Ladder(privateKey, publicKey, shared);
        var acc = 0;
        for (var i = 0; i < KeySize; i++)
        {
            acc |= shared[i];
        }

        // Branch-free "acc != 0".
        return ((acc | -acc) >> 31 & 1) == 1;
    }

    private static void Ladder(ReadOnlySpan<byte> scalarBytes, ReadOnlySpan<byte> uBytes, Span<byte> output)
    {
        if (scalarBytes.Length != KeySize || uBytes.Length != KeySize || output.Length != KeySize)
        {
            throw new ArgumentException("X25519 keys are 32 bytes");
        }

        // RFC 7748 §5: clamp the scalar (clear the low 3 bits, set bit 254, clear bit 255).
        Span<byte> k = stackalloc byte[KeySize];
        scalarBytes.CopyTo(k);
        k[0] &= 248;
        k[31] &= 127;
        k[31] |= 64;

        Span<long> x1 = stackalloc long[Limbs];
        Span<long> x2 = stackalloc long[Limbs];
        Span<long> z2 = stackalloc long[Limbs];
        Span<long> x3 = stackalloc long[Limbs];
        Span<long> z3 = stackalloc long[Limbs];
        Span<long> a = stackalloc long[Limbs];
        Span<long> aa = stackalloc long[Limbs];
        Span<long> b = stackalloc long[Limbs];
        Span<long> bb = stackalloc long[Limbs];
        Span<long> e = stackalloc long[Limbs];
        Span<long> c = stackalloc long[Limbs];
        Span<long> d = stackalloc long[Limbs];
        Span<long> t = stackalloc long[Limbs];

        Load(uBytes, x1); // the top bit of u is ignored (RFC 7748 §5)
        Set(x2, 1);
        z2.Clear();
        x1.CopyTo(x3);
        Set(z3, 1);

        long swap = 0;
        for (var pos = 254; pos >= 0; pos--)
        {
            var bit = (long)((k[pos >> 3] >> (pos & 7)) & 1);
            swap ^= bit;
            CSwap(swap, x2, x3);
            CSwap(swap, z2, z3);
            swap = bit;

            // RFC 7748 §5 ladder step.
            Add(a, x2, z2);
            Square(aa, a);
            Sub(b, x2, z2);
            Square(bb, b);
            Sub(e, aa, bb);
            Add(c, x3, z3);
            Sub(d, x3, z3);
            Mul(d, d, a);   // DA
            Mul(c, c, b);   // CB
            Add(t, d, c);
            Square(x3, t);
            Sub(t, d, c);
            Square(t, t);
            Mul(z3, x1, t);
            Mul(x2, aa, bb);
            MulSmall(t, e, 121665);
            Add(t, aa, t);
            Mul(z2, e, t);
        }

        CSwap(swap, x2, x3);
        CSwap(swap, z2, z3);

        Invert(z2, z2, a, b, c);
        Mul(x2, x2, z2);
        Store(x2, output);

        k.Clear();
        ClearAll(x1, x2, z2, x3, z3, a, aa, b, bb, e, c, d, t);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ClearAll(Span<long> a, Span<long> b, Span<long> c, Span<long> d, Span<long> e, Span<long> f, Span<long> g, Span<long> h, Span<long> i, Span<long> j, Span<long> k, Span<long> l, Span<long> m)
    {
        a.Clear(); b.Clear(); c.Clear(); d.Clear(); e.Clear(); f.Clear(); g.Clear(); h.Clear(); i.Clear(); j.Clear(); k.Clear(); l.Clear(); m.Clear();
    }

    private static void Set(Span<long> f, long v)
    {
        f.Clear();
        f[0] = v;
    }

    private static int Width(int i) => 26 - (i & 1); // limb i holds 26 bits when even, 25 when odd

    // Start bit of each limb: ceil(25.5 * i).
    private static ReadOnlySpan<byte> Offsets => [0, 26, 51, 77, 102, 128, 153, 179, 204, 230];

    private static void Load(ReadOnlySpan<byte> bytes, Span<long> f)
    {
        Span<byte> padded = stackalloc byte[40];
        padded.Clear();
        bytes.CopyTo(padded);
        padded[31] &= 127;
        for (var i = 0; i < Limbs; i++)
        {
            int off = Offsets[i];
            var window = BinaryPrimitives.ReadUInt64LittleEndian(padded[(off >> 3)..]);
            f[i] = (long)((window >> (off & 7)) & ((1UL << Width(i)) - 1));
        }
    }

    // Reduce to the canonical value in [0, 2^255 - 19) and write 32 little-endian bytes.
    private static void Store(ReadOnlySpan<long> g, Span<byte> output)
    {
        Span<long> h = stackalloc long[Limbs];
        g.CopyTo(h);
        // Floor-carry twice so every limb is in [0, 2^w) and the value is below 2^255 + 38.
        for (var pass = 0; pass < 2; pass++)
        {
            for (var i = 0; i < Limbs; i++)
            {
                var w = Width(i);
                var carry = h[i] >> w;
                h[i] -= carry << w;
                if (i + 1 < Limbs)
                {
                    h[i + 1] += carry;
                }
                else
                {
                    h[0] += carry * 19;
                }
            }
        }

        // q = 1 exactly when h >= p: the carry out of the top limb when 19 is added.
        var q = (h[0] + 19) >> 26;
        for (var i = 1; i < Limbs; i++)
        {
            q = (h[i] + q) >> Width(i);
        }

        h[0] += 19 * q;
        for (var i = 0; i < Limbs; i++)
        {
            var w = Width(i);
            var carry = h[i] >> w;
            h[i] -= carry << w; // the carry out of limb 9 is the 2^255 bit, dropped
            if (i + 1 < Limbs)
            {
                h[i + 1] += carry;
            }
        }

        output.Clear();
        ulong acc = 0;
        var bits = 0;
        var o = 0;
        for (var i = 0; i < Limbs; i++)
        {
            acc |= (ulong)h[i] << bits;
            bits += Width(i);
            while (bits >= 8)
            {
                output[o++] = (byte)acc;
                acc >>= 8;
                bits -= 8;
            }
        }

        if (o < KeySize)
        {
            output[o] = (byte)acc;
        }

        h.Clear();
    }

    // Constant-time conditional swap: mask is all ones when swap == 1.
    private static void CSwap(long swap, Span<long> a, Span<long> b)
    {
        var mask = -swap;
        for (var i = 0; i < Limbs; i++)
        {
            var x = mask & (a[i] ^ b[i]);
            a[i] ^= x;
            b[i] ^= x;
        }
    }

    // Additions and subtractions are carried at once so every limb stays within the bounds Mul relies on
    // (|limb| < 2^26, 2^25 for odd limbs, plus slack).
    private static void Add(Span<long> h, ReadOnlySpan<long> f, ReadOnlySpan<long> g)
    {
        for (var i = 0; i < Limbs; i++)
        {
            h[i] = f[i] + g[i];
        }

        Carry(h);
    }

    private static void Sub(Span<long> h, ReadOnlySpan<long> f, ReadOnlySpan<long> g)
    {
        for (var i = 0; i < Limbs; i++)
        {
            h[i] = f[i] - g[i];
        }

        Carry(h);
    }

    // Signed carry with rounding: each limb ends in [-2^(w-1), 2^(w-1)], the top carry wraps times 19.
    private static void Carry(Span<long> h)
    {
        for (var i = 0; i < Limbs; i++)
        {
            var w = Width(i);
            var carry = (h[i] + (1L << (w - 1))) >> w;
            h[i] -= carry << w;
            if (i + 1 < Limbs)
            {
                h[i + 1] += carry;
            }
            else
            {
                h[0] += carry * 19;
            }
        }

        var c0 = (h[0] + (1L << 25)) >> 26;
        h[0] -= c0 << 26;
        h[1] += c0;
    }

    private static void MulSmall(Span<long> h, ReadOnlySpan<long> f, long small)
    {
        for (var i = 0; i < Limbs; i++)
        {
            h[i] = f[i] * small;
        }

        Carry(h);
    }

    private static void Square(Span<long> h, ReadOnlySpan<long> f) => Mul(h, f, f);

    // h = f * g. Radix 2^25.5: a product of two odd limbs carries an extra factor 2; wrapping past limb 9 multiplies by 19
    // because 2^255 = 19 mod p. Sums stay below 2^63 for limbs within the Carry bounds.
    private static void Mul(Span<long> h, ReadOnlySpan<long> f, ReadOnlySpan<long> g)
    {
        Span<long> r = stackalloc long[Limbs];
        r.Clear();
        for (var i = 0; i < Limbs; i++)
        {
            for (var j = 0; j < Limbs; j++)
            {
                var term = f[i] * g[j];
                if ((i & j & 1) == 1)
                {
                    term *= 2;
                }

                var k = i + j;
                if (k >= Limbs)
                {
                    k -= Limbs;
                    term *= 19;
                }

                r[k] += term;
            }
        }

        Carry(r);
        r.CopyTo(h);
    }

    private static void SquareTimes(Span<long> h, ReadOnlySpan<long> f, int n)
    {
        Square(h, f);
        for (var i = 1; i < n; i++)
        {
            Square(h, h);
        }
    }

    // z^(p-2) by the fixed addition chain for 2^255 - 21 (Fermat inversion).
    private static void Invert(Span<long> output, ReadOnlySpan<long> z, Span<long> t0, Span<long> t1, Span<long> t2)
    {
        Span<long> z9 = stackalloc long[Limbs];
        Span<long> z11 = stackalloc long[Limbs];
        Span<long> z2 = stackalloc long[Limbs];
        Span<long> zin = stackalloc long[Limbs];
        z.CopyTo(zin);

        Square(z2, zin);                 // 2
        SquareTimes(t0, z2, 2);          // 8
        Mul(z9, t0, zin);                // 9
        Mul(z11, z9, z2);                // 11
        Square(t0, z11);                 // 22
        Mul(t0, t0, z9);                 // 2^5 - 1
        SquareTimes(t1, t0, 5);
        Mul(t1, t1, t0);                 // 2^10 - 1
        SquareTimes(t2, t1, 10);
        Mul(t2, t2, t1);                 // 2^20 - 1
        SquareTimes(z9, t2, 20);         // z9 is free now
        Mul(z9, z9, t2);                 // 2^40 - 1
        SquareTimes(z9, z9, 10);
        Mul(z9, z9, t1);                 // 2^50 - 1
        SquareTimes(t1, z9, 50);
        Mul(t1, t1, z9);                 // 2^100 - 1
        SquareTimes(t2, t1, 100);
        Mul(t2, t2, t1);                 // 2^200 - 1
        SquareTimes(t2, t2, 50);
        Mul(t2, t2, z9);                 // 2^250 - 1
        SquareTimes(t2, t2, 5);          // 2^255 - 32
        Mul(output, t2, z11);            // 2^255 - 21
    }
}
