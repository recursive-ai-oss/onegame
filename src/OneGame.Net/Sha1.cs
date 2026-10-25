using System.Buffers.Binary;
using System.Numerics;

namespace OneGame.Net;

/// <summary>
/// SHA-1 as specified in FIPS 180-4 §6.1, for one thing only: the Sec-WebSocket-Accept value RFC 6455 defines. It is a handshake
/// check against confused HTTP caches, not a security measure, so it is written out here instead of leaning on a platform
/// hash that browsers' runtimes and hardened systems may not provide.
/// </summary>
internal static class Sha1
{
    /// <summary>Hashes up to 119 bytes (two blocks) into 20 bytes.</summary>
    public static void Hash(ReadOnlySpan<byte> data, Span<byte> digest)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(data.Length, 119);
        Span<byte> padded = stackalloc byte[128];
        padded.Clear();
        data.CopyTo(padded);
        padded[data.Length] = 0x80;
        var blocks = data.Length + 9 <= 64 ? 1 : 2;
        BinaryPrimitives.WriteUInt64BigEndian(padded[(blocks * 64 - 8)..], (ulong)data.Length * 8);

        uint h0 = 0x67452301, h1 = 0xEFCDAB89, h2 = 0x98BADCFE, h3 = 0x10325476, h4 = 0xC3D2E1F0;
        Span<uint> w = stackalloc uint[80];
        for (var b = 0; b < blocks; b++)
        {
            for (var t = 0; t < 16; t++)
            {
                w[t] = BinaryPrimitives.ReadUInt32BigEndian(padded[((b * 64) + (t * 4))..]);
            }

            for (var t = 16; t < 80; t++)
            {
                w[t] = BitOperations.RotateLeft(w[t - 3] ^ w[t - 8] ^ w[t - 14] ^ w[t - 16], 1);
            }

            uint a = h0, bb = h1, c = h2, d = h3, e = h4;
            for (var t = 0; t < 80; t++)
            {
                uint f, k;
                if (t < 20)
                {
                    f = (bb & c) | (~bb & d);
                    k = 0x5A827999;
                }
                else if (t < 40)
                {
                    f = bb ^ c ^ d;
                    k = 0x6ED9EBA1;
                }
                else if (t < 60)
                {
                    f = (bb & c) | (bb & d) | (c & d);
                    k = 0x8F1BBCDC;
                }
                else
                {
                    f = bb ^ c ^ d;
                    k = 0xCA62C1D6;
                }

                var temp = BitOperations.RotateLeft(a, 5) + f + e + k + w[t];
                e = d;
                d = c;
                c = BitOperations.RotateLeft(bb, 30);
                bb = a;
                a = temp;
            }

            h0 += a;
            h1 += bb;
            h2 += c;
            h3 += d;
            h4 += e;
        }

        BinaryPrimitives.WriteUInt32BigEndian(digest, h0);
        BinaryPrimitives.WriteUInt32BigEndian(digest[4..], h1);
        BinaryPrimitives.WriteUInt32BigEndian(digest[8..], h2);
        BinaryPrimitives.WriteUInt32BigEndian(digest[12..], h3);
        BinaryPrimitives.WriteUInt32BigEndian(digest[16..], h4);
    }
}
