using System.Buffers;
using System.Buffers.Binary;

namespace OneGame.Net;

/// <summary>What happened to one incoming data packet.</summary>
internal enum PacketResult : byte
{
    /// <summary>Parsed and applied.</summary>
    Accepted,

    /// <summary>Malformed, duplicate, or too old; ignored.</summary>
    Dropped,

    /// <summary>Parsed, but the peer broke the protocol (for example an oversize message); the connection must close.</summary>
    Violation,
}

/// <summary>
/// The reliable-ordered and unreliable machinery of one UDP connection, with no socket in it: packets go in through
/// <see cref="OnPacket"/> and come out of <see cref="Build"/>. That keeps it testable and fuzzable on bytes alone.
/// </summary>
/// <remarks>
/// A message is cut into fragments that carry consecutive sequence numbers; the receiver delivers fragments in sequence order and
/// joins them. Every packet carries a packet number (to drop duplicates), a cumulative acknowledgement, a 64-bit selective
/// acknowledgement and the receiver's free window, so there are no separate ack packets unless nothing else is going out.
/// Loss is repaired by timeout or by three later fragments being acknowledged first; the congestion window follows slow start and
/// additive increase, and is cut once per round trip.
/// </remarks>
internal sealed class ReliableCore
{
    /// <summary>Bytes before the first frame: kind, connection id, packet number, ack base, ack bitmap, window.</summary>
    public const int HeaderSize = 23;

    /// <summary>Bytes of framing around a reliable fragment: frame byte, sequence, length.</summary>
    public const int RelOverhead = 7;

    /// <summary>Bytes of framing around an unreliable message.</summary>
    public const int UnrelOverhead = 3;

    private const uint InitialPeerLimit = 32;
    private const int PacketThreshold = 3;
    private const int MaxUnreliableOut = 64;
    private const int MaxUnreliableIn = 256;
    private const byte FlagBegin = 1;
    private const byte FlagEnd = 2;
    private const byte FrameReliable = 1;
    private const byte FrameUnreliable = 2;

    private readonly UdpOptions _o;
    private readonly int _maxFrag;
    private readonly int _slotMask;

    // Sending side.
    private SendFrag[] _ring = new SendFrag[64];
    private int _head;
    private int _count;
    private int _sentCount;
    private uint _baseSeq;
    private uint _nextSeq;
    private int _queuedBytes;
    private int _inFlight;
    private double _cwnd;
    private double _ssthresh = 1 << 30;
    private uint _peerLimit = InitialPeerLimit;
    private long _lastCongestion = long.MinValue / 2;
    private readonly Stack<SendFrag> _fragPool = new();

    // Round-trip estimate (RFC 6298 smoothing).
    private double _srtt;
    private double _rttvar;
    private bool _hasRtt;

    // Receiving side.
    private readonly Slot[] _slots;
    private uint _rcvBase;
    private uint _ackNext;
    private bool _ackPending;
    private byte[]? _asm;
    private int _asmLen;
    private bool _assembling;
    private readonly Queue<Msg> _inbox = new();
    private int _inboxBytes;
    private byte[]? _lent;

    // Packet numbers.
    private uint _txPkt;
    private uint _rxHighest;
    private ulong _rxMask;
    private bool _rxAny;
    private double _lossSeen;
    private double _lossTotal;

    // Unreliable messages.
    private readonly Queue<Msg> _unrelOut = new();
    private readonly Queue<Msg> _unrelIn = new();
    private byte[]? _lentUnrel;

    private long _packetsSent;
    private long _packetsReceived;
    private long _packetsDropped;
    private long _bytesSent;
    private long _bytesReceived;
    private long _messagesSent;
    private long _messagesReceived;
    private long _retransmits;
    private long _fragsSent;

    public ReliableCore(UdpOptions options, long now)
    {
        _o = options;
        _maxFrag = options.Mtu - HeaderSize - RelOverhead;
        _slots = new Slot[options.ReceiveWindow];
        _slotMask = options.ReceiveWindow - 1;
        _cwnd = (double)options.MinCongestionWindowPackets * options.Mtu;
        LastSend = now;
        LastReceive = now;
    }

    /// <summary>Largest unreliable message.</summary>
    public int MaxUnreliableBytes => _o.Mtu - HeaderSize - UnrelOverhead;

    /// <summary>When the last datagram went out, for keepalive.</summary>
    public long LastSend { get; private set; }

