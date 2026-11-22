using System.Threading.Tasks;
using RecursiveAI.OneModelContext;
using RecursiveAI.OneModelContext.JsonRpc;
using RecursiveAI.OneModelContext.Protocol;

namespace OneGame.Control;

/// <summary>
/// A game's control operations served as Model Context Protocol tools by RecursiveAI.OneModelContext: every <see cref="ControlOp"/> is a tool of the
/// same name with the same schema, and an operation's <see cref="ControlResult"/> is the tool's text (and, for a capture, an image). Version
/// negotiation, framing, errors and cancellation are the library's. The class knows nothing about sockets: <c>OneGame.Live</c> hands it streams,
/// and a page-dialled channel hands it one message at a time through <see cref="OpenSession"/>.
/// </summary>
/// <example>
/// <code>
/// var control = new ControlServer(new ControlSurface(game));
/// await control.Server.RunAsync(ContextTransport.Stream(stream));   // a socket's stream
/// </code>
/// </example>
public sealed class ControlServer
{
    /// <summary>The newest protocol revision of the pre-2026 era that live control has always spoken; older ones negotiate as before.</summary>
    public const string LatestVersion = "2025-06-18";

    /// <summary>Longest message accepted, in bytes.</summary>
    public const int MaxMessageBytes = 8 * 1024 * 1024;

    private readonly Func<string, string?, ValueTask<ControlResult>> _invoke;

    /// <summary>Creates the server for a control endpoint; every call runs on the thread that handles the message.</summary>
    /// <param name="endpoint">The endpoint whose operations become tools.</param>
    /// <exception cref="ArgumentNullException"><paramref name="endpoint"/> is null.</exception>
    public ControlServer(IControlEndpoint endpoint)
        : this(endpoint, null)
    {
    }

    /// <summary>Creates the server, running every call through <paramref name="invoke"/> (a host that runs operations on its own thread).</summary>
    /// <param name="endpoint">The endpoint whose operations become tools.</param>
    /// <param name="invoke">Runs an operation by name with its arguments as JSON text; null calls the endpoint directly.</param>
    /// <exception cref="ArgumentNullException"><paramref name="endpoint"/> is null.</exception>
    public ControlServer(IControlEndpoint endpoint, Func<string, string?, ValueTask<ControlResult>>? invoke)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        _invoke = invoke ?? endpoint.InvokeAsync;
        // A request may carry a scenario or a file as text, so messages are allowed the same 8 MiB a live socket line always was.
        var options = new ContextServerOptions { ReaderOptions = new JsonRpcReaderOptions { MaxMessageBytes = MaxMessageBytes } };
        Server = new ContextServer("onegame", "0.1.0", options) { Instructions = endpoint.Instructions };
        foreach (var op in endpoint.Operations)
        {
            var schema = JsonDocument.Parse(op.InputSchema).RootElement.Clone();
            var definition = new ToolDefinition(op.Name, op.Description, schema)
            {
                Annotations = new ToolAnnotations { ReadOnlyHint = !op.ChangesState },
            };
            var name = op.Name;
            Server.Tools.Add(definition, (arguments, _) => CallAsync(name, arguments));
        }
    }

    /// <summary>The library's server, to run over a stream (<c>ContextTransport.Stream</c>) or any other transport.</summary>
    public ContextServer Server { get; }

    /// <summary>Starts a session that takes whole JSON-RPC messages and returns the answers, for a channel with no stream (a page's WebSocket).</summary>
    public ContextSession OpenSession() => Server.OpenSession();

    private async ValueTask<CallToolResult> CallAsync(string name, JsonElement arguments)
    {
        try
        {
            var result = await _invoke(name, arguments.ValueKind == JsonValueKind.Object ? arguments.GetRawText() : null).ConfigureAwait(false);
            List<ContentBlock> content = [TextContent.Of(result.Json)];
            if (result.Png is { Length: > 0 } png)
            {
                // The server drops images over its inline limit (4 MiB by default) and says so in a text block.
                content.Add(new ImageContent { Data = Convert.ToBase64String(png), MimeType = "image/png" });
            }

            return new CallToolResult { Content = content };
        }
        catch (ControlException ex)
        {
            return Failure(ex.Code, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException)
        {
            // A bug in an operation, or in the game's own code that it ran (a view that throws while a frame is captured): the library would answer
            // "The tool failed" and keep the text to itself, but an agent needs to read what broke, so it travels in the error result.
            return Failure("internal_error", ex.GetType().Name + ": " + ex.Message);
        }
    }

    // The failure a caller can act on is a tool result with isError, as live control has always answered; the text is {"error":{code,message}}.
    private static CallToolResult Failure(string code, string message)
    {
        var buffer = new System.Buffers.ArrayBufferWriter<byte>();
        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();
            w.WriteStartObject("error");
            w.WriteString("code", code);
            w.WriteString("message", message);
            w.WriteEndObject();
            w.WriteEndObject();
        }

        return CallToolResult.FromError(System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan));
    }
}
