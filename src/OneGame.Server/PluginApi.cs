namespace OneGame.Server;

/// <summary>The version of the plugin API a host speaks. A plugin records the version it was built against and the host refuses it when that is outside <see cref="MinimumVersion"/>..<see cref="Version"/>.</summary>
public static class PluginApi
{
    /// <summary>The newest API version this host implements.</summary>
    public const int Version = 1;

    /// <summary>The oldest API version this host still accepts.</summary>
    public const int MinimumVersion = 1;
}

/// <summary>
/// Marks a plugin assembly and names its entry class. Put <c>[assembly: ServerPlugin(typeof(MyPlugin))]</c> in the plugin project. The API
/// version is a default argument, so the compiler writes the version the plugin was built against into the assembly and the host can
/// check it before any plugin code runs.
/// </summary>
/// <param name="entryType">The <see cref="ServerPlugin"/> subclass to instantiate. It needs a public parameterless constructor.</param>
/// <param name="apiVersion">The plugin API version the plugin was built against. Leave the default.</param>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class ServerPluginAttribute(Type entryType, int apiVersion = PluginApi.Version) : Attribute
{
    /// <summary>The entry class.</summary>
    public Type EntryType { get; } = entryType;

    /// <summary>The API version the plugin was built against.</summary>
    public int ApiVersion { get; } = apiVersion;
}

/// <summary>
/// A server plugin. The host creates one instance per load, calls <see cref="Load"/> (every load and reload), <see cref="Seed"/> (first load only)
/// and finally <see cref="Unload"/>. State belongs in entities, so it survives a reload; keep nothing in static fields, threads, timers
/// or event subscriptions, because the assembly can only be unloaded once nothing outside it refers to it.
/// </summary>
public abstract class ServerPlugin
{
    /// <summary>Registers the plugin's entity types and systems and reads its settings. Do not touch scopes here: this also runs on reload, and a failure here leaves the world untouched.</summary>
    /// <param name="context">The registration surface.</param>
    public abstract void Load(PluginContext context);

    /// <summary>Creates the plugin's initial entities in one scope. Called once per bound scope on the first load only; a reload keeps the existing state.</summary>
    /// <param name="scope">The scope and its authority world.</param>
    public virtual void Seed(PluginScope scope)
    {
    }

    /// <summary>Releases anything held outside the world. Called before the plugin is unloaded or replaced; an exception here is reported and does not stop the unload.</summary>
    public virtual void Unload()
    {
    }
}

/// <summary>One scope a plugin is bound to.</summary>
/// <param name="Id">The scope id.</param>
/// <param name="World">The authority world. Only touch it from <see cref="ServerPlugin.Seed"/> and from sim code the host calls.</param>
public sealed record PluginScope(ScopeId Id, World World);

/// <summary>What a plugin sees while it loads: its settings, the scopes it is bound to, and the places to register types and systems.</summary>
public sealed class PluginContext
{
    private readonly List<StagedEntity> _entities = [];
    private readonly List<ISimSystem> _systems = [];
    private readonly Action<string> _log;
    private readonly KeyValuePair<string, string>[] _settings;

    internal PluginContext(string name, bool isReload, ScopeId[] scopes, KeyValuePair<string, string>[] settings, Action<string> log)
    {
        Name = name;
        IsReload = isReload;
        Scopes = scopes;
        _settings = settings;
        _log = log;
    }

    /// <summary>The plugin's configured name.</summary>
    public string Name { get; }

    /// <summary>True when this load replaces a running version.</summary>
    public bool IsReload { get; }

    /// <summary>The scopes the plugin is bound to.</summary>
    public IReadOnlyList<ScopeId> Scopes { get; }

    internal IReadOnlyList<StagedEntity> Entities => _entities;

    internal IReadOnlyList<ISimSystem> Systems => _systems;

    /// <summary>A setting from the plugin's <c>settings</c> block in the server configuration, or null.</summary>
    /// <param name="key">The setting name.</param>
    public string? Setting(string key)
    {
        foreach (var kv in _settings)
        {
            if (kv.Key == key)
            {
                return kv.Value;
            }
        }

        return null;
    }

