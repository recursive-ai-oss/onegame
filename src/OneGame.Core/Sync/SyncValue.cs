using System.Globalization;
using System.Text;

namespace OneGame.Core;

/// <summary>
/// A value type with a binary codec. Generated code, <see cref="Fixed"/>, <see cref="EntityId"/> and other engine value types
/// implement it, and a synced property may have any type that does.
/// </summary>
/// <typeparam name="TSelf">The implementing type.</typeparam>
public interface ISyncValue<TSelf> : IEquatable<TSelf>
    where TSelf : ISyncValue<TSelf>
{
    /// <summary>Writes the value in the sync format.</summary>
    void Write(SyncWriter writer);

    /// <summary>Reads a value written by <see cref="Write"/>. Problems are recorded on the reader, and a default value is returned.</summary>
    static abstract TSelf Read(ref SyncReader reader);

    /// <summary>Writes the value as JSON for inspection.</summary>
    void WriteJson(JsonOut json);
}

/// <summary>Reads synced values by type argument, which is how generated code reads types whose reader is an explicit interface member.</summary>
public static class SyncRead
{
    /// <summary>Reads a <typeparamref name="T"/> from <paramref name="reader"/>.</summary>
    public static T Value<T>(ref SyncReader reader)
        where T : ISyncValue<T>
        => T.Read(ref reader);
}

/// <summary>A value that can be blended between two of its kind; <see cref="Timed{T}"/> uses it to interpolate.</summary>
/// <typeparam name="TSelf">The implementing type.</typeparam>
public interface ILerp<TSelf>
    where TSelf : ILerp<TSelf>
{
    /// <summary>Blends from <paramref name="a"/> (at 0) to <paramref name="b"/> (at 1).</summary>
    static abstract TSelf Lerp(TSelf a, TSelf b, Fixed t);
}

/// <summary>A deterministic 64-bit hash of bytes (FNV-1a with a final avalanche), used for drift detection. Not for security.</summary>
public static class StateHash
{
    private const ulong Offset = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    /// <summary>Hashes <paramref name="bytes"/>.</summary>
    public static ulong Of(ReadOnlySpan<byte> bytes)
    {
        var h = Offset;
        foreach (var b in bytes)
        {
            h = (h ^ b) * Prime;
        }

        return Mix(h);
    }

    /// <summary>Hashes a string by its UTF-8 bytes.</summary>
    public static ulong Of(string text)
    {
        // Short strings are encoded on the stack so hashing a world allocates nothing.
        if (text.Length <= 80)
        {
            Span<byte> buffer = stackalloc byte[240];
            return Of(buffer[..Encoding.UTF8.GetBytes(text, buffer)]);
        }

        return Of(Encoding.UTF8.GetBytes(text));
    }

    /// <summary>Folds <paramref name="value"/> into <paramref name="hash"/>; the result depends on the order of folding.</summary>
    public static ulong Combine(ulong hash, ulong value) => Mix(hash ^ (value + 0x9E3779B97F4A7C15UL + (hash << 6) + (hash >> 2)));

    /// <summary>The 32-bit FNV-1a hash of a string's UTF-8 bytes, used for stable type and command ids.</summary>
    public static uint Fnv32(string text)
    {
        var h = 2166136261u;
        foreach (var b in Encoding.UTF8.GetBytes(text))
        {
            h = (h ^ b) * 16777619u;
        }

        return h;
    }

    /// <summary>Splitmix64 finalizer.</summary>
    internal static ulong Mix(ulong z)
    {
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}

/// <summary>A small JSON writer for state dumps. Output is compact, ordered as written and identical on every platform.</summary>
public sealed class JsonOut
{
    private readonly StringBuilder _text = new();
    private readonly List<bool> _needsComma = [];
    private bool _afterName;

    /// <summary>The JSON written so far.</summary>
    public override string ToString() => _text.ToString();

    private void Prefix()
    {
        if (_afterName)
        {
            _afterName = false;
            return;
        }

        if (_needsComma.Count > 0)
        {
            if (_needsComma[^1])
            {
                _text.Append(',');
            }

            _needsComma[^1] = true;
        }
    }

    /// <summary>Starts an object.</summary>
    public void BeginObject()
    {
        Prefix();
        _text.Append('{');
        _needsComma.Add(false);
    }

    /// <summary>Ends the current object.</summary>
    public void EndObject()
    {
        _needsComma.RemoveAt(_needsComma.Count - 1);
        _text.Append('}');
    }

    /// <summary>Starts an array.</summary>
    public void BeginArray()
    {
        Prefix();
        _text.Append('[');
        _needsComma.Add(false);
    }

    /// <summary>Ends the current array.</summary>
    public void EndArray()
    {
        _needsComma.RemoveAt(_needsComma.Count - 1);
        _text.Append(']');
    }

    /// <summary>Writes a property name inside an object; the next call writes its value.</summary>
    public void Name(string name)
    {
        Prefix();
        AppendString(name);
        _text.Append(':');
        _afterName = true;
    }

    /// <summary>Writes a bool.</summary>
    public void Value(bool value)
    {
        Prefix();
        _text.Append(value ? "true" : "false");
    }

    /// <summary>Writes a signed integer.</summary>
    public void Value(long value)
    {
        Prefix();
        _text.Append(value.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>Writes an unsigned integer.</summary>
    public void Value(ulong value)
    {
        Prefix();
        _text.Append(value.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>Writes a string.</summary>
    public void Value(string? value)
    {
        Prefix();
        AppendString(value ?? string.Empty);
    }

    /// <summary>Writes already-formatted JSON, such as a number.</summary>
    public void Raw(string json)
    {
        Prefix();
        _text.Append(json);
    }

    private void AppendString(string s)
    {
        _text.Append('"');
        foreach (var c in s)
        {
            switch (c)
            {
                case '"': _text.Append("\\\""); break;
                case '\\': _text.Append("\\\\"); break;
                case '\n': _text.Append("\\n"); break;
                case '\r': _text.Append("\\r"); break;
                case '\t': _text.Append("\\t"); break;
                default:
                    if (c < 0x20)
                    {
                        _text.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        _text.Append(c);
                    }

                    break;
            }
        }

        _text.Append('"');
    }
}

/// <summary>Parsing helpers the generated state-patch code uses. They never throw.</summary>
public static class TextParse
{
    /// <summary>Parses a decimal integer within a range.</summary>
    public static bool TryInteger(string text, long min, long max, out long value)
        => long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value) && value >= min && value <= max;

    /// <summary>Parses a decimal unsigned integer up to <paramref name="max"/>.</summary>
    public static bool TryUnsigned(string text, ulong max, out ulong value)
        => ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value <= max;

    /// <summary>Parses <c>true</c> or <c>false</c>.</summary>
    public static bool TryBool(string text, out bool value) => bool.TryParse(text, out value);
}