    /// <summary>When the last valid datagram arrived, for the timeout.</summary>
    public long LastReceive { get; private set; }

    /// <summary>The peer broke the protocol while delivering buffered data; the connection must close.</summary>
    public bool PendingViolation { get; private set; }

    /// <summary>Restarts the keepalive and timeout clocks, when the handshake finishes.</summary>
    public void Touch(long now)
    {
        LastSend = now;
        LastReceive = now;
    }

    /// <summary>All queued messages have been acknowledged.</summary>
    public bool Drained => _count == 0;

    /// <summary>Queues a message. Returns false if the send queue is full.</summary>
    /// <exception cref="MessageTooLargeException">The message exceeds the maximum.</exception>
    public bool Enqueue(ReadOnlySpan<byte> message)
    {
        if (message.Length > _o.MaxMessageBytes)
        {
            throw new MessageTooLargeException(message.Length, _o.MaxMessageBytes);
        }

        if ((long)_queuedBytes + message.Length > _o.MaxSendQueueBytes)
        {
            return false;
        }

        var offset = 0;
        do
        {
            var take = Math.Min(_maxFrag, message.Length - offset);
            var f = _fragPool.Count > 0 ? _fragPool.Pop() : new SendFrag();
            f.Buf = NetBuffers.Rent(Math.Max(1, take));
            message.Slice(offset, take).CopyTo(f.Buf);
            f.Len = take;
            f.Flags = (byte)((offset == 0 ? FlagBegin : 0) | (offset + take == message.Length ? FlagEnd : 0));
            f.Seq = _nextSeq++;
            f.Sends = 0;
            f.Acked = false;
            f.Lost = false;
            f.InFlight = false;
            if (_count == _ring.Length)
            {
                GrowRing();
            }

            _ring[(_head + _count) & (_ring.Length - 1)] = f;
            _count++;
            _queuedBytes += take;
            offset += take;
        }
        while (offset < message.Length);

        _messagesSent++;
        return true;
    }

    /// <summary>Queues an unreliable message for the next packet. The oldest is dropped if too many wait.</summary>
    /// <exception cref="MessageTooLargeException">The message does not fit one packet.</exception>
    public void EnqueueUnreliable(ReadOnlySpan<byte> message)
    {
        if (message.Length > MaxUnreliableBytes)
        {
            throw new MessageTooLargeException(message.Length, MaxUnreliableBytes);
        }

        if (_unrelOut.Count >= MaxUnreliableOut)
        {
            NetBuffers.Return(_unrelOut.Dequeue().Buf);
        }

        var buf = NetBuffers.Rent(Math.Max(1, message.Length));
        message.CopyTo(buf);
        _unrelOut.Enqueue(new Msg(buf, message.Length));
        _messagesSent++;
    }

