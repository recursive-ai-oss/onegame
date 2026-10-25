using OneYaml;

namespace OneGame.Server;

/// <summary>One scope the server hosts.</summary>
/// <param name="Id">The scope id.</param>
/// <param name="Seed">Seeds the scope's random streams.</param>
public sealed record ScopeConfig(uint Id, ulong Seed);

/// <summary>One plugin the server loads at start.</summary>
/// <param name="Name">The plugin's name; unique, and used by every plugin operation.</param>
/// <param name="Path">A folder holding <c>&lt;name&gt;.dll</c>, or the dll. Relative paths resolve against the configuration file.</param>
/// <param name="Scopes">The scopes it is bound to. Empty means all of them.</param>
/// <param name="Settings">Free-form text settings the plugin reads through <see cref="PluginContext.Setting"/>.</param>
public sealed record PluginConfig(string Name, string Path, uint[] Scopes, KeyValuePair<string, string>[] Settings);

/// <summary>The server configuration file is not valid: the message names the file, the line and what to change.</summary>
public sealed class ServerConfigException : Exception
{
    /// <summary>Creates the exception.</summary>
    public ServerConfigException(string message, string? source, int line)
        : base(source is null ? message : $"{source}:{line}: {message}")
    {
        Source = source;
        Line = line;
    }

    /// <summary>The file, when known.</summary>
    public new string? Source { get; }

    /// <summary>The 1-based line, or 0.</summary>
    public int Line { get; }
}

/// <summary>
/// The server configuration. Unknown keys are errors, so a misspelled setting never silently does nothing.
/// <code>
/// name: arena
/// tickRate: 60
/// scopes:
///   - { id: 1, seed: 42 }
/// plugins:
///   - name: counter
///     path: plugins/counter
///     scopes: [1]
///     settings: { step: 2 }
/// </code>
/// </summary>
public sealed class ServerConfig
{
    private static readonly string[] TopKeys = ["name", "tickRate", "maxCatchUp", "scopes", "plugins", "live"];
    private static readonly string[] ScopeKeys = ["id", "seed"];
    private static readonly string[] PluginKeys = ["name", "path", "scopes", "settings"];

    /// <summary>The most scopes or plugins a file may declare; a bound so a hostile file cannot ask for millions.</summary>
    public const int MaxEntries = 1024;

    /// <summary>The server's name, used for the node and in logs.</summary>
    public string Name { get; init; } = "server";

    /// <summary>Ticks per second.</summary>
    public int TickRate { get; init; } = 60;

    /// <summary>The most ticks run for one pass of the loop when the server falls behind; the rest are dropped.</summary>
    public int MaxCatchUp { get; init; } = 5;

    /// <summary>The scopes to host.</summary>
    public IReadOnlyList<ScopeConfig> Scopes { get; init; } = [new ScopeConfig(1, 1)];

    /// <summary>The plugins to load, in order.</summary>
    public IReadOnlyList<PluginConfig> Plugins { get; init; } = [];

    /// <summary>A socket path for live control, or null for none (the runner may supply its own).</summary>
    public string? Live { get; init; }

    /// <summary>Reads a configuration file.</summary>
    /// <param name="path">The YAML file.</param>
    /// <exception cref="ServerConfigException">The file is missing or invalid.</exception>
    public static ServerConfig Load(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        string text;
        try
        {
            var info = new FileInfo(path);
            if (info.Length > 1024 * 1024)
            {
                throw new ServerConfigException("the file is larger than 1 MiB", path, 0);
            }

            text = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new ServerConfigException("cannot read the file: " + ex.Message, path, 0);
        }

        return Parse(text, path);
    }

