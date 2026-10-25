using OneYaml;

namespace OneGame.Testing;

/// <summary>What a node of a process cluster is.</summary>
public enum NodeRole
{
    /// <summary>A dedicated server: it listens and owns scopes.</summary>
    Server,

    /// <summary>A game (a development build, headless) connected to a server.</summary>
    Client,
}

/// <summary>A scope a server node hosts.</summary>
/// <param name="Id">The scope id.</param>
/// <param name="Seed">The seed of its random streams.</param>
public sealed record ScopeSpec(uint Id, ulong Seed);

/// <summary>One process of a cluster.</summary>
public sealed record NodeSpec
{
    /// <summary>The node's name; unique. Scenarios and operations address it by this.</summary>
    public required string Name { get; init; }

    /// <summary>Server or client.</summary>
    public NodeRole Role { get; init; }

    /// <summary>The program to start: an executable, or a <c>.dll</c> that is run with <c>dotnet</c>. Relative paths resolve against the spec file.</summary>
    public required string Exe { get; init; }

    /// <summary>Extra command-line arguments, before the ones the harness adds.</summary>
    public IReadOnlyList<string> Args { get; init; } = [];

    /// <summary>Servers: what to listen on, <c>udp</c>, <c>ws</c> or <c>udp+ws</c>. Ports are picked by the system.</summary>
    public string Listen { get; init; } = "udp";

    /// <summary>Servers: the scopes to host (scope 1 with seed 1 when empty).</summary>
    public IReadOnlyList<ScopeSpec> Scopes { get; init; } = [];

    /// <summary>Servers: ticks per second; the server's default when 0.</summary>
    public int TickRate { get; init; }

    /// <summary>Servers: tick only on the <c>step</c> operation, so a test decides exactly when time passes.</summary>
    public bool Manual { get; init; }

    /// <summary>Clients: the server node to connect to.</summary>
    public string? Connect { get; init; }

    /// <summary>Clients: the transport to reach it by, <c>udp</c> or <c>ws</c>.</summary>
    public string Transport { get; init; } = "udp";

    /// <summary>Clients: the scope to subscribe to.</summary>
    public uint Scope { get; init; } = 1;

    /// <summary>Clients: network conditions applied to this client's traffic, as <c>latency=100,loss=0.02</c>.</summary>
    public string? Net { get; init; }

    /// <summary>
    /// Clients: an exported release build, which has no live control. It runs on its own (give it <c>--until-tick N</c> in <see cref="Args"/>), prints
    /// its result when it stops, and the cluster reads that result to compare it with its server.
    /// </summary>
    public bool Release { get; init; }
}

/// <summary>The nodes of a process cluster and how long to wait for them.</summary>
public sealed record ClusterSpec
{
    private static readonly string[] TopKeys = ["name", "dir", "startupTimeout", "nodes"];
    private static readonly string[] NodeKeys = ["name", "role", "exe", "args", "listen", "scopes", "tickRate", "manual", "connect", "transport", "scope", "net", "release"];
    private static readonly string[] ScopeKeys = ["id", "seed"];

    /// <summary>The cluster's name.</summary>
    public string Name { get; init; } = "cluster";

    /// <summary>Where logs, sockets, generated files and <c>cluster.json</c> go. A fresh temporary folder when null.</summary>
    public string? Dir { get; init; }

    /// <summary>How long each node may take to come up, in milliseconds.</summary>
    public int StartupTimeoutMs { get; init; } = 60_000;

    /// <summary>The nodes. Servers start first, then clients in order.</summary>
    public IReadOnlyList<NodeSpec> Nodes { get; init; } = [];

    /// <summary>Reads a spec file.</summary>
    /// <exception cref="ScenarioException">The file is not a valid cluster spec.</exception>
    public static ClusterSpec Load(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length > 1024 * 1024)
        {
            throw new ScenarioException(info.Exists ? "the spec file is larger than 1 MiB" : "the spec file does not exist", path, 0);
        }

