using System.Security.Cryptography;

namespace OneGame.Control.Tests;

public class Sha256HexTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(55)]
    [InlineData(56)]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(119)]
    [InlineData(120)]
    [InlineData(1000)]
    public void MatchesThePlatformHash(int length)
    {
        var data = new byte[length];
        new Random(length).NextBytes(data);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(data)), Sha256Hex.Hash(data));
    }

    [Fact]
    public void KnownAnswerForAbc() =>
        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", Sha256Hex.Hash("abc"u8));
}
