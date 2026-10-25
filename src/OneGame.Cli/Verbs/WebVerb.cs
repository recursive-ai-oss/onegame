using OneGame.Host;
using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace OneGame.Cli.Verbs;

/// <summary>
/// Runs a game's scenarios in a browser. The tool exports the game for the web with development tools, serves the folder, opens it in headless
/// Chrome and drives it over a WebSocket the page dials back to (a page cannot listen, so the page is the client and this tool the server).
/// </summary>
internal sealed class WebVerb : IVerb
{
    public string Name => "web";

    public string Summary => "Run a game in headless Chrome: web test [--scenario <folder>] [--dir <exported folder>] [--tolerance N] [--max-diff F] [--timeout S]; needs Chrome (ONEGAME_CHROME).";

    public int Run(string[] args, bool json, TextWriter output, TextWriter error)
    {
        if (args.Length == 0 || args[0] != "test")
        {
            throw new CliException(ExitCodes.Usage, "bad_args", "Usage: onegame web test [--project <dir>] [--scenario <folder>] [--dir <exported folder>] [--tolerance N] [--max-diff F] [--timeout S].");
        }

        var a = new ProjectArgs(args.Skip(1).ToArray(), "--scenario", "--dir", "--tolerance", "--max-diff", "--timeout");
        var project = Projects.Find(a.Get("--project"));
        var timeout = TimeSpan.FromSeconds(double.Parse(a.Get("--timeout") ?? "300", CultureInfo.InvariantCulture));
        var tolerance = int.Parse(a.Get("--tolerance") ?? "0", CultureInfo.InvariantCulture);
        var maxDiff = double.Parse(a.Get("--max-diff") ?? "0", CultureInfo.InvariantCulture);
        var scenarios = Path.GetFullPath(a.Get("--scenario") ?? Path.Combine(project.Directory, "scenarios"));
        var folder = a.Get("--dir");
        WebExportResult? export = null;
        if (folder is null)
        {
            export = WebExport.Export(project, dev: true, Path.Combine(project.Directory, "obj", "onegame-web", "site-dev"));
            folder = export.Folder;
        }

        folder = Path.GetFullPath(folder);
        var chrome = FindChrome();
        var port = FreePort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();
        var profile = Path.Combine(Path.GetTempPath(), "onegame-chrome-" + Environment.ProcessId);
        Process? browser = null;
        using var cancel = new CancellationTokenSource(timeout);
        try
        {
            var pending = new TaskCompletionSource<WebSocket>(TaskCreationOptions.RunContinuationsAsynchronously);
            var serving = Serve(listener, folder, pending, cancel.Token);
            var page = $"http://127.0.0.1:{port}/index.html?args=--no-present&control=ws://127.0.0.1:{port}/control";
            browser = Process.Start(new ProcessStartInfo(chrome)
            {
                ArgumentList =
                {
                    "--headless=new", "--enable-logging=stderr", "--no-sandbox", "--enable-unsafe-webgpu", "--use-webgpu-adapter=swiftshader", "--enable-unsafe-swiftshader", "--ignore-gpu-blocklist",
                    "--user-data-dir=" + profile, "--window-size=1280,720", "--hide-scrollbars", "--mute-audio", page,
                },
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            }) ?? throw new CliException(ExitCodes.RuntimeError, "no_browser", "Chrome did not start.");
            var browserLog = Environment.GetEnvironmentVariable("ONEGAME_WEB_LOG");
            browser.ErrorDataReceived += (_, e) =>
            {
                if (browserLog is { Length: > 0 } && e.Data is not null)
                {
                    lock (browserLog)
                    {
                        File.AppendAllText(browserLog, e.Data + "\n");
                    }
                }
            };
            browser.OutputDataReceived += (_, _) => { };
            browser.BeginErrorReadLine();
            browser.BeginOutputReadLine();

            var socket = pending.Task.WaitAsync(cancel.Token).GetAwaiter().GetResult();
            var channel = new Channel(socket, cancel.Token);
            var hello = channel.Receive();
            var files = Directory.Exists(scenarios) ? Directory.GetFiles(scenarios, "*", SearchOption.AllDirectories) : [];
            var baseName = Path.GetFileName(scenarios.TrimEnd(Path.DirectorySeparatorChar));
            foreach (var file in files)
            {
                var relative = Path.Combine("scenarios", Path.GetRelativePath(scenarios, file)).Replace('\\', '/');
                channel.Call("put", w =>
                {
                    w.WriteString("path", relative);
                    w.WriteString("data", Convert.ToBase64String(File.ReadAllBytes(file)));
                });
            }

            var clock = Stopwatch.StartNew();
            var result = channel.Call("scenarios", w =>
            {
                w.WriteString("target", "scenarios");
                w.WriteString("artifacts", "artifacts");
                w.WriteNumber("tolerance", tolerance);
                w.WriteNumber("maxDiff", maxDiff);
            });
            var elapsed = clock.Elapsed;
            // Images the run wrote (actual and diff frames of failed checks) come back next to the game's other artifacts.
            var pulled = Path.Combine(project.Directory, "artifacts", "web");
            foreach (var name in channel.Call("list", w => w.WriteString("path", "artifacts")).GetProperty("files").EnumerateArray().Select(f => f.GetString()!).Where(n => n.EndsWith(".png", StringComparison.Ordinal)))
            {
                Directory.CreateDirectory(pulled);
                var data = channel.Call("get", w => w.WriteString("path", "artifacts/" + name)).GetProperty("data").GetString()!;
                File.WriteAllBytes(Path.Combine(pulled, name), Convert.FromBase64String(data));
            }

            var ok = result.GetProperty("ok").GetBoolean() && result.GetProperty("result").GetProperty("ok").GetBoolean();
            var buffer = new ArrayBufferWriter<byte>();
            using (var w = new Utf8JsonWriter(buffer))
            {
                w.WriteStartObject();
                w.WriteBoolean("ok", ok);
                w.WriteString("target", "web");
                w.WriteString("browser", chrome);
                w.WriteString("game", JsonDocument.Parse(hello).RootElement.GetProperty("hello").GetString());
                w.WriteNumber("elapsedMs", (long)elapsed.TotalMilliseconds);
                w.WriteNumber("tolerance", tolerance);
                w.WriteNumber("maxDiff", maxDiff);
                if (export is not null)
                {
                    w.WriteStartObject("export");
                    w.WriteString("folder", export.Folder);
                    w.WriteNumber("files", export.Files);
                    w.WriteNumber("bytes", export.Bytes);
                    w.WriteNumber("wireBytes", export.WireBytes);
                    w.WriteEndObject();
                }

                w.WritePropertyName("scenarios");
                result.GetProperty("result").WriteTo(w);
                w.WriteEndObject();
            }

            var text = Encoding.UTF8.GetString(buffer.WrittenSpan);
            if (json)
            {
                output.WriteLine(text);
            }
            else
            {
                using var doc = JsonDocument.Parse(text);
                var r = doc.RootElement.GetProperty("scenarios");
                foreach (var s in r.GetProperty("scenarios").EnumerateArray())
                {
                    output.WriteLine($"{(s.GetProperty("passed").GetBoolean() ? "PASS" : "FAIL")} {s.GetProperty("name").GetString()}");
                }

                output.WriteLine($"{r.GetProperty("passed").GetInt32()} passed, {r.GetProperty("failed").GetInt32()} failed (in headless Chrome)");
            }

            _ = baseName;
            channel.Close();
            listener.Stop();
            return ok ? ExitCodes.Ok : ExitCodes.Failed;
        }
        catch (Exception ex) when (ex is OperationCanceledException or WebSocketException or HttpListenerException)
        {
            return Program.Fail(json, output, error, ExitCodes.RuntimeError, "web_failed", $"The page did not answer in time or the channel broke: {ex.Message}");
        }
        finally
        {
            try
            {
                browser?.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            if (Directory.Exists(profile))
            {
                try
                {
                    Directory.Delete(profile, true);
                }
                catch (IOException)
                {
                }
            }
        }
    }

    private static string FindChrome()
    {
        if (Environment.GetEnvironmentVariable("ONEGAME_CHROME") is { Length: > 0 } given)
        {
            return given;
        }

        foreach (var name in new[] { "google-chrome", "google-chrome-stable", "chromium", "chromium-browser" })
        {
            foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
            {
                var candidate = Path.Combine(dir, name);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        throw new CliException(ExitCodes.Usage, "no_browser", "No Chrome found. Install Chrome or Chromium, or set ONEGAME_CHROME to its path.");
    }

    private static int FreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    private static readonly Dictionary<string, string> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html", [".js"] = "text/javascript", [".mjs"] = "text/javascript", [".wasm"] = "application/wasm", [".json"] = "application/json",
        [".png"] = "image/png", [".css"] = "text/css", [".svg"] = "image/svg+xml",
    };

    private static async Task Serve(HttpListener listener, string root, TaskCompletionSource<WebSocket> pending, CancellationToken token)
    {
        var fullRoot = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        while (!token.IsCancellationRequested && listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync().WaitAsync(token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException or OperationCanceledException)
            {
                return;
            }

            if (Environment.GetEnvironmentVariable("ONEGAME_WEB_LOG") is { Length: > 0 } trace)
            {
                File.AppendAllText(trace, $"request {context.Request.Url!.PathAndQuery} websocket={context.Request.IsWebSocketRequest}\n");
            }

            if (context.Request.IsWebSocketRequest)
            {
                try
                {
                    var ws = await context.AcceptWebSocketAsync(null).ConfigureAwait(false);
                    pending.TrySetResult(ws.WebSocket);
                }
                catch (Exception ex) when (ex is WebSocketException or HttpListenerException or InvalidOperationException)
                {
                    pending.TrySetException(ex);
                }

                continue;
            }

            var path = context.Request.Url!.AbsolutePath.TrimStart('/');
            var file = Path.GetFullPath(Path.Combine(fullRoot, path.Length == 0 ? "index.html" : Uri.UnescapeDataString(path)));
            if (!file.StartsWith(fullRoot, StringComparison.Ordinal) || !File.Exists(file))
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                continue;
            }

            var bytes = await File.ReadAllBytesAsync(file, token).ConfigureAwait(false);
            context.Response.ContentType = Types.GetValueOrDefault(Path.GetExtension(file), "application/octet-stream");
            context.Response.Headers["Cache-Control"] = "no-store";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, token).ConfigureAwait(false);
            context.Response.Close();
        }
    }

