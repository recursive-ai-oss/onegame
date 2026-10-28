using System.Text;
using System.Text.Json;

namespace OneGame.Core.Tests;

// Known answers from the RFCs and Google's Wycheproof vectors (testdata/crypto, Apache-2.0), then the Noise test vectors.
public class CryptoTests
{
    private static byte[] Hex(string s) => Convert.FromHexString(s);

    private static string TestData(string name)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var path = Path.Combine(dir.FullName, "testdata", "crypto", name);
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException(name);
    }

    // RFC 8439 §2.3.2: the block at counter 1 is the keystream, so encrypting zeros shows it.
    [Fact]
    public void ChaCha20BlockFunctionMatchesRfc8439()
    {
        var key = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        var output = new byte[64];
        ChaCha20Poly1305.ChaCha20Xor(key, Hex("000000090000004a00000000"), 1, new byte[64], output);
        Assert.Equal("10f1e7e4d13b5915500fdd1fa32071c4c7d1f4c733c068030422aa9ac3d46c4ed2826446079faa0914c2d705d98b02a2b5129cd1de164eb9cbd083e8a2503c4e", Convert.ToHexStringLower(output));
    }

    // RFC 8439 §2.5.2.
    [Fact]
    public void Poly1305MatchesRfc8439()
    {
        var poly = new ChaCha20Poly1305.Poly1305(Hex("85d6be7857556d337f4452fe42d506a80103808afb0db2fd4abff6af4149f51b"));
        poly.Update(Encoding.ASCII.GetBytes("Cryptographic Forum Research Group"));
        var tag = new byte[16];
        poly.Finish(tag);
        Assert.Equal("a8061dc1305136c6c22b8baf0c0127a9", Convert.ToHexStringLower(tag));
    }

    // RFC 8439 §2.8.2.
    [Fact]
    public void AeadSealMatchesRfc8439()
    {
        var plaintext = Encoding.ASCII.GetBytes("Ladies and Gentlemen of the class of '99: If I could offer you only one tip for the future, sunscreen would be it.");
        var output = new byte[plaintext.Length + 16];
        ChaCha20Poly1305.Seal(Hex("808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9f"), Hex("070000004041424344454647"), Hex("50515253c0c1c2c3c4c5c6c7"), plaintext, output);
        Assert.Equal("d31a8d34648e60db7b86afbc53ef7ec2a4aded51296e08fea9e2b5a736ee62d63dbea45e8ca9671282fafb69da92728b1a71de0a9e060b2905d6a5b67ecd3b3692ddbd7f2d778b8c9803aee328091b58fab324e4fad675945585808b4831d7bc3ff4def08e4b7a9de576d26586cec64b61161ae10b594f09e26a7e902ecbd0600691", Convert.ToHexStringLower(output));
    }

    // RFC 8439 Appendix A.5.
    [Fact]
    public void AeadOpenMatchesRfc8439AppendixA5()
    {
        var input = Hex("64a0861575861af460f062c79be643bd5e805cfd345cf389f108670ac76c8cb24c6cfc18755d43eea09ee94e382d26b0bdb7b73c321b0100d4f03b7f355894cf332f830e710b97ce98c8a84abd0b948114ad176e008d33bd60f982b1ff37c8559797a06ef4f0ef61c186324e2b3506383606907b6a7c02b0f9f6157b53c867e4b9166c767b804d46a59b5216cde7a4e99040c5a40433225ee282a1b0a06c523eaf4534d7f83fa1155b0047718cbc546a0d072b04b3564eea1b422273f548271a0bb2316053fa76991955ebd63159434ecebb4e466dae5a1073a6727627097a1049e617d91d361094fa68f0ff77987130305beaba2eda04df997b714d6c6f2c29a6ad5cb4022b02709b" + "eead9d67890cbb22392336fea1851f38");
        var output = new byte[input.Length - 16];
        Assert.True(ChaCha20Poly1305.Open(Hex("1c9240a5eb55d38af333888604f6b5f0473917c1402b80099dca5cbc207075c0"), Hex("000000000102030405060708"), Hex("f33388860000000000004e91"), input, output));
        Assert.StartsWith("Internet-Drafts are draft documents valid for a maximum of six months", Encoding.ASCII.GetString(output));
    }

    [Fact]
    public void AeadRejectsEveryBitFlipAndZeroesTheOutput()
    {
        var key = new byte[32];
        var nonce = new byte[12];
        var sealedMessage = new byte[40 + 16];
        ChaCha20Poly1305.Seal(key, nonce, [1, 2, 3], new byte[40], sealedMessage);
        var output = new byte[40];
        for (var bit = 0; bit < sealedMessage.Length * 8; bit++)
        {
            var copy = (byte[])sealedMessage.Clone();
            copy[bit / 8] ^= (byte)(1 << (bit % 8));
            Array.Fill(output, (byte)0xAA);
            Assert.False(ChaCha20Poly1305.Open(key, nonce, [1, 2, 3], copy, output));
            Assert.All(output, b => Assert.Equal(0, b));
        }

        Assert.False(ChaCha20Poly1305.Open(key, nonce, [1, 2, 4], sealedMessage, output));
        Assert.False(ChaCha20Poly1305.Open(key, nonce, [1, 2, 3], sealedMessage.AsSpan(0, 10), Array.Empty<byte>()));
    }

    [Fact]
    public void AeadWorksInPlaceAndAtEveryLengthAcrossBlockBoundaries()
    {
        var key = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        var nonce = new byte[12];
        for (var length = 0; length <= 200; length++)
        {
            var plain = new byte[length];
            new Random(length).NextBytes(plain);
            var buffer = new byte[length + 16];
            plain.CopyTo(buffer, 0);
            ChaCha20Poly1305.Seal(key, nonce, [9], buffer.AsSpan(0, length), buffer);
            Assert.True(ChaCha20Poly1305.Open(key, nonce, [9], buffer, buffer.AsSpan(0, length)));
            Assert.Equal(plain, buffer.AsSpan(0, length).ToArray());
        }
    }

    [Fact]
    public void AeadMatchesTheWycheproofVectors()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(TestData("wycheproof-chacha20_poly1305.json")));
        int valid = 0, invalid = 0, skipped = 0;
        foreach (var group in doc.RootElement.GetProperty("testGroups").EnumerateArray())
        {
            foreach (var t in group.GetProperty("tests").EnumerateArray())
            {
                var key = Hex(t.GetProperty("key").GetString()!);
                var iv = Hex(t.GetProperty("iv").GetString()!);
                var aad = Hex(t.GetProperty("aad").GetString()!);
                var msg = Hex(t.GetProperty("msg").GetString()!);
                var ct = Hex(t.GetProperty("ct").GetString()!);
                var tag = Hex(t.GetProperty("tag").GetString()!);
                var result = t.GetProperty("result").GetString();
                var id = t.GetProperty("tcId").GetInt32();
                if (key.Length != 32 || iv.Length != 12 || tag.Length != 16)
                {
                    // The wrong-size cases are refused by argument checks, not by the tag.
                    Assert.Equal("invalid", result);
                    skipped++;
                    continue;
                }

                var full = ct.Concat(tag).ToArray();
                var output = new byte[ct.Length];
                var opened = ChaCha20Poly1305.Open(key, iv, aad, full, output);
                if (result == "valid")
                {
                    Assert.True(opened, "tcId " + id);
                    Assert.Equal(msg, output);
                    var sealedMessage = new byte[msg.Length + 16];
                    ChaCha20Poly1305.Seal(key, iv, aad, msg, sealedMessage);
                    Assert.Equal(full, sealedMessage);
                    valid++;
                }
                else
                {
                    Assert.False(opened, "tcId " + id);
                    invalid++;
                }
            }
        }

        Assert.True(valid >= 100 && invalid >= 50, $"valid {valid} invalid {invalid} skipped {skipped}");
        Console.WriteLine($"Wycheproof ChaCha20-Poly1305: valid {valid}, invalid {invalid}, wrong-size {skipped}");
    }

    [Theory]
    [InlineData("a546e36bf0527c9d3b16154b82465edd62144c0ac1fc5a18506a2244ba449ac4", "e6db6867583030db3594c1a424b15f7c726624ec26b3353b10a903a6d0ab1c4c", "c3da55379de9c6908e94ea4df28d084f32eccf03491c71f754b4075577a28552")]
    [InlineData("4b66e9d4d1b4673c5ad22691957d6af5c11b6421e0ea01d42ca4169e7918ba0d", "e5210f12786811d3f4b7959d0538ae2c31dbe7106fc03c3efc4cd549c715a493", "95cbde9476e8907d7aade45cb4b873f88b595a68799fa152e6f8f7647aac7957")]
    public void X25519MatchesRfc7748Section52(string scalar, string u, string expected)
    {
        var output = new byte[32];
        Assert.True(X25519.Agree(Hex(scalar), Hex(u), output));
        Assert.Equal(expected, Convert.ToHexStringLower(output));
    }

    // RFC 7748 §5.2: feed the output back in as the next scalar, with the previous scalar as u.
    [Theory]
    [InlineData(1, "422c8e7a6227d7bca1350b3e2bb7279f7897b87bb6854b783c60e80311ae3079")]
    [InlineData(1000, "684cf59ba83309552800ef566f2f4d3c1c3887c49360e3875f2eb94d99532c51")]
    public void X25519IteratedMatchesRfc7748(int iterations, string expected)
    {
        var k = new byte[32];
        k[0] = 9;
        var u = (byte[])k.Clone();
        var next = new byte[32];
        for (var i = 0; i < iterations; i++)
        {
            X25519.Agree(k, u, next);
            u = k;
            k = (byte[])next.Clone();
        }

        Assert.Equal(expected, Convert.ToHexStringLower(k));
    }

    // The RFC's one-million-iteration vector: about a minute of work, so it only runs on request.
    [Fact(Explicit = true)]
    public void X25519IteratedMillionMatchesRfc7748()
    {
        var k = new byte[32];
        k[0] = 9;
        var u = (byte[])k.Clone();
        var next = new byte[32];
        for (var i = 0; i < 1_000_000; i++)
        {
            X25519.Agree(k, u, next);
            (u, k, next) = (k, next, u);
        }

        Assert.Equal("7c3911e0ab2586fd864497297e575e6f3bc601c0883c30df5f4dd2d24f665424", Convert.ToHexStringLower(k));
    }

    // RFC 7748 §6.1.
    [Fact]
    public void X25519DiffieHellmanMatchesRfc7748Section61()
    {
        var alice = Hex("77076d0a7318a57d3c16c17251b26645df4c2f87ebc0992ab177fba51db92c2a");
        var bob = Hex("5dab087e624a8a4b79e17f8b83800ee66f3bb1292618b6fd1c2f8b27ff88e0eb");
        var alicePublic = new byte[32];
        var bobPublic = new byte[32];
        X25519.PublicKey(alice, alicePublic);
        X25519.PublicKey(bob, bobPublic);
        Assert.Equal("8520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a", Convert.ToHexStringLower(alicePublic));
        Assert.Equal("de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f", Convert.ToHexStringLower(bobPublic));
        var s1 = new byte[32];
        var s2 = new byte[32];
        Assert.True(X25519.Agree(alice, bobPublic, s1));
        Assert.True(X25519.Agree(bob, alicePublic, s2));
        Assert.Equal("4a5d9d5ba4ce2de1728e3bf480350f25e07e21c947d19e3376f09b3c1e161742", Convert.ToHexStringLower(s1));
        Assert.Equal(s1, s2);
    }

    [Fact]
    public void X25519MatchesTheWycheproofVectors()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(TestData("wycheproof-x25519.json")));
        int valid = 0, acceptable = 0, zero = 0;
        foreach (var group in doc.RootElement.GetProperty("testGroups").EnumerateArray())
        {
            foreach (var t in group.GetProperty("tests").EnumerateArray())
            {
                var result = t.GetProperty("result").GetString();
                var output = new byte[32];
                var ok = X25519.Agree(Hex(t.GetProperty("private").GetString()!), Hex(t.GetProperty("public").GetString()!), output);
                Assert.Equal(t.GetProperty("shared").GetString(), Convert.ToHexStringLower(output));
                Assert.True(result is "valid" or "acceptable", "unexpected result " + result);
                if (!ok)
                {
                    // Only low-order public keys give an all-zero secret, and those are the cases the vectors mark acceptable.
                    Assert.Equal("acceptable", result);
                    zero++;
                }

                if (result == "valid")
                {
                    valid++;
                }
                else
                {
                    acceptable++;
                }
            }
        }

        Assert.True(valid + acceptable >= 500);
        Console.WriteLine($"Wycheproof X25519: valid {valid}, acceptable {acceptable} (of which all-zero secret {zero})");
    }

    // RFC 5869 Appendix A.1 and A.3.
    [Fact]
    public void HkdfMatchesRfc5869()
    {
        var ikm = Enumerable.Repeat((byte)0x0b, 22).ToArray();
        var prk = new byte[32];
        var okm = new byte[42];
        Hkdf.Extract(Hex("000102030405060708090a0b0c"), ikm, prk);
        Assert.Equal("077709362c2e32df0ddc3f0dc47bba6390b6c73bb50f9c3122ec844ad7c2b3e5", Convert.ToHexStringLower(prk));
        Hkdf.Expand(prk, Hex("f0f1f2f3f4f5f6f7f8f9"), okm);
        Assert.Equal("3cb25f25faacd57a90434f64d0362f2a2d2d0a90cf1a5a4c5db02d56ecc4c5bf34007208d5b887185865", Convert.ToHexStringLower(okm));

        Hkdf.Extract(default, ikm, prk);
        Assert.Equal("19ef24a32c717b167f33a91d6f648bdf96596776afdb6377ac434c1c293ccb04", Convert.ToHexStringLower(prk));
        Hkdf.Expand(prk, default, okm);
        Assert.Equal("8da4e775a563c18f715f802a063c5a31b8a11f5c5ee1879ec3454e5f3c738d2d9d201395faa4b61a96c8", Convert.ToHexStringLower(okm));
    }

    // The Noise HKDF with three outputs is HKDF-Expand with an empty info string over the extracted key: the counter byte is
    // the only input beyond the previous block (spec §4.3).
    [Fact]
    public void NoiseHkdfAgreesWithRfc5869Expand()
    {
        var ck = Enumerable.Range(0, 32).Select(i => (byte)(i * 7)).ToArray();
        var ikm = new byte[] { 1, 2, 3, 4, 5 };
        var prk = new byte[32];
        Hkdf.Extract(ck, ikm, prk);
        var expanded = new byte[96];
        Hkdf.Expand(prk, default, expanded);
        var a = new byte[32];
        var b = new byte[32];
        var c = new byte[32];
        Hkdf.Noise(ck, ikm, a, b, c);
        Assert.Equal(expanded, a.Concat(b).Concat(c).ToArray());
    }

    [Fact]
    public void NoiseMatchesThePublishedVectors()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(TestData("noise-vectors.json")));
        var count = 0;
        var messagesChecked = 0;
        foreach (var v in doc.RootElement.GetProperty("vectors").EnumerateArray())
        {
            var name = v.GetProperty("protocol_name").GetString()!;
            var pattern = Enum.Parse<NoisePattern>(name.Split('_')[1]);

            byte[] Field(string key) => v.TryGetProperty(key, out var e) ? Hex(e.GetString()!) : [];

            var init = new NoiseHandshake(pattern, true, Field("init_prologue"), Field("init_static"), Field("init_remote_static"));
            var resp = new NoiseHandshake(pattern, false, Field("resp_prologue"), Field("resp_static"), Field("resp_remote_static"));
            init.SetEphemeralForTest(Field("init_ephemeral"));
            resp.SetEphemeralForTest(Field("resp_ephemeral"));
            var messages = v.GetProperty("messages").EnumerateArray().ToArray();
            NoiseCipherState? initSend = null, initReceive = null, respSend = null, respReceive = null;
            var buffer = new byte[65535];
            var plain = new byte[65535];
            var handshakeMessages = HandshakeMessages(pattern);
            var oneWay = pattern is NoisePattern.N or NoisePattern.K or NoisePattern.X;
            for (var i = 0; i < messages.Length; i++)
            {
                var payload = Hex(messages[i].GetProperty("payload").GetString()!);
                var expected = Hex(messages[i].GetProperty("ciphertext").GetString()!);
                if (i < handshakeMessages)
                {
                    var fromInitiator = init.MyTurnToWrite;
                    var writer = fromInitiator ? init : resp;
                    var reader = fromInitiator ? resp : init;
                    var length = writer.WriteMessage(payload, buffer);
                    Assert.True(expected.AsSpan().SequenceEqual(buffer.AsSpan(0, length)), name + " message " + i);
                    Assert.True(reader.ReadMessage(buffer.AsSpan(0, length), plain, out var plainLength), name + " message " + i);
                    Assert.Equal(payload, plain.AsSpan(0, plainLength).ToArray());
                    if (i == handshakeMessages - 1)
                    {
                        Assert.True(init.IsComplete || oneWay);
                        Assert.Equal(Field("handshake_hash"), init.HandshakeHash.ToArray());
                        Assert.Equal(Field("handshake_hash"), resp.HandshakeHash.ToArray());
                        init.Split(out initSend, out initReceive);
                        resp.Split(out respSend, out respReceive);
                    }
                }
                else
                {
                    // Transport messages keep alternating from the handshake (the initiator wrote message 0); one-way patterns only use the initiator.
                    var fromInitiator = oneWay || i % 2 == 0;
                    var sender = fromInitiator ? initSend! : respSend!;
                    var receiver = fromInitiator ? respReceive! : initReceive!;
                    var output = new byte[payload.Length + 16];
                    sender.Encrypt(default, payload, output);
                    Assert.True(expected.AsSpan().SequenceEqual(output), name + " transport " + i + " fromInitiator=" + fromInitiator);
                    var back = new byte[payload.Length];
                    Assert.True(receiver.Decrypt(default, output, back));
                    Assert.Equal(payload, back);
                }

                messagesChecked++;
            }

            Assert.NotNull(initSend);
            count++;
        }

        Assert.Equal(15, count);
        Console.WriteLine($"Noise vectors: {count} protocols, {messagesChecked} messages");
    }

    private static int HandshakeMessages(NoisePattern p) => p switch
    {
        NoisePattern.N or NoisePattern.K or NoisePattern.X => 1,
        NoisePattern.NN or NoisePattern.KN or NoisePattern.NK or NoisePattern.KK or NoisePattern.IN or NoisePattern.IK or NoisePattern.NX or NoisePattern.KX or NoisePattern.IX => 2,
        _ => 3,
    };

    [Fact]
    public void NoiseHandshakeRejectsTamperingAndWrongKeys()
    {
        var serverKey = new byte[32];
        new Random(5).NextBytes(serverKey);
        var serverPublic = new byte[32];
        X25519.PublicKey(serverKey, serverPublic);
        var buffer = new byte[200];
        var plain = new byte[200];

        // A flipped bit anywhere in the second message fails authentication (the ephemeral key bits change the secrets, the rest the tag).
        for (var bit = 0; bit < 50 * 8; bit++)
        {
            var client = new NoiseHandshake(NoisePattern.NK, true, [], [], serverPublic);
            var server = new NoiseHandshake(NoisePattern.NK, false, [], serverKey, []);
            var n = client.WriteMessage([1], buffer);
            Assert.True(server.ReadMessage(buffer.AsSpan(0, n), plain, out _));
            n = server.WriteMessage([2, 3], buffer);
            Assert.Equal(32 + 2 + 16, n);
            buffer[bit / 8] ^= (byte)(1 << (bit % 8));
            Assert.False(client.ReadMessage(buffer.AsSpan(0, n), plain, out _));
            Assert.False(client.IsComplete);
            Assert.Throws<InvalidOperationException>(() => client.WriteMessage([], buffer));
        }

        // A client that pinned a different server key cannot complete the handshake.
        var other = new byte[32];
        new Random(6).NextBytes(other);
        var otherPublic = new byte[32];
        X25519.PublicKey(other, otherPublic);
        var c2 = new NoiseHandshake(NoisePattern.NK, true, [], [], otherPublic);
        var s2 = new NoiseHandshake(NoisePattern.NK, false, [], serverKey, []);
        var len = c2.WriteMessage([], buffer);
        Assert.False(s2.ReadMessage(buffer.AsSpan(0, len), plain, out _));

        // Different prologues fail too, and a low-order ephemeral key is refused.
        var c3 = new NoiseHandshake(NoisePattern.NK, true, [1], [], serverPublic);
        var s3 = new NoiseHandshake(NoisePattern.NK, false, [2], serverKey, []);
        len = c3.WriteMessage([], buffer);
        Assert.False(s3.ReadMessage(buffer.AsSpan(0, len), plain, out _));
        var nn = new NoiseHandshake(NoisePattern.NN, false, [], [], []);
        Assert.True(nn.ReadMessage(new byte[32], plain, out _)); // message one only carries e; the DH comes in the reply
        Assert.Throws<InvalidOperationException>(() => nn.WriteMessage([], buffer)); // ee with an all-zero e is degenerate
    }

    [Fact]
    public void TransportCiphersHonourCountersAndRekey()
    {
        var a = new NoiseCipherState();
        var b = new NoiseCipherState();
        var key = new byte[32];
        a.Initialize(key);
        b.Initialize(key);
        var one = new byte[1 + 16];
        var two = new byte[1 + 16];
        a.Encrypt([], [7], one);
        a.Encrypt([], [7], two);
        Assert.NotEqual(one, two);
        var back = new byte[1];
        Assert.False(b.Decrypt([], two, back)); // out of order: counter 1 against 0
        Assert.True(b.Decrypt([], one, back));
        Assert.True(b.Decrypt([], two, back));
        a.Rekey();
        b.Rekey();
        a.Encrypt([], [8], one);
        Assert.True(b.Decrypt([], one, back));
        Assert.Equal(8, back[0]);
    }

    // Review M3: a cipher with no key never passes plaintext and never says a message is authentic.
    [Fact]
    public void ACipherWithoutAKeyFailsClosed()
    {
        var c = new NoiseCipherState();
        var output = new byte[1 + 16];
        Assert.Throws<InvalidOperationException>(() => c.Encrypt([], [1], output));
        Assert.False(c.Decrypt([], new byte[17], new byte[1]));
        Assert.Throws<InvalidOperationException>(c.Rekey);
        c.Initialize(new byte[32]);
        c.Clear();
        Assert.Throws<InvalidOperationException>(() => c.Encrypt([], [1], output));
        Assert.False(c.Decrypt([], new byte[17], new byte[1]));
        Assert.Throws<ArgumentException>(() => c.Initialize(new byte[16]));
    }

    // Review M8: the counter limit and Rekey are pinned by known answers, not only by "both sides agree".
    [Fact]
    public void RekeyAndCounterExhaustionAreChecked()
    {
        var key = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();
        var rekeyed = new NoiseCipherState();
        rekeyed.Initialize(key);
        rekeyed.Rekey();

        // REKEY (spec §4.2): the new key is the first 32 bytes of the encryption of 32 zeros at counter 2^64 - 1.
        var nonce = new byte[12];
        Array.Fill(nonce, (byte)0xff, 4, 8);
        var expected = new byte[48];
        ChaCha20Poly1305.Seal(key, nonce, default, new byte[32], expected);
        var reference = new NoiseCipherState();
        reference.Initialize(expected.AsSpan(0, 32));
        var a = new byte[17];
        var b = new byte[17];
        rekeyed.Encrypt([], [5], a);
        reference.Encrypt([], [5], b);
        Assert.Equal(b, a);

        // The last usable counter is 2^64 - 2; after it Encrypt throws and Decrypt refuses.
        var last = new NoiseCipherState();
        last.Initialize(key);
        last.SetNonceForTest(NoiseCipherState.MaxNonce);
        last.Encrypt([], [1], a);
        Assert.Throws<InvalidOperationException>(() => last.Encrypt([], [1], a));
        var peer = new NoiseCipherState();
        peer.Initialize(key);
        peer.SetNonceForTest(NoiseCipherState.MaxNonce + 1);
        Assert.False(peer.Decrypt([], a, new byte[1]));
    }

    // Review L1/L7: one bad datagram or one small buffer must not wreck a handshake.
    [Fact]
    public void AHandshakeSurvivesForgedRepliesOnAForkAndSmallBuffers()
    {
        var serverKey = new byte[32];
        serverKey[0] = 7;
        var serverPublic = new byte[32];
        X25519.PublicKey(serverKey, serverPublic);
        var clientKey = new byte[32];
        clientKey[0] = 9;
        var client = new NoiseHandshake(NoisePattern.IK, true, [1], clientKey, serverPublic);
        var server = new NoiseHandshake(NoisePattern.IK, false, [1], serverKey, []);
        var buffer = new byte[200];
        var tooSmall = new byte[40];
        Assert.Throws<ArgumentException>(() => client.WriteMessage([], tooSmall)); // e fits, es and s do not
        Assert.Throws<InvalidOperationException>(() => client.WriteMessage([], buffer)); // and the handshake is over, not half-mutated

        client = new NoiseHandshake(NoisePattern.IK, true, [1], clientKey, serverPublic);
        var n = client.WriteMessage([], buffer);
        var plain = new byte[64];
        Assert.True(server.ReadMessage(buffer.AsSpan(0, n), plain, out _));
        var reply = new byte[100];
        var m = server.WriteMessage([1, 2, 3], reply);
        var forged = reply.AsSpan(0, m).ToArray();
        forged[40] ^= 1;
        var fork = client.Fork();
        Assert.False(fork.ReadMessage(forged, plain, out _));
        fork = client.Fork();
        Assert.True(fork.ReadMessage(reply.AsSpan(0, m), plain, out var length)); // the original client is untouched by the failed fork
        Assert.Equal(3, length);
        Assert.True(client.ReadMessage(reply.AsSpan(0, m), plain, out _));
    }

    [Fact]
    public void DatagramSenderNeverReusesANumberAndReceiverOwnsTheWindow()
    {
        var key = new byte[32];
        key[0] = 1;
        NoiseCipherState Fresh()
        {
            var c = new NoiseCipherState();
            c.Initialize(key);
            return c;
        }

        var sender = new DatagramSender(Fresh(), 1000);
        var receiver = new DatagramReceiver(Fresh());
        var one = new byte[1 + 16];
        var plain = new byte[1];
        sender.Seal(0, [], [7], one);
        Assert.Throws<InvalidOperationException>(() => sender.Seal(0, [], [8], one)); // the same number twice: refused
        sender.Seal(5, [], [7], one);
        Assert.Throws<InvalidOperationException>(() => sender.Seal(3, [], [8], one)); // going backwards: refused
        Assert.Throws<InvalidOperationException>(() => sender.Seal(1000, [], [8], one)); // at the limit
        Assert.Throws<InvalidOperationException>(() => sender.Seal(ulong.MaxValue, [], [8], one));
        Assert.Equal(6UL, sender.Next);

        Assert.True(receiver.Open(5, [], one, plain));
        Assert.False(receiver.Open(5, [], one, plain)); // replay
        Assert.False(receiver.Open(6, [], one, plain)); // wrong number for this ciphertext
        sender.Seal(6, [], [9], one);
        Assert.True(receiver.Open(6, [], one, plain));
        Assert.Equal(9, plain[0]);
        sender.Seal(100, [], [1], one);
        Assert.True(receiver.Open(100, [], one, plain));
        Assert.False(receiver.IsFresh(30)); // older than the 64-packet window
        Assert.True(receiver.IsFresh(60));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DatagramSender(Fresh(), (1UL << 32) + 1));

        var close = new byte[2 + 16];
        sender.SealClose([4], [1, 2], close);
        Assert.True(receiver.OpenClose([4], close, new byte[2]));
        Assert.False(receiver.OpenClose([5], close, new byte[2]));
    }

    // Review M8: the final reduction of Poly1305 (h >= p) is only reached by crafted inputs; compare with a big-integer reference.
    [Fact]
    public void Poly1305MatchesABigIntegerReferenceOnEdgeInputs()
    {
        var p = (System.Numerics.BigInteger.One << 130) - 5;
        static byte[] Tag(byte[] key, byte[] message)
        {
            var poly = new ChaCha20Poly1305.Poly1305(key);
            poly.Update(message);
            var tag = new byte[16];
            poly.Finish(tag);
            return tag;
        }

        byte[] Reference(byte[] key, byte[] message)
        {
            var r = new System.Numerics.BigInteger(key.AsSpan(0, 16), isUnsigned: true);
            r &= System.Numerics.BigInteger.Parse("0ffffffc0ffffffc0ffffffc0fffffff", System.Globalization.NumberStyles.HexNumber);
            var s = new System.Numerics.BigInteger(key.AsSpan(16, 16), isUnsigned: true);
            var h = System.Numerics.BigInteger.Zero;
            for (var i = 0; i < message.Length; i += 16)
            {
                var n = Math.Min(16, message.Length - i);
                var block = new byte[n + 1];
                message.AsSpan(i, n).CopyTo(block);
                block[n] = 1;
                h = (h + new System.Numerics.BigInteger(block, isUnsigned: true)) * r % p;
            }

            var tag = ((h + s) & ((System.Numerics.BigInteger.One << 128) - 1)).ToByteArray(isUnsigned: true);
            return [.. tag, .. new byte[16 - tag.Length]];
        }

        // RFC 8439 A.3 #5: r = 2, s = 0, message = 16 bytes of ff gives h = 2^129 - 1, times 2, reduced = 3.
        var k5 = new byte[32];
        k5[0] = 2;
        Assert.Equal("03000000000000000000000000000000", Convert.ToHexStringLower(Tag(k5, Enumerable.Repeat((byte)0xff, 16).ToArray())));

        var rng = new Random(8439);
        var count = 0;
        foreach (var rByte in new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x0f, 0xff })
        {
            foreach (var fill in new byte[] { 0xff, 0xfb, 0xfe, 0xfd, 0xf0, 0x00, 0x01 })
            {
                for (var blocks = 1; blocks <= 4; blocks++)
                {
                    foreach (var tail in new[] { 0, 1, 7 })
                    {
                        var key = new byte[32];
                        key[0] = rByte;
                        if ((count & 1) == 1)
                        {
                            rng.NextBytes(key.AsSpan(16));
                        }

                        var message = new byte[(blocks * 16) + tail];
                        Array.Fill(message, fill);
                        if (message.Length > 20)
                        {
                            message[17] ^= (byte)count;
                        }

                        Assert.Equal(Convert.ToHexStringLower(Reference(key, message)), Convert.ToHexStringLower(Tag(key, message)));
                        count++;
                    }
                }
            }
        }

        for (var i = 0; i < 300; i++)
        {
            var key = new byte[32];
            rng.NextBytes(key);
            var message = new byte[rng.Next(0, 200)];
            rng.NextBytes(message);
            Assert.Equal(Convert.ToHexStringLower(Reference(key, message)), Convert.ToHexStringLower(Tag(key, message)));
        }
    }

    [Fact]
    public void SealAndOpenAllocateNothing()
    {
        var key = new byte[32];
        var nonce = new byte[12];
        var plain = new byte[1200];
        var sealedMessage = new byte[1216];
        var back = new byte[1200];
        var cipher = new NoiseCipherState();
        cipher.Initialize(key);
        var peer = new NoiseCipherState();
        peer.Initialize(key);
        var shared = new byte[32];
        var scalar = new byte[32];
        scalar[0] = 9;
        for (var i = 0; i < 20; i++)
        {
            ChaCha20Poly1305.Seal(key, nonce, plain.AsSpan(0, 12), plain, sealedMessage);
            ChaCha20Poly1305.Open(key, nonce, plain.AsSpan(0, 12), sealedMessage, back);
            cipher.Encrypt(default, plain, sealedMessage);
            peer.Decrypt(default, sealedMessage, back);
            X25519.Agree(scalar, scalar, shared);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 2000; i++)
        {
            ChaCha20Poly1305.Seal(key, nonce, plain.AsSpan(0, 12), plain, sealedMessage);
            ChaCha20Poly1305.Open(key, nonce, plain.AsSpan(0, 12), sealedMessage, back);
            cipher.Encrypt(default, plain, sealedMessage);
            peer.Decrypt(default, sealedMessage, back);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 20; i++)
        {
            X25519.Agree(scalar, scalar, shared);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    // Not a benchmark: a rough number to know the cipher is fast enough for game traffic.
    [Fact]
    public void ThroughputIsReported()
    {
        var key = new byte[32];
        var nonce = new byte[12];
        var plain = new byte[1200];
        var sealedMessage = new byte[1216];
        var back = new byte[1200];
        for (var i = 0; i < 2000; i++)
        {
            ChaCha20Poly1305.Seal(key, nonce, default, plain, sealedMessage);
        }

        const int n = 50000;
        var t = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < n; i++)
        {
            ChaCha20Poly1305.Seal(key, nonce, default, plain, sealedMessage);
            ChaCha20Poly1305.Open(key, nonce, default, sealedMessage, back);
        }

        var seconds = t.Elapsed.TotalSeconds;
        Console.WriteLine($"ChaCha20-Poly1305 1200-byte packets: {n * 2 / seconds:F0} ops/s, {n * 2 * 1200 / seconds / 1e6:F0} MB/s (seal and open)");
        var shared = new byte[32];
        var scalar = new byte[32];
        scalar[0] = 9;
        t.Restart();
        for (var i = 0; i < 500; i++)
        {
            X25519.Agree(scalar, scalar, shared);
        }

        Console.WriteLine($"X25519: {t.Elapsed.TotalMilliseconds / 500 * 1000:F0} us per scalar multiplication");
        Assert.True(seconds < 60);
    }

    // A dudect-style check (Reparaz et al.): time two input classes interleaved at random and compare the means with Welch's t-test
    // after cropping the slowest tail. |t| above about 4.5 suggests a timing difference. It needs a quiet machine, so it only runs on
    // request: dotnet run the test app with -explicit only -method '*ConstantTimeSmokeCheck'.
    [Fact(Explicit = true)]
    public void ConstantTimeSmokeCheck()
    {
        var u = new byte[32];
        u[0] = 9;
        var rng = new Random(11);
        var shared = new byte[32];
        var randomScalar = new byte[32];
        var x25519 = Welch(4000, cls =>
        {
            if (cls == 0)
            {
                Array.Clear(randomScalar); // the all-zero scalar clamps to a single set bit
            }
            else
            {
                rng.NextBytes(randomScalar);
            }

            return () => X25519.Agree(randomScalar, u, shared);
        });

        var key = new byte[32];
        var nonce = new byte[12];
        var plain = new byte[1200];
        var sealedMessage = new byte[1216];
        ChaCha20Poly1305.Seal(key, nonce, default, plain, sealedMessage);
        var early = (byte[])sealedMessage.Clone();
        early[1200] ^= 1;
        var late = (byte[])sealedMessage.Clone();
        late[1215] ^= 1;
        var back = new byte[1200];
        var tagCompare = Welch(20000, cls =>
        {
            var message = cls == 0 ? early : late;
            return () => ChaCha20Poly1305.Open(key, nonce, default, message, back);
        });

        var zeroKeyed = new byte[32];
        var randomKeyed = new byte[32];
        var keySeal = Welch(20000, cls =>
        {
            var k = cls == 0 ? zeroKeyed : randomKeyed;
            if (cls == 1)
            {
                rng.NextBytes(randomKeyed);
            }

            return () => ChaCha20Poly1305.Seal(k, nonce, default, plain, sealedMessage);
        });

        Console.WriteLine($"dudect |t|: X25519 sparse vs random scalar {x25519:F2}; open with first vs last tag byte wrong {tagCompare:F2}; seal with zero vs random key {keySeal:F2}");
    }

    private static double Welch(int samples, Func<int, Action> prepare)
    {
        var times = new double[2][];
        times[0] = new double[samples];
        times[1] = new double[samples];
        var counts = new int[2];
        var order = new Random(3);
        for (var i = 0; i < samples * 2; i++)
        {
            var cls = order.Next(2);
            if (counts[cls] == samples)
            {
                cls ^= 1;
            }

            var action = prepare(cls);
            var t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            action();
            times[cls][counts[cls]++] = System.Diagnostics.Stopwatch.GetTimestamp() - t0;
        }

        // Crop everything above the 90th percentile of the pooled sample (interrupts and cache noise).
        var pooled = times[0].Concat(times[1]).OrderBy(x => x).ToArray();
        var cut = pooled[(int)(pooled.Length * 0.9)];
        var a = times[0].Where(x => x <= cut).ToArray();
        var b = times[1].Where(x => x <= cut).ToArray();
        double ma = a.Average(), mb = b.Average();
        double va = a.Sum(x => (x - ma) * (x - ma)) / (a.Length - 1), vb = b.Sum(x => (x - mb) * (x - mb)) / (b.Length - 1);
        return Math.Abs(ma - mb) / Math.Sqrt((va / a.Length) + (vb / b.Length));
    }
}
