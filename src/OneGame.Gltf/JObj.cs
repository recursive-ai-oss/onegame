using System.Text.Json;
using System.Globalization;

namespace OneGame.Gltf;

/// <summary>A JSON object with typed, checked reads. Every wrong type, missing field or out-of-range reference becomes a <see cref="GltfException"/> that names the place.</summary>
internal readonly struct JObj
{
    private readonly JsonElement _el;
    private readonly string _kind;
    private readonly int _index;

    internal JObj(JsonElement el, string kind, int index = -1)
    {
        if (el.ValueKind != JsonValueKind.Object)
        {
            throw new GltfException("schema", $"{Where(kind, index)} must be an object.");
        }

        _el = el;
        _kind = kind;
        _index = index;
    }

    internal JsonElement Element => _el;

    private static string Where(string kind, int index) => index < 0 ? kind : $"{kind}[{index.ToString(CultureInfo.InvariantCulture)}]";

    internal string Path(string name) => $"{Where(_kind, _index)}.{name}";

    internal GltfException Error(string code, string name, string what) => new(code, $"{Path(name)} {what}");

    /// <summary>Reads a JSON string. The parser accepts bytes that are not valid UTF-8 until the text is requested, so that is where they are refused.</summary>
    internal static string? Text(JsonElement value)
    {
        try
        {
            return value.GetString();
        }
        catch (InvalidOperationException)
        {
            throw new GltfException("json_invalid", "The JSON holds a string that is not valid UTF-8.");
        }
    }

    /// <summary>Reads a property name with the same check as <see cref="Text"/>.</summary>
    internal static string NameOf(JsonProperty property)
    {
        try
        {
            return property.Name;
        }
        catch (InvalidOperationException)
        {
            throw new GltfException("json_invalid", "The JSON holds a property name that is not valid UTF-8.");
        }
    }

    internal bool Has(string name) => _el.TryGetProperty(name, out _);

    internal bool TryGet(string name, out JsonElement value)
    {
        if (_el.TryGetProperty(name, out value))
        {
            // An explicit null is the same as absent.
            return value.ValueKind != JsonValueKind.Null;
        }

        return false;
    }

    internal string Str(string name, string fallback = "")
    {
        if (!TryGet(name, out var v))
        {
            return fallback;
        }

        if (v.ValueKind != JsonValueKind.String)
        {
            throw Error("schema", name, "must be a string.");
        }

        return Text(v) ?? fallback;
    }

    internal bool Bool(string name, bool fallback)
    {
        if (!TryGet(name, out var v))
        {
            return fallback;
        }

        return v.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw Error("schema", name, "must be true or false."),
        };
    }

    internal long Long(string name, long fallback, long min = long.MinValue, long max = long.MaxValue)
    {
        if (!TryGet(name, out var v))
        {
            return fallback;
        }

        return AsLong(v, name, min, max);
    }

    internal long ReqLong(string name, long min = long.MinValue, long max = long.MaxValue)
    {
        if (!TryGet(name, out var v))
        {
            throw Error("schema", name, "is required.");
        }

        return AsLong(v, name, min, max);
    }

    internal int Int(string name, int fallback, int min = int.MinValue, int max = int.MaxValue) => (int)Long(name, fallback, min, max);

    internal int ReqInt(string name, int min = int.MinValue, int max = int.MaxValue) => (int)ReqLong(name, min, max);

    private long AsLong(JsonElement v, string name, long min, long max)
    {
        long n;
        if (v.ValueKind != JsonValueKind.Number)
        {
            throw Error("schema", name, "must be an integer.");
        }

        if (!v.TryGetInt64(out n))
        {
            // Some exporters write 3.0 for 3.
            if (!v.TryGetDouble(out var d) || d != Math.Floor(d) || Math.Abs(d) > 9e15)
            {
                throw Error("schema", name, "must be an integer.");
            }

            n = (long)d;
        }

        if (n < min || n > max)
        {
            throw Error("schema", name, $"is {n.ToString(CultureInfo.InvariantCulture)}, outside {min.ToString(CultureInfo.InvariantCulture)}..{max.ToString(CultureInfo.InvariantCulture)}.");
        }

        return n;
    }

    /// <summary>Reads an index into a list of the given length; -1 when absent.</summary>
    internal int Ref(string name, int count)
    {
        if (!TryGet(name, out _))
        {
            return -1;
        }

        var n = (int)ReqLong(name, int.MinValue, int.MaxValue);
        if (n < 0 || n >= count)
        {
            throw Error("reference", name, $"is {n.ToString(CultureInfo.InvariantCulture)}, but only {count.ToString(CultureInfo.InvariantCulture)} exist.");
        }

        return n;
    }

    internal int ReqRef(string name, int count)
    {
        var r = Ref(name, count);
        if (r < 0)
        {
            throw Error("schema", name, "is required.");
        }

        return r;
    }

    internal float Float(string name, float fallback)
    {
        if (!TryGet(name, out var v))
        {
            return fallback;
        }

        return AsFloat(v, name);
    }

    internal float? FloatOpt(string name) => TryGet(name, out var v) ? AsFloat(v, name) : null;

    internal float ReqFloat(string name) => TryGet(name, out var v) ? AsFloat(v, name) : throw Error("schema", name, "is required.");

    private float AsFloat(JsonElement v, string name)
    {
        if (v.ValueKind != JsonValueKind.Number || !v.TryGetDouble(out var d) || double.IsNaN(d) || Math.Abs(d) > float.MaxValue)
        {
            throw Error("schema", name, "must be a finite number.");
        }

        return (float)d;
    }

    /// <summary>Reads an array of numbers of an exact length, or null when absent.</summary>
    internal float[]? Floats(string name, int length)
    {
        if (!TryGet(name, out var v))
        {
            return null;
        }

        if (v.ValueKind != JsonValueKind.Array || v.GetArrayLength() != length)
        {
            throw Error("schema", name, $"must be an array of {length.ToString(CultureInfo.InvariantCulture)} numbers.");
        }

        var result = new float[length];
        var i = 0;
        foreach (var item in v.EnumerateArray())
        {
            result[i++] = AsFloat(item, name);
        }

        return result;
    }

    /// <summary>Reads an array of indices into a list of the given length. The array's own length is limited to <paramref name="maxCount"/> first.</summary>
    internal int[] Refs(string name, int count, int maxCount)
    {
        if (!TryGet(name, out var v))
        {
            return [];
        }

        if (v.ValueKind != JsonValueKind.Array)
        {
            throw Error("schema", name, "must be an array.");
        }

        var n = v.GetArrayLength();
        if (n > maxCount)
        {
            throw Error("limit_exceeded", name, $"has {n.ToString(CultureInfo.InvariantCulture)} entries; the limit is {maxCount.ToString(CultureInfo.InvariantCulture)}.");
        }

        var result = new int[n];
        var i = 0;
        foreach (var item in v.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Number || !item.TryGetInt32(out var r) || r < 0 || r >= count)
            {
                throw Error("reference", name, $"entry {i.ToString(CultureInfo.InvariantCulture)} is not a valid index (0..{(count - 1).ToString(CultureInfo.InvariantCulture)}).");
            }

            result[i++] = r;
        }

        return result;
    }

    /// <summary>The child object, or null when absent.</summary>
    internal JObj? Obj(string name)
    {
        if (!TryGet(name, out var v))
        {
            return null;
        }

        return new JObj(v, Path(name));
    }

    /// <summary>The length of an array property (0 when absent), checked against a limit before anything is built from it.</summary>
    internal int ArrayLength(string name, int max)
    {
        if (!TryGet(name, out var v))
        {
            return 0;
        }

        if (v.ValueKind != JsonValueKind.Array)
        {
            throw Error("schema", name, "must be an array.");
        }

        var n = v.GetArrayLength();
        if (n > max)
        {
            throw Error("limit_exceeded", name, $"has {n.ToString(CultureInfo.InvariantCulture)} entries; the limit is {max.ToString(CultureInfo.InvariantCulture)}.");
        }

        return n;
    }

    /// <summary>The i-th object of an array property.</summary>
    internal JObj Item(string name, int i)
    {
        _el.TryGetProperty(name, out var arr);
        return new JObj(arr[i], name, i);
    }

    /// <summary>The array's string entries (for <c>extensionsUsed</c>).</summary>
    internal string[] Strings(string name, int max)
    {
        var n = ArrayLength(name, max);
        if (n == 0)
        {
            return [];
        }

        var result = new string[n];
        var i = 0;
        foreach (var item in _el.GetProperty(name).EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                throw Error("schema", name, "must hold strings.");
            }

            result[i++] = Text(item) ?? "";
        }

        return result;
    }
}
