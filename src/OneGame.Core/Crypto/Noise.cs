using System.Buffers.Binary;

namespace OneGame.Core;

/// <summary>
/// The Noise CipherState (Noise Protocol Framework, revision 34, §5.1) for ChaChaPoly: a 32-byte key and a 64-bit message
/// counter, owned by the object (callers never choose a counter). Nothing here allocates per message. A cipher state with no key
/// fails closed: <see cref="Encrypt"/> throws and <see cref="Decrypt"/> returns false. Only the handshake's own internal state passes
/// data through before its first key (spec §5.1), and that one is private to <see cref="NoiseHandshake"/>. Datagram transports use
/// <see cref="DatagramSender"/> and <see cref="DatagramReceiver"/> instead, which own the packet-number rules.
/// </summary>
internal sealed class NoiseCipherState
{
    /// <summary>Key size.</summary>
    public const int KeySize = 32;

    /// <summary>Tag bytes added to every encrypted message.</summary>
    public const int TagSize = ChaCha20Poly1305.TagSize;

    /// <summary>The largest usable counter; 2^64 - 1 is reserved for rekeying (spec §5.1).</summary>
    public const ulong MaxNonce = ulong.MaxValue - 1;

    private readonly byte[] _key = new byte[KeySize];
    private readonly bool _passThroughWithoutKey;

    /// <summary>Creates an unkeyed state.</summary>
    /// <param name="passThroughWithoutKey">True only for the handshake's own state, which copies data through until the first <c>MixKey</c>.</param>
    public NoiseCipherState(bool passThroughWithoutKey = false)
    {
        _passThroughWithoutKey = passThroughWithoutKey;
    }

    /// <summary>True once a key is set.</summary>
    public bool HasKey { get; private set; }

    /// <summary>The counter the next <see cref="Encrypt"/> or <see cref="Decrypt"/> uses.</summary>
    public ulong Nonce { get; private set; }

    /// <summary>InitializeKey: sets the key and resets the counter.</summary>
    /// <exception cref="ArgumentException">The key is not 32 bytes.</exception>
    public void Initialize(ReadOnlySpan<byte> key)
    {
        if (key.Length != KeySize)
        {
            throw new ArgumentException("a cipher key is 32 bytes", nameof(key));
        }

        key.CopyTo(_key);
        HasKey = true;
        Nonce = 0;
    }

    /// <summary>Forgets the key.</summary>
    public void Clear()
    {
        ChaCha20Poly1305.Wipe(_key);
        HasKey = false;
        Nonce = 0;
    }

    /// <summary>
    /// EncryptWithAd. <paramref name="output"/> must be exactly <c>plaintext.Length + 16</c> bytes. Throws when the counter is exhausted,
    /// and when there is no key (never plaintext).
    /// </summary>
    public void Encrypt(ReadOnlySpan<byte> ad, ReadOnlySpan<byte> plaintext, Span<byte> output)
    {
        if (!HasKey)
        {
            if (!_passThroughWithoutKey)
            {
                throw new InvalidOperationException("The cipher has no key.");
            }

            plaintext.CopyTo(output);
            return;
        }

        if (Nonce > MaxNonce)
        {
            throw new InvalidOperationException("The cipher's message counter is exhausted; rekey or close the session.");
        }

        Span<byte> n = stackalloc byte[ChaCha20Poly1305.NonceSize];
        EncodeNonce(Nonce, n);
        ChaCha20Poly1305.Seal(_key, n, ad, plaintext, output);
        Nonce++;
    }

    /// <summary>DecryptWithAd. Returns false (and does not advance the counter) when authentication fails, the counter is exhausted, or there is no key.</summary>
    public bool Decrypt(ReadOnlySpan<byte> ad, ReadOnlySpan<byte> input, Span<byte> output)
    {
        if (!HasKey)
        {
            if (!_passThroughWithoutKey)
            {
                return false;
            }

            input.CopyTo(output);
            return true;
        }

        if (Nonce > MaxNonce)
        {
            return false;
        }

        Span<byte> n = stackalloc byte[ChaCha20Poly1305.NonceSize];
        EncodeNonce(Nonce, n);
        if (!ChaCha20Poly1305.Open(_key, n, ad, input, output))
        {
            return false;
        }

        Nonce++;
        return true;
    }

