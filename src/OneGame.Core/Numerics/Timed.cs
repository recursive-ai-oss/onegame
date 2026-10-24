namespace OneGame.Core;

/// <summary>
/// A value that moves from one value to another over a span of ticks. The authority writes it once; every node evaluates
/// <see cref="ValueAt"/> for the tick it is showing, so nothing is sent per tick. Evaluation is integer-only and identical everywhere.
/// </summary>
/// <typeparam name="T">The value type: <see cref="Fixed"/>, <see cref="FVec2"/> or another interpolatable synced value.</typeparam>
public readonly struct Timed<T> : ISyncValue<Timed<T>>
    where T : ISyncValue<T>, ILerp<T>
{
    /// <summary>Creates a timed value.</summary>
    /// <param name="from">The value at <paramref name="startTick"/> and before.</param>
    /// <param name="to">The value at <paramref name="endTick"/> and after.</param>
    /// <param name="startTick">The tick the move starts.</param>
    /// <param name="endTick">The tick the move ends.</param>
    public Timed(T from, T to, long startTick, long endTick)
    {
        From = from;
        To = to;
        StartTick = startTick;
        EndTick = endTick;
    }

    /// <summary>The start value.</summary>
    public T From { get; }

    /// <summary>The end value.</summary>
    public T To { get; }

    /// <summary>The tick the move starts.</summary>
    public long StartTick { get; }

    /// <summary>The tick the move ends.</summary>
    public long EndTick { get; }

    /// <summary>The value at <paramref name="tick"/>: <see cref="From"/> before the start, <see cref="To"/> from the end, a linear blend between.</summary>
    public T ValueAt(long tick)
    {
        if (tick <= StartTick || EndTick <= StartTick)
        {
            return tick >= EndTick ? To : From;
        }

        if (tick >= EndTick)
        {
            return To;
        }

        return T.Lerp(From, To, Fixed.FromRatio(tick - StartTick, EndTick - StartTick));
    }

    /// <inheritdoc />
    public bool Equals(Timed<T> other)
        => EqualityComparer<T>.Default.Equals(From, other.From) && EqualityComparer<T>.Default.Equals(To, other.To)
            && StartTick == other.StartTick && EndTick == other.EndTick;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Timed<T> t && Equals(t);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(From, To, StartTick, EndTick);

    /// <summary>Equality of all four parts.</summary>
    public static bool operator ==(Timed<T> a, Timed<T> b) => a.Equals(b);

    /// <summary>Inequality of any part.</summary>
    public static bool operator !=(Timed<T> a, Timed<T> b) => !a.Equals(b);

    /// <inheritdoc />
    public void Write(SyncWriter writer)
    {
        From.Write(writer);
        To.Write(writer);
        writer.WriteVarInt(StartTick);
        writer.WriteVarInt(EndTick);
    }

    static Timed<T> ISyncValue<Timed<T>>.Read(ref SyncReader reader)
    {
        var from = T.Read(ref reader);
        var to = T.Read(ref reader);
        var start = reader.ReadVarInt();
        var end = reader.ReadVarInt();
        return new Timed<T>(from, to, start, end);
    }

    /// <inheritdoc />
    public void WriteJson(JsonOut json)
    {
        json.BeginObject();
        json.Name("from");
        From.WriteJson(json);
        json.Name("to");
        To.WriteJson(json);
        json.Name("t0");
        json.Value(StartTick);
        json.Name("t1");
        json.Value(EndTick);
        json.EndObject();
    }
}
