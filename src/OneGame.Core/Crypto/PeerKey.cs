using System.IO;
using System.Text;

namespace OneGame.Core;

/// <summary>A key or key file was malformed.</summary>
public sealed class KeyFormatException : FormatException
{
    /// <summary>Creates the exception.</summary>
    /// <param name="message">What was wrong with the key text.</param>
    public KeyFormatException(string message) : base(message)
    {
    }
}

/// <summary>
/// A 32-byte X25519 public key. It names a server (the keys a client trusts) or a client (who a server sees after the
/// handshake). Public keys are not secret: print them, commit them, ship them in a client build or a server list.
/// Text form is unpadded base64url, 43 characters.
/// </summary>
public readonly struct PeerKey : IEquatable<PeerKey>
{
    /// <summary>Size of a key in bytes.</summary>
    public const int Size = 32;

    private readonly byte[]? _bytes;

    /// <summary>Wraps 32 bytes.</summary>
    /// <param name="bytes">The key bytes.</param>
    /// <exception cref="ArgumentException">The span is not 32 bytes.</exception>
    public PeerKey(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != Size)
        {
            throw new ArgumentException("a key is 32 bytes", nameof(bytes));
        }

        _bytes = bytes.ToArray();
    }

    /// <summary>True for the default value, which is no key.</summary>
    public bool IsEmpty => _bytes is null;

    /// <summary>The key bytes (empty for <see cref="IsEmpty"/>).</summary>
    public ReadOnlySpan<byte> Bytes => _bytes;

    /// <summary>Reads the text form written by <see cref="ToString"/>.</summary>
    /// <param name="text">43 characters of unpadded base64url (surrounding white space is ignored).</param>
    /// <exception cref="KeyFormatException">The text is not a key.</exception>
    public static PeerKey Parse(string text) => TryParse(text, out var key) ? key : throw new KeyFormatException("a public key is 43 characters of base64url");

    /// <summary>Reads the text form, returning false instead of throwing.</summary>
    /// <param name="text">The key text.</param>
    /// <param name="key">The key when this returns true.</param>
    public static bool TryParse(string? text, out PeerKey key)
    {
        key = default;
        if (text is null)
        {
            return false;
        }

        var t = text.Trim().Replace('-', '+').Replace('_', '/');
        if (t.Length != 43)
        {
            return false;
        }

        Span<byte> bytes = stackalloc byte[33];
        if (!Convert.TryFromBase64String(t + "=", bytes, out var written) || written != Size)
        {
            return false;
        }

        // Unpadded base64 of 32 bytes has two spare bits in the last character; only the canonical text is a key.
        key = new PeerKey(bytes[..Size]);
        if (key.ToString() != text.Trim())
        {
            key = default;
            return false;
        }

        return true;
    }

    /// <summary>The unpadded base64url text of the key.</summary>
    public override string ToString() => _bytes is null ? "" : Convert.ToBase64String(_bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <inheritdoc />
    public bool Equals(PeerKey other) => _bytes is null ? other._bytes is null : other._bytes is not null && _bytes.AsSpan().SequenceEqual(other._bytes);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is PeerKey k && Equals(k);

    /// <inheritdoc />
    public override int GetHashCode() => _bytes is null ? 0 : BitConverter.ToInt32(_bytes, 0);

    /// <summary>Compares two keys.</summary>
    public static bool operator ==(PeerKey left, PeerKey right) => left.Equals(right);

    /// <summary>Compares two keys.</summary>
    public static bool operator !=(PeerKey left, PeerKey right) => !left.Equals(right);
}

/// <summary>
/// An X25519 key pair: a server's identity or a client's. <b>The private half is a secret.</b> Whoever holds a server's private
/// key can impersonate that server to every client that trusts its public key, and whoever holds a client's can act as that
/// player. Keep private keys out of source control and out of client builds; only <see cref="PublicKey"/> is meant to be shared.
/// </summary>
public sealed class NetKeyPair
{
    private const string DevelopmentMark = "# DEV KEY";

    private readonly byte[] _private;

    private NetKeyPair(byte[] privateKey)
    {
        _private = privateKey;
        Span<byte> pub = stackalloc byte[PeerKey.Size];
        X25519.PublicKey(privateKey, pub);
        PublicKey = new PeerKey(pub);
    }

    /// <summary>The shareable half.</summary>
    public PeerKey PublicKey { get; }

    /// <summary>
    /// True for a key made by the development tools (<c>onegame run</c>, clusters, samples). The mark is in the key file, and a server
    /// refuses to start with such a key unless it is told it is a development run, so a dev key cannot quietly become a production key.
    /// </summary>
    public bool IsDevelopment { get; private set; }

    /// <summary>Makes a new random key pair marked <see cref="IsDevelopment"/>.</summary>
    public static NetKeyPair GenerateDevelopment()
    {
        var pair = Generate();
        pair.IsDevelopment = true;
        return pair;
    }

    /// <summary>Makes a new random key pair from <see cref="Entropy"/>.</summary>
    public static NetKeyPair Generate()
    {
        var priv = new byte[PeerKey.Size];
        Entropy.Fill(priv);
        return new NetKeyPair(priv);
    }

