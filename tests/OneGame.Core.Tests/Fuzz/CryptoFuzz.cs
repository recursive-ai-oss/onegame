namespace OneGame.Core.Tests.Fuzzing;

/// <summary>
/// Fuzz entry points for the wire-facing crypto: opening an AEAD message and reading a handshake message. Both must return a
/// verdict for any bytes, never throw, and never leave plaintext behind for a message that failed to authenticate.
/// </summary>
public static class CryptoFuzz
{
    /// <summary>The crypto targets, registered next to <see cref="FuzzTargets.All"/> by the fuzzer and the regression test.</summary>
    public static IReadOnlyList<FuzzTargets.Target> All { get; } =
    [
        new FuzzTargets.Target("aead-open", AeadOpen, AeadSeeds),
        new FuzzTargets.Target("noise-read", NoiseRead, NoiseSeeds),
    ];

    private static readonly byte[] Key = Enumerable.Range(0, 32).Select(i => (byte)(i * 3 + 1)).ToArray();

    // Input: nonce (12) | aad length (1) | aad | ciphertext and tag.
    private static bool AeadOpen(byte[] input)
    {
        if (input.Length < 13)
        {
            return false;
        }

        var nonce = input.AsSpan(0, 12);
        var aadLength = Math.Min((int)input[12], input.Length - 13);
        var aad = input.AsSpan(13, aadLength);
        var body = input.AsSpan(13 + aadLength);
        var output = new byte[Math.Max(0, body.Length - 16)];
        Array.Fill(output, (byte)0xCC);
        var ok = ChaCha20Poly1305.Open(Key, nonce, aad, body, output);
        if (!ok)
        {
            if (output.Any(b => b != 0) && body.Length >= 16)
            {
                throw new InvalidOperationException("a failed open left plaintext in the output");
            }

            // Whatever the bytes were, sealing them must round trip, and a damaged copy must not open.
            var sealedBody = new byte[input.Length + 16];
            ChaCha20Poly1305.Seal(Key, nonce, aad, input, sealedBody);
            var back = new byte[input.Length];
            if (!ChaCha20Poly1305.Open(Key, nonce, aad, sealedBody, back) || !back.AsSpan().SequenceEqual(input))
            {
                throw new InvalidOperationException("seal and open did not round trip");
            }

            sealedBody[input.Length / 2] ^= 1;
            if (ChaCha20Poly1305.Open(Key, nonce, aad, sealedBody, back))
            {
                throw new InvalidOperationException("a damaged message opened");
            }

            return false;
        }

        var again = new byte[body.Length];
        ChaCha20Poly1305.Seal(Key, nonce, aad, output, again);
        return again.AsSpan().SequenceEqual(body);
    }

    private static IEnumerable<byte[]> AeadSeeds()
    {
        foreach (var (aad, plain) in new[] { (0, 0), (5, 1), (12, 64), (0, 1200) })
        {
            var nonce = new byte[12];
            nonce[0] = 7;
            var aadBytes = Enumerable.Range(0, aad).Select(i => (byte)(i + 9)).ToArray();
            var sealedBody = new byte[plain + 16];
            ChaCha20Poly1305.Seal(Key, nonce, aadBytes, new byte[plain], sealedBody);
            yield return nonce.Concat([(byte)aad]).Concat(aadBytes).Concat(sealedBody).ToArray();
        }
    }

    private static readonly byte[] ServerKey = Enumerable.Range(0, 32).Select(i => (byte)(i * 5 + 2)).ToArray();
    private static readonly byte[] ClientKey = Enumerable.Range(0, 32).Select(i => (byte)(i * 11 + 4)).ToArray();

    private static byte[] Public(byte[] priv)
    {
        var pub = new byte[32];
        X25519.PublicKey(priv, pub);
        return pub;
    }

    // Input: selector | message. Selector bit 0 picks XX or IK, the next bits pick which message of the handshake is fuzzed.
    // The earlier messages are produced by honest peers with fixed ephemeral keys so the fuzzed one reaches deep states.
    private static bool NoiseRead(byte[] input)
    {
        if (input.Length < 1)
        {
            return false;
        }

        var pattern = (input[0] & 1) == 0 ? NoisePattern.XX : NoisePattern.IK;
        var count = pattern == NoisePattern.XX ? 3 : 2;
        var stage = (input[0] >> 1) % count;
        var (initiator, responder) = Pair(pattern);
        var buffer = new byte[400];
        var plain = new byte[400];
        for (var i = 0; i < stage; i++)
        {
            var writer = i % 2 == 0 ? initiator : responder;
            var reader = i % 2 == 0 ? responder : initiator;
            var n = writer.WriteMessage([1, 2, 3], buffer);
            if (!reader.ReadMessage(buffer.AsSpan(0, n), plain, out _))
            {
                throw new InvalidOperationException("honest handshake failed");
            }
        }

        var target = stage % 2 == 0 ? responder : initiator;
        var ok = target.ReadMessage(input.AsSpan(1), plain, out var length);
        if (ok && length > input.Length)
        {
            throw new InvalidOperationException("inconsistent handshake state");
        }

        // A failed handshake must refuse further use rather than continue with half-applied state.
        if (!ok)
        {
            try
            {
                target.ReadMessage([], plain, out _);
                throw new InvalidOperationException("a failed handshake kept working");
            }
            catch (InvalidOperationException e) when (e.Message.StartsWith("it is not", StringComparison.Ordinal))
            {
            }
        }

        return ok;
    }

    private static (NoiseHandshake, NoiseHandshake) Pair(NoisePattern pattern)
    {
        var xx = pattern == NoisePattern.XX;
        var initiator = new NoiseHandshake(pattern, true, [], ClientKey, xx ? [] : Public(ServerKey));
        var responder = new NoiseHandshake(pattern, false, [], ServerKey, []);
        initiator.SetEphemeralForTest(Enumerable.Range(0, 32).Select(i => (byte)(i + 50)).ToArray());
        responder.SetEphemeralForTest(Enumerable.Range(0, 32).Select(i => (byte)(i + 90)).ToArray());
        return (initiator, responder);
    }

    private static IEnumerable<byte[]> NoiseSeeds()
    {
        foreach (var pattern in new[] { NoisePattern.XX, NoisePattern.IK })
        {
            var count = pattern == NoisePattern.XX ? 3 : 2;
            for (var stage = 0; stage < count; stage++)
            {
                var (initiator, responder) = Pair(pattern);
                var buffer = new byte[400];
                var plain = new byte[400];
                var n = 0;
                for (var i = 0; i <= stage; i++)
                {
                    var writer = i % 2 == 0 ? initiator : responder;
                    var reader = i % 2 == 0 ? responder : initiator;
                    n = writer.WriteMessage([1, 2, 3], buffer);
                    if (i < stage)
                    {
                        reader.ReadMessage(buffer.AsSpan(0, n), plain, out _);
                    }
                }

                var selector = (byte)((stage << 1) | (pattern == NoisePattern.XX ? 0 : 1));
                yield return new[] { selector }.Concat(buffer.AsSpan(0, n).ToArray()).ToArray();
            }
        }
    }
}