    /// <summary>Reads configuration text.</summary>
    /// <param name="yaml">The YAML text.</param>
    /// <param name="source">The file name for messages, and the folder relative plugin paths resolve against.</param>
    /// <exception cref="ServerConfigException">The text is not a valid configuration.</exception>
    public static ServerConfig Parse(string yaml, string? source = null)
    {
        ArgumentNullException.ThrowIfNull(yaml);
        YamlDocument doc;
        try
        {
            doc = YamlDocument.Parse(yaml, new YamlDocumentOptions());
        }
        catch (YamlException ex)
        {
            throw new ServerConfigException(ex.Message, source, ex.Line);
        }

        using (doc)
        {
            var r = new Reader(source);
            var root = doc.Root;
            if (root.Kind == YamlNodeKind.Scalar && root.IsNull)
            {
                return new ServerConfig();
            }

            r.Check(root, TopKeys, "the configuration");
            var baseDir = source is null ? null : System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(source));
            var scopes = new List<ScopeConfig>();
            if (root.TryGetValue("scopes", out var scopesNode) && !scopesNode.IsNull)
            {
                foreach (var item in r.Seq(scopesNode, "scopes"))
                {
                    r.Check(item, ScopeKeys, "a scope");
                    var id = r.UInt(item, "id", required: true) ?? 0;
                    var seed = r.ULong(item, "seed") ?? 1UL;
                    if (scopes.Exists(s => s.Id == id))
                    {
                        throw r.Error(item, $"scope {id} is declared twice");
                    }

                    scopes.Add(new ScopeConfig(id, seed));
                    r.Bound(scopes.Count, item, "scopes");
                }
            }
            else
            {
                scopes.Add(new ScopeConfig(1, 1));
            }

            var plugins = new List<PluginConfig>();
            if (root.TryGetValue("plugins", out var pluginsNode) && !pluginsNode.IsNull)
            {
                foreach (var item in r.Seq(pluginsNode, "plugins"))
                {
                    r.Check(item, PluginKeys, "a plugin");
                    var name = r.Text(item, "name") ?? throw r.Error(item, "a plugin needs a name");
                    var path = r.Text(item, "path") ?? throw r.Error(item, $"plugin '{name}' needs a path");
                    if (baseDir is not null && !System.IO.Path.IsPathRooted(path))
                    {
                        path = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, path));
                    }

                    if (plugins.Exists(p => p.Name == name))
                    {
                        throw r.Error(item, $"plugin '{name}' is declared twice");
                    }

                    var bound = new List<uint>();
                    if (item.TryGetValue("scopes", out var boundNode) && !boundNode.IsNull)
                    {
                        foreach (var s in r.Seq(boundNode, "scopes"))
                        {
                            var id = r.UIntValue(s, "a scope id");
                            if (!scopes.Exists(c => c.Id == id))
                            {
                                throw r.Error(s, $"plugin '{name}' is bound to scope {id}, which is not declared");
                            }

                            bound.Add(id);
                        }
                    }

                    var settings = new List<KeyValuePair<string, string>>();
                    if (item.TryGetValue("settings", out var settingsNode) && !settingsNode.IsNull)
                    {
                        if (settingsNode.Kind != YamlNodeKind.Mapping)
                        {
                            throw r.Error(settingsNode, "settings must be a mapping of names to plain values");
                        }

                        foreach (var entry in settingsNode.Entries)
                        {
                            settings.Add(new(entry.Key.GetValue<string>(), r.TextValue(entry.Value, "a setting value")));
                        }
                    }

                    plugins.Add(new PluginConfig(name, path, [.. bound], [.. settings]));
                    r.Bound(plugins.Count, item, "plugins");
                }
            }

            var tickRate = (int?)r.UInt(root, "tickRate") ?? 60;
            if (tickRate is < 1 or > 1000)
            {
                throw r.Error(root["tickRate"], "tickRate must be between 1 and 1000");
            }

            var catchUp = (int?)r.UInt(root, "maxCatchUp") ?? 5;
            if (catchUp is < 1 or > 1000)
            {
                throw r.Error(root["maxCatchUp"], "maxCatchUp must be between 1 and 1000");
            }

            return new ServerConfig
            {
                Name = r.Text(root, "name") ?? "server",
                TickRate = tickRate,
                MaxCatchUp = catchUp,
                Scopes = scopes,
                Plugins = plugins,
                Live = r.Text(root, "live"),
            };
        }
    }

    private sealed class Reader(string? source)
    {
        public ServerConfigException Error(YamlNode at, string message) => new(message, source, at.Position.Line);

        public void Bound(int count, YamlNode at, string what)
        {
            if (count > MaxEntries)
            {
                throw Error(at, $"more than {MaxEntries} entries in {what}");
            }
        }

        public void Check(YamlNode node, string[] allowed, string what)
        {
            if (node.Kind != YamlNodeKind.Mapping)
            {
                throw Error(node, $"{what} must be a mapping of {string.Join(", ", allowed)}");
            }

            foreach (var entry in node.Entries)
            {
                var key = entry.Key.GetValue<string>();
                if (Array.IndexOf(allowed, key) < 0)
                {
                    throw Error(entry.Key, $"unknown key '{key}' in {what}; allowed: {string.Join(", ", allowed)}");
                }
            }
        }

        public YamlNode.ItemEnumerable Seq(YamlNode node, string what)
        {
            if (node.Kind != YamlNodeKind.Sequence)
            {
                throw Error(node, $"{what} must be a list");
            }

            return node.Items;
        }

        public string TextValue(YamlNode node, string what)
        {
            if (node.Kind != YamlNodeKind.Scalar || node.IsNull)
            {
                throw Error(node, $"{what} must be a plain value");
            }

            return node.GetValue<string>();
        }

        public string? Text(YamlNode map, string key) =>
            map.TryGetValue(key, out var n) && !n.IsNull ? TextValue(n, key) : null;

        public uint UIntValue(YamlNode node, string what) =>
            uint.TryParse(TextValue(node, what), NumberStyles.None, CultureInfo.InvariantCulture, out var v) ? v : throw Error(node, $"{what} must be a whole number");

        public uint? UInt(YamlNode map, string key, bool required = false)
        {
            if (map.TryGetValue(key, out var n) && !n.IsNull)
            {
                return UIntValue(n, key);
            }

            return required ? throw Error(map, $"'{key}' is required") : null;
        }

        public ulong? ULong(YamlNode map, string key)
        {
            var t = Text(map, key);
            return t is null ? null : ulong.TryParse(t, NumberStyles.None, CultureInfo.InvariantCulture, out var v) ? v : throw Error(map[key], $"{key} must be a whole number");
        }
    }
}
