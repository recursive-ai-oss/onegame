using System.Buffers.Binary;
using OneGame.Core;

namespace OneGame.Sync;

/// <summary>
/// A channel whose far end proved who it is during the transport handshake. Transports with encryption implement it; the key
/// is the peer's static public key, and only the holder of the private half could have completed the handshake.
/// </summary>
public interface IAuthenticatedChannel : IMessageChannel
{
    /// <summary>The peer's authenticated public key, or null on a link without authentication (an <c>Insecure</c> transport, a loopback).</summary>
    PeerKey? RemoteKey => null;
}

/// <summary>Who is on the other end of an accepted channel, as far as the transport can prove.</summary>
/// <param name="Key">The peer's authenticated public key; <see cref="PeerKey.IsEmpty"/> when the link has no authentication.</param>
/// <param name="Address">The peer's network address as text, for logs. It is not an identity: addresses change and can be shared.</param>
public readonly record struct PeerIdentity(PeerKey Key, string Address)
{
    /// <summary>True when <see cref="Key"/> was proven by a handshake.</summary>
    public bool IsAuthenticated => !Key.IsEmpty;

    /// <summary>Reads the identity of a channel (an empty key for channels that cannot prove one).</summary>
    /// <param name="channel">The accepted channel.</param>
    /// <param name="address">The peer's address, for logs.</param>
    public static PeerIdentity Of(IMessageChannel channel, string address)
        => new(channel is IAuthenticatedChannel { RemoteKey: { } key } ? key : default, address);
}

/// <summary>
/// Gives each authenticated key a stable small player id, so a client that reconnects (a new connection, the same key) is the same
/// player. The ids are what <see cref="Trust.As(uint, uint)"/> takes. A game owns one registry per authority and saves it with its
/// own data (<see cref="Save"/>), or the ids start over when the process does.
/// </summary>
public sealed class PlayerRegistry
{
    private const int MaxPlayers = 1 << 22;

    private readonly Dictionary<PeerKey, uint> _byKey = [];
    private readonly Dictionary<uint, PeerKey> _byPlayer = [];
    private uint _next;

    /// <summary>Creates an empty registry that hands out ids from <paramref name="firstId"/> upward.</summary>
    /// <param name="firstId">The first player id; ids start at 1 by default because 0 means no player.</param>
    public PlayerRegistry(uint firstId = 1)
    {
        _next = Math.Max(1u, firstId);
    }

    /// <summary>Players known so far.</summary>
    public int Count => _byKey.Count;

    /// <summary>The player of <paramref name="key"/>, adding it on first sight.</summary>
    /// <param name="key">An authenticated key.</param>
    /// <param name="isNew">True if this call added the player.</param>
    /// <exception cref="ArgumentException"><paramref name="key"/> is empty (an unauthenticated peer has no identity to register).</exception>
    public uint PlayerFor(PeerKey key, out bool isNew)
    {
        if (key.IsEmpty)
        {
            throw new ArgumentException("an unauthenticated peer has no key to register", nameof(key));
        }

        if (_byKey.TryGetValue(key, out var id))
        {
            isNew = false;
            return id;
        }

        isNew = true;
        id = _next++;
        _byKey.Add(key, id);
        _byPlayer.Add(id, key);
        return id;
    }

    /// <summary>The player of <paramref name="key"/> if it has been seen.</summary>
    /// <param name="key">An authenticated key.</param>
    /// <param name="player">The id when this returns true.</param>
    public bool TryGetPlayer(PeerKey key, out uint player) => _byKey.TryGetValue(key, out player);

    /// <summary>The key a player id was issued to.</summary>
    /// <param name="player">A player id.</param>
    /// <param name="key">The key when this returns true.</param>
    public bool TryGetKey(uint player, out PeerKey key) => _byPlayer.TryGetValue(player, out key);

    /// <summary>Serialises the registry: next id, count, then (id, key) pairs in id order.</summary>
    public byte[] Save()
    {
        var ids = _byPlayer.Keys.ToArray();
        Array.Sort(ids);
        var bytes = new byte[8 + (ids.Length * (4 + PeerKey.Size))];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, _next);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), (uint)ids.Length);
        var pos = 8;
        foreach (var id in ids)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(pos), id);
            _byPlayer[id].Bytes.CopyTo(bytes.AsSpan(pos + 4));
            pos += 4 + PeerKey.Size;
        }

        return bytes;
    }

    /// <summary>Reads what <see cref="Save"/> wrote. A damaged or oversize file returns false and changes nothing.</summary>
    /// <param name="data">The saved bytes.</param>
    /// <param name="registry">The registry when this returns true.</param>
    public static bool TryLoad(ReadOnlySpan<byte> data, out PlayerRegistry registry)
    {
        registry = null!;
        if (data.Length < 8)
        {
            return false;
        }

        var next = BinaryPrimitives.ReadUInt32LittleEndian(data);
        var count = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]);
        if (count > MaxPlayers || data.Length != 8 + ((long)count * (4 + PeerKey.Size)))
        {
            return false;
        }

        var r = new PlayerRegistry(next);
        var pos = 8;
        for (var i = 0; i < count; i++)
        {
            var id = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
            var key = new PeerKey(data.Slice(pos + 4, PeerKey.Size));
            if (id == 0 || id >= next || r._byPlayer.ContainsKey(id) || r._byKey.ContainsKey(key))
            {
                return false;
            }

            r._byKey.Add(key, id);
            r._byPlayer.Add(id, key);
            pos += 4 + PeerKey.Size;
        }

        registry = r;
        return true;
    }
}