    /// <summary>Test hook: moves the counter, to reach the exhaustion limit without 2^64 messages.</summary>
    internal void SetNonceForTest(ulong nonce) => Nonce = nonce;

    internal void CopyStateFrom(NoiseCipherState o)
    {
        o._key.CopyTo(_key, 0);
        HasKey = o.HasKey;
        Nonce = o.Nonce;
    }

    /// <summary>Hands the key to a datagram cipher and forgets it here, so one key is never live in two places.</summary>
    internal void MoveKeyTo(Span<byte> key)
    {
        if (!HasKey)
        {
            throw new InvalidOperationException("The cipher has no key.");
        }

        _key.CopyTo(key);
        Clear();
    }

    /// <summary>REKEY (spec §4.2): the new key is the first 32 bytes of the encryption of zeros under counter 2^64 - 1.</summary>
    public void Rekey()
    {
        if (!HasKey)
        {
            throw new InvalidOperationException("The cipher has no key.");
        }

        Span<byte> n = stackalloc byte[ChaCha20Poly1305.NonceSize];
        EncodeNonce(ulong.MaxValue, n);
        Span<byte> zeros = stackalloc byte[KeySize];
        zeros.Clear();
        Span<byte> output = stackalloc byte[KeySize + TagSize];
        ChaCha20Poly1305.Seal(_key, n, default, zeros, output);
        output[..KeySize].CopyTo(_key);
        ChaCha20Poly1305.Wipe(output);
    }

    // Spec §12.3: four zero bytes, then the counter as 64-bit little endian.
    internal static void EncodeNonce(ulong nonce, Span<byte> n)
    {
        n[..4].Clear();
        BinaryPrimitives.WriteUInt64LittleEndian(n[4..], nonce);
    }
}

/// <summary>The handshake patterns of Noise spec §7 that this engine implements, by their one- and two-letter names.</summary>
internal enum NoisePattern
{
    /// <summary>One-way, no sender authentication: <c>&lt;- s ... -&gt; e, es</c>.</summary>
    N,
    /// <summary>One-way, sender static known to the recipient in advance.</summary>
    K,
    /// <summary>One-way, sender static sent in the message.</summary>
    X,
    /// <summary>Anonymous both ways, no keys known.</summary>
    NN,
    /// <summary>Initiator static known to the responder in advance.</summary>
    KN,
    /// <summary>Responder static known to the initiator in advance; the initiator is anonymous.</summary>
    NK,
    /// <summary>Both statics known in advance.</summary>
    KK,
    /// <summary>Initiator anonymous, responder sends its static.</summary>
    NX,
    /// <summary>Initiator static known in advance, responder sends its static.</summary>
    KX,
    /// <summary>Initiator sends its static after the responder's ephemeral.</summary>
    XN,
    /// <summary>Initiator sends its static immediately, no responder static.</summary>
    IN,
    /// <summary>Responder static known in advance, initiator static sent encrypted in the third message.</summary>
    XK,
    /// <summary>Responder static known in advance, initiator static sent in the first message.</summary>
    IK,
    /// <summary>Both statics sent during the handshake.</summary>
    XX,
    /// <summary>Initiator static in the first message, responder static in the second.</summary>
    IX,
}

/// <summary>
/// The Noise HandshakeState and SymmetricState (spec rev 34 §5.2, §5.3) for <c>Noise_*_25519_ChaChaPoly_SHA256</c>. It takes and
/// returns bytes and knows nothing about sockets. Any failure to read a message (bad length, failed authentication, a
/// low-order public key) marks the handshake failed and it must be thrown away, as the spec requires.
/// </summary>
internal sealed class NoiseHandshake
{
    /// <summary>Largest Noise message (spec §3).</summary>
    public const int MaxMessageSize = 65535;

