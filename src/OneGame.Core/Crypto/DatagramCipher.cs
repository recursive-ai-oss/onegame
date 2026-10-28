namespace OneGame.Core;

/// <summary>
/// The sending half of a datagram session: it owns the rule that a packet number is used for at most one transmission. Numbers must
/// strictly increase, so a retransmission is a new packet with a new number and a rebuilt packet can never reuse a nonce (same key and
/// nonce with different plaintext would reveal the keystream and the one-time MAC key). Numbers at or past the limit are refused, which
/// keeps the counter far from the 2^64 - 1 value reserved for rekeying.
/// </summary>
internal sealed class DatagramSender
{
    /// <summary>The nonce of the single close message; packet numbers are 32 bits, so it can never collide with one.</summary>
    public const ulong CloseNonce = 1UL << 32;

    private readonly byte[] _key = new byte[NoiseCipherState.KeySize];
    private readonly ulong _limit;
    private ulong _next;

    /// <summary>Takes over the key of a Noise cipher state (which is cleared).</summary>
    /// <param name="state">The send cipher from <see cref="NoiseHandshake.Split"/>.</param>
    /// <param name="limit">Packet numbers must stay below this; at most 2^32.</param>
    public DatagramSender(NoiseCipherState state, ulong limit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, 1UL << 32);
        _limit = limit;
        state.MoveKeyTo(_key);
    }

    /// <summary>Wipes the key.</summary>
    public void Clear() => ChaCha20Poly1305.Wipe(_key);

    /// <summary>The smallest number <see cref="Seal"/> will still accept.</summary>
    public ulong Next => _next;

    /// <summary>True when no number is left: the session must end or be re-established.</summary>
    public bool Exhausted => _next >= _limit;

    /// <summary>Encrypts one packet under <paramref name="number"/>; <paramref name="output"/> is <c>plaintext.Length + 16</c> bytes (in place is allowed).</summary>
    /// <exception cref="InvalidOperationException">The number was already used (or passed), or the limit is reached.</exception>
    public void Seal(ulong number, ReadOnlySpan<byte> ad, ReadOnlySpan<byte> plaintext, Span<byte> output)
    {
        if (number < _next || number >= _limit)
        {
            throw new InvalidOperationException($"Packet number {number} cannot be used: the next free number is {_next} and the limit is {_limit}.");
        }

        _next = number + 1;
        Span<byte> n = stackalloc byte[ChaCha20Poly1305.NonceSize];
        NoiseCipherState.EncodeNonce(number, n);
        ChaCha20Poly1305.Seal(_key, n, ad, plaintext, output);
    }

    /// <summary>Encrypts the close message under its own nonce. Every call must carry the same plaintext (the close is sent a few times).</summary>
    public void SealClose(ReadOnlySpan<byte> ad, ReadOnlySpan<byte> plaintext, Span<byte> output)
    {
        Span<byte> n = stackalloc byte[ChaCha20Poly1305.NonceSize];
        NoiseCipherState.EncodeNonce(CloseNonce, n);
        ChaCha20Poly1305.Seal(_key, n, ad, plaintext, output);
    }
}

/// <summary>
/// The receiving half of a datagram session: it owns the replay window (the last 64 numbers). A number is checked before any
/// decryption is spent on it and recorded only after the tag verifies, so a forged packet changes nothing.
/// </summary>
internal sealed class DatagramReceiver
{
    private readonly byte[] _key = new byte[NoiseCipherState.KeySize];
    private bool _any;
    private uint _highest;
    private ulong _mask;

    /// <summary>Takes over the key of a Noise cipher state (which is cleared).</summary>
    /// <param name="state">The receive cipher from <see cref="NoiseHandshake.Split"/>.</param>
    public DatagramReceiver(NoiseCipherState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        state.MoveKeyTo(_key);
    }

    /// <summary>Wipes the key.</summary>
    public void Clear() => ChaCha20Poly1305.Wipe(_key);

    /// <summary>True if the number is neither a duplicate nor older than the window. Spends no cryptography.</summary>
    public bool IsFresh(uint number)
    {
        if (!_any)
        {
            return true;
        }

        var d = (int)(number - _highest);
        if (d > 0)
        {
            return true;
        }

        var back = -(long)d;
        return back <= 63 && (_mask & (1UL << (int)back)) == 0;
    }

    /// <summary>Decrypts a packet. False (and no change) for a replayed number or a failed tag; <paramref name="output"/> is zeroed on failure.</summary>
    public bool Open(uint number, ReadOnlySpan<byte> ad, ReadOnlySpan<byte> input, Span<byte> output)
    {
        if (!IsFresh(number))
        {
            return false;
        }

        Span<byte> n = stackalloc byte[ChaCha20Poly1305.NonceSize];
        NoiseCipherState.EncodeNonce(number, n);
        if (!ChaCha20Poly1305.Open(_key, n, ad, input, output))
        {
            return false;
        }

        var d = _any ? (int)(number - _highest) : 1;
        if (d > 0)
        {
            _mask = d >= 64 || !_any ? 1UL : (_mask << d) | 1UL;
            _highest = number;
            _any = true;
        }
        else
        {
            _mask |= 1UL << -d;
        }

        return true;
    }

    /// <summary>Decrypts the close message (its nonce is outside the packet-number space, so there is no window to consult).</summary>
    public bool OpenClose(ReadOnlySpan<byte> ad, ReadOnlySpan<byte> input, Span<byte> output)
    {
        Span<byte> n = stackalloc byte[ChaCha20Poly1305.NonceSize];
        NoiseCipherState.EncodeNonce(DatagramSender.CloseNonce, n);
        return ChaCha20Poly1305.Open(_key, n, ad, input, output);
    }
}