    /// <summary>A whole-number setting, or <paramref name="fallback"/> when it is absent.</summary>
    /// <param name="key">The setting name.</param>
    /// <param name="fallback">The value when the setting is absent.</param>
    /// <exception cref="PluginException">The setting is present but not a whole number.</exception>
    public int SettingInt(string key, int fallback)
    {
        var text = Setting(key);
        if (text is null)
        {
            return fallback;
        }

        return int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var v)
            ? v
            : throw new PluginException("bad_setting", $"Setting '{key}' of plugin '{Name}' must be a whole number, not '{text}'.");
    }

    /// <summary>Registers an entity type the plugin provides. Its commands become available on the bound scopes, and saves containing it restore through the type's migrations.</summary>
    /// <typeparam name="T">A generated entity class with a public parameterless constructor.</typeparam>
    public void AddEntity<T>()
        where T : Entity, new()
    {
        var probe = new T();
        _entities.Add(new StagedEntity(probe.TypeId, probe.TypeName, probe.SchemaVersion, static () => new T()));
    }

    /// <summary>Adds a system to every bound scope. Systems run after all entities, in plugin order then registration order. Keep their state in entities or tables.</summary>
    /// <param name="system">The system.</param>
    public void AddSystem(ISimSystem system)
    {
        ArgumentNullException.ThrowIfNull(system);
        _systems.Add(system);
    }

    /// <summary>Writes a line to the server log, tagged with the plugin name.</summary>
    /// <param name="message">The text.</param>
    public void Log(string message) => _log(message);
}

internal sealed record StagedEntity(uint TypeId, string TypeName, uint Schema, Func<Entity> Factory);

/// <summary>A plugin could not be loaded, reloaded or unloaded, or a setting was wrong. <see cref="Code"/> is stable and the message says what to change.</summary>
public class PluginException : Exception
{
    /// <summary>Creates the exception.</summary>
    public PluginException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>Creates the exception.</summary>
    public PluginException(string code, string message, Exception inner)
        : base(message, inner)
    {
        Code = code;
    }

    /// <summary>A short stable identifier: <c>api_mismatch</c>, <c>no_entry</c>, <c>not_found</c>, <c>duplicate</c>, <c>load_failed</c>, <c>conflict</c>, <c>reload_failed</c>, <c>bad_setting</c>, <c>unsupported</c>.</summary>
    public string Code { get; }
}

/// <summary>Loads plugin assemblies. The CoreCLR implementation lives in <c>OneGame.Server.Plugins</c>; a host built without it cannot load plugins.</summary>
public interface IPluginLoader
{
    /// <summary>Loads the plugin at <paramref name="path"/> (a folder holding <c>&lt;name&gt;.dll</c>, or the dll itself) and creates its entry object. No plugin code runs before the API version is checked.</summary>
    /// <param name="name">The configured plugin name.</param>
    /// <param name="path">The folder or dll.</param>
    /// <exception cref="PluginException">The files are missing, the API version is refused, or there is no entry class.</exception>
    PluginImage Load(string name, string path);
}

/// <summary>One loaded copy of a plugin: the entry object plus the means to find which exceptions came from it and to release it.</summary>
public abstract class PluginImage
{
    /// <summary>The entry object.</summary>
    public abstract ServerPlugin Plugin { get; }

    /// <summary>A short identity of the loaded build, such as the assembly version and module id.</summary>
    public abstract string Build { get; }

    /// <summary>The API version the plugin declared.</summary>
    public abstract int ApiVersion { get; }

    /// <summary>Whether any frame of <paramref name="exception"/> belongs to this plugin's code.</summary>
    /// <param name="exception">The exception.</param>
    public abstract bool Owns(Exception exception);

    /// <summary>Starts unloading. Afterwards the caller must drop every reference to the plugin and its types.</summary>
    public abstract void Release();

    /// <summary>Forces collections until the plugin's load context is gone or the attempts run out, and reports whether it is.</summary>
    public abstract bool WaitUntilCollected();
}

/// <summary>An image for plugins already linked into the process (a game that bundles its own, or tests). It cannot be unloaded.</summary>
/// <param name="plugin">The plugin object.</param>
public sealed class InProcessPluginImage(ServerPlugin plugin) : PluginImage
{
    /// <inheritdoc />
    public override ServerPlugin Plugin { get; } = plugin;

    /// <inheritdoc />
    public override string Build => "in-process";

    /// <inheritdoc />
    public override int ApiVersion => PluginApi.Version;

    /// <inheritdoc />
    public override bool Owns(Exception exception) => true;

    /// <inheritdoc />
    public override void Release()
    {
    }

    /// <inheritdoc />
    public override bool WaitUntilCollected() => true;
}