    private const int DhLen = 32;
    private const int HashLen = 32;

    private readonly string[] _messages;
    private readonly bool _initiator;
    private readonly byte[] _h = new byte[HashLen];
    private readonly byte[] _ck = new byte[HashLen];
    private readonly NoiseCipherState _cipher = new(passThroughWithoutKey: true);
    private readonly byte[] _sPriv = new byte[DhLen];
    private readonly byte[] _sPub = new byte[DhLen];
    private readonly byte[] _ePriv = new byte[DhLen];
    private readonly byte[] _ePub = new byte[DhLen];
    private readonly byte[] _rs = new byte[DhLen];
    private readonly byte[] _re = new byte[DhLen];
    private bool _hasS, _hasE, _hasRs, _hasRe;
    private bool _fixedEphemeral;
    private int _next;
    private bool _failed;

    /// <summary>
    /// Starts a handshake. <paramref name="staticPrivate"/> is this side's static private key when the pattern needs one
    /// (empty otherwise); <paramref name="remoteStatic"/> is the peer's static public key when the pattern has it as a
    /// pre-message (empty otherwise).
    /// </summary>
    public NoiseHandshake(NoisePattern pattern, bool initiator, ReadOnlySpan<byte> prologue, ReadOnlySpan<byte> staticPrivate, ReadOnlySpan<byte> remoteStatic, ReadOnlySpan<byte> staticPublic = default)
    {
        var (pre, messages) = Describe(pattern);
        _messages = messages;
        _initiator = initiator;
        if (staticPrivate.Length != 0)
        {
            if (staticPrivate.Length != DhLen)
            {
                throw new ArgumentException("static key must be 32 bytes", nameof(staticPrivate));
            }

            staticPrivate.CopyTo(_sPriv);
            if (staticPublic.Length == DhLen)
            {
                // The caller already has the public half (a server computes it once, not per handshake).
                staticPublic.CopyTo(_sPub);
            }
            else
            {
                X25519.PublicKey(_sPriv, _sPub);
            }

            _hasS = true;
        }

        if (remoteStatic.Length != 0)
        {
            if (remoteStatic.Length != DhLen)
            {
                throw new ArgumentException("remote static key must be 32 bytes", nameof(remoteStatic));
            }

            remoteStatic.CopyTo(_rs);
            _hasRs = true;
        }

        if (!_hasS && RequiresOwnStatic(pattern, initiator))
        {
            throw new ArgumentException("this pattern needs a static private key for this role", nameof(staticPrivate));
        }

        // Spec §5.2/§5.3: h = the protocol name (hashed when longer than 32 bytes), ck = h, then the prologue, then pre-messages.
        var name = System.Text.Encoding.ASCII.GetBytes("Noise_" + pattern + "_25519_ChaChaPoly_SHA256");
        if (name.Length <= HashLen)
        {
            name.CopyTo(_h, 0);
        }
        else
        {
            Sha256.Hash(name, _h);
        }

        _h.CopyTo(_ck, 0);
        MixHash(prologue);

        // Pre-messages (spec §5.3): the initiator's static first, then the responder's. Each side holds its own as _sPub and the
        // other's as _rs.
        if (pre.Initiator)
        {
            MixHash(PreMessageKey(initiator));
        }

        if (pre.Responder)
        {
            MixHash(PreMessageKey(!initiator));
        }
    }

    private ReadOnlySpan<byte> PreMessageKey(bool own)
    {
        if (own)
        {
            return _hasS ? _sPub : throw new ArgumentException("this pattern needs a static private key for this role");
        }

        return _hasRs ? _rs : throw new ArgumentException("this pattern needs the peer's static public key");
    }

    /// <summary>True when every message has been written or read and the transport keys can be taken.</summary>
    public bool IsComplete => !_failed && _next == _messages.Length;

