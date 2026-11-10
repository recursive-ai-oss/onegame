namespace OneGame.Sync;

/// <summary>
/// A channel whose transport keeps a bounded send queue and can say how full it is. The node uses it to pace what it sends: a snapshot
/// larger than the queue is fed to the transport as it drains instead of being refused all at once.
/// </summary>
public interface IBoundedSendChannel : IMessageChannel
{
    /// <summary>Message bytes queued for sending and not yet acknowledged.</summary>
    long QueuedBytes { get; }

    /// <summary>The queue size at which the transport closes the connection with a send-queue-full reason.</summary>
    long QueueLimitBytes { get; }

    /// <summary>Closes the connection with the transport's send-queue-full reason, which the peer receives and both ends report.</summary>
    void FailSendQueue();
}

/// <summary>
/// Keeps ordering and pace for a bounded channel. Messages go straight through while the transport's queue is under half its limit; past
/// that they wait here, in order, and are fed in by <see cref="Drain"/> as acknowledgements free room. What waits is capped at the queue
/// limit, so a peer that never catches up still ends in <see cref="IBoundedSendChannel.FailSendQueue"/>, never in unbounded memory.
/// </summary>
internal sealed class PacedChannel(IBoundedSendChannel inner) : IMessageChannel
{
    private readonly Queue<byte[]> _outbox = new();
    private long _outboxBytes;
    private bool _failed;

    public void Send(ReadOnlySpan<byte> message)
    {
        if (_failed)
        {
            return;
        }

        if (_outbox.Count == 0 && Fits(message.Length))
        {
            inner.Send(message);
            return;
        }

        if (_outboxBytes + message.Length > inner.QueueLimitBytes)
        {
            _failed = true;
            _outbox.Clear();
            _outboxBytes = 0;
            inner.FailSendQueue();
            return;
        }

        _outbox.Enqueue(message.ToArray());
        _outboxBytes += message.Length;
    }

    public bool TryReceive(out ReadOnlySpan<byte> message) => inner.TryReceive(out message);

    /// <summary>Feeds waiting messages to the transport while its queue has room.</summary>
    public void Drain()
    {
        while (_outbox.Count > 0 && Fits(_outbox.Peek().Length))
        {
            var next = _outbox.Dequeue();
            _outboxBytes -= next.Length;
            inner.Send(next);
        }
    }

    // An empty transport queue always takes the next message, so one message larger than half the limit cannot wait forever.
    private bool Fits(int length) => inner.QueuedBytes == 0 || inner.QueuedBytes + length <= inner.QueueLimitBytes / 2;
}
