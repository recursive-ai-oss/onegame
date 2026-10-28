using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using OneGame.Core;

namespace OneGame.Storage;

/// <summary>The kind of value a settings field holds.</summary>
internal enum SettingKind : byte
{
    Int = 0,
    Bool = 1,
    Float = 2,
    Text = 3,
}

/// <summary>
/// Describes a settings class: which properties are saved, under which keys, and the range each one may take. The defaults are the
/// values a fresh <typeparamref name="T"/> has. Built once with the fluent methods, no reflection, so it is trim and AOT safe.
/// Open it through <c>GameInstance.Storage.OpenSettings</c> to load and save the player's choices.
/// </summary>
/// <example>
/// <code>
/// public sealed class Options
/// {
///     public int Volume { get; set; } = 7;
///     public bool Fullscreen { get; set; }
///
///     public static SettingsSchema&lt;Options&gt; Schema { get; } = new SettingsSchema&lt;Options&gt;("options.bin")
///         .Whole("volume", o =&gt; o.Volume, (o, v) =&gt; o.Volume = v, min: 0, max: 10)
///         .Flag("fullscreen", o =&gt; o.Fullscreen, (o, v) =&gt; o.Fullscreen = v);
/// }
/// </code>
/// </example>
/// <typeparam name="T">The settings class: a class with a public parameterless constructor holding the player's choices.</typeparam>
public sealed class SettingsSchema<T>
    where T : class, new()
{
    private readonly List<Field> _fields = [];

    /// <summary>Creates a schema stored in the file <paramref name="fileName"/> (default <c>settings.bin</c>).</summary>
    /// <param name="fileName">A valid store name.</param>
    /// <param name="version">Your own settings version, saved in the file; read it from <see cref="SettingsLoad.FileVersion"/> to migrate.</param>
    public SettingsSchema(string fileName = "settings.bin", int version = 1)
    {
        StoreNames.Require(fileName);
        FileName = fileName;
        Version = version;
    }

    /// <summary>The file the settings are kept in.</summary>
    public string FileName { get; }

    /// <summary>The version written into the file.</summary>
    public int Version { get; }

    /// <summary>The number of saved fields.</summary>
    public int Count => _fields.Count;

    internal IReadOnlyList<Field> Fields => _fields;

    /// <summary>Adds a whole number kept between <paramref name="min"/> and <paramref name="max"/> (a value outside it loads as the default).</summary>
    public SettingsSchema<T> Whole(string key, Func<T, int> get, Action<T, int> set, int min = int.MinValue, int max = int.MaxValue)
    {
        Check(key, get, set);
        if (min > max)
        {
            throw new ArgumentException("The minimum is above the maximum.", nameof(min));
        }

        _fields.Add(new Field(key, SettingKind.Int, o => (long)get(o), (o, v) => set(o, (int)(long)v), min, max, 0));
        return this;
    }

    /// <summary>Adds an on/off choice.</summary>
    /// <param name="key">The field's name in the file: 1 to 64 UTF-8 bytes, unique in the schema.</param>
    /// <param name="get">Reads the value from the settings object.</param>
    /// <param name="set">Writes a loaded value into the settings object.</param>
    public SettingsSchema<T> Flag(string key, Func<T, bool> get, Action<T, bool> set)
    {
        Check(key, get, set);
        _fields.Add(new Field(key, SettingKind.Bool, o => get(o), (o, v) => set(o, (bool)v), 0, 1, 0));
        return this;
    }

    /// <summary>Adds a number with a fraction (a volume, a scale) kept between <paramref name="min"/> and <paramref name="max"/>. NaN and infinities load as the default.</summary>
    public SettingsSchema<T> Number(string key, Func<T, float> get, Action<T, float> set, float min = float.MinValue, float max = float.MaxValue)
    {
        Check(key, get, set);
        _fields.Add(new Field(key, SettingKind.Float, o => get(o), (o, v) => set(o, (float)v), min, max, 0));
        return this;
    }

    /// <summary>Adds text of at most <paramref name="maxLength"/> UTF-8 bytes (a player name, a language code). Longer text loads as the default.</summary>
    public SettingsSchema<T> Text(string key, Func<T, string> get, Action<T, string> set, int maxLength = 256)
    {
        Check(key, get, set);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxLength, SettingsFile<T>.MaxTextBytes);
        _fields.Add(new Field(key, SettingKind.Text, o => get(o), (o, v) => set(o, (string)v), 0, 0, maxLength));
        return this;
    }

    /// <summary>Adds a choice from an enum with a 32-bit underlying type. A number that is not a member loads as the default.</summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="key">The field's name in the file: 1 to 64 UTF-8 bytes, unique in the schema.</param>
    /// <param name="get">Reads the value from the settings object.</param>
    /// <param name="set">Writes a loaded value into the settings object.</param>
    public SettingsSchema<T> Choice<TEnum>(string key, Func<T, TEnum> get, Action<T, TEnum> set)
        where TEnum : unmanaged, Enum
    {
        Check(key, get, set);
        if (Unsafe.SizeOf<TEnum>() != sizeof(int))
        {
            throw new ArgumentException($"{typeof(TEnum).Name} must have a 32-bit underlying type (the default for an enum).", nameof(key));
        }

        _fields.Add(new Field(
            key,
            SettingKind.Int,
            o =>
            {
                var e = get(o);
                return (long)Unsafe.As<TEnum, int>(ref e);
            },
            (o, v) =>
            {
                var i = (int)(long)v;
                set(o, Unsafe.As<int, TEnum>(ref i));
            },
            int.MinValue,
            int.MaxValue,
            0,
            static v =>
            {
                var i = (int)(long)v;
                return Enum.IsDefined(Unsafe.As<int, TEnum>(ref i));
            }));
        return this;
    }

    private void Check(string key, Delegate get, Delegate set)
    {
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        if (string.IsNullOrEmpty(key) || Encoding.UTF8.GetByteCount(key) > 64)
        {
            throw new ArgumentException("A settings key is 1 to 64 UTF-8 bytes.", nameof(key));
        }

        foreach (var f in _fields)
        {
            if (f.Key == key)
            {
                throw new ArgumentException($"The key '{key}' is already in the schema.", nameof(key));
            }
        }
    }

    internal sealed class Field(string key, SettingKind kind, Func<T, object> get, Action<T, object> set, double min, double max, int maxText, Func<object, bool>? valid = null)
    {
        public string Key { get; } = key;

        public SettingKind Kind { get; } = kind;

        public Func<T, object> Get { get; } = get;

        public Action<T, object> Set { get; } = set;

        public double Min { get; } = min;

        public double Max { get; } = max;

        public int MaxText { get; } = maxText;

        public Func<object, bool>? Valid { get; } = valid;
    }
}

