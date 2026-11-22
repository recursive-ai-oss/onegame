using System.Collections.Concurrent;
using System.Net.Sockets;
using OneGame.Control;
using RecursiveAI.OneModelContext;

namespace OneGame.Live;

/// <summary>
/// The listener of a development build: a local socket that speaks MCP (newline-delimited JSON-RPC, the protocol's stdio framing) to
/// one agent at a time or several in turn. Connections are read on background threads, but every request runs on the thread that calls
/// <see cref="Pump"/>, the game's own, so the simulation never sees a second thread. Only development builds contain it; a game started
/// with <c>onegame run --live</c> already hosts one.
/// </summary>
/// <example>
/// Hosting live control in a custom game loop:
/// <code>
/// using var live = new LiveServer(new ControlSurface(game));
/// live.Start(LiveServer.DefaultPath());
/// while (running)
/// {
///     live.Pump();            // run requests that arrived, on this thread
///     game.Step(1);
///     live.WaitForWork(5);    // or sleep until the next frame
/// }
/// </code>
/// </example>
public sealed class LiveServer : IDisposable
{
    /// <summary>Longest request line accepted, in bytes.</summary>
    public const int MaxLineBytes = ControlServer.MaxMessageBytes;

    private readonly IControlEndpoint _endpoint;
    private readonly ControlServer _control;
    private readonly ConcurrentQueue<Pending> _queue = new();
    private readonly SemaphoreSlim _work = new(0);
    private readonly List<Socket> _clients = [];
    private readonly CancellationTokenSource _stop = new();
    private Socket? _listener;
    private Thread? _acceptor;

    /// <summary>Creates a server for a game's control surface. Nothing listens until <see cref="Start"/>.</summary>
    /// <param name="surface">The game's control surface.</param>
    public LiveServer(ControlSurface surface)
        : this((IControlEndpoint)surface)
    {
    }

    /// <summary>Creates a server for any control endpoint, such as a game server's. Nothing listens until <see cref="Start"/>.</summary>
    /// <param name="endpoint">The endpoint whose operations are served.</param>
    /// <exception cref="ArgumentNullException"><paramref name="endpoint"/> is null.</exception>
    public LiveServer(IControlEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        _endpoint = endpoint;
        _control = new ControlServer(endpoint, Enqueue);
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
    /// <param name="path">The socket file; see <see cref="DefaultPath"/>. An old file at the path is replaced. <c>tcp:HOST:PORT</c> listens on TCP instead, for a game on a device; that listener has no authentication, so use it on test devices only.</param>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">The server is already listening.</exception>
    /// <exception cref="IOException">The socket could not be created.</exception>
    public void Start(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (_listener is not null)
        {
            throw new InvalidOperationException("The server is already listening.");
        }

        if (path.StartsWith("tcp:", StringComparison.Ordinal))
        {
            StartTcp(path);
            return;
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

    // "tcp:port" listens on the loopback interface only. A phone has no folder that both the game and the tool can reach, so `adb forward`
    // maps this port to the computer. "tcp:*:port" listens on every interface (IPv4 and IPv6), for a device the computer can only reach
    // over the network, such as an iPad on a cable: nothing authenticates the listener, so use it on test devices only. Development builds only,
    // like the rest of the listener.
    private void StartTcp(string spec)
    {
        var rest = spec.AsSpan(4);
        var any = rest.StartsWith("*:", StringComparison.Ordinal);
        if (any)
        {
            rest = rest[2..];
        }

        if (!int.TryParse(rest, NumberStyles.None, CultureInfo.InvariantCulture, out var port) || port is < 1 or > 65535)
        {
            throw new IOException($"'{spec}' is not tcp:<port> or tcp:*:<port>.");
        }

        try
        {
            if (any)
            {
                _listener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp) { DualMode = true };
                _listener.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.IPv6Any, port));
            }
            else
            {
                _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listener.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port));
            }

            _listener.Listen(8);
        }
        catch (SocketException ex)
        {
            throw new IOException($"Cannot listen on {spec}: {ex.Message}", ex);
        }

        Path = (any ? "tcp:*:" : "tcp:127.0.0.1:") + port.ToString(CultureInfo.InvariantCulture);
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
                // Closing a listening socket does not wake a thread blocked in Accept on macOS, so wait in short slices and look at _stop.
                if (!_listener!.Poll(100_000, SelectMode.SelectRead))
                {
                    continue;
                }

                client = _listener.Accept();
            }
            catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
            {
                return;
            }

            lock (_clients)
            {
                _clients.Add(client);
            }

            _ = Task.Run(() => Serve(client));
        }
    }

    // The library reads and frames the stream; each tool call is queued for the game's thread and answered when Pump has run it.
    private async Task Serve(Socket client)
    {
        try
        {
            await using var stream = new NetworkStream(client, ownsSocket: false);
            await _control.Server.RunAsync(ContextTransport.Stream(stream, MaxLineBytes), _stop.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            // The agent went away, or the server is stopping.
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

    private ValueTask<ControlResult> Enqueue(string op, string? argsJson)
    {
        var request = new Pending(op, argsJson);
        _queue.Enqueue(request);
        _work.Release();
        return new ValueTask<ControlResult>(request.Done.Task);
    }

    /// <summary>Runs the requests that have arrived. Call it from the game's thread, between frames. Returns how many ran.</summary>
    public int Pump()
    {
        var count = 0;
        while (_queue.TryDequeue(out var request))
        {
            try
            {
                request.Done.SetResult(_endpoint.InvokeAsync(request.Op, request.Args).AsTask().GetAwaiter().GetResult());
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // A ControlException becomes the tool's error result; anything else is a bug in an operation, which the library
                // reports to the agent as an internal error while the game goes on.
                request.Done.SetException(ex);
            }

            count++;
        }

        return count;
    }

    /// <summary>Waits up to <paramref name="milliseconds"/> for a request to arrive, so a loop can sleep without adding latency.</summary>
    /// <param name="milliseconds">The longest wait.</param>
    /// <returns>True when a request arrived.</returns>
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
            request.Done.TrySetException(new ObjectDisposedException(nameof(LiveServer)));
        }

        if (Path is not null && File.Exists(Path))
        {
            File.Delete(Path);
        }
    }

    private sealed class Pending(string op, string? args)
    {
        public string Op { get; } = op;

        public string? Args { get; } = args;

        public TaskCompletionSource<ControlResult> Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