    /// <summary>True when the next call must be <see cref="WriteMessage"/> (otherwise <see cref="ReadMessage"/>).</summary>
    public bool MyTurnToWrite => (_next % 2 == 0) == _initiator;

    /// <summary>The handshake hash h; both sides hold the same value once complete, and it can bind channel-specific data.</summary>
    public ReadOnlySpan<byte> HandshakeHash => _h;

    /// <summary>The peer's static public key once known (from a pre-message or received), else empty.</summary>
    public ReadOnlySpan<byte> RemoteStatic => _hasRs ? _rs : default;

    /// <summary>Test hook: use this ephemeral private key for the next <c>e</c> token instead of a random one.</summary>
    internal void SetEphemeralForTest(ReadOnlySpan<byte> privateKey)
    {
        privateKey.CopyTo(_ePriv);
        _fixedEphemeral = true;
    }

    /// <summary>
    /// Writes the next handshake message (spec §5.3 WriteMessage) into <paramref name="output"/> and returns its length.
    /// Throws <see cref="InvalidOperationException"/> if it is not this side's turn or the handshake has failed, and
    /// <see cref="ArgumentException"/> if <paramref name="output"/> is too small or the message would pass 65535 bytes.
    /// </summary>
    public int WriteMessage(ReadOnlySpan<byte> payload, Span<byte> output)
    {
        if (_failed || _next >= _messages.Length || !MyTurnToWrite)
        {
            throw new InvalidOperationException("it is not this side's turn to write, or the handshake has ended");
        }

        var o = 0;
        foreach (var token in _messages[_next].Split(' '))
        {
            switch (token)
            {
                case "e":
                    if (!_fixedEphemeral)
                    {
                        Entropy.Fill(_ePriv);
                    }

                    _fixedEphemeral = false;
                    X25519.PublicKey(_ePriv, _ePub);
                    _hasE = true;
                    Need(output, o, DhLen);
                    _ePub.CopyTo(output[o..]);
                    o += DhLen;
                    MixHash(_ePub);
                    break;
                case "s":
                {
                    var n = DhLen + (_cipher.HasKey ? NoiseCipherState.TagSize : 0);
                    Need(output, o, n);
                    EncryptAndHash(_sPub, output.Slice(o, n));
                    o += n;
                    break;
                }

                default:
                    if (!MixDh(token))
                    {
                        _failed = true;
                        throw new InvalidOperationException("Diffie-Hellman produced a degenerate secret");
                    }

                    break;
            }
        }

        var m = payload.Length + (_cipher.HasKey ? NoiseCipherState.TagSize : 0);
        Need(output, o, m);
        EncryptAndHash(payload, output.Slice(o, m));
        o += m;
        if (o > MaxMessageSize)
        {
            _failed = true;
            throw new ArgumentException("a Noise message is at most 65535 bytes");
        }

        _next++;
        return o;
    }

    /// <summary>
    /// Reads the peer's next handshake message (spec §5.3 ReadMessage), writing its decrypted payload to
    /// <paramref name="payload"/> (which needs <c>message.Length</c> bytes at most). Returns false for anything malformed or
    /// forged; the handshake is then failed.
    /// </summary>
    public bool ReadMessage(ReadOnlySpan<byte> message, Span<byte> payload, out int payloadLength)
    {
        payloadLength = 0;
        if (_failed || _next >= _messages.Length || MyTurnToWrite)
        {
            throw new InvalidOperationException("it is not this side's turn to read, or the handshake has ended");
        }

        if (message.Length > MaxMessageSize)
        {
            _failed = true;
            return false;
        }

        var o = 0;
        foreach (var token in _messages[_next].Split(' '))
        {
            switch (token)
            {
                case "e":
                    if (message.Length - o < DhLen)
                    {
                        _failed = true;
                        return false;
                    }

                    message.Slice(o, DhLen).CopyTo(_re);
                    _hasRe = true;
                    o += DhLen;
                    MixHash(_re);
                    break;
                case "s":
                {
                    var n = DhLen + (_cipher.HasKey ? NoiseCipherState.TagSize : 0);
                    if (message.Length - o < n)
                    {
                        _failed = true;
                        return false;
                    }

                    if (!DecryptAndHash(message.Slice(o, n), _rs))
                    {
                        _failed = true;
                        return false;
                    }

                    _hasRs = true;
                    o += n;
                    break;
                }

                default:
                    if (!MixDh(token))
                    {
                        _failed = true;
                        return false;
                    }

                    break;
            }
        }

        var rest = message[o..];
        var plainLength = rest.Length - (_cipher.HasKey ? NoiseCipherState.TagSize : 0);
        if (plainLength < 0 || payload.Length < plainLength || !DecryptAndHash(rest, payload[..plainLength]))
        {
            _failed = true;
            return false;
        }

        payloadLength = plainLength;
        _next++;
        return true;
    }

