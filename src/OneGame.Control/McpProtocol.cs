using System.Buffers;

namespace OneGame.Control;

/// <summary>
/// The Model Context Protocol (https://modelcontextprotocol.io, revision 2025-06-18) for one game: JSON-RPC 2.0 messages in, messages out.
/// Every control operation is a tool of the same name. The class knows nothing about sockets; <c>OneGame.Live</c> moves the messages.
/// </summary>
public sealed class McpProtocol
{
    /// <summary>The newest protocol revision this server speaks.</summary>
    public const string LatestVersion = "2025-06-18";

    private static readonly string[] Supported = ["2025-06-18", "2025-03-26", "2024-11-05"];
    private const int MaxInlineImageBytes = 4 * 1024 * 1024;

    private readonly IControlEndpoint _surface;

    /// <summary>Creates the protocol handler for a game's control surface.</summary>
    public McpProtocol(ControlSurface surface)
        : this((IControlEndpoint)surface)
    {
    }

    /// <summary>Creates the protocol handler for any control endpoint, such as a server's.</summary>
    public McpProtocol(IControlEndpoint endpoint)
    {
        _surface = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
    }

    /// <summary>
    /// Handles one JSON-RPC message (one line of the stdio transport). Returns the response message, or null for a notification.
    /// Never throws on bad input: malformed messages get a JSON-RPC error.
    /// </summary>
    public string? Handle(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(message, new JsonDocumentOptions { MaxDepth = 32 });
        }
        catch (JsonException)
        {
            return Error(null, -32700, "Parse error: the message is not valid JSON.");
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Error(null, -32600, "Invalid request: expected a single JSON-RPC object (batches are not supported).");
            }

            var hasId = root.TryGetProperty("id", out var id) && id.ValueKind is JsonValueKind.String or JsonValueKind.Number;
            if (!root.TryGetProperty("method", out var methodElement) || methodElement.ValueKind != JsonValueKind.String)
            {
                // A response from the client to something we sent: nothing to do.
                return root.TryGetProperty("result", out _) || root.TryGetProperty("error", out _) ? null : Error(hasId ? id : null, -32600, "Invalid request: no method.");
            }

            var method = methodElement.GetString()!;
            if (!hasId)
            {
                return null;
            }

            var parameters = root.TryGetProperty("params", out var p) && p.ValueKind == JsonValueKind.Object ? p : default;
            return method switch
            {
                "initialize" => Initialize(id, parameters),
                "ping" => Result(id, static w =>
                {
                    w.WriteStartObject();
                    w.WriteEndObject();
                }),
                "tools/list" => ListTools(id),
                "tools/call" => CallTool(id, parameters),
                _ => Error(id, -32601, $"Method not found: {method}."),
            };
        }
    }

    private string Initialize(JsonElement id, JsonElement parameters)
    {
        var version = LatestVersion;
        if (parameters.ValueKind == JsonValueKind.Object
            && parameters.TryGetProperty("protocolVersion", out var v)
            && v.ValueKind == JsonValueKind.String
            && Array.IndexOf(Supported, v.GetString()) >= 0)
        {
            version = v.GetString()!;
        }

        return Result(id, w =>
        {
            w.WriteStartObject();
            w.WriteString("protocolVersion", version);
            w.WriteStartObject("capabilities");
            w.WriteStartObject("tools");
            w.WriteBoolean("listChanged", false);
            w.WriteEndObject();
            w.WriteEndObject();
            w.WriteStartObject("serverInfo");
            w.WriteString("name", "onegame");
            w.WriteString("version", "0.1.0");
            w.WriteEndObject();
            w.WriteString("instructions", _surface.Instructions);
            w.WriteEndObject();
        });
    }

    private string ListTools(JsonElement id) => Result(id, w =>
    {
        w.WriteStartObject();
        w.WriteStartArray("tools");
        foreach (var op in _surface.Operations)
        {
            w.WriteStartObject();
            w.WriteString("name", op.Name);
            w.WriteString("description", op.Description);
            w.WritePropertyName("inputSchema");
            using (var schema = JsonDocument.Parse(op.InputSchema))
            {
                schema.RootElement.WriteTo(w);
            }

            w.WriteStartObject("annotations");
            w.WriteBoolean("readOnlyHint", !op.ChangesState);
            w.WriteEndObject();
            w.WriteEndObject();
        }

        w.WriteEndArray();
        w.WriteEndObject();
    });

    private string CallTool(JsonElement id, JsonElement parameters)
    {
        if (parameters.ValueKind != JsonValueKind.Object || !parameters.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
        {
            return Error(id, -32602, "Invalid params: tools/call needs a tool name.");
        }

        var name = nameElement.GetString()!;
        if (!_surface.Operations.Any(o => o.Name == name))
        {
            return Error(id, -32602, $"Unknown tool: {name}.");
        }

        var args = parameters.TryGetProperty("arguments", out var a) && a.ValueKind == JsonValueKind.Object ? a.GetRawText() : null;
        ControlResult? result = null;
        ControlException? failure = null;
        try
        {
            result = _surface.Invoke(name, args);
        }
        catch (ControlException ex)
        {
            failure = ex;
        }

        return Result(id, w =>
        {
            w.WriteStartObject();
            w.WriteStartArray("content");
            w.WriteStartObject();
            w.WriteString("type", "text");
            if (failure is null)
            {
                w.WriteString("text", result!.Json);
            }
            else
            {
                var buffer = new ArrayBufferWriter<byte>();
                using (var ew = new Utf8JsonWriter(buffer))
                {
                    ew.WriteStartObject();
                    ew.WriteStartObject("error");
                    ew.WriteString("code", failure.Code);
                    ew.WriteString("message", failure.Message);
                    ew.WriteEndObject();
                    ew.WriteEndObject();
                }

                w.WriteString("text", Encoding.UTF8.GetString(buffer.WrittenSpan));
            }

            w.WriteEndObject();
            if (failure is null && result!.Png is { Length: > 0 and <= MaxInlineImageBytes } png)
            {
                w.WriteStartObject();
                w.WriteString("type", "image");
                w.WriteString("data", Convert.ToBase64String(png));
                w.WriteString("mimeType", "image/png");
                w.WriteEndObject();
            }

            w.WriteEndArray();
            w.WriteBoolean("isError", failure is not null);
            w.WriteEndObject();
        });
    }

    private static string Result(JsonElement id, Action<Utf8JsonWriter> writeResult)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();
            w.WriteString("jsonrpc", "2.0");
            w.WritePropertyName("id");
            id.WriteTo(w);
            w.WritePropertyName("result");
            writeResult(w);
            w.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static string Error(JsonElement? id, int code, string message)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();
            w.WriteString("jsonrpc", "2.0");
            if (id is { } i)
            {
                w.WritePropertyName("id");
                i.WriteTo(w);
            }
            else
            {
                w.WriteNull("id");
            }

            w.WriteStartObject("error");
            w.WriteNumber("code", code);
            w.WriteString("message", message);
            w.WriteEndObject();
            w.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
