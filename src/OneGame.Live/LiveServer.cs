using System.Collections.Concurrent;
using System.Net.Sockets;
using OneGame.Control;

namespace OneGame.Live;

/// <summary>
/// The listener of a development build: a local socket that speaks MCP (newline-delimited JSON-RPC, the protocol's stdio framing) to
/// one agent at a time or several in turn. Connections are read on background threads, but every request runs on the thread that calls
/// <see cref="Pump"/>, the game's own, so the simulation never sees a second thread.
/// </summary>
public sealed class LiveServer : IDisposable
{
    /// <summary>Longest request line accepted, in bytes.</summary>
    public const int MaxLineBytes = 8 * 1024 * 1024;

    private readonly McpProtocol _protocol;
    private readonly ConcurrentQueue<Pending> _queue = new();
    private readonly SemaphoreSlim _work = new(0);
    private readonly List<Socket> _clients = [];
    private readonly CancellationTokenSource _stop = new();
    private Socket? _listener;
    private Thread? _acceptor;

    /// <summary>Creates a server for a game's control surface. Nothing listens until <see cref="Start"/>.</summary>
    public LiveServer(ControlSurface surface)
        : this((IControlEndpoint)surface)
    {
    }

    /// <summary>Creates a server for any control endpoint, such as a game server's. Nothing listens until <see cref="Start"/>.</summary>
    public LiveServer(IControlEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        _protocol = new McpProtocol(endpoint);
    }

    /// <summary>The socket path being listened on.</summary>
    public string? Path { get; private set; }

    /// <summary>A path in a private per-user folder for a game process to listen on.</summary>
    public static string DefaultPath()
    {
        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "onegame-live");
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(dir);
        }
        else
        {
            Directory.CreateDirectory(dir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        return System.IO.Path.Combine(dir, Environment.ProcessId.ToString(CultureInfo.InvariantCulture) + ".sock");
    }

    /// <summary>Starts listening on a Unix domain socket at <paramref name="path"/>.</summary>
    /// <exception cref="IOException">The socket could not be created.</exception>
    public void Start(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (_listener is not null)
        {
            throw new InvalidOperationException("The server is already listening.");
        }

        Path = System.IO.Path.GetFullPath(path);
        if (File.Exists(Path))
        {
            File.Delete(Path);
        }

        try
        {
            _listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            _listener.Bind(new UnixDomainSocketEndPoint(Path));
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(Path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }

            _listener.Listen(8);
        }
        catch (SocketException ex)
        {
            throw new IOException($"Cannot listen on {Path}: {ex.Message}", ex);
        }

        _acceptor = new Thread(AcceptLoop) { IsBackground = true, Name = "onegame-live-accept" };
        _acceptor.Start();
    }

    private void AcceptLoop()
    {
        while (!_stop.IsCancellationRequested)
        {
            Socket client;
            try
            {
                client = _listener!.Accept();
            }
            catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
            {
                return;
            }

            lock (_clients)
            {
                _clients.Add(client);
            }

            new Thread(() => Serve(client)) { IsBackground = true, Name = "onegame-live-client" }.Start();
        }
    }

    private void Serve(Socket client)
    {
        try
        {
            using var stream = new NetworkStream(client, ownsSocket: false);
            var line = new MemoryStream();
            var buffer = new byte[16 * 1024];
            while (!_stop.IsCancellationRequested)
            {
                var n = stream.Read(buffer, 0, buffer.Length);
                if (n == 0)
                {
                    return;
                }

                for (var i = 0; i < n; i++)
                {
                    if (buffer[i] != (byte)'\n')
                    {
                        if (line.Length >= MaxLineBytes)
                        {
                            return;
                        }

                        line.WriteByte(buffer[i]);
                        continue;
                    }

                    if (line.Length > 0)
                    {
                        var request = new Pending(Encoding.UTF8.GetString(line.GetBuffer(), 0, (int)line.Length));
                        line.SetLength(0);
                        _queue.Enqueue(request);
                        _work.Release();
                        var response = request.Done.Task.GetAwaiter().GetResult();
                        if (response is not null)
                        {
                            var bytes = Encoding.UTF8.GetBytes(response + "\n");
                            stream.Write(bytes, 0, bytes.Length);
                        }
                    }
                }
            }
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
        {
            // The agent went away.
        }
        finally
        {
            lock (_clients)
            {
                _clients.Remove(client);
            }

            client.Dispose();
        }
    }

    /// <summary>Runs the requests that have arrived. Call it from the game's thread, between frames. Returns how many ran.</summary>
    public int Pump()
    {
        var count = 0;
        while (_queue.TryDequeue(out var request))
        {
            string? response;
            try
            {
                response = _protocol.Handle(request.Message);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // The handler turns every expected failure into a JSON-RPC error; this guards the game against a bug in an operation.
                response = "{\"jsonrpc\":\"2.0\",\"id\":null,\"error\":{\"code\":-32603,\"message\":" + JsonSerializer.Serialize(ex.GetType().Name + ": " + ex.Message, LiveJson.Default.String) + "}}";
            }

            request.Done.SetResult(response);
            count++;
        }

        return count;
    }

    /// <summary>Waits up to <paramref name="milliseconds"/> for a request to arrive.</summary>
    public bool WaitForWork(int milliseconds) => _work.Wait(milliseconds);

    /// <inheritdoc />
    public void Dispose()
    {
        _stop.Cancel();
        try
        {
            _listener?.Dispose();
        }
        catch (SocketException)
        {
            // Closing a listener that is already gone is fine.
        }

        lock (_clients)
        {
            foreach (var c in _clients)
            {
                c.Dispose();
            }
        }

        while (_queue.TryDequeue(out var request))
        {
            request.Done.TrySetResult(null);
        }

        if (Path is not null && File.Exists(Path))
        {
            File.Delete(Path);
        }
    }

    private sealed class Pending(string message)
    {
        public string Message { get; } = message;

        public TaskCompletionSource<string?> Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