    /// <summary>
    /// Split (spec §5.2): the two transport ciphers, <paramref name="send"/> for what this side sends and
    /// <paramref name="receive"/> for what it receives. Only valid once <see cref="IsComplete"/>; one-way patterns give a
    /// receive cipher that the initiator must not use, as the spec says.
    /// </summary>
    public void Split(out NoiseCipherState send, out NoiseCipherState receive)
    {
        if (!IsComplete)
        {
            throw new InvalidOperationException("the handshake is not complete");
        }

        Span<byte> k1 = stackalloc byte[HashLen];
        Span<byte> k2 = stackalloc byte[HashLen];
        Hkdf.Noise(_ck, default, k1, k2);
        var c1 = new NoiseCipherState();
        var c2 = new NoiseCipherState();
        c1.Initialize(k1);
        c2.Initialize(k2);
        send = _initiator ? c1 : c2;
        receive = _initiator ? c2 : c1;
        ChaCha20Poly1305.Wipe(k1);
        ChaCha20Poly1305.Wipe(k2);
        Destroy();
    }

    /// <summary>
    /// A copy of this handshake's state. A client reads the server's reply on a fork, so one forged datagram that fails to authenticate
    /// cannot kill the handshake the real reply is waiting for.
    /// </summary>
    internal NoiseHandshake Fork() => new(this);

    private NoiseHandshake(NoiseHandshake o)
    {
        _messages = o._messages;
        _initiator = o._initiator;
        o._h.CopyTo(_h, 0);
        o._ck.CopyTo(_ck, 0);
        o._sPriv.CopyTo(_sPriv, 0);
        o._sPub.CopyTo(_sPub, 0);
        o._ePriv.CopyTo(_ePriv, 0);
        o._ePub.CopyTo(_ePub, 0);
        o._rs.CopyTo(_rs, 0);
        o._re.CopyTo(_re, 0);
        (_hasS, _hasE, _hasRs, _hasRe, _fixedEphemeral, _next, _failed) = (o._hasS, o._hasE, o._hasRs, o._hasRe, o._fixedEphemeral, o._next, o._failed);
        _cipher.CopyStateFrom(o._cipher);
    }

    /// <summary>Wipes the keys. Called by <see cref="Split"/>; call it when abandoning a handshake.</summary>
    public void Destroy()
    {
        ChaCha20Poly1305.Wipe(_sPriv);
        ChaCha20Poly1305.Wipe(_ePriv);
        ChaCha20Poly1305.Wipe(_ck);
        _cipher.Clear();
        _failed = true;
    }

    private void Need(Span<byte> output, int offset, int count)
    {
        if (output.Length - offset < count)
        {
            // Earlier tokens already changed h and ck: this handshake cannot be retried with a bigger buffer.
            _failed = true;
            throw new ArgumentException("output is too small for the handshake message");
        }
    }

    private void MixHash(ReadOnlySpan<byte> data)
    {
        var s = Sha256.Create();
        s.Update(_h);
        s.Update(data);
        s.Final(_h);
    }

