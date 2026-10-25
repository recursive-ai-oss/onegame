using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace OneGame.Net.Tests;

// Wire compatibility: the cookie and the WebSocket accept value must be byte-identical to what the platform hashes produce, so
// peers built before the engine carried its own hashes still interoperate.
public class HashesTests
{
    [Theory]
    [InlineData("", "da39a3ee5e6b4b0d3255bfef95601890afd80709")]
    [InlineData("abc", "a9993e364706816aba3e25717850c26c9cd0d89d")]
    public void Sha1KnownAnswers(string text, string hex)
    {
        Span<byte> d = stackalloc byte[20];
        Sha1.Hash(Encoding.ASCII.GetBytes(text), d);
        Assert.Equal(hex, Convert.ToHexStringLower(d));
    }

    [Fact]
    public void Sha1MatchesPlatformUpToTwoBlocks()
    {
        var rng = new Random(11);
        for (var length = 0; length <= 119; length++)
        {
            var data = new byte[length];
            rng.NextBytes(data);
            var d = new byte[20];
            Sha1.Hash(data, d);
#pragma warning disable CA5350 // RFC 6455 mandates SHA-1 for the accept key; the platform hash is the test oracle.
            Assert.Equal(SHA1.HashData(data), d);
#pragma warning restore CA5350
        }
    }

    [Fact]
    public void AcceptValueFromRfc6455()
    {
        // RFC 6455 §1.3 example.
        var accept = new byte[28];
        WsHandshake.ComputeAccept("dGhlIHNhbXBsZSBub25jZQ=="u8, accept);
        Assert.Equal("s3pPLMBiTxaQ9kYGzzhZRbK+xOo=", Encoding.ASCII.GetString(accept));
    }

    [Theory]
    [InlineData(AddressFamily.InterNetwork)]
    [InlineData(AddressFamily.InterNetworkV6)]
    public void CookieEqualsThePlatformHmac(AddressFamily family)
    {
        var secret = new byte[32];
        for (var i = 0; i < secret.Length; i++)
        {
            secret[i] = (byte)((i * 7) + 1);
        }

        var ip = family == AddressFamily.InterNetwork ? IPAddress.Parse("192.0.2.33") : IPAddress.Parse("2001:db8::7");
        var from = new IPEndPoint(ip, 40123).Serialize();
        const ulong nonce = 0x1122334455667788;
        const long epoch = 171234567;
        var cookie = new byte[Wire.CookieSize];
        Wire.Cookie(secret, from, nonce, epoch, cookie);

        var n = Math.Min(from.Size, 40);
        var input = new byte[n + 16];
        from.Buffer.Span[..n].CopyTo(input);
        BitConverter.TryWriteBytes(input.AsSpan(n), nonce);
        BitConverter.TryWriteBytes(input.AsSpan(n + 8), epoch);
        Assert.Equal(HMACSHA256.HashData(secret, input)[..Wire.CookieSize], cookie);
    }
}