    private static void Trace(string line)
    {
        if (Environment.GetEnvironmentVariable("ONEGAME_WEB_LOG") is { Length: > 0 } path)
        {
            File.AppendAllText(path, line + "\n");
        }
    }

    /// <summary>One request at a time over the control socket.</summary>
    private sealed class Channel(WebSocket socket, CancellationToken token)
    {
        private long _id;

        public string Receive()
        {
            var buffer = new byte[64 * 1024];
            using var all = new MemoryStream();
            WebSocketReceiveResult r;
            do
            {
                r = socket.ReceiveAsync(buffer, token).GetAwaiter().GetResult();
                if (r.MessageType == WebSocketMessageType.Close)
                {
                    throw new WebSocketException("The page closed the control channel.");
                }

                all.Write(buffer, 0, r.Count);
                if (all.Length > 256 * 1024 * 1024)
                {
                    throw new WebSocketException("The page sent a reply larger than 256 MiB.");
                }
            }
            while (!r.EndOfMessage);
            var text = Encoding.UTF8.GetString(all.GetBuffer(), 0, (int)all.Length);
            Trace("page: " + (text.Length > 300 ? text[..300] : text));
            return text;
        }

        public JsonElement Call(string op, Action<Utf8JsonWriter> body)
        {
            var id = ++_id;
            var buffer = new ArrayBufferWriter<byte>();
            using (var w = new Utf8JsonWriter(buffer))
            {
                w.WriteStartObject();
                w.WriteNumber("id", id);
                w.WriteString("op", op);
                body(w);
                w.WriteEndObject();
            }

            Trace($"tool: {op} {id}");
            socket.SendAsync(buffer.WrittenMemory, WebSocketMessageType.Binary, true, token).AsTask().GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(Receive());
            var root = doc.RootElement.Clone();
            if (!root.GetProperty("ok").GetBoolean())
            {
                var e = root.GetProperty("error");
                throw new CliException(ExitCodes.Failed, e.GetProperty("code").GetString() ?? "control_failed", e.GetProperty("message").GetString() ?? "");
            }

            return root;
        }

        public void Close()
        {
            try
            {
                socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (WebSocketException)
            {
            }
        }
    }
}
