using System.Text.Json.Serialization;
using OneGame.Server;

namespace OneGame.Server.Live;

/// <summary>Result of <c>server_info</c>.</summary>
/// <remarks><c>Listen</c> is where the server accepts clients (ports resolved) and <c>Clients</c> how many are connected.</remarks>
public sealed record ServerInfoResult(string Name, string State, long Tick, int TickRate, ScopeInfo[] Scopes, PluginInfo[] Plugins, PluginLoadFailure[] LoadFailures, ListenEndpoint[] Listen, int Clients);

/// <summary>Result of <c>hash</c> and <c>step</c>: the server tick and each scope's tick and state hash. Compare the hashes with a client's view to check for drift.</summary>
/// <param name="Tick">The server's tick count.</param>
/// <param name="Scopes">One entry per hosted scope.</param>
public sealed record ServerStateResult(long Tick, ScopeInfo[] Scopes);

/// <summary>Result of <c>command_send</c> on a server: the command is queued for the next tick, trusted as if the host sent it.</summary>
/// <param name="Scope">The scope id.</param>
/// <param name="Entity">The entity id in that scope.</param>
/// <param name="Command">The command name.</param>
/// <param name="Status">The command status's name, <c>Queued</c> when accepted.</param>
public sealed record ServerSendResult(uint Scope, ulong Entity, string Command, string Status);

/// <summary>Result of <c>shutdown</c>: the server finishes its current tick, then stops.</summary>
/// <param name="Stopping">True once the stop was requested.</param>
/// <param name="Tick">The tick the server was at.</param>
public sealed record ShutdownResult(bool Stopping, long Tick);

/// <summary>Source-generated JSON metadata for server control results.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, UseStringEnumConverter = true)]
[JsonSerializable(typeof(ServerInfoResult))]
[JsonSerializable(typeof(ServerStateResult))]
[JsonSerializable(typeof(PluginInfo))]
[JsonSerializable(typeof(PluginInfo[]))]
[JsonSerializable(typeof(ReloadResult))]
[JsonSerializable(typeof(UnloadResult))]
[JsonSerializable(typeof(ServerSendResult))]
[JsonSerializable(typeof(ShutdownResult))]
[JsonSerializable(typeof(LogEntry[]))]
[JsonSerializable(typeof(ClientInfo[]))]
[JsonSerializable(typeof(ScopeHashAt[]))]
public sealed partial class ServerJson : JsonSerializerContext
{
}

/// <summary>
/// The control surface of a game server: the operations an agent or the command line uses to look at a running server and to load,
/// reload and unload its plugins. It is an <see cref="IControlEndpoint"/>, so it speaks MCP through the same listener a development
/// game uses. Operations run on the thread that calls them, which must be the server's own, between ticks.
/// </summary>
public sealed class ServerControl : IControlEndpoint
{
    private const string NoArgs = """{"type":"object","properties":{}}""";

    private static readonly IReadOnlyList<ControlOp> All =
    [
        new("server_info", "Describe the running server: state, tick, tick rate, every scope (tick, state hash, entity count, subscribers), every plugin and the plugins that failed to load.", NoArgs, false),
        new("hash", "The tick and state hash of every scope.", NoArgs, false),
        new("step", "Run N server ticks now (default 1).", """{"type":"object","properties":{"ticks":{"type":"integer","minimum":1,"maximum":1000000}}}""", true),
        new("plugin_list", "List the loaded plugins: build, state (loaded or faulted), scopes, entity types, systems, reload count and the last fault.", NoArgs, false),
        new("plugin_load", "Load a plugin. 'path' is a folder holding <name>.dll or the dll. 'scopes' binds it to some scopes (default all). 'settings' is an object of text settings.",
            """{"type":"object","properties":{"name":{"type":"string"},"path":{"type":"string"},"scopes":{"type":"array","items":{"type":"integer"}},"settings":{"type":"object"}},"required":["name","path"]}""", true),
        new("plugin_reload", "Hot-reload a plugin: load the rebuilt dll, save the bound scopes, swap the new types and systems in and restore the state through the entity migrations. Fails with the old build still running.",
            """{"type":"object","properties":{"name":{"type":"string"},"path":{"type":"string"}},"required":["name"]}""", true),
        new("plugin_unload", "Unload a plugin: its entities are removed, its systems stop and its load context is released. Reports whether the context was garbage collected.",
            """{"type":"object","properties":{"name":{"type":"string"}},"required":["name"]}""", true),
        new("command_send", "Send a command to an entity as trusted server input; it runs on the next tick. 'args' are the parameters as text.",
            """{"type":"object","properties":{"scope":{"type":"integer"},"entity":{"type":"integer"},"command":{"type":"string"},"args":{"type":"array","items":{"type":"string"}}},"required":["entity","command"]}""", true),
        new("state_query", "Read a scope's synced state as JSON, or one entity's with 'entity' (a numeric id, a type name for the first of that type, or 'Type#n'), or one value with 'property' too.",
            """{"type":"object","properties":{"scope":{"type":"integer"},"entity":{"type":["string","integer"]},"property":{"type":"string"}}}""", false),
        new("clients", "The connected network clients: name, transport, address, state, round-trip time, loss and retransmits.", NoArgs, false),
        new("hash_at", "The recorded state hash of every scope at one tick (null when not recorded; see hashHistory). Compare it with a client's hash at the same tick.",
            """{"type":"object","properties":{"tick":{"type":"integer","minimum":0}},"required":["tick"]}""", false),
        new("log_tail", "The last N server log lines (default 50).", """{"type":"object","properties":{"max":{"type":"integer"}}}""", false),
        new("shutdown", "Ask the server to finish its tick, run plugin unload hooks and stop.", NoArgs, true),
    ];

