using System.Globalization;
using System.Threading;
using System.Text;

namespace OneGame.Platform;

/// <summary>What the UIKit callbacks on the main thread tell the game thread.</summary>
internal enum IosEventKind : byte
{
    TouchDown,
    TouchMove,
    TouchUp,
    TouchCancel,
    KeyDown,
    KeyUp,
    Text,
    Composition,
    Layout,
    Paused,
    Resumed,
    LowMemory,
    Focus,
}

/// <summary>One thing that happened on the main thread, queued for the game thread to turn into platform events.</summary>
internal struct IosEvent
{
    public IosEventKind Kind;
    public int Id;
    public float X;
    public float Y;
    public float Pressure;
    public Key Key;
    public KeyModifiers Modifiers;
    public bool Repeat;
    public string? Text;
    public int CursorStart;
    public int CursorEnd;
    public int Width;
    public int Height;
    public float Scale;
    public EdgeInsets Insets;
    public bool Flag;
}

/// <summary>
/// The iOS app: UIKit on the process's main thread, the game on a thread of its own. The main thread runs <c>UIApplicationMain</c>; its
/// delegate callbacks (scene lifecycle, touches, key presses, text input, layout) only queue events for the game thread. The game thread
/// owns the <see cref="PlatformApp"/> and turns the queue into platform events in the iOS backend's pump, so game code never
/// touches UIKit and UIKit never calls into game code.
/// </summary>
/// <remarks>
/// Command-line arguments are the process arguments (<c>xcrun simctl launch &lt;device&gt; &lt;bundle id&gt; --headless --frames 3</c>).
/// Standard output and error are written to the console and to <c>Documents/onegame-stdout.txt</c> in the app's data folder; the exit
/// code of <c>Main</c> is written to <c>Documents/onegame-exit.txt</c> when it returns, and the process then exits. That marker is how a
/// tool knows a headless run ended.
/// </remarks>
public sealed class IosAppHost
{
    internal const string StdoutFile = "onegame-stdout.txt";
    internal const string ExitFile = "onegame-exit.txt";

    private readonly object _gate = new();
    private readonly Queue<IosEvent> _events = new();
    private readonly string[] _args;
    private readonly Func<string[], int> _main;
    private bool _sceneReady;
    private int _pumpCount;
    private bool _gameStarted;
    private IosEvent _lastLayout;
    private bool _haveLayout;

    private IosAppHost(string[] args, Func<string[], int> main)
    {
        _args = args;
        _main = main;
    }

    /// <summary>The running host, or null outside an iOS app.</summary>
    internal static IosAppHost? Current { get; private set; }

    /// <summary>The app's data folder (its Documents directory).</summary>
    internal string Documents { get; private set; } = "";

    /// <summary>The drawing layer (<c>CAMetalLayer*</c>) of the game's view, or 0 before the scene connects.</summary>
    internal nint Layer { get; set; }

