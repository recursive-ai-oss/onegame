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

/// <summary>The registry holds as many players as it may and every one of them has been activated, so there is nobody to forget.</summary>
public sealed class PlayerRegistryFullException : InvalidOperationException
{
    /// <summary>Creates the exception.</summary>
    /// <param name="limit">The registry's limit.</param>
    public PlayerRegistryFullException(int limit)
        : base($"The player registry holds {limit} activated players and cannot admit another key. Raise maxPlayers or use TryPlayerFor to refuse new keys.")
    {
    }
}

/// <summary>
/// Gives each authenticated key a stable small player id, so a client that reconnects (a new connection, the same key) is the same
/// player. The ids are what <see cref="Trust.As(uint, uint)"/> takes. A game owns one registry per authority and saves it with its
/// own data (<see cref="Save"/>), or the ids start over when the process does.
/// <para>
/// A key proves a key, not a person: anyone can make keys at will, and a handshake with a fresh key succeeds. So the registry is
/// bounded, and it forgets the right keys. A key is <em>provisional</em> when first seen and becomes <em>active</em> when the game calls
/// <see cref="Activate(uint)"/> for something a spammer cannot mass-produce (the player played for a while, finished the tutorial,
/// linked a platform account). When the registry is full, a new key replaces the oldest provisional one; active players are never
/// replaced, so key spam cannot lock real players out, and cannot take away anyone who earned a place. A returning provisional key
/// that was forgotten simply gets a new id.
/// </para>
/// </summary>
public sealed class PlayerRegistry
{
    private const int DefaultMaxPlayers = 1 << 22;

    private readonly Dictionary<PeerKey, uint> _byKey = [];
    private readonly Dictionary<uint, PeerKey> _byPlayer = [];
    private readonly HashSet<uint> _active = [];
    private readonly Queue<uint> _provisional = new(); // oldest first; entries that were activated or forgotten are skipped when they reach the front
    private readonly int _maxPlayers;
    private uint _next;