    /// <summary>Rebuilds a key pair from its private key bytes.</summary>
    /// <param name="privateKey">32 bytes.</param>
    /// <exception cref="ArgumentException">The span is not 32 bytes.</exception>
    public static NetKeyPair FromPrivateKey(ReadOnlySpan<byte> privateKey)
    {
        if (privateKey.Length != PeerKey.Size)
        {
            throw new ArgumentException("a private key is 32 bytes", nameof(privateKey));
        }

        return new NetKeyPair(privateKey.ToArray());
    }

    /// <summary>Reads the text written by <see cref="ToFileText"/>.</summary>
    /// <param name="text">A key file's contents.</param>
    /// <exception cref="KeyFormatException">The text is not a key file, or its public line does not match its private line.</exception>
    public static NetKeyPair FromFileText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string? priv = null;
        string? pub = null;
        var development = false;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith(DevelopmentMark, StringComparison.Ordinal))
            {
                development = true;
            }
            else if (line.StartsWith("private:", StringComparison.Ordinal))
            {
                priv = line[8..].Trim();
            }
            else if (line.StartsWith("public:", StringComparison.Ordinal))
            {
                pub = line[7..].Trim();
            }
        }

        if (priv is null || !PeerKey.TryParse(priv, out var privateKey))
        {
            throw new KeyFormatException("a key file needs a 'private:' line with 43 characters of base64url");
        }

        var pair = new NetKeyPair(privateKey.Bytes.ToArray()) { IsDevelopment = development };
        if (pub is not null && (!PeerKey.TryParse(pub, out var declared) || declared != pair.PublicKey))
        {
            throw new KeyFormatException("the 'public:' line does not belong to the private key");
        }

        return pair;
    }

    /// <summary>Reads a key file written by <see cref="Save"/>.</summary>
    /// <param name="path">The file.</param>
    /// <exception cref="KeyFormatException">The file is not a key file.</exception>
    public static NetKeyPair Load(string path) => FromFileText(File.ReadAllText(path));

    /// <summary>The key file text: comment lines, then <c>private:</c> and <c>public:</c> lines. Treat it as a secret.</summary>
    public string ToFileText()
    {
        var sb = new StringBuilder();
        if (IsDevelopment)
        {
            sb.Append(DevelopmentMark).Append(" - made for local development; never use it on a real server\n");
        }

        sb.Append("# onegame private key. SECRET: anyone who has this file can pose as the owner. Do not commit it or ship it in a client build.\n");
        sb.Append("private: ").Append(new PeerKey(_private)).Append('\n');
        sb.Append("public: ").Append(PublicKey).Append('\n');
        return sb.ToString();
    }

    /// <summary>Writes the key file. Where the platform has file modes, the file is readable by its owner only.</summary>
    /// <param name="path">The file to create or replace.</param>
    public void Save(string path)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, string.Empty);
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsBrowser())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        File.WriteAllText(path, ToFileText());
    }

    /// <summary>
    /// A key pair derived from this one for one server: HKDF-SHA-256 of the private key with the server's public key as salt. A player
    /// who connects to many servers then shows each a different key, so hosts cannot recognise the same player by comparing keys, while
    /// each server still sees the same key every time that player connects to it.
    /// </summary>
    /// <param name="server">The server's public key.</param>
    public NetKeyPair DeriveFor(PeerKey server)
    {
        if (server.IsEmpty)
        {
            throw new ArgumentException("a server key is needed to derive a per-server key", nameof(server));
        }

        Span<byte> prk = stackalloc byte[32];
        Span<byte> derived = stackalloc byte[32];
        Hkdf.Extract(server.Bytes, _private, prk);
        Hkdf.Expand(prk, "onegame per-server client key v1"u8, derived);
        var pair = new NetKeyPair(derived.ToArray()) { IsDevelopment = IsDevelopment };
        ChaCha20Poly1305.Wipe(prk);
        ChaCha20Poly1305.Wipe(derived);
        return pair;
    }

    /// <summary>
    /// A key pair derived from this one for a server that is named rather than keyed: the same derivation as <see cref="DeriveFor(PeerKey)"/>
    /// with the server's stable name in place of its key. A server that rotates its key keeps its name, so its players keep their keys.
    /// </summary>
    /// <param name="serverId">The server's stable name. Make it unique to that server (for example <c>"eu-1.my-game"</c>): two servers with the same name could link a player.</param>
    public NetKeyPair DeriveFor(string serverId)
    {
        ArgumentException.ThrowIfNullOrEmpty(serverId);
        Span<byte> prk = stackalloc byte[32];
        Span<byte> derived = stackalloc byte[32];
        Hkdf.Extract(Encoding.UTF8.GetBytes(serverId), _private, prk);
        Hkdf.Expand(prk, "onegame per-server-name client key v1"u8, derived);
        var pair = new NetKeyPair(derived.ToArray()) { IsDevelopment = IsDevelopment };
        ChaCha20Poly1305.Wipe(prk);
        ChaCha20Poly1305.Wipe(derived);
        return pair;
    }

    internal ReadOnlySpan<byte> Private => _private;

    /// <summary>The key's text form is its public key; the private half never prints.</summary>
    public override string ToString() => PublicKey.ToString();
}