        return Parse(File.ReadAllText(path), path);
    }

    /// <summary>Reads spec text. Relative <c>exe</c> and <c>dir</c> paths resolve against the folder of <paramref name="source"/>.</summary>
    /// <exception cref="ScenarioException">The text is not a valid cluster spec.</exception>
    public static ClusterSpec Parse(string yaml, string? source = null)
    {
        ArgumentNullException.ThrowIfNull(yaml);
        YamlDocument doc;
        try
        {
            doc = YamlDocument.Parse(yaml, new YamlDocumentOptions());
        }
        catch (YamlException ex)
        {
            throw new ScenarioException(ex.Message, source, ex.Line);
        }

        using (doc)
        {
            var baseDir = source is null ? Directory.GetCurrentDirectory() : System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(source))!;
            var root = doc.Root;
            Check(root, TopKeys, "the cluster spec", source);
            var nodes = new List<NodeSpec>();
            if (root.TryGetValue("nodes", out var nodesNode) && !nodesNode.IsNull)
            {
                if (nodesNode.Kind != YamlNodeKind.Sequence)
                {
                    throw Error(nodesNode, "nodes must be a list", source);
                }

                foreach (var item in nodesNode.Items)
                {
                    nodes.Add(ReadNode(item, baseDir, source));
                    if (nodes.Count > 256)
                    {
                        throw Error(item, "more than 256 nodes", source);
                    }
                }
            }

            var spec = new ClusterSpec
            {
                Name = Text(root, "name", source) ?? "cluster",
                Dir = Text(root, "dir", source) is { } dir ? System.IO.Path.GetFullPath(dir, baseDir) : null,
                StartupTimeoutMs = Whole(root, "startupTimeout", source) ?? 60_000,
                Nodes = nodes,
            };
            spec.Validate(source);
            return spec;
        }
    }

    /// <summary>Checks the nodes against each other.</summary>
    /// <exception cref="ScenarioException">A name is repeated, a client has no server, or a transport is not offered.</exception>
    public void Validate(string? source = null)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var n in Nodes)
        {
            if (!names.Add(n.Name))
            {
                throw new ScenarioException($"node '{n.Name}' is declared twice", source, 0);
            }
        }

        foreach (var n in Nodes.Where(n => n.Role == NodeRole.Client))
        {
            if (n.Release && !n.Args.Contains("--until-tick"))
            {
                throw new ScenarioException($"client '{n.Name}' is a release build with no live control; give it args: [--until-tick, N] so it stops by itself", source, 0);
            }

            var server = Nodes.FirstOrDefault(s => s.Name == n.Connect && s.Role == NodeRole.Server)
                ?? throw new ScenarioException($"client '{n.Name}' connects to '{n.Connect}', which is not a server node of this cluster", source, 0);
            if (!server.Listen.Split('+').Contains(n.Transport))
            {
                throw new ScenarioException($"client '{n.Name}' uses {n.Transport}, but server '{server.Name}' listens on {server.Listen}", source, 0);
            }
        }
    }

    private static NodeSpec ReadNode(YamlNode item, string baseDir, string? source)
    {
        Check(item, NodeKeys, "a node", source);
        var name = Text(item, "name", source) ?? throw Error(item, "a node needs a name", source);
        var role = Text(item, "role", source) switch
        {
            "server" => NodeRole.Server,
            "client" => NodeRole.Client,
            null => throw Error(item, $"node '{name}' needs a role (server or client)", source),
            _ => throw Error(item["role"], "role is server or client", source),
        };
        var exe = Text(item, "exe", source) ?? throw Error(item, $"node '{name}' needs an exe", source);
        var listen = Text(item, "listen", source) ?? "udp";
        if (listen is not ("udp" or "ws" or "udp+ws"))
        {
            throw Error(item["listen"], "listen is udp, ws or udp+ws", source);
        }

        var transport = Text(item, "transport", source) ?? "udp";
        if (transport is not ("udp" or "ws"))
        {
            throw Error(item["transport"], "transport is udp or ws", source);
        }

        var args = new List<string>();
        if (item.TryGetValue("args", out var argsNode) && !argsNode.IsNull)
        {
            if (argsNode.Kind != YamlNodeKind.Sequence)
            {
                throw Error(argsNode, "args must be a list", source);
            }

            foreach (var a in argsNode.Items)
            {
                args.Add(Scalar(a, "an argument", source));
            }
        }

        var scopes = new List<ScopeSpec>();
        if (item.TryGetValue("scopes", out var scopesNode) && !scopesNode.IsNull)
        {
            if (scopesNode.Kind != YamlNodeKind.Sequence)
            {
                throw Error(scopesNode, "scopes must be a list", source);
            }

            foreach (var s in scopesNode.Items)
            {
                Check(s, ScopeKeys, "a scope", source);
                scopes.Add(new ScopeSpec((uint)(Whole(s, "id", source) ?? throw Error(s, "a scope needs an id", source)), (ulong)(Whole(s, "seed", source) ?? 1)));
            }
        }

        return new NodeSpec
        {
            Name = name,
            Role = role,
            Exe = System.IO.Path.IsPathRooted(exe) ? exe : System.IO.Path.GetFullPath(exe, baseDir),
            Args = args,
            Listen = listen,
            Scopes = scopes,
            TickRate = Whole(item, "tickRate", source) ?? 0,
            Manual = Text(item, "manual", source) == "true",
            Connect = Text(item, "connect", source),
            Transport = transport,
            Scope = (uint)(Whole(item, "scope", source) ?? 1),
            Net = NetText(item, source),
            Release = Text(item, "release", source) == "true",
        };
    }

    // net: either the text form (latency=100,loss=0.02) or a mapping (latencyMs, jitterMs, lossRate, ...).
    private static string? NetText(YamlNode item, string? source)
    {
        if (!item.TryGetValue("net", out var net) || net.IsNull)
        {
            return null;
        }

        if (net.Kind == YamlNodeKind.Scalar)
        {
            return net.GetValue<string>();
        }

        if (net.Kind != YamlNodeKind.Mapping)
        {
            throw Error(net, "net is text like latency=100,loss=0.02 or a mapping", source);
        }

        var parts = new List<string>();
        foreach (var entry in net.Entries)
        {
            var key = entry.Key.GetValue<string>() switch
            {
                "latencyMs" or "latency" => "latency",
                "jitterMs" or "jitter" => "jitter",
                "lossRate" or "loss" => "loss",
                "duplicateRate" or "duplicate" => "duplicate",
                "reorderRate" or "reorder" => "reorder",
                "bandwidth" => "bandwidth",
                "seed" => "seed",
                var other => throw Error(entry.Key, $"unknown network setting '{other}'", source),
            };
            parts.Add(key + "=" + Scalar(entry.Value, key, source));
        }

        return string.Join(',', parts);
    }

    private static ScenarioException Error(YamlNode at, string message, string? source) => new(message, source, at.Position.Line);

    private static void Check(YamlNode node, string[] allowed, string what, string? source)
    {
        if (node.Kind != YamlNodeKind.Mapping)
        {
            throw Error(node, $"{what} must be a mapping of {string.Join(", ", allowed)}", source);
        }

        foreach (var entry in node.Entries)
        {
            var key = entry.Key.GetValue<string>();
            if (Array.IndexOf(allowed, key) < 0)
            {
                throw Error(entry.Key, $"unknown key '{key}' in {what}; allowed: {string.Join(", ", allowed)}", source);
            }
        }
    }

    private static string Scalar(YamlNode node, string what, string? source) =>
        node.Kind == YamlNodeKind.Scalar && !node.IsNull ? node.GetValue<string>() : throw Error(node, $"{what} must be a plain value", source);

    private static string? Text(YamlNode map, string key, string? source) =>
        map.TryGetValue(key, out var n) && !n.IsNull ? Scalar(n, key, source) : null;

    private static int? Whole(YamlNode map, string key, string? source)
    {
        var t = Text(map, key, source);
        return t is null ? null : int.TryParse(t, NumberStyles.None, CultureInfo.InvariantCulture, out var v) ? v : throw Error(map[key], $"{key} must be a whole number", source);
    }
}
