using System.Security.Cryptography;
using System.Text;

namespace OneGame.Core.Tests;

// The platform implementations are the oracle here; the engine's own hashes exist because the platform ones are missing on Android.
public class HashingTests
{
    [Theory]
    [InlineData("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
    [InlineData("abc", "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
    [InlineData("abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq", "248d6a61d20638b8e5c026930c3e6039a33ce45964ff2167f6ecedd419db06c1")]
    public void Sha256KnownAnswers(string text, string hex) =>
        Assert.Equal(hex, Sha256.HashHex(Encoding.ASCII.GetBytes(text)));

    [Fact]
    public void Sha256MillionAs() =>
        Assert.Equal("cdc76e5c9914fb9281a1c7e284d73e67f1809a48a497200e046d39ccc7112cd0", Sha256.HashHex(Enumerable.Repeat((byte)'a', 1_000_000).ToArray()));

    [Fact]
    public void Sha256MatchesPlatformAtEveryLengthAroundBlockBoundaries()
    {
        var rng = new Random(7);
        for (var length = 0; length <= 300; length++)
        {
            var data = new byte[length];
            rng.NextBytes(data);
            Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(data)), Sha256.HashHex(data));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(100)]
    public void IncrementalUpdatesEqualOneShot(int chunk)
    {
        var data = new byte[1000];
        new Random(3).NextBytes(data);
        var s = Sha256.Create();
        for (var i = 0; i < data.Length; i += chunk)
        {
            s.Update(data.AsSpan(i, Math.Min(chunk, data.Length - i)));
        }

        var d = new byte[32];
        s.Final(d);
        Assert.Equal(SHA256.HashData(data), d);
    }

    // RFC 4231 test cases 1, 2 and 6 (6 uses a key longer than the block).
    [Fact]
    public void HmacRfc4231Vectors()
    {
        Assert.Equal("b0344c61d8db38535ca8afceaf0bf12b881dc200c9833da726e9376c2e32cff7", Mac(Enumerable.Repeat((byte)0x0b, 20).ToArray(), "Hi There"u8));
        Assert.Equal("5bdcc146bf60754e6a042426089575c75a003f089d2739839dec58b964ec3843", Mac("Jefe"u8, "what do ya want for nothing?"u8));
        Assert.Equal(
            "60e431591ee0b67f0d8a26aacbf5b77f8e0bc6213728c5140546040f0ee37f54",
            Mac(Enumerable.Repeat((byte)0xaa, 131).ToArray(), "Test Using Larger Than Block-Size Key - Hash Key First"u8));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(16, 5)]
    [InlineData(32, 56)]
    [InlineData(64, 64)]
    [InlineData(65, 200)]
    [InlineData(200, 1)]
    public void HmacMatchesPlatform(int keyLength, int dataLength)
    {
        var rng = new Random((keyLength * 1000) + dataLength);
        var key = new byte[keyLength];
        var data = new byte[dataLength];
        rng.NextBytes(key);
        rng.NextBytes(data);
        Assert.Equal(Convert.ToHexStringLower(HMACSHA256.HashData(key, data)), Mac(key, data));
    }

    private static string Mac(ReadOnlySpan<byte> key, ReadOnlySpan<byte> data)
    {
        Span<byte> mac = stackalloc byte[32];
        HmacSha256.Hash(key, data, mac);
        return Convert.ToHexStringLower(mac);
    }
}

public class EntropyTests
{
    [Fact]
    public void DefaultSourceGivesDistinctNonZeroBytes()
    {
        var a = new byte[32];
        var b = new byte[32];
        Entropy.Fill(a);
        Entropy.Fill(b);
        Assert.NotEqual(a, b);
        Assert.Contains(a, x => x != 0);
        Entropy.Fill(Span<byte>.Empty);
    }

    [Fact]
    public void ConcurrentFillsAreSafe() =>
        Parallel.For(0, 8, _ =>
        {
            var buf = new byte[64];
            for (var i = 0; i < 200; i++)
            {
                Entropy.Fill(buf);
            }
        });
}
