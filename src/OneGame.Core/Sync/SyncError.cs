namespace OneGame.Core;

/// <summary>Why decoding untrusted bytes failed. Decoders never throw on bad input; they set the first error and stop.</summary>
public enum SyncError
{
    /// <summary>No error.</summary>
    None = 0,

    /// <summary>The input ended before the value was complete.</summary>
    Truncated,

    /// <summary>A variable-length integer was longer than its type allows.</summary>
    VarIntOverflow,

    /// <summary>A length, count or size was above its limit, or larger than the bytes left in the input.</summary>
    LimitExceeded,

    /// <summary>A value was outside what its type allows (a bool that is not 0 or 1, an integer out of range, bad UTF-8, unknown delta bits).</summary>
    InvalidValue,

    /// <summary>The input did not start with the expected magic bytes or format version.</summary>
    BadHeader,

    /// <summary>An entity or table type in the input is not registered.</summary>
    UnknownType,

    /// <summary>The input holds a schema version this build cannot read (newer than the code, or older with no migration).</summary>
    UnsupportedVersion,

    /// <summary>Bytes were left over after a value that should have used the whole input.</summary>
    TrailingBytes,

    /// <summary>The input refers to an entity or table that does not exist or does not match.</summary>
    Mismatch,
}

/// <summary>Hard limits applied while decoding, so a hostile input cannot make the decoder allocate without bound.</summary>
public static class SyncLimits
{
    /// <summary>Longest string, in UTF-8 bytes.</summary>
    public const int MaxStringBytes = 64 * 1024;

    /// <summary>Largest element count of a synced list.</summary>
    public const int MaxListCount = 64 * 1024;

    /// <summary>Largest row count of a hot-data table.</summary>
    public const int MaxTableRows = 1 << 22;

    /// <summary>Largest number of entities in a snapshot.</summary>
    public const int MaxEntities = 1 << 22;

    /// <summary>Largest number of random streams or tables in a snapshot.</summary>
    public const int MaxNamed = 4096;

    /// <summary>Largest command payload, in bytes.</summary>
    public const int MaxCommandBytes = 64 * 1024;
}