    /// <summary>
    /// Writes the next datagram into <paramref name="dst"/> (at most the MTU) and returns its length, or 0 if there is nothing to send.
    /// </summary>
    public int Build(Span<byte> dst, uint connectionId, long now)
    {
        var limit = Math.Min(dst.Length, _o.Mtu);
        var pos = HeaderSize;
        var frames = 0;

        // Repeats first: they unblock the receiver.
        for (var i = 0; i < _sentCount; i++)
        {
            var f = At(i);
            if (f.Acked)
            {
                continue;
            }

            var timedOut = !f.Lost && now - f.SentAt >= Rto(f.Sends);
            if (!f.Lost && !timedOut)
            {
                continue;
            }

            var need = RelOverhead + f.Len;
            if (pos + need > limit)
            {
                break;
            }

            if (timedOut)
            {
                OnLoss(now);
            }

            if (!f.InFlight)
            {
                f.InFlight = true;
                _inFlight += need;
            }

            WriteReliable(dst, ref pos, f);
            f.SentAt = now;
            f.Pkt = _txPkt + 1;
            f.Sends++;
            f.Lost = false;
            _retransmits++;
            _fragsSent++;
            frames++;
        }

        // Then new data, as far as the receiver's window and the congestion window allow.
        while (_sentCount < _count)
        {
            var f = At(_sentCount);
            var need = RelOverhead + f.Len;
            if (pos + need > limit || (int)(f.Seq - _peerLimit) >= 0 || (_inFlight > 0 && _inFlight + need > (int)_cwnd))
            {
                break;
            }

            WriteReliable(dst, ref pos, f);
            f.SentAt = now;
            f.Pkt = _txPkt + 1;
            f.Sends = 1;
            f.InFlight = true;
            _inFlight += need;
            _sentCount++;
            _fragsSent++;
            frames++;
        }

        while (_unrelOut.Count > 0 && pos + UnrelOverhead + _unrelOut.Peek().Len <= limit)
        {
            var m = _unrelOut.Dequeue();
            dst[pos++] = FrameUnreliable;
            BinaryPrimitives.WriteUInt16LittleEndian(dst[pos..], (ushort)m.Len);
            pos += 2;
            m.Buf.AsSpan(0, m.Len).CopyTo(dst[pos..]);
            pos += m.Len;
            NetBuffers.Return(m.Buf);
            frames++;
        }

        if (frames == 0 && !_ackPending && now - LastSend < _o.KeepAliveMs)
        {
            return 0;
        }

        // The header comes last because the acknowledgement fields are the freshest it can be.
        dst[0] = Wire.Header(Wire.KindData);
        BinaryPrimitives.WriteUInt32LittleEndian(dst[1..], connectionId);
        BinaryPrimitives.WriteUInt32LittleEndian(dst[5..], ++_txPkt);
        BinaryPrimitives.WriteUInt32LittleEndian(dst[9..], _ackNext);
        BinaryPrimitives.WriteUInt64LittleEndian(dst[13..], BuildSack());
        BinaryPrimitives.WriteUInt16LittleEndian(dst[21..], (ushort)(_rcvBase + (uint)_slots.Length - _ackNext));
        _ackPending = false;
        LastSend = now;
        _packetsSent++;
        _bytesSent += pos;
        return pos;
    }

    /// <summary>Processes one data datagram (the whole datagram, starting at the kind byte).</summary>
    public PacketResult OnPacket(ReadOnlySpan<byte> p, long now)
    {
        if (p.Length < HeaderSize || !ValidFrames(p))
        {
            _packetsDropped++;
            return PacketResult.Dropped;
        }

        // Duplicate and stale datagrams (including replays) are dropped before they can refresh the timeout or the acknowledgements.
        var pkt = BinaryPrimitives.ReadUInt32LittleEndian(p[5..]);
        if (_rxAny)
        {
            var d = (int)(pkt - _rxHighest);
            if (d <= 0)
            {
                var back = -(long)d;
                if (back > 63 || (_rxMask & (1UL << (int)back)) != 0)
                {
                    _packetsDropped++;
                    return PacketResult.Dropped;
                }

                _rxMask |= 1UL << (int)back;
                if (_lossSeen > 0)
                {
                    _lossSeen--; // it was counted missing; it was only late
                }
            }
            else
            {
                var gap = Math.Min(d - 1, 1000);
                _lossSeen += gap;
                _rxMask = d >= 64 ? 1UL : (_rxMask << d) | 1UL;
                _rxHighest = pkt;
            }
        }
        else
        {
            _rxAny = true;
            _rxHighest = pkt;
            _rxMask = 1UL;
        }

        _lossTotal++;
        if (_lossTotal > 4096)
        {
            _lossTotal /= 2;
            _lossSeen /= 2;
        }

        _packetsReceived++;
        _bytesReceived += p.Length;
        LastReceive = now;

        OnAck(BinaryPrimitives.ReadUInt32LittleEndian(p[9..]), BinaryPrimitives.ReadUInt64LittleEndian(p[13..]), BinaryPrimitives.ReadUInt16LittleEndian(p[21..]), now);

        var pos = HeaderSize;
        while (pos < p.Length && p[pos] != 0)
        {
            var kind = p[pos] & 3;
            var flags = (byte)((p[pos] >> 2) & 3);
            if (kind == FrameReliable)
            {
                var seq = BinaryPrimitives.ReadUInt32LittleEndian(p[(pos + 1)..]);
                var len = BinaryPrimitives.ReadUInt16LittleEndian(p[(pos + 5)..]);
                StoreFragment(seq, flags, p.Slice(pos + RelOverhead, len));
                pos += RelOverhead + len;
            }
            else
            {
                var len = BinaryPrimitives.ReadUInt16LittleEndian(p[(pos + 1)..]);
                StoreUnreliable(p.Slice(pos + UnrelOverhead, len));
                pos += UnrelOverhead + len;
            }
        }

        Deliver();
        return PendingViolation ? PacketResult.Violation : PacketResult.Accepted;
    }

