namespace OneGame.Core;

/// <summary>HKDF (RFC 5869) over HMAC-SHA-256, and the two-and-three output form the Noise Protocol Framework builds on.</summary>
internal static class Hkdf
{
    /// <summary>Hash output size.</summary>
    public const int HashSize = Sha256.DigestSize;

    /// <summary>HKDF-Extract (RFC 5869 §2.2): a pseudorandom key from input keying material and a salt.</summary>
    public static void Extract(ReadOnlySpan<byte> salt, ReadOnlySpan<byte> ikm, Span<byte> prk)
    {
        // An empty salt is a string of HashLen zeros.
        if (salt.Length == 0)
        {
            Span<byte> zeros = stackalloc byte[HashSize];
            zeros.Clear();
            HmacSha256.Hash(zeros, ikm, prk);
        }
        else
        {
            HmacSha256.Hash(salt, ikm, prk);
        }
    }

    /// <summary>HKDF-Expand (RFC 5869 §2.3) into <paramref name="okm"/>, at most 255 blocks (8160 bytes).</summary>
    public static void Expand(ReadOnlySpan<byte> prk, ReadOnlySpan<byte> info, Span<byte> okm)
    {
        if (okm.Length > 255 * HashSize)
        {
            throw new ArgumentException("HKDF output is limited to 255 blocks", nameof(okm));
        }

        Span<byte> t = stackalloc byte[HashSize];
        Span<byte> input = stackalloc byte[HashSize + 128];
        if (info.Length > 128)
        {
            throw new ArgumentException("info is limited to 128 bytes", nameof(info));
        }

        var previous = 0;
        var written = 0;
        for (byte counter = 1; written < okm.Length; counter++)
        {
            t[..previous].CopyTo(input);
            info.CopyTo(input[previous..]);
            input[previous + info.Length] = counter;
            HmacSha256.Hash(prk, input[..(previous + info.Length + 1)], t);
            previous = HashSize;
            var n = Math.Min(HashSize, okm.Length - written);
            t[..n].CopyTo(okm[written..]);
            written += n;
        }

        ChaCha20Poly1305.Wipe(t);
    }

    /// <summary>
    /// The Noise HKDF (spec rev 34 §4.3): outputs are HMAC chains keyed by <c>HMAC(chainingKey, input)</c> with counter bytes 1, 2, 3.
    /// <paramref name="output3"/> is optional.
    /// </summary>
    public static void Noise(ReadOnlySpan<byte> chainingKey, ReadOnlySpan<byte> input, Span<byte> output1, Span<byte> output2, Span<byte> output3 = default)
    {
        Span<byte> temp = stackalloc byte[HashSize];
        HmacSha256.Hash(chainingKey, input, temp);
        Span<byte> buffer = stackalloc byte[HashSize + 1];
        buffer[0] = 1;
        HmacSha256.Hash(temp, buffer[..1], output1);
        output1[..HashSize].CopyTo(buffer);
        buffer[HashSize] = 2;
        HmacSha256.Hash(temp, buffer, output2);
        if (output3.Length > 0)
        {
            output2[..HashSize].CopyTo(buffer);
            buffer[HashSize] = 3;
            HmacSha256.Hash(temp, buffer, output3);
        }

        ChaCha20Poly1305.Wipe(temp);
        ChaCha20Poly1305.Wipe(buffer);
    }
}
