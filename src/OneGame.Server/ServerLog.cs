namespace OneGame.Server;

/// <summary>One line of the server log.</summary>
/// <param name="Sequence">1, 2, 3... in the order written.</param>
/// <param name="Tick">The server tick when it was written.</param>
/// <param name="Level"><c>info</c>, <c>warn</c> or <c>error</c>.</param>
/// <param name="Message">The text.</param>
public sealed record LogEntry(long Sequence, long Tick, string Level, string Message);

/// <summary>
/// The server log: lines go to a writer (standard error for the runner) and the last few hundred are kept for the control surface.
/// Entries hold only text, never objects, so a plugin's types are not kept alive by its own log lines.
/// </summary>
public sealed class ServerLog
{
    private const int Capacity = 512;

    private readonly LogEntry[] _ring = new LogEntry[Capacity];
    private readonly TextWriter? _sink;
    private long _count;

    /// <summary>Creates a log writing to <paramref name="sink"/>, or only keeping entries when it is null.</summary>
    /// <param name="sink">Where lines are written, such as <c>Console.Error</c>.</param>
    public ServerLog(TextWriter? sink = null)
    {
        _sink = sink;
    }

    /// <summary>The tick stamped on new entries; the host keeps it current.</summary>
    public long Tick { get; set; }

    /// <summary>Entries written so far.</summary>
    public long Count => _count;

    /// <summary>Writes an informational line.</summary>
    /// <param name="message">The text.</param>
    public void Info(string message) => Write("info", message);

    /// <summary>Writes a warning.</summary>
    /// <param name="message">The text.</param>
    public void Warn(string message) => Write("warn", message);

    /// <summary>Writes an error.</summary>
    /// <param name="message">The text.</param>
    public void Error(string message) => Write("error", message);

    /// <summary>The last <paramref name="max"/> entries, oldest first.</summary>
    /// <param name="max">At most this many (up to the 512 kept).</param>
    public IReadOnlyList<LogEntry> Tail(int max = 50)
    {
        var n = (int)Math.Min(Math.Min(_count, Capacity), Math.Max(max, 0));
        var list = new List<LogEntry>(n);
        for (var i = 0; i < n; i++)
        {
            list.Add(_ring[(_count - n + i) % Capacity]);
        }

        return list;
    }

    private void Write(string level, string message)
    {
        var entry = new LogEntry(_count + 1, Tick, level, message);
        _ring[_count % Capacity] = entry;
        _count++;
        if (_sink is not null)
        {
            // The wall clock is fine here: logging is host code, outside the simulation.
            _sink.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{DateTime.UtcNow:HH:mm:ss.fff} {level,-5} [t{entry.Tick}] {message}"));
        }
    }
}