    /// <summary>Takes the next complete message. The span is valid until the next call.</summary>
    public bool TryReceive(out ReadOnlySpan<byte> message)
    {
        if (_lent is not null)
        {
            NetBuffers.Return(_lent);
            _lent = null;
        }

        if (!_inbox.TryDequeue(out var m))
        {
            message = default;
            return false;
        }

        _inboxBytes -= m.Len;
        _lent = m.Buf;
        message = m.Buf.AsSpan(0, m.Len);
        Deliver(); // the queue has room again; pull more from the reorder window
        return true;
    }

    /// <summary>Takes the next unreliable message. The span is valid until the next call.</summary>
    public bool TryReceiveUnreliable(out ReadOnlySpan<byte> message)
    {
        if (_lentUnrel is not null)
        {
            NetBuffers.Return(_lentUnrel);
            _lentUnrel = null;
        }

        if (!_unrelIn.TryDequeue(out var m))
        {
            message = default;
            return false;
        }

        _lentUnrel = m.Buf;
        message = m.Buf.AsSpan(0, m.Len);
        return true;
    }

    /// <summary>Copies the counters.</summary>
    public void FillStats(ref NetStats s)
    {
        s.RttMs = _hasRtt ? _srtt : 0;
        s.RttVarMs = _hasRtt ? _rttvar : 0;
        s.PacketsSent = _packetsSent;
        s.PacketsReceived = _packetsReceived;
        s.PacketsDropped = _packetsDropped;
        s.BytesSent = _bytesSent;
        s.BytesReceived = _bytesReceived;
        s.MessagesSent = _messagesSent;
        s.MessagesReceived = _messagesReceived;
        s.Retransmits = _retransmits;
        s.InboundLoss = _lossTotal + _lossSeen > 0 ? _lossSeen / (_lossTotal + _lossSeen) : 0;
        s.OutboundLoss = _fragsSent > 0 ? (double)_retransmits / _fragsSent : 0;
        s.SendQueueBytes = _queuedBytes;
        s.InFlightBytes = _inFlight;
        s.CongestionWindowBytes = (int)Math.Min(_cwnd, int.MaxValue);
    }

    /// <summary>Returns every rented buffer.</summary>
    public void Release()
    {
        for (var i = 0; i < _count; i++)
        {
            NetBuffers.Return(At(i).Buf);
        }

        _count = 0;
        _sentCount = 0;
        foreach (ref var s in _slots.AsSpan())
        {
            if (s.Buf is not null)
            {
                NetBuffers.Return(s.Buf);
                s.Buf = null;
            }
        }

        ReturnAll(_inbox);
        ReturnAll(_unrelIn);
        ReturnAll(_unrelOut);
        _inboxBytes = 0;
        foreach (var b in new[] { _asm, _lent, _lentUnrel })
        {
            if (b is not null)
            {
                NetBuffers.Return(b);
            }
        }

        _asm = null;
        _lent = null;
        _lentUnrel = null;
        PendingViolation = true;
    }

    private static void ReturnAll(Queue<Msg> q)
    {
        while (q.TryDequeue(out var m))
        {
            NetBuffers.Return(m.Buf);
        }
    }

