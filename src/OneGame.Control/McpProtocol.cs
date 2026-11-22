using System.Threading.Tasks;
using RecursiveAI.OneModelContext;

namespace OneGame.Control;

/// <summary>
/// One MCP session for a game's control surface: JSON-RPC 2.0 messages in, messages out. Every control operation is a tool of the same name.
/// It is now a thin adapter over <see cref="ControlServer"/> (RecursiveAI.OneModelContext does the protocol); new code uses that class.
/// </summary>
[Obsolete("Use ControlServer: its Server runs over a stream, and OpenSession handles messages one at a time.")]
public sealed class McpProtocol
{
    /// <summary>The newest protocol revision this server has always spoken.</summary>
    public const string LatestVersion = ControlServer.LatestVersion;

    private readonly ContextSession _session;

    /// <summary>Creates the protocol handler for a game's control surface.</summary>
    /// <param name="surface">The game's control surface.</param>
    public McpProtocol(ControlSurface surface)
        : this((IControlEndpoint)surface)
    {
    }

    /// <summary>Creates the protocol handler for any control endpoint, such as a server's.</summary>
    /// <param name="endpoint">The endpoint whose operations become MCP tools.</param>
    /// <exception cref="ArgumentNullException"><paramref name="endpoint"/> is null.</exception>
    public McpProtocol(IControlEndpoint endpoint)
    {
        _session = new ControlServer(endpoint).OpenSession();
    }

    /// <summary>
    /// Handles one JSON-RPC message (one line of the stdio transport). Returns the response message, or null for a notification.
    /// Never throws on bad input: malformed messages get a JSON-RPC error.
    /// </summary>
    /// <param name="message">One JSON-RPC message.</param>
    public string? Handle(string message) => HandleAsync(message).AsTask().GetAwaiter().GetResult();

    /// <summary>Like <see cref="Handle"/>, for hosts that cannot block while a tool waits for the graphics device (a browser page).</summary>
    /// <param name="message">One JSON-RPC message.</param>
    public ValueTask<string?> HandleAsync(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return _session.HandleAsync(message);
    }
}