/// <summary>How a settings load went.</summary>
public enum SettingsStatus
{
    /// <summary>The file was read; fields that were missing or invalid took their defaults (see <see cref="SettingsLoad.Notes"/>).</summary>
    Loaded = 0,

    /// <summary>There is no settings file yet: everything is at its default.</summary>
    Missing,

    /// <summary>The file is damaged, from another program, or too large: everything is at its default and the file was left alone.</summary>
    Defaulted,
}

/// <summary>What <see cref="SettingsFile{T}.Load"/> found.</summary>
/// <param name="Status">The outcome.</param>
/// <param name="Reason">Why everything is at its default, or an empty string.</param>
/// <param name="Notes">One line for each field that took its default, and for each key in the file the schema does not know.</param>
/// <param name="FileVersion">The version the file was saved with, or 0 when none was read.</param>
public sealed record SettingsLoad(SettingsStatus Status, string Reason, IReadOnlyList<string> Notes, int FileVersion)
{
    /// <summary>True when nothing was wrong: the file was read in full, or there is no file yet.</summary>
    public bool Clean => Status == SettingsStatus.Missing || (Status == SettingsStatus.Loaded && Notes.Count == 0);
}

/// <summary>The type-independent face of a <see cref="SettingsFile{T}"/>, for tools that list a game's settings.</summary>
public interface ISettingsFile
{
    /// <summary>The file the settings are kept in.</summary>
    string FileName { get; }

    /// <summary>What the latest load found, or null before the first.</summary>
    SettingsLoad? LastLoad { get; }

    /// <summary>Reads the file again.</summary>
    SettingsLoad Load();

    /// <summary>Writes the current values.</summary>
    bool Save();

    /// <summary>The values as <c>key = value</c> lines.</summary>
    string ToText();
}