    /// <summary>Runs the app. Call it from the program's entry point; it returns only if UIKit stops the process loop.</summary>
    /// <param name="args">The process arguments.</param>
    /// <param name="main">The game's <c>Main</c>; it runs on its own thread once the window exists and its result is the exit code.</param>
    /// <returns>The exit code of <c>UIApplicationMain</c>.</returns>
    public static int Run(string[] args, Func<string[], int> main)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(main);
        var host = new IosAppHost(args, main);
        Current = host;
        IosObjc.LoadFrameworks();
        var home = Environment.GetEnvironmentVariable("HOME") ?? Path.GetTempPath();
        host.Documents = Path.Combine(home, "Documents");
        Directory.CreateDirectory(host.Documents);
        IosUi.Register();
        return IosObjc.ApplicationMain(IosUi.AppDelegateClass);
    }

    // ---- queue ----

    internal void Post(in IosEvent e)
    {
        lock (_gate)
        {
            if (e.Kind == IosEventKind.Layout)
            {
                _lastLayout = e;
                _haveLayout = true;
            }

            _events.Enqueue(e);
            Monitor.PulseAll(_gate);
        }
    }

    /// <summary>The newest layout the main thread reported, if any.</summary>
    internal bool TryGetLayout(out IosEvent layout)
    {
        lock (_gate)
        {
            layout = _lastLayout;
            return _haveLayout;
        }
    }

    /// <summary>Takes everything queued, waiting up to <paramref name="timeoutMilliseconds"/> for the first event.</summary>
    internal int Drain(List<IosEvent> into, int timeoutMilliseconds)
    {
        lock (_gate)
        {
            _pumpCount++;
            Monitor.PulseAll(_gate);
            if (_events.Count == 0 && timeoutMilliseconds > 0)
            {
                Monitor.Wait(_gate, timeoutMilliseconds);
            }

            while (_events.Count > 0)
            {
                into.Add(_events.Dequeue());
            }
        }

        return into.Count;
    }

    /// <summary>Blocks the caller (the main thread) until the game thread has looked at its queue twice more, or the time is up.</summary>
    internal void WaitForGamePump(int timeoutMilliseconds)
    {
        if (!_gameStarted)
        {
            return;
        }

        lock (_gate)
        {
            var target = _pumpCount + 2;
            var deadline = Environment.TickCount64 + timeoutMilliseconds;
            while (_pumpCount < target)
            {
                var left = deadline - Environment.TickCount64;
                if (left <= 0)
                {
                    break;
                }

                Monitor.Wait(_gate, (int)left);
            }
        }
    }

    internal bool WaitForScene(int timeoutMilliseconds)
    {
        lock (_gate)
        {
            var deadline = Environment.TickCount64 + timeoutMilliseconds;
            while (!_sceneReady)
            {
                var left = deadline - Environment.TickCount64;
                if (left <= 0)
                {
                    return false;
                }

                Monitor.Wait(_gate, (int)left);
            }

            return true;
        }
    }

    // ---- scene ----

    internal void SceneConnected()
    {
        lock (_gate)
        {
            _sceneReady = true;
            Monitor.PulseAll(_gate);
        }

        if (_gameStarted)
        {
            return;
        }

        _gameStarted = true;
        // UIKit stays on this thread. The game gets a generous stack: the default for secondary threads on Apple platforms is small.
        var thread = new Thread(GameThread, 16 * 1024 * 1024) { Name = "onegame", IsBackground = true };
        thread.Start();
    }

    private void GameThread()
    {
        var exit = 4;
        TeeWriter? tee = null;
        try
        {
            Directory.SetCurrentDirectory(Documents);
            TryDelete(Path.Combine(Documents, ExitFile));
            tee = new TeeWriter(Console.Out, Path.Combine(Documents, StdoutFile));
            Console.SetOut(tee);
            Console.SetError(tee);
            exit = _main(_args);
            tee.Flush();
        }
        catch (Exception ex)
        {
            try
            {
                File.AppendAllText(Path.Combine(Documents, StdoutFile), "unhandled exception: " + ex + "\n");
                Console.Error.WriteLine("unhandled exception: " + ex);
            }
            catch (IOException)
            {
                // Nothing else can be said.
            }
        }
        finally
        {
            try
            {
                File.WriteAllText(Path.Combine(Documents, ExitFile), exit.ToString(CultureInfo.InvariantCulture));
            }
            catch (IOException)
            {
                // Without the marker a waiting tool times out, which is the best that can be said.
            }

            tee?.Flush();
            // The game is over, so is the app: iOS has no other way to leave a process that was started to do one job.
            IosObjc.exit(exit);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // A stale marker is replaced when the run ends.
        }
    }

    /// <summary>Standard output with a copy in a file.</summary>
    private sealed class TeeWriter : TextWriter
    {
        private readonly TextWriter _console;
        private readonly StreamWriter _file;
        private readonly object _lock = new();

        public TeeWriter(TextWriter console, string path)
        {
            _console = console;
            _file = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite), new UTF8Encoding(false)) { AutoFlush = true };
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            lock (_lock)
            {
                _console.Write(value);
                _file.Write(value);
            }
        }

        public override void Write(string? value)
        {
            if (value is null)
            {
                return;
            }

            lock (_lock)
            {
                _console.Write(value);
                _file.Write(value);
            }
        }

        public override void WriteLine(string? value)
        {
            lock (_lock)
            {
                _console.WriteLine(value);
                _file.WriteLine(value);
            }
        }

        public override void Flush()
        {
            lock (_lock)
            {
                _console.Flush();
                _file.Flush();
            }
        }
    }
}
