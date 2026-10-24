using System.Buffers.Binary;

namespace OneGame.Sync;

/// <summary>
/// One end of a message pipe between two nodes. Messages are whole, ordered and reliable; a real transport fragments, retransmits
/// and reassembles underneath this. Everything a node sends is already bytes, so the loopback and a network carry the same thing.
/// </summary>
public interface IMessageChannel
{
    /// <summary>Queues a message for the other end.</summary>
    /// <exception cref="ArgumentException">The message is larger than <see cref="Loopback.MaxMessageBytes"/>.</exception>
    void Send(ReadOnlySpan<byte> message);

    /// <summary>Takes the next message, or returns false if none is waiting. The span is valid until the next call to this method.</summary>
    bool TryReceive(out ReadOnlySpan<byte> message);
}

/// <summary>In-process transport: two channel ends joined by byte queues that frame messages with a length prefix, so receivers see exactly the bytes a socket would deliver.</summary>
public static class Loopback
{
    /// <summary>Largest message a channel carries.</summary>
    public const int MaxMessageBytes = 256 * 1024 * 1024;

    /// <summary>Creates two connected ends: what one sends, the other receives.</summary>
    public static (IMessageChannel A, IMessageChannel B) CreatePair()
    {
        var ab = new Queue();
        var ba = new Queue();
        return (new End(ba, ab), new End(ab, ba));
    }

    private sealed class Queue
    {
        private byte[] _buffer = new byte[4096];
        private int _head;
        private int _tail;

        public void Write(ReadOnlySpan<byte> message)
        {
            if (message.Length > MaxMessageBytes)
            {
                throw new ArgumentException($"A message of {message.Length} bytes exceeds the {MaxMessageBytes} byte limit.", nameof(message));
            }

            if (_head == _tail)
            {
                _head = _tail = 0;
            }

            var need = checked(_tail + 4 + message.Length);
            if (need > _buffer.Length)
            {
                // Reclaim consumed space first; grow only if the live data still does not fit.
                if (_head > 0)
                {
                    Buffer.BlockCopy(_buffer, _head, _buffer, 0, _tail - _head);
                    _tail -= _head;
                    _head = 0;
                    need = checked(_tail + 4 + message.Length);
                }

                if (need > _buffer.Length)
                {
                    Array.Resize(ref _buffer, (int)Math.Min(Math.Max((long)need, _buffer.Length * 2L), int.MaxValue));
                }
            }

            BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(_tail), message.Length);
            message.CopyTo(_buffer.AsSpan(_tail + 4));
            _tail += 4 + message.Length;
        }

        public bool TryRead(out ReadOnlySpan<byte> message)
        {
            if (_tail - _head < 4)
            {
                message = default;
                return false;
            }

            var length = BinaryPrimitives.ReadInt32LittleEndian(_buffer.AsSpan(_head));
            message = _buffer.AsSpan(_head + 4, length);
            _head += 4 + length;
            return true;
        }
    }

    private sealed class End(Queue incoming, Queue outgoing) : IMessageChannel
    {
        public void Send(ReadOnlySpan<byte> message) => outgoing.Write(message);

        public bool TryReceive(out ReadOnlySpan<byte> message) => incoming.TryRead(out message);
    }
}
