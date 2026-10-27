using System.Buffers;
using System.Runtime.Versioning;
using OneGame.Control;
using OneGame.Host;
using OneGame.Platform;
using OneGame.Testing;

namespace OneGame.Live;

/// <summary>
/// Live control for a game running in a web page. A page cannot listen for connections, so it dials out: the tool that served the page
/// (<c>onegame web test</c>, a test runner) runs a WebSocket endpoint and the page connects to it with <c>?control=ws://…</c>.
/// With <c>?control=page</c> there is no socket: the page's script calls <c>__control(message)</c> (MCP JSON-RPC) directly, which is how tests drive the page with injected input.
/// Messages are UTF-8 JSON in binary frames (the page's socket helper is binary-only). Each request carries an <c>id</c> and an <c>op</c>; the reply repeats the id.
/// <list type="bullet">
/// <item><c>put</c> (<c>path</c>, <c>data</c> base64), <c>get</c> (<c>path</c>) and <c>list</c> (<c>path</c>, a folder): files in the page's in-memory file system, for scenarios and references.</item>
/// <item><c>mcp</c> (<c>message</c>): one MCP JSON-RPC message, answered with <c>reply</c>; the same operations as the desktop live listener.</item>
/// <item><c>scenarios</c> (<c>target</c>, <c>artifacts</c>, <c>updateReferences</c>, optional <c>tolerance</c> and <c>maxDiff</c> to loosen capture checks): runs scenario files and returns the result object <c>onegame test</c> prints.</item>
/// <item><c>pause</c> (<c>paused</c>): holds the page's simulation still while it keeps drawing.</item>
/// </list>
/// </summary>
[SupportedOSPlatform("browser")]
internal static class WebControl
{
    private const int MaxFileBytes = 64 * 1024 * 1024;

    public static void Start(GameInstance game, ControlSurface surface, string url)
    {
        var mcp = new McpProtocol(surface);
        BrowserPage.SetControlHandler(async message => await mcp.HandleAsync(message).ConfigureAwait(true));
        if (url != "page")
        {
            _ = Guarded(game, surface, url);
        }
    }

    private static async Task Guarded(GameInstance game, ControlSurface surface, string url)
    {
        try
        {
            await Loop(game, surface, url).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Nothing awaits this task: say why the channel ended.
            Console.Error.WriteLine("onegame: the control channel stopped: " + ex);
        }
    }

    private static async Task Loop(GameInstance game, ControlSurface surface, string url)
    {
        var socket = new BrowserWebSocket(url);
        var mcp = new McpProtocol(surface);
        var waited = 0;
        while (socket.State == 0 && waited++ < 2000)
        {
            await Task.Delay(5).ConfigureAwait(true);
        }

        if (socket.State != 1)
        {
            Console.Error.WriteLine("onegame: the control channel did not open: " + url);
            return;
        }

        Send(socket, Reply(0, w =>
        {
            w.WriteString("hello", game.Definition.Name);
            w.WriteString("protocol", McpProtocol.LatestVersion);
        }));
        while (socket.State == 1)
        {
            var text = Poll(socket);
            if (text is null)
            {
                await Task.Delay(4).ConfigureAwait(true);
                continue;
            }

            string answer;
            try
            {
                answer = await Handle(game, surface, mcp, text).ConfigureAwait(true);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                answer = Error(IdOf(text), ex is ControlException c ? c.Code : "control_failed", ex.Message);
            }

            Send(socket, answer);
        }
    }

    private static string? Poll(BrowserWebSocket socket) => socket.TryReceive(out var message) ? Encoding.UTF8.GetString(message) : null;

    private static void Send(BrowserWebSocket socket, string text) => socket.Send(Encoding.UTF8.GetBytes(text));

    private static long IdOf(string text)
    {
        using var doc = JsonDocument.Parse(text);
        return doc.RootElement.TryGetProperty("id", out var id) && id.TryGetInt64(out var v) ? v : 0;
    }