    /// <summary>Creates an empty registry that hands out ids from <paramref name="firstId"/> upward.</summary>
    /// <param name="firstId">The first player id; ids start at 1 by default because 0 means no player.</param>
    /// <param name="maxPlayers">How many keys it will remember. Set it to what the game can hold. When it is reached, a new key replaces the oldest provisional one; if every key is active, <see cref="TryPlayerFor"/> refuses and <see cref="PlayerFor"/> throws <see cref="PlayerRegistryFullException"/>.</param>
    public PlayerRegistry(uint firstId = 1, int maxPlayers = DefaultMaxPlayers)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPlayers, 1);
        _next = Math.Max(1u, firstId);
        _maxPlayers = maxPlayers;
    }

    /// <summary>Counts every change that <see cref="Save"/> would write, so a game can tell whether the registry needs saving.</summary>
    public long Version { get; private set; }

    /// <summary>Players known so far, provisional and active.</summary>
    public int Count => _byKey.Count;

    /// <summary>Players that were activated.</summary>
    public int ActiveCount => _active.Count;

    /// <summary>
    /// The player of <paramref name="key"/>, adding it (provisionally) on first sight. Returns false instead of adding a key when the
    /// registry is full of active players.
    /// </summary>
    /// <param name="key">An authenticated key.</param>
    /// <param name="player">The player id when this returns true.</param>
    /// <param name="isNew">True if this call added the player.</param>
    /// <exception cref="ArgumentException"><paramref name="key"/> is empty (an unauthenticated peer has no identity to register).</exception>
    public bool TryPlayerFor(PeerKey key, out uint player, out bool isNew)
    {
        if (key.IsEmpty)
        {
            throw new ArgumentException("an unauthenticated peer has no key to register", nameof(key));
        }

        if (_byKey.TryGetValue(key, out player))
        {
            isNew = false;
            return true;
        }

        while (_byKey.Count >= _maxPlayers)
        {
            if (!ForgetOldestProvisional())
            {
                player = 0;
                isNew = false;
                return false;
            }
        }

        isNew = true;
        player = _next++;
        _byKey.Add(key, player);
        _byPlayer.Add(player, key);
        _provisional.Enqueue(player);
        Version++;
        return true;
    }

    /// <summary>The player of <paramref name="key"/>, adding it (provisionally) on first sight; the registry never holds more than its limit.</summary>
    /// <param name="key">An authenticated key.</param>
    /// <param name="isNew">True if this call added the player.</param>
    /// <exception cref="ArgumentException"><paramref name="key"/> is empty (an unauthenticated peer has no identity to register).</exception>
    /// <exception cref="PlayerRegistryFullException">The registry is full of active players; use <see cref="TryPlayerFor"/> to refuse the key instead.</exception>
    public uint PlayerFor(PeerKey key, out bool isNew)
        => TryPlayerFor(key, out var player, out isNew) ? player : throw new PlayerRegistryFullException(_maxPlayers);

    /// <summary>
    /// Marks a player as one worth keeping: it is never replaced to make room for a new key. Call it when the player has done something
    /// a spammer cannot do at scale.
    /// </summary>
    /// <param name="player">A player id from this registry.</param>
    /// <returns>True if the player exists (activating twice is fine).</returns>
    public bool Activate(uint player)
    {
        if (!_byPlayer.ContainsKey(player))
        {
            return false;
        }

        if (_active.Add(player))
        {
            Version++;
        }

        return true;
    }

    /// <summary>Whether the player was activated.</summary>
    /// <param name="player">A player id.</param>
    public bool IsActive(uint player) => _active.Contains(player);

    // Makes room by forgetting the oldest key nobody activated.
    private bool ForgetOldestProvisional()
    {
        while (_provisional.TryDequeue(out var id))
        {
            if (_active.Contains(id) || !_byPlayer.TryGetValue(id, out var key))
            {
                continue;
            }

            _ = _byPlayer.Remove(id);
            _ = _byKey.Remove(key);
            Version++;
            return true;
        }

        return false;
    }

    /// <summary>The player of <paramref name="key"/> if it has been seen.</summary>
    /// <param name="key">An authenticated key.</param>
    /// <param name="player">The id when this returns true.</param>
    public bool TryGetPlayer(PeerKey key, out uint player) => _byKey.TryGetValue(key, out player);

    /// <summary>The key a player id was issued to.</summary>
    /// <param name="player">A player id.</param>
    /// <param name="key">The key when this returns true.</param>
    public bool TryGetKey(uint player, out PeerKey key) => _byPlayer.TryGetValue(player, out key);

    private const int EntrySize = 4 + PeerKey.Size + 1;

    /// <summary>Serialises the registry: next id, count, then (id, key, active) entries in id order.</summary>
    public byte[] Save()
    {
        var ids = _byPlayer.Keys.ToArray();
        Array.Sort(ids);
        var bytes = new byte[8 + (ids.Length * EntrySize)];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, _next);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), (uint)ids.Length);
        var pos = 8;
        foreach (var id in ids)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(pos), id);
            _byPlayer[id].Bytes.CopyTo(bytes.AsSpan(pos + 4));
            bytes[pos + 4 + PeerKey.Size] = _active.Contains(id) ? (byte)1 : (byte)0;
            pos += EntrySize;
        }

        return bytes;
    }

    /// <summary>Reads what <see cref="Save"/> wrote. A damaged or oversize file returns false and changes nothing.</summary>
    /// <param name="data">The saved bytes.</param>
    /// <param name="registry">The registry when this returns true.</param>
    /// <param name="maxPlayers">The limit the loaded registry keeps (see the constructor).</param>
    public static bool TryLoad(ReadOnlySpan<byte> data, out PlayerRegistry registry, int maxPlayers = DefaultMaxPlayers)
    {
        registry = null!;
        if (data.Length < 8 || maxPlayers < 1)
        {
            return false;
        }

        var next = BinaryPrimitives.ReadUInt32LittleEndian(data);
        var count = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]);
        if (count > DefaultMaxPlayers || data.Length != 8 + ((long)count * EntrySize))
        {
            return false;
        }

        var r = new PlayerRegistry(next, maxPlayers);
        var pos = 8;
        for (var i = 0; i < count; i++)
        {
            var id = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
            var key = new PeerKey(data.Slice(pos + 4, PeerKey.Size));
            var flag = data[pos + 4 + PeerKey.Size];
            if (id == 0 || id >= next || flag > 1 || r._byPlayer.ContainsKey(id) || r._byKey.ContainsKey(key))
            {
                return false;
            }

            r._byKey.Add(key, id);
            r._byPlayer.Add(id, key);
            if (flag == 1)
            {
                _ = r._active.Add(id);
            }
            else
            {
                r._provisional.Enqueue(id); // ids only grow, so the order they were issued in is the order they were first seen
            }

            pos += EntrySize;
        }

        registry = r;
        return true;
    }
}