    private readonly ServerHost _host;

    /// <summary>Creates the control surface for a server.</summary>
    /// <param name="host">The server.</param>
    public ServerControl(ServerHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    /// <inheritdoc />
    public IReadOnlyList<ControlOp> Operations => All;

    /// <inheritdoc />
    public string Instructions => "Controls one running onegame game server. Call server_info first. plugin_load, plugin_reload, plugin_unload, step and command_send change the server; the rest only read.";

    /// <summary>Runs an operation and returns its JSON text.</summary>
    /// <param name="op">The operation name.</param>
    /// <param name="argsJson">A JSON object, or null.</param>
    /// <exception cref="ControlException">The operation failed; <see cref="ControlException.Code"/> says why.</exception>
    public string Call(string op, string? argsJson = null) => Invoke(op, argsJson).Json;

    /// <inheritdoc />
    public ControlResult Invoke(string op, string? argsJson = null)
    {
        ArgumentNullException.ThrowIfNull(op);
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argsJson) ? "{}" : argsJson, new JsonDocumentOptions { MaxDepth = 16 });
        }
        catch (JsonException ex)
        {
            throw new ControlException("bad_args", "The arguments are not valid JSON: " + ex.Message, ex);
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ControlException("bad_args", "The arguments must be a JSON object.");
            }

            try
            {
                return new ControlResult(Dispatch(op, doc.RootElement));
            }
            catch (PluginException ex)
            {
                throw new ControlException(ex.Code, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                throw new ControlException("bad_state", ex.Message, ex);
            }
        }
    }

    private string Dispatch(string op, JsonElement a) => op switch
    {
        "server_info" => Serialize(Info(), ServerJson.Default.ServerInfoResult),
        "hash" => Serialize(State(), ServerJson.Default.ServerStateResult),
        "step" => Step(a),
        "plugin_list" => Serialize([.. _host.Plugins()], ServerJson.Default.PluginInfoArray),
        "plugin_load" => Serialize(_host.LoadPlugin(LoadSpec(a)), ServerJson.Default.PluginInfo),
        "plugin_reload" => Serialize(_host.ReloadPlugin(Required(a, "name"), Str(a, "path")), ServerJson.Default.ReloadResult),
        "plugin_unload" => Serialize(_host.UnloadPlugin(Required(a, "name")), ServerJson.Default.UnloadResult),
        "command_send" => Send(a),
        "state_query" => Query(a),
        "clients" => Serialize(_host.Clients(), ServerJson.Default.ClientInfoArray),
        "hash_at" => Serialize(_host.HashesAt(Long(a, "tick") ?? throw new ControlException("bad_args", "tick is required.")), ServerJson.Default.ScopeHashAtArray),
        "log_tail" => Serialize([.. _host.Log.Tail(Math.Clamp(Int(a, "max") ?? 50, 1, 512))], ServerJson.Default.LogEntryArray),
        "shutdown" => Shutdown(),
        _ => throw new ControlException("unknown_op", $"Unknown operation '{op}'. Known: {string.Join(", ", All.Select(o => o.Name))}."),
    };

    private static string Serialize<T>(T value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> info) => JsonSerializer.Serialize(value, info);

    private ServerInfoResult Info() => new(_host.Config.Name, _host.State.ToString().ToLowerInvariant(), _host.Tick, _host.Config.TickRate, [.. _host.ScopeInfos()], [.. _host.Plugins()], [.. _host.LoadFailures], [.. _host.Endpoints], _host.Clients().Length);

    private ServerStateResult State() => new(_host.Tick, [.. _host.ScopeInfos()]);

    private string Step(JsonElement a)
    {
        var ticks = Int(a, "ticks") ?? 1;
        if (ticks is < 1 or > 1_000_000)
        {
            throw new ControlException("bad_args", "ticks must be between 1 and 1000000.");
        }

        _host.Step(ticks);
        return Serialize(State(), ServerJson.Default.ServerStateResult);
    }

    private string Send(JsonElement a)
    {
        var scope = (uint)(Int(a, "scope") ?? 1);
        var entity = Int(a, "entity") ?? throw new ControlException("bad_args", "entity is required.");
        var command = Required(a, "command");
        var args = new List<string>();
        if (a.TryGetProperty("args", out var list) && list.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in list.EnumerateArray())
            {
                args.Add(item.ValueKind == JsonValueKind.String ? item.GetString()! : item.GetRawText());
            }
        }

        var status = _host.SendCommand(new ScopeId(scope), new EntityId((ulong)entity), command, [.. args]);
        if (status is CommandStatus.UnknownCommand or CommandStatus.Malformed)
        {
            throw new ControlException("rejected", $"The command '{command}' was {status}. Check its name and arguments.");
        }

        return Serialize(new ServerSendResult(scope, (ulong)entity, command, status.ToString()), ServerJson.Default.ServerSendResult);
    }

    private string Query(JsonElement a)
    {
        var id = new ScopeId((uint)(Int(a, "scope") ?? 1));
        var scope = _host.FindScope(id) ?? throw new ControlException("not_found", $"There is no {id}.");
        if (!a.TryGetProperty("entity", out var spec) || spec.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return scope.World.DumpJson();
        }

        var e = Resolve(scope.World, id, spec.ValueKind == JsonValueKind.String ? spec.GetString()! : spec.GetRawText());
        var json = new JsonOut();
        e.WriteJson(json);
        var text = json.ToString();
        if (Str(a, "property") is not { } property)
        {
            return text;
        }

        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.TryGetProperty(property, out var v)
            ? v.GetRawText()
            : throw new ControlException("not_found", $"{e.TypeName} has no property '{property}'.");
    }

    // An id, a type name (the first entity of that type) or Type#n (the nth, from 1), as the game control surface accepts them.
    private static Entity Resolve(World world, ScopeId scope, string spec)
    {
        if (ulong.TryParse(spec, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            return world.Find(new EntityId(number)) ?? throw new ControlException("not_found", $"{scope} has no entity {spec}.");
        }

        var hash = spec.IndexOf('#', StringComparison.Ordinal);
        var type = hash < 0 ? spec : spec[..hash];
        var nth = 1;
        if (hash >= 0 && !int.TryParse(spec[(hash + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out nth))
        {
            throw new ControlException("bad_args", $"'{spec}' is not an entity: use an id, a type name or Type#n.");
        }

        foreach (var e in world.Entities)
        {
            if ((e.TypeName == type || e.TypeName.EndsWith("." + type, StringComparison.Ordinal)) && --nth == 0)
            {
                return e;
            }
        }

        throw new ControlException("not_found", $"{scope} has no entity '{spec}'.");
    }

    private string Shutdown()
    {
        _host.RequestStop();
        return Serialize(new ShutdownResult(true, _host.Tick), ServerJson.Default.ShutdownResult);
    }

    private static PluginConfig LoadSpec(JsonElement a)
    {
        var scopes = new List<uint>();
        if (a.TryGetProperty("scopes", out var s) && s.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in s.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Number || !item.TryGetUInt32(out var v))
                {
                    throw new ControlException("bad_args", "scopes must be whole numbers.");
                }

                scopes.Add(v);
            }
        }

        var settings = new List<KeyValuePair<string, string>>();
        if (a.TryGetProperty("settings", out var st) && st.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in st.EnumerateObject())
            {
                settings.Add(new(p.Name, p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString()! : p.Value.GetRawText()));
            }
        }

        var path = Path.GetFullPath(Required(a, "path"));
        return new PluginConfig(Required(a, "name"), path, [.. scopes], [.. settings]);
    }

    private static string Required(JsonElement a, string key) =>
        Str(a, key) ?? throw new ControlException("bad_args", $"'{key}' is required.");

    private static string? Str(JsonElement a, string key) =>
        a.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static long? Long(JsonElement a, string key) =>
        a.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var n) ? n : null;

    private static int? Int(JsonElement a, string key) =>
        a.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n) ? n : null;
}
