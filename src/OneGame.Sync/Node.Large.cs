using System.Globalization;

namespace OneGame.Sync;

public sealed partial class Node
{
    /// <summary>
    /// Sends a message to a subscriber, in pieces when it is longer than <see cref="ClusterOptions.PieceBytes"/>. Transports cap one message
    /// (1 MiB by default); a big snapshot or a frame that adds thousands of entities must not hit that cap or fail inside a tick. The channel
    /// is ordered and a message is sent in one call, so the pieces of one message are never interleaved with anything else.
    /// </summary>
    internal void SendMessage(IMessageChannel channel, ReadOnlySpan<byte> message)
    {
        var piece = Math.Max(1024, Options.PieceBytes);
        if (message.Length <= piece)
        {
            channel.Send(message);
            return;
        }

        for (var offset = 0; offset < message.Length; offset += piece)
        {
            var length = Math.Min(piece, message.Length - offset);
            _pieceOut.Reset();
            _pieceOut.WriteByte(Msg.Part);
            _pieceOut.WriteVarUInt((ulong)message.Length);
            _pieceOut.WriteVarUInt((ulong)offset);
            _pieceOut.WriteRaw(message.Slice(offset, length));
            channel.Send(_pieceOut.Written);
        }
    }

    private readonly SyncWriter _pieceOut = new(1 << 12);

    // The authority splits long messages with SendMessage. The limit is checked from the first piece's header, before the buffer exists, and
    // every later piece must continue exactly where the last stopped, so a hostile or broken peer can neither make us allocate more than
    // MaxAssembledBytes per link nor leave a half-built message that could be mistaken for another.
    private void HandlePart(Link link, ReadOnlySpan<byte> message)
    {
        var r = new SyncReader(message);
        r.ReadByte();
        var total = r.ReadVarUInt();
        var offset = r.ReadVarUInt();
        if (!r.Ok || total == 0 || total > (ulong)Options.MaxAssembledBytes || offset >= total)
        {
            PartFault(link, "bad message piece header (limit " + Options.MaxAssembledBytes.ToString(CultureInfo.InvariantCulture) + " bytes)");
            return;
        }

        var data = message[r.Position..];
        if (offset == 0)
        {
            link.Assembly = new byte[(int)total];
            link.Assembled = 0;
        }
        else if (link.Assembly is null || (ulong)link.Assembly.Length != total || (ulong)link.Assembled != offset)
        {
            PartFault(link, "message piece out of order");
            return;
        }

        if (data.Length == 0 || (ulong)data.Length > total - offset)
        {
            PartFault(link, "message piece longer than its message");
            return;
        }

        data.CopyTo(link.Assembly.AsSpan(link.Assembled));
        link.Assembled += data.Length;
        if (link.Assembled < link.Assembly.Length)
        {
            return;
        }

        var whole = link.Assembly;
        link.Assembly = null;
        link.Assembled = 0;
        if (whole[0] == Msg.Part)
        {
            PartFault(link, "message piece inside a message piece");
            return;
        }

        HandleClientMessage(link, whole);
    }

    private void PartFault(Link link, string reason)
    {
        link.Assembly = null;
        link.Assembled = 0;
        ReportProtocolError(default, 0, reason);

        // What was being sent is lost: ask for fresh snapshots (bounded: a replica gives up after three failures in a row).
        foreach (var replica in _replicas.ToArray())
        {
            if (replica.Link == link)
            {
                replica.LinkFault(reason);
            }
        }
    }
}