    private static async Task<string> Handle(GameInstance game, ControlSurface surface, McpProtocol mcp, string text)
    {
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        var id = root.TryGetProperty("id", out var idEl) && idEl.TryGetInt64(out var v) ? v : 0;
        var op = root.GetProperty("op").GetString();
        switch (op)
        {
            case "put":
            {
                var path = Path.GetFullPath(root.GetProperty("path").GetString()!);
                var data = Convert.FromBase64String(root.GetProperty("data").GetString()!);
                if (data.Length > MaxFileBytes)
                {
                    throw new ControlException("file_too_large", $"{path} is larger than {MaxFileBytes} bytes.");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, data);
                return Reply(id, w => w.WriteNumber("bytes", data.Length));
            }

            case "get":
            {
                var path = Path.GetFullPath(root.GetProperty("path").GetString()!);
                var info = new FileInfo(path);
                if (!info.Exists || info.Length > MaxFileBytes)
                {
                    throw new ControlException("no_file", $"{path} does not exist or is larger than {MaxFileBytes} bytes.");
                }

                return Reply(id, w => w.WriteString("data", Convert.ToBase64String(File.ReadAllBytes(path))));
            }

            case "list":
            {
                var dir = Path.GetFullPath(root.GetProperty("path").GetString()!);
                var names = Directory.Exists(dir) ? Directory.GetFiles(dir).Select(Path.GetFileName).Order(StringComparer.Ordinal).ToArray() : [];
                return Reply(id, w =>
                {
                    w.WriteStartArray("files");
                    foreach (var n in names)
                    {
                        w.WriteStringValue(n);
                    }

                    w.WriteEndArray();
                });
            }

            case "flush":
            {
                // Waits (up to the given milliseconds) until the browser has committed every saved file to IndexedDB, so a tool may close the page.
                var limit = Environment.TickCount64 + Math.Clamp(root.TryGetProperty("timeoutMs", out var t) ? t.GetInt32() : 5000, 0, 60_000);
                var pending = BrowserByteStore.Pending;
                while (pending > 0 && Environment.TickCount64 < limit)
                {
                    await Task.Delay(10).ConfigureAwait(true);
                    pending = BrowserByteStore.Pending;
                }

                return Reply(id, w => w.WriteNumber("pending", pending));
            }

            case "pause":
                game.HostPaused = root.GetProperty("paused").GetBoolean();
                return Reply(id, w => w.WriteBoolean("paused", game.HostPaused));

            case "mcp":
            {
                var reply = await mcp.HandleAsync(root.GetProperty("message").GetString()!).ConfigureAwait(true);
                return Reply(id, w =>
                {
                    if (reply is null)
                    {
                        w.WriteNull("reply");
                    }
                    else
                    {
                        w.WriteString("reply", reply);
                    }
                });
            }

            case "scenarios":
            {
                var target = root.GetProperty("target").GetString()!;
                var artifacts = Path.GetFullPath(root.TryGetProperty("artifacts", out var a) ? a.GetString()! : "artifacts");
                var update = root.TryGetProperty("updateReferences", out var u) && u.GetBoolean();
                var wasPaused = game.HostPaused;
                game.HostPaused = true;
                try
                {
                    var json = await RunScenarios(game, surface, target, artifacts, update, root.TryGetProperty("tolerance", out var tol) ? tol.GetInt32() : 0, root.TryGetProperty("maxDiff", out var md) ? md.GetDouble() : 0).ConfigureAwait(true);
                    return Reply(id, w => w.WriteRawValue(json), "result");
                }
                finally
                {
                    game.HostPaused = wasPaused;
                }
            }

            default:
                throw new ControlException("bad_op", $"Unknown control operation '{op}'. Operations: put, get, list, flush, pause, mcp, scenarios.");
        }
    }

    private static async Task<string> RunScenarios(GameInstance game, ControlSurface surface, string target, string artifacts, bool update, int tolerance, double maxDiff)
    {
        var files = new List<string>();
        if (Directory.Exists(target))
        {
            files.AddRange(Directory.GetFiles(target, "*.yaml", SearchOption.AllDirectories));
            files.Sort(StringComparer.Ordinal);
        }
        else
        {
            files.Add(target);
        }

        var options = new ScenarioRunOptions { ArtifactsDirectory = artifacts, UpdateReferences = update, MinCaptureTolerance = tolerance, MinCaptureMaxDiff = maxDiff };
        var main = game.CurrentScreen ?? "main";
        var (width, height) = (game.Window.Width, game.Window.Height);
        var results = new List<ScenarioResult>();
        foreach (var file in files)
        {
            // Every scenario starts from the same place: a fresh world with the game's own seed (a page may have been opened with a random one), the first screen, the original window size.
            game.Reset(game.Definition.Seed);
            game.OpenScreen(main);
            game.Resize(width, height);
            results.Add(await new ScenarioRunner(surface, options).RunAsync(ScenarioParser.Load(file)).ConfigureAwait(true));
        }

        var failed = results.Count(r => !r.Passed);
        var buffer = new ArrayBufferWriter<byte>();
        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();
            w.WriteBoolean("ok", failed == 0);
            w.WriteNumber("passed", results.Count - failed);
            w.WriteNumber("failed", failed);
            w.WritePropertyName("scenarios");
            w.WriteRawValue(JsonSerializer.Serialize([.. results], ScenarioJson.Default.ScenarioResultArray));
            w.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static string Reply(long id, Action<Utf8JsonWriter> body, string? rawProperty = null)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();
            w.WriteNumber("id", id);
            w.WriteBoolean("ok", true);
            if (rawProperty is not null)
            {
                w.WritePropertyName(rawProperty);
            }

            body(w);
            w.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static string Error(long id, string code, string message)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();
            w.WriteNumber("id", id);
            w.WriteBoolean("ok", false);
            w.WriteStartObject("error");
            w.WriteString("code", code);
            w.WriteString("message", message);
            w.WriteEndObject();
            w.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
