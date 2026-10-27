using OneGame.Sync;

namespace OneGame.Corpus;

/// <summary>
/// Wraps one end of a connection and copies the exact bytes it sends into the corpus. The server end sees snapshots and frames; the client
/// end of a link between servers sees the entity bundles one server hands another. Messages longer than the piece size travel as the pieces
/// the sync layer made, and each piece is kept as sent.
/// </summary>
internal sealed class Tap(IMessageChannel inner, Capture capture, bool serverEnd) : IMessageChannel
{
    // Message type bytes of the sync wire (docs/interfaces/sync.md, "Wire summary").
    private const byte TypeSnapshot = 16;
    private const byte TypeFrame = 17;
    private const byte TypePart = 23;
    private const byte TypeXferOffer = 32;
    private const byte TypeXferCommit = 33;

    private string? _pieceKind;

    public void Send(ReadOnlySpan<byte> message)
    {
        if (message.Length > 0)
        {
            var type = message[0];
            if (serverEnd)
            {
                if (type == TypePart)
                {
                    capture.Store(PieceKind(message), message);
                }
                else if (type == TypeFrame)
                {
                    capture.Store(Kinds.Frames, message);
                }
                else if (type == TypeSnapshot)
                {
                    capture.Store(Kinds.Snapshots, message);
                }
            }
            else if (type is TypeXferOffer or TypeXferCommit)
            {
                capture.Store(Kinds.Transfer, message);
            }
        }

        inner.Send(message);
    }

    public bool TryReceive(out ReadOnlySpan<byte> message) => inner.TryReceive(out message);

    // The first piece of a long message holds the start of the message, so its first byte says what the whole thing is.
    private string PieceKind(ReadOnlySpan<byte> piece)
    {
        var at = 1;
        Varint(piece, ref at);
        var offset = Varint(piece, ref at);
        if (offset == 0 && at < piece.Length)
        {
            _pieceKind = piece[at] == TypeSnapshot ? Kinds.Snapshots : Kinds.Frames;
        }

        return _pieceKind ?? Kinds.Frames;
    }

    private static ulong Varint(ReadOnlySpan<byte> data, ref int at)
    {
        ulong value = 0;
        for (var shift = 0; at < data.Length && shift < 64; shift += 7)
        {
            var b = data[at++];
            value |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                break;
            }
        }

        return value;
    }
}

/// <summary>Where a game's captured payloads go, and the tick and schema to file them under.</summary>
internal sealed class Capture(Sink sink, string game, Func<long> tick)
{
    /// <summary>The highest entity schema version seen in the game's worlds (refreshed by the run).</summary>
    public uint EntitySchema { get; set; } = 1;

    public string Game => game;

    public Sink Sink => sink;

    public long Tick => tick();

    /// <summary>Files a payload with the schema label for its kind.</summary>
    public void Store(string kind, ReadOnlySpan<byte> bytes, string? schema = null) =>
        sink.Add(game, kind, bytes, schema ?? Label(kind), tick());

    /// <summary>
    /// The schema label is the container format the codec writes, then the highest entity schema version in the game's worlds:
    /// <c>snap2.e1</c> for a view snapshot, <c>save1.e1</c>, <c>rec1.e1</c>. Frames and bundles carry the entity schema of each entity inside
    /// them and no container version, so they read <c>frame.e1</c> and <c>xfer.e1</c>.
    /// </summary>
    public string Label(string kind)
    {
        var container = kind switch
        {
            Kinds.Snapshots => "snap2",
            Kinds.Saves => "save1",
            Kinds.Recordings => "rec1",
            Kinds.Transfer => "xfer",
            _ => "frame",
        };
        return container + ".e" + EntitySchema.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public IMessageChannel ServerEnd(IMessageChannel inner) => new Tap(inner, this, true);

    public IMessageChannel ClientEnd(IMessageChannel inner) => new Tap(inner, this, false);
}