    // Walks the frames without applying anything, so a malformed datagram changes no state.
    private bool ValidFrames(ReadOnlySpan<byte> p)
    {
        var pos = HeaderSize;
        while (pos < p.Length)
        {
            var fb = p[pos];
            if (fb == 0)
            {
                return true;
            }

            if ((fb & 0xF0) != 0)
            {
                return false;
            }

            var kind = fb & 3;
            if (kind == FrameReliable)
            {
                if (pos + RelOverhead > p.Length)
                {
                    return false;
                }

                var len = BinaryPrimitives.ReadUInt16LittleEndian(p[(pos + 5)..]);
                if (len > _maxFrag || pos + RelOverhead + len > p.Length)
                {
                    return false;
                }

                pos += RelOverhead + len;
            }
            else if (kind == FrameUnreliable)
            {
                if (pos + UnrelOverhead > p.Length)
                {
                    return false;
                }

                var len = BinaryPrimitives.ReadUInt16LittleEndian(p[(pos + 1)..]);
                if (len > MaxUnreliableBytes || pos + UnrelOverhead + len > p.Length)
                {
                    return false;
                }

                pos += UnrelOverhead + len;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    private void StoreUnreliable(ReadOnlySpan<byte> data)
    {
        if (_unrelIn.Count >= MaxUnreliableIn)
        {
            NetBuffers.Return(_unrelIn.Dequeue().Buf);
        }

        var buf = NetBuffers.Rent(Math.Max(1, data.Length));
        data.CopyTo(buf);
        _unrelIn.Enqueue(new Msg(buf, data.Length));
    }

    private void StoreFragment(uint seq, byte flags, ReadOnlySpan<byte> data)
    {
        _ackPending = true; // even a repeat is acknowledged again, so the sender stops repeating it
        var diff = (int)(seq - _rcvBase);
        if (diff < 0 || diff >= _slots.Length)
        {
            return;
        }

        ref var slot = ref _slots[seq & (uint)_slotMask];
        if (slot.Buf is not null)
        {
            return;
        }

        slot.Buf = NetBuffers.Rent(Math.Max(1, data.Length));
        data.CopyTo(slot.Buf);
        slot.Len = data.Length;
        slot.Flags = flags;
        while ((int)(_ackNext - _rcvBase) < _slots.Length && _slots[_ackNext & (uint)_slotMask].Buf is not null)
        {
            _ackNext++;
        }
    }

    // Moves fragments from the reorder window, in sequence, into messages.
    private void Deliver()
    {
        var freed = false;
        while (!PendingViolation && _inboxBytes < _o.MaxReceiveQueueBytes)
        {
            ref var slot = ref _slots[_rcvBase & (uint)_slotMask];
            if (slot.Buf is null)
            {
                break;
            }

            var begin = (slot.Flags & FlagBegin) != 0;
            var end = (slot.Flags & FlagEnd) != 0;
            var data = slot.Buf.AsSpan(0, slot.Len);
            if (begin == _assembling || (long)_asmLen + data.Length > _o.MaxMessageBytes)
            {
                // A new message began before the last one ended, a continuation had no start, or the message outgrew the limit.
                PendingViolation = true;
                break;
            }

            if (begin && end)
            {
                var buf = NetBuffers.Rent(Math.Max(1, data.Length));
                data.CopyTo(buf);
                Complete(buf, data.Length);
            }
            else
            {
                _assembling = !end;
                EnsureAssembly(_asmLen + data.Length);
                data.CopyTo(_asm.AsSpan(_asmLen));
                _asmLen += data.Length;
                if (end)
                {
                    Complete(_asm!, _asmLen);
                    _asm = null;
                    _asmLen = 0;
                }
            }

            NetBuffers.Return(slot.Buf);
            slot.Buf = null;
            _rcvBase++;
            freed = true;
        }

        if (freed)
        {
            _ackPending = true;
        }
    }

    private void Complete(byte[] buf, int length)
    {
        _inbox.Enqueue(new Msg(buf, length));
        _inboxBytes += length;
        _messagesReceived++;
    }

    private void EnsureAssembly(int needed)
    {
        if (_asm is null)
        {
            _asm = NetBuffers.Rent(Math.Max(needed, 2048));
        }
        else if (_asm.Length < needed)
        {
            var bigger = NetBuffers.Rent((int)Math.Min(Math.Max(needed, (long)_asm.Length * 2), _o.MaxMessageBytes + 2048L));
            _asm.AsSpan(0, _asmLen).CopyTo(bigger);
            NetBuffers.Return(_asm);
            _asm = bigger;
        }
    }

    private ulong BuildSack()
    {
        ulong sack = 0;
        for (var b = 0; b < 64; b++)
        {
            var seq = _ackNext + 1 + (uint)b;
            if ((int)(seq - _rcvBase) >= _slots.Length)
            {
                break;
            }

            if (_slots[seq & (uint)_slotMask].Buf is not null)
            {
                sack |= 1UL << b;
            }
        }

        return sack;
    }

    private void OnAck(uint ackBase, ulong sack, ushort window, long now)
    {
        var adv = (int)(ackBase - _baseSeq);
        if (adv < 0 || adv > _sentCount)
        {
            return; // old news, or an acknowledgement of data never sent
        }

        _peerLimit = ackBase + window;
        var highest = adv - 1;
        for (var i = 0; i < adv; i++)
        {
            AckFragment(At(i), now);
        }

        for (var b = 0; b < 64 && sack >> b != 0; b++)
        {
            var idx = adv + 1 + b;
            if ((sack >> b & 1) != 0 && idx < _sentCount)
            {
                AckFragment(At(idx), now);
                highest = idx;
            }
        }

        // A fragment is presumed lost once one sent three or more datagrams after it has been acknowledged; it is sent again without
        // waiting for the timer. Counting datagrams, not fragments, keeps one reordered datagram full of small messages from looking like a loss.
        if (highest >= 0)
        {
            var g = At(highest);
            for (var i = 0; i < highest; i++)
            {
                var f = At(i);
                if (!f.Acked && !f.Lost && (int)(g.Pkt - f.Pkt) >= PacketThreshold)
                {
                    f.Lost = true;
                    if (f.InFlight)
                    {
                        f.InFlight = false;
                        _inFlight -= RelOverhead + f.Len;
                    }

                    OnLoss(now);
                }
            }
        }

        while (_count > 0 && At(0).Acked)
        {
            var f = At(0);
            _queuedBytes -= f.Len;
            NetBuffers.Return(f.Buf);
            f.Buf = [];
            _fragPool.Push(f);
            _ring[_head] = null!;
            _head = (_head + 1) & (_ring.Length - 1);
            _count--;
            _sentCount--;
            _baseSeq++;
        }
    }

    private void AckFragment(SendFrag f, long now)
    {
        if (f.Acked)
        {
            return;
        }

        f.Acked = true;
        var bytes = RelOverhead + f.Len;
        if (f.InFlight)
        {
            f.InFlight = false;
            _inFlight -= bytes;
        }

        if (f.Sends == 1)
        {
            SampleRtt(now - f.SentAt);
        }

        _cwnd = Math.Min(_cwnd + (_cwnd < _ssthresh ? bytes : _o.Mtu * (double)bytes / _cwnd), 64 << 20);
    }

    private void OnLoss(long now)
    {
        if (now - _lastCongestion < Math.Max(_srtt, 10))
        {
            return; // one reduction per round trip
        }

        _lastCongestion = now;
        // Back off by 30% (a gentler cut than halving, as loss here is mostly not congestion), never below the configured floor.
        _ssthresh = Math.Max(_cwnd * 0.7, (double)_o.MinCongestionWindowPackets * _o.Mtu);
        _cwnd = _ssthresh;
    }

    private void SampleRtt(double r)
    {
        if (!_hasRtt)
        {
            _srtt = r;
            _rttvar = r / 2;
            _hasRtt = true;
        }
        else
        {
            _rttvar = (0.75 * _rttvar) + (0.25 * Math.Abs(_srtt - r));
            _srtt = (0.875 * _srtt) + (0.125 * r);
        }
    }

    // RFC 6298 §2: srtt + 4 * rttvar, doubled for each repeat of the same fragment.
    private double Rto(int sends)
    {
        var b = _hasRtt ? _srtt + Math.Max(10, 4 * _rttvar) : 300;
        b = Math.Clamp(b, _o.MinRtoMs, _o.MaxRtoMs);
        return Math.Min(b * (1 << Math.Min(Math.Max(sends - 1, 0), 5)), 8000);
    }

    private static void WriteReliable(Span<byte> dst, ref int pos, SendFrag f)
    {
        dst[pos] = (byte)(FrameReliable | (f.Flags << 2));
        BinaryPrimitives.WriteUInt32LittleEndian(dst[(pos + 1)..], f.Seq);
        BinaryPrimitives.WriteUInt16LittleEndian(dst[(pos + 5)..], (ushort)f.Len);
        f.Buf.AsSpan(0, f.Len).CopyTo(dst[(pos + RelOverhead)..]);
        pos += RelOverhead + f.Len;
    }

    private SendFrag At(int i) => _ring[(_head + i) & (_ring.Length - 1)];

    private void GrowRing()
    {
        var bigger = new SendFrag[_ring.Length * 2];
        for (var i = 0; i < _count; i++)
        {
            bigger[i] = At(i);
        }

        _ring = bigger;
        _head = 0;
    }

    private sealed class SendFrag
    {
        public byte[] Buf = [];
        public int Len;
        public byte Flags;
        public uint Seq;
        public long SentAt;
        public uint Pkt;
        public int Sends;
        public bool Acked;
        public bool Lost;
        public bool InFlight;
    }

    private struct Slot
    {
        public byte[]? Buf;
        public int Len;
        public byte Flags;
    }

    private readonly record struct Msg(byte[] Buf, int Len);
}
