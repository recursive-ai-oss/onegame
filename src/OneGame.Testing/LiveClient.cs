using System.Net.Sockets;
using OneGame.Control;

namespace OneGame.Testing;

/// <summary>
/// A client of a running process's live control socket: one connection that sends operations as MCP tool calls and returns their JSON
/// results. This is what an agent's MCP bridge does, so a harness that uses it sees exactly what an agent sees.
/// </summary>
public sealed class LiveClient : IDisposable
{
    private const int MaxReplyBytes = 64 * 1024 * 1024;
    private readonly string _path;
    private readonly int _timeoutMs;
    private Socket? _socket;
    private int _id;

    /// <summary>Creates a client; it connects on the first call.</summary>
    /// <param name="socketPath">The Unix socket the process listens on.</param>
    /// <param name="timeoutMs">How long to wait for a reply to one call.</param>
    public LiveClient(string socketPath, int timeoutMs = 60_000)
    {
        ArgumentException.ThrowIfNullOrEmpty(socketPath);
        _path = socketPath;
        _timeoutMs = timeoutMs;
    }

    /// <summary>The socket path.</summary>
    public string Path => _path;

    /// <summary>Runs an operation and returns its result JSON.</summary>
    /// <param name="op">The operation name.</param>
    /// <param name="argsJson">A JSON object, or null.</param>
    /// <exception cref="ControlException">The operation failed (its code is the process's), or the process could not be reached (<c>unreachable</c>).</exception>
    public string Call(string op, string? argsJson = null) => Request("tools/call", "{\"name\":" + Quote(op) + ",\"arguments\":" + (argsJson ?? "{}") + "}", op);

    /// <summary>The names of the operations the process offers (its MCP tool list).</summary>
    public string[] Tools()
    {
        using var doc = JsonDocument.Parse(Request("tools/list", "{}", "tools/list"));
        var names = new List<string>();
        foreach (var t in doc.RootElement.GetProperty("tools").EnumerateArray())
        {
            names.Add(t.GetProperty("name").GetString() ?? "");
        }

        return [.. names];
    }

    /// <summary>The process's MCP tool list as JSON: each tool's name, description and input schema.</summary>
    public string ToolsJson() => Request("tools/list", "{}", "tools/list");

    private string Request(string method, string paramsJson, string what)
    {
        try
        {
            _socket ??= Connect();
            var request = "{\"jsonrpc\":\"2.0\",\"id\":" + (++_id).ToString(CultureInfo.InvariantCulture) + ",\"method\":" + Quote(method) + ",\"params\":" + paramsJson + "}\n";
            _socket.Send(Encoding.UTF8.GetBytes(request));
            var line = ReadLine(_socket);
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.TryGetProperty("error", out var rpc))
            {
                throw new ControlException("rpc_error", rpc.TryGetProperty("message", out var m) ? m.GetString() ?? "error" : "error");
            }

            var result = root.GetProperty("result");
            if (method == "tools/list")
            {
                return result.GetRawText();
            }

            var content = result.GetProperty("content");
            var text = content.GetArrayLength() > 0 && content[0].TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
            if (result.TryGetProperty("isError", out var isError) && isError.GetBoolean())
            {
                using var inner = JsonDocument.Parse(text);
                var e = inner.RootElement.GetProperty("error");
                throw new ControlException(e.GetProperty("code").GetString() ?? "error", e.GetProperty("message").GetString() ?? "");
            }

            return text;
        }
        catch (Exception ex) when (ex is SocketException or IOException or ObjectDisposedException or JsonException)
        {
            Reset();
            throw new ControlException("unreachable", $"Cannot reach the process at {_path} for '{what}': {ex.Message}", ex);
        }
    }

    private Socket Connect()
    {
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified) { ReceiveTimeout = _timeoutMs, SendTimeout = _timeoutMs };
        try
        {
            socket.Connect(new UnixDomainSocketEndPoint(_path));
            return socket;
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static string ReadLine(Socket socket)
    {
        var line = new MemoryStream();
        var one = new byte[4096];
        while (true)
        {
            var n = socket.Receive(one);
            if (n == 0)
            {
                throw new IOException("the process closed the connection");
            }

            var end = Array.IndexOf(one, (byte)'\n', 0, n);
            line.Write(one, 0, end < 0 ? n : end);
            if (line.Length > MaxReplyBytes)
            {
                throw new IOException("the reply is too long");
            }

            if (end >= 0)
            {
                return Encoding.UTF8.GetString(line.GetBuffer(), 0, (int)line.Length);
            }
        }
    }

    private void Reset()
    {
        _socket?.Dispose();
        _socket = null;
    }

    private static string Quote(string s) => JsonSerializer.Serialize(s, TestJson.Default.String);

    /// <inheritdoc />
    public void Dispose() => Reset();
}
