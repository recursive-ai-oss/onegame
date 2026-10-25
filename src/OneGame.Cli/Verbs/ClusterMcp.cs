using System.Buffers;
using System.Globalization;
using OneGame.Control;
using OneGame.Host;
using OneGame.Testing;

namespace OneGame.Cli.Verbs;

/// <summary>
/// One MCP server (stdio, one JSON-RPC message per line) for every node of a process cluster. The tool list is the union of the nodes'
/// tools, each with an extra <c>node</c> argument naming where to run it; a call goes to that node's live socket and its result comes back
/// unchanged. An agent configures this command once and drives servers and clients alike.
/// </summary>
internal static class ClusterMcp
{
    private const int MaxLineBytes = 8 * 1024 * 1024;

    public static int Serve(string where, Stream input, Stream output)
    {
        using var cluster = ProcessCluster.Attach(where);
        using var reader = new StreamReader(input, Encoding.UTF8);
        using var writer = new StreamWriter(output, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0 || line.Length > MaxLineBytes)
            {
                continue;
            }

            var reply = Handle(cluster, line);
            if (reply is not null)
            {
                writer.WriteLine(reply);
            }
        }

        return ExitCodes.Ok;
    }

    private static string? Handle(ProcessCluster cluster, string line)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(line, new JsonDocumentOptions { MaxDepth = 32 });
        }
        catch (JsonException)
        {
            return Error("null", -32700, "parse error");
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("method", out var m))
            {
                return null;
            }

            var id = root.TryGetProperty("id", out var idEl) ? idEl.GetRawText() : null;
            var method = m.GetString();
            if (id is null)
            {
                return null; // a notification: nothing to answer
            }

            try
            {
                return method switch
                {
                    "initialize" => Result(id, "{\"protocolVersion\":\"2025-06-18\",\"capabilities\":{\"tools\":{}},\"serverInfo\":{\"name\":\"onegame-cluster\",\"version\":" + Quote(Program.GetVersion()) + "},\"instructions\":" + Quote($"Controls every node of the process cluster '{cluster.State.Name}' ({string.Join(", ", cluster.State.Nodes.Select(n => n.Name + " (" + n.Role + ")"))}). Every tool takes a 'node' argument. Servers: server_info, hash, hash_at, clients. Clients: info, net_status, net_wait, command_send, state_query.") + "}"),
                    "ping" => Result(id, "{}"),
                    "tools/list" => Result(id, ToolList(cluster)),
                    "tools/call" => Call(cluster, id, root.GetProperty("params")),
                    _ => Error(id, -32601, "method not found: " + method),
                };
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or JsonException)
            {
                return Error(id, -32602, "bad request: " + ex.Message);
            }
        }
    }

    private static string Call(ProcessCluster cluster, string id, JsonElement p)
    {
        var tool = p.GetProperty("name").GetString() ?? "";
        string? node = null;
        var args = new ArrayBufferWriter<byte>();
        using (var w = new Utf8JsonWriter(args))
        {
            w.WriteStartObject();
            if (p.TryGetProperty("arguments", out var a) && a.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in a.EnumerateObject())
                {
                    if (prop.Name == "node")
                    {
                        node = prop.Value.GetString();
                    }
                    else
                    {
                        prop.WriteTo(w);
                    }
                }
            }

            w.WriteEndObject();
        }

        try
        {
            node ??= OnlyNodeOffering(cluster, tool);
            var text = cluster.Call(node, tool, Encoding.UTF8.GetString(args.WrittenSpan));
            return Result(id, "{\"content\":[{\"type\":\"text\",\"text\":" + Quote(text) + "}]}");
        }
        catch (Exception ex) when (ex is ControlException or ClusterException)
        {
            var code = ex is ControlException c ? c.Code : ((ClusterException)ex).Code;
            var body = "{\"ok\":false,\"error\":{\"code\":" + Quote(code) + ",\"message\":" + Quote(ex.Message) + "}}";
            return Result(id, "{\"isError\":true,\"content\":[{\"type\":\"text\",\"text\":" + Quote(body) + "}]}");
        }
    }

    // Without a node argument a tool runs where it is unambiguous.
    private static string OnlyNodeOffering(ProcessCluster cluster, string tool)
    {
        var offering = cluster.State.Nodes.Where(n => cluster.Operations(n.Name).Contains(tool)).Select(n => n.Name).ToArray();
        return offering.Length == 1
            ? offering[0]
            : throw new ControlException("bad_args", $"'{tool}' is offered by {(offering.Length == 0 ? "no node" : string.Join(", ", offering))}; add a 'node' argument naming one of: {string.Join(", ", cluster.State.Nodes.Select(n => n.Name))}.");
    }

    private static string ToolList(ProcessCluster cluster)
    {
        // name -> (description, schema, nodes offering it)
        var tools = new SortedDictionary<string, (string Description, string Schema, List<string> Nodes)>(StringComparer.Ordinal);
        foreach (var n in cluster.State.Nodes)
        {
            using var client = new LiveClient(n.Socket, 10_000);
            using var doc = JsonDocument.Parse(client.ToolsJson());
            foreach (var t in doc.RootElement.GetProperty("tools").EnumerateArray())
            {
                var name = t.GetProperty("name").GetString() ?? "";
                if (tools.TryGetValue(name, out var existing))
                {
                    existing.Nodes.Add(n.Name);
                }
                else
                {
                    tools[name] = (t.GetProperty("description").GetString() ?? "", t.GetProperty("inputSchema").GetRawText(), [n.Name]);
                }
            }
        }

        var buffer = new ArrayBufferWriter<byte>();
        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();
            w.WriteStartArray("tools");
            foreach (var (name, (description, schema, nodes)) in tools)
            {
                w.WriteStartObject();
                w.WriteString("name", name);
                w.WriteString("description", description + " Runs on the node named by 'node' (offered by: " + string.Join(", ", nodes) + ").");
                w.WritePropertyName("inputSchema");
                w.WriteStartObject();
                w.WriteString("type", "object");
                w.WritePropertyName("properties");
                w.WriteStartObject();
                using var s = JsonDocument.Parse(schema);
                if (s.RootElement.TryGetProperty("properties", out var props))
                {
                    foreach (var prop in props.EnumerateObject())
                    {
                        prop.WriteTo(w);
                    }
                }

                w.WriteStartObject("node");
                w.WriteString("type", "string");
                w.WriteStartArray("enum");
                foreach (var n in nodes)
                {
                    w.WriteStringValue(n);
                }

                w.WriteEndArray();
                w.WriteEndObject();
                w.WriteEndObject();
                w.WriteStartArray("required");
                if (s.RootElement.TryGetProperty("required", out var req))
                {
                    foreach (var r in req.EnumerateArray())
                    {
                        r.WriteTo(w);
                    }
                }

                if (nodes.Count > 1)
                {
                    w.WriteStringValue("node");
                }

                w.WriteEndArray();
                w.WriteEndObject();
                w.WriteEndObject();
            }

            w.WriteEndArray();
            w.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static string Quote(string s) => JsonSerializer.Serialize(s, CliJson.Default.String);

    private static string Result(string id, string result) => "{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"result\":" + result + "}";

    private static string Error(string id, int code, string message) => "{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"error\":{\"code\":" + code.ToString(CultureInfo.InvariantCulture) + ",\"message\":" + Quote(message) + "}}";
}
