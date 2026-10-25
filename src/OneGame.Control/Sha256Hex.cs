using OneGame.Core;

namespace OneGame.Control;

/// <summary>The lower-case hex SHA-256 of a byte span, through the engine's own hash (the platform hash is missing on Android).</summary>
internal static class Sha256Hex
{
    /// <summary>The lower-case hex digest of <paramref name="data"/>.</summary>
    public static string Hash(ReadOnlySpan<byte> data) => Sha256.HashHex(data);
}