/// <summary>
/// A typed settings object kept in a store: load it once at start, change its properties, call <see cref="Save"/>. A damaged or
/// out-of-range file never throws; it falls back to defaults and says why in the <see cref="SettingsLoad"/>.
/// </summary>
/// <typeparam name="T">The settings class.</typeparam>
public sealed class SettingsFile<T> : ISettingsFile
    where T : class, new()
{
    /// <summary>The largest settings file read, in bytes.</summary>
    public const int MaxBytes = 64 * 1024;

    /// <summary>The longest text value, in UTF-8 bytes.</summary>
    public const int MaxTextBytes = 4096;

    private const uint Magic = 0x5453474F; // "OGST"
    private const int FormatVersion = 1;
    private const int MaxFields = 512;

    private readonly IByteStore _store;
    private readonly SettingsSchema<T> _schema;

    /// <summary>Creates the settings for <paramref name="schema"/> in <paramref name="store"/>. <see cref="Value"/> holds the defaults until <see cref="Load"/>.</summary>
    public SettingsFile(IByteStore store, SettingsSchema<T> schema)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(schema);
        _store = store;
        _schema = schema;
        Value = new T();
    }

    /// <summary>The settings object. It is the same instance for the life of this file, so a screen can hold on to it.</summary>
    public T Value { get; }

    /// <inheritdoc />
    public string FileName => _schema.FileName;

    /// <summary>The schema.</summary>
    public SettingsSchema<T> Schema => _schema;

    /// <summary>What the latest <see cref="Load"/> or <see cref="Apply"/> found, or null before the first.</summary>
    public SettingsLoad? LastLoad { get; private set; }

    /// <summary>Reads the file into <see cref="Value"/>. Never throws for bad content.</summary>
    public SettingsLoad Load() => LastLoad = LoadCore();

    private SettingsLoad LoadCore()
    {
        ApplyDefaults();
        if (!_store.TryRead(_schema.FileName, MaxBytes, out var bytes))
        {
            // Missing and unreadable look the same to a store; a file over the limit is the one case worth naming.
            return Listed(_schema.FileName)
                ? new SettingsLoad(SettingsStatus.Defaulted, $"{_schema.FileName} could not be read (unreadable or larger than {MaxBytes} bytes)", [], 0)
                : new SettingsLoad(SettingsStatus.Missing, string.Empty, [], 0);
        }

        return ApplyCore(bytes);
    }

    /// <summary>Applies settings bytes to <see cref="Value"/> without a store (the file's content, already read).</summary>
    /// <param name="bytes">The settings file's content. Untrusted input is fine; problems are reported in the result.</param>
    /// <returns>What was found and applied.</returns>
    public SettingsLoad Apply(ReadOnlySpan<byte> bytes) => LastLoad = ApplyCore(bytes);

    private SettingsLoad ApplyCore(ReadOnlySpan<byte> bytes)
    {
        ApplyDefaults();
        var notes = new List<string>();
        if (bytes.Length is 0 or > MaxBytes)
        {
            return new SettingsLoad(SettingsStatus.Defaulted, bytes.Length == 0 ? "the file is empty" : $"the file is larger than {MaxBytes} bytes", notes, 0);
        }

        var r = new SyncReader(bytes);
        if (r.ReadFixed32() != Magic || r.ReadVarUInt() != FormatVersion)
        {
            return new SettingsLoad(SettingsStatus.Defaulted, "this is not a settings file of this format", notes, 0);
        }

        var version = r.ReadVarInt();
        var count = r.ReadCount(MaxFields);
        var entries = new List<(string Key, SettingKind Kind, object Value)>(count);
        for (var i = 0; i < count && r.Ok; i++)
        {
            var key = r.ReadString();
            var kind = (SettingKind)r.ReadByte();
            object? value = kind switch
            {
                SettingKind.Int => r.ReadVarInt(),
                SettingKind.Bool => r.ReadBool(),
                SettingKind.Float => (double)BitConverter.UInt32BitsToSingle(r.ReadFixed32()),
                SettingKind.Text => r.ReadString(),
                _ => null,
            };
            if (value is null)
            {
                r.Fail(SyncError.InvalidValue);
            }
            else if (r.Ok)
            {
                entries.Add((key, kind, value));
            }
        }

        r.ExpectEnd();
        if (!r.Ok || version is < 0 or > int.MaxValue)
        {
            return new SettingsLoad(SettingsStatus.Defaulted, $"the file is damaged ({(r.Ok ? SyncError.InvalidValue : r.Error)})", notes, 0);
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (key, kind, value) in entries)
        {
            SettingsSchema<T>.Field? field = null;
            foreach (var f in _schema.Fields)
            {
                if (f.Key == key)
                {
                    field = f;
                }
            }

            if (field is null)
            {
                notes.Add($"'{key}' is not a setting of this version and was ignored");
            }
            else if (!seen.Add(key))
            {
                notes.Add($"'{key}' appears twice; the first value is used");
            }
            else if (!TryApply(field, kind, value, out var why))
            {
                notes.Add($"'{key}' {why}; the default is used");
            }
        }

        return new SettingsLoad(SettingsStatus.Loaded, string.Empty, notes, (int)version);
    }

    /// <summary>Writes <see cref="Value"/> to the store. Returns false when the store could not write it (the old file stays).</summary>
    public bool Save() => _store.Write(_schema.FileName, ToBytes());

    /// <summary>Puts every field back to its default (the values of a new <typeparamref name="T"/>) without touching the file.</summary>
    public void Reset() => ApplyDefaults();

    /// <summary>The file's bytes for the current values.</summary>
    public byte[] ToBytes()
    {
        var w = new SyncWriter(128);
        w.WriteFixed32(Magic);
        w.WriteVarUInt(FormatVersion);
        w.WriteVarInt(_schema.Version);
        w.WriteCount(_schema.Fields.Count, MaxFields);
        foreach (var f in _schema.Fields)
        {
            w.WriteString(f.Key);
            w.WriteByte((byte)f.Kind);
            var v = f.Get(Value);
            switch (f.Kind)
            {
                case SettingKind.Int:
                    w.WriteVarInt((long)v);
                    break;
                case SettingKind.Bool:
                    w.WriteBool((bool)v);
                    break;
                case SettingKind.Float:
                    w.WriteFixed32(BitConverter.SingleToUInt32Bits((float)v));
                    break;
                default:
                    w.WriteString((string)v);
                    break;
            }
        }

        return w.ToArray();
    }

    /// <summary>The values as <c>key = value</c> lines in schema order, for logs and the harness.</summary>
    public string ToText()
    {
        var sb = new StringBuilder();
        foreach (var f in _schema.Fields)
        {
            sb.Append(f.Key).Append(" = ").Append(Format(f.Get(Value))).Append('\n');
        }

        return sb.ToString();
    }

    private static string Format(object v) => v switch
    {
        bool b => b ? "true" : "false",
        float x => x.ToString("R", CultureInfo.InvariantCulture),
        long l => l.ToString(CultureInfo.InvariantCulture),
        string s => s,
        _ => Convert.ToString(v, CultureInfo.InvariantCulture) ?? string.Empty,
    };

    private bool Listed(string name)
    {
        foreach (var n in _store.List(name))
        {
            if (n == name)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyDefaults()
    {
        var fresh = new T();
        foreach (var f in _schema.Fields)
        {
            f.Set(Value, f.Get(fresh));
        }
    }

    private bool TryApply(SettingsSchema<T>.Field f, SettingKind kind, object value, out string why)
    {
        why = string.Empty;
        if (kind != f.Kind)
        {
            why = $"is stored as {kind}, expected {f.Kind}";
            return false;
        }

        switch (kind)
        {
            case SettingKind.Int:
                var i = (long)value;
                if (i < f.Min || i > f.Max || (f.Valid is not null && !f.Valid(i)))
                {
                    why = $"is {i}, outside its allowed values";
                    return false;
                }

                f.Set(Value, i);
                return true;
            case SettingKind.Float:
                var d = (double)value;
                if (!double.IsFinite(d) || d < f.Min || d > f.Max)
                {
                    why = $"is {d.ToString(CultureInfo.InvariantCulture)}, outside its allowed values";
                    return false;
                }

                f.Set(Value, (float)d);
                return true;
            case SettingKind.Text:
                var s = (string)value;
                if (Encoding.UTF8.GetByteCount(s) > f.MaxText)
                {
                    why = $"is longer than {f.MaxText} bytes";
                    return false;
                }

                f.Set(Value, s);
                return true;
            default:
                f.Set(Value, value);
                return true;
        }
    }
}