    private void MixKey(ReadOnlySpan<byte> input)
    {
        Span<byte> next = stackalloc byte[HashLen];
        Span<byte> k = stackalloc byte[HashLen];
        Hkdf.Noise(_ck, input, next, k);
        next.CopyTo(_ck);
        _cipher.Initialize(k);
        ChaCha20Poly1305.Wipe(k);
    }

    private void EncryptAndHash(ReadOnlySpan<byte> plaintext, Span<byte> output)
    {
        _cipher.Encrypt(_h, plaintext, output);
        MixHash(output);
    }

    private bool DecryptAndHash(ReadOnlySpan<byte> ciphertext, Span<byte> output)
    {
        if (!_cipher.Decrypt(_h, ciphertext, output))
        {
            return false;
        }

        MixHash(ciphertext);
        return true;
    }

    // Spec §5.3: es is DH(initiator e, responder s); se is DH(initiator s, responder e).
    private bool MixDh(string token)
    {
        var (mine, theirs) = token switch
        {
            "ee" => (Key(_ePriv, _hasE), Key(_re, _hasRe)),
            "ss" => (Key(_sPriv, _hasS), Key(_rs, _hasRs)),
            "es" => _initiator ? (Key(_ePriv, _hasE), Key(_rs, _hasRs)) : (Key(_sPriv, _hasS), Key(_re, _hasRe)),
            "se" => _initiator ? (Key(_sPriv, _hasS), Key(_re, _hasRe)) : (Key(_ePriv, _hasE), Key(_rs, _hasRs)),
            _ => throw new InvalidOperationException("unknown token " + token),
        };
        if (mine is null || theirs is null)
        {
            throw new InvalidOperationException("the pattern needs a key this side does not have");
        }

        Span<byte> secret = stackalloc byte[DhLen];
        var ok = X25519.Agree(mine, theirs, secret);
        if (ok)
        {
            MixKey(secret);
        }

        ChaCha20Poly1305.Wipe(secret);
        return ok;
    }

    private static byte[]? Key(byte[] key, bool present) => present ? key : null;

    private static bool RequiresOwnStatic(NoisePattern pattern, bool initiator)
    {
        var (pre, messages) = Describe(pattern);
        if (initiator ? pre.Initiator : pre.Responder)
        {
            return true;
        }

        for (var i = 0; i < messages.Length; i++)
        {
            if (((i % 2 == 0) == initiator) && Array.IndexOf(messages[i].Split(' '), "s") >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private readonly record struct Pre(bool Initiator, bool Responder);

    // Spec §7.2 and §7.4: the fundamental patterns as written in the specification.
    private static (Pre, string[]) Describe(NoisePattern p) => p switch
    {
        NoisePattern.N => (new Pre(false, true), ["e es"]),
        NoisePattern.K => (new Pre(true, true), ["e es ss"]),
        NoisePattern.X => (new Pre(false, true), ["e es s ss"]),
        NoisePattern.NN => (new Pre(false, false), ["e", "e ee"]),
        NoisePattern.KN => (new Pre(true, false), ["e", "e ee se"]),
        NoisePattern.NK => (new Pre(false, true), ["e es", "e ee"]),
        NoisePattern.KK => (new Pre(true, true), ["e es ss", "e ee se"]),
        NoisePattern.NX => (new Pre(false, false), ["e", "e ee s es"]),
        NoisePattern.KX => (new Pre(true, false), ["e", "e ee se s es"]),
        NoisePattern.XN => (new Pre(false, false), ["e", "e ee", "s se"]),
        NoisePattern.IN => (new Pre(false, false), ["e s", "e ee se"]),
        NoisePattern.XK => (new Pre(false, true), ["e es", "e ee", "s se"]),
        NoisePattern.IK => (new Pre(false, true), ["e es s ss", "e ee se"]),
        NoisePattern.XX => (new Pre(false, false), ["e", "e ee s es", "s se"]),
        NoisePattern.IX => (new Pre(false, false), ["e s", "e ee se s es"]),
        _ => throw new ArgumentOutOfRangeException(nameof(p)),
    };
}
