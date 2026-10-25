using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace OneGame.Platform;

/// <summary>
/// The program side of an Android native activity. The app's shared library exports <c>ANativeActivity_onCreate</c> and passes the
/// activity here along with the game's <c>Main</c>; the game then runs on its own thread while the activity's callbacks (which arrive
/// on the system's main thread and must return quickly) only queue what they were told. The input queue is read on the game thread.
/// </summary>
/// <remarks>
/// Command-line arguments come from the launch intent's string extra <c>onegame.args</c> (for example
/// <c>adb shell am start -n app/android.app.NativeActivity -e onegame.args "--headless --frames 3"</c>). Standard output and error go to
/// logcat (tag <c>onegame</c>) and to <c>onegame-stdout.txt</c> in the app's files folder; the exit code of <c>Main</c> is written to
/// <c>onegame-exit.txt</c> there when it returns, which is how a tool waits for a headless run to finish.
/// </remarks>
public sealed unsafe class AndroidActivityHost
{
    internal const string ArgsExtra = "onegame.args";
    internal const string StdoutFile = "onegame-stdout.txt";
    internal const string ExitFile = "onegame-exit.txt";

    private readonly ANativeActivity* _activity;
    private readonly Func<string[], int> _main;
    private readonly ConcurrentQueue<Command> _commands = new();
    private readonly object _inputLock = new();
    private readonly Signal _looperReady = new();
    private readonly Signal _firstSurface = new();
    private readonly Signal _surfaceLossAck = new();
    private readonly AndroidJni _mainJni;
    private Thread? _thread;
    private nint _looper;
    private nint _inputQueue;
    private volatile bool _gameFinished;

    private AndroidActivityHost(ANativeActivity* activity, Func<string[], int> main)
    {
        _activity = activity;
        _main = main;
        _mainJni = AndroidJni.ForMainThread(activity->Vm, activity->Env);
    }

    internal static AndroidActivityHost? Current { get; private set; }

    internal nint Activity => (nint)_activity;

    internal string FilesDirectory { get; private set; } = "";

    /// <summary>
    /// Call this from the game's exported <c>ANativeActivity_onCreate</c>. It installs the activity callbacks, reads the launch arguments
    /// and starts the game thread, then returns at once.
    /// </summary>
    /// <param name="activity">The <c>ANativeActivity*</c> the system passed to <c>ANativeActivity_onCreate</c>.</param>
    /// <param name="main">The game's entry point: it receives the launch arguments and returns the exit code.</param>
    public static void Start(nint activity, Func<string[], int> main)
    {
        ArgumentNullException.ThrowIfNull(main);
        if (activity == 0)
        {
            throw new ArgumentException("The activity pointer is null.", nameof(activity));
        }

        var native = (ANativeActivity*)activity;
        var host = new AndroidActivityHost(native, main) { FilesDirectory = Utf8(native->InternalDataPath) };
        Current = host;
        var cb = native->Callbacks;
        cb->OnStart = &OnStart;
        cb->OnResume = &OnResume;
        cb->OnPause = &OnPause;
        cb->OnStop = &OnStop;
        cb->OnDestroy = &OnDestroy;
        cb->OnWindowFocusChanged = &OnWindowFocusChanged;
        cb->OnNativeWindowCreated = &OnNativeWindowCreated;
        cb->OnNativeWindowResized = &OnNativeWindowResized;
        cb->OnNativeWindowRedrawNeeded = &OnNativeWindowRedrawNeeded;
        cb->OnNativeWindowDestroyed = &OnNativeWindowDestroyed;
        cb->OnInputQueueCreated = &OnInputQueueCreated;
        cb->OnInputQueueDestroyed = &OnInputQueueDestroyed;
        cb->OnContentRectChanged = &OnContentRectChanged;
        cb->OnConfigurationChanged = &OnConfigurationChanged;
        cb->OnLowMemory = &OnLowMemory;

        var args = host._mainJni.GetIntentStringExtra(native->Clazz, ArgsExtra) ?? "";
        host._thread = new Thread(() => host.GameThread(args), 16 * 1024 * 1024) { Name = "onegame-game" };
        host._thread.Start();
    }

    // ---- game thread ----

    private void GameThread(string argLine)
    {
        _looper = AndroidNative.ALooper_prepare(AndroidNative.LooperPrepareAllowNonCallbacks);
        // The activity's callbacks keep waking this looper after the game thread has ended (finishing the activity sends them). A looper is
        // destroyed with its thread unless someone holds a reference, and waking a destroyed one aborts the process. One reference is kept for good.
        AndroidNative.ALooper_acquire(_looper);
        _looperReady.Set();
        var jni = AndroidJni.Attach(_activity->Vm);
        GameJni = jni;
        var exit = 4;
        try
        {
            var stdout = new AndroidConsole(Path.Combine(FilesDirectory, StdoutFile));
            Console.SetOut(stdout);
            Console.SetError(stdout);
            TryDelete(Path.Combine(FilesDirectory, ExitFile));
            PrepareProcessEnvironment();
            exit = _main(SplitArguments(argLine));
            stdout.Flush();
        }
        catch (Exception ex)
        {
            AndroidNative.WriteLog(AndroidNative.LogError, "onegame", "unhandled exception: " + ex);
            try
            {
                File.AppendAllText(Path.Combine(FilesDirectory, StdoutFile), "unhandled exception: " + ex + "\n");
            }
            catch (IOException)
            {
                // The log line above is all there is.
            }
        }
        finally
        {
            try
            {
                File.WriteAllText(Path.Combine(FilesDirectory, ExitFile), exit.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            catch (IOException)
            {
                // Without the marker a waiting tool times out, which is the best that can be said.
            }

            _gameFinished = true;
            jni?.Detach();
            AndroidNative.ANativeActivity_finish((nint)_activity);
        }
    }

    // An Android process starts with no home folder, no temporary folder and "/" as its working directory. Point them at the app's own
    // files folder, which is the only place it can write, so library code that asks for them gets a usable answer.
    private void PrepareProcessEnvironment()
    {
        try
        {
            var temp = Path.Combine(FilesDirectory, "tmp");
            Directory.CreateDirectory(temp);
            Environment.SetEnvironmentVariable("HOME", FilesDirectory);
            Environment.SetEnvironmentVariable("TMPDIR", temp);
            Directory.SetCurrentDirectory(FilesDirectory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AndroidNative.WriteLog(AndroidNative.LogError, "onegame", "cannot prepare the files folder: " + ex.Message);
        }
    }

    // A quoted run is one argument; everything else splits on white space.
    internal static string[] SplitArguments(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var quote = '\0';
        var any = false;
        foreach (var c in line)
        {
            if (quote != '\0')
            {
                if (c == quote)
                {
                    quote = '\0';
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c is '"' or '\'')
            {
                quote = c;
                any = true;
            }
            else if (char.IsWhiteSpace(c))
            {
                if (current.Length > 0 || any)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    any = false;
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0 || any)
        {
            result.Add(current.ToString());
        }

        return [.. result];
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

    internal AndroidJni? GameJni { get; set; }

    internal bool TryDequeue(out Command command) => _commands.TryDequeue(out command);

    internal bool WaitForFirstSurface(int milliseconds) => _firstSurface.Wait(milliseconds);

    internal void SurfaceLossHandled() => _surfaceLossAck.Set();

    internal bool GameFinished => _gameFinished;

    internal nint Looper => _looper;

    // Runs under the input lock so the queue cannot be detached while an event is being read.
    internal void DrainInput(Func<nint, bool> handle)
    {
        lock (_inputLock)
        {
            var queue = _inputQueue;
            if (queue == 0)
            {
                return;
            }

            nint ev;
            while (AndroidNative.AInputQueue_getEvent(queue, &ev) >= 0)
            {
                if (AndroidNative.AInputQueue_preDispatchEvent(queue, ev) != 0)
                {
                    continue;
                }

                AndroidNative.AInputQueue_finishEvent(queue, ev, handle(ev) ? 1 : 0);
            }
        }
    }

    internal void ShowKeyboard(bool show)
    {
        if (show)
        {
            AndroidNative.ANativeActivity_showSoftInput((nint)_activity, AndroidNative.ShowSoftInputForced);
        }
        else
        {
            AndroidNative.ANativeActivity_hideSoftInput((nint)_activity, 1);
        }
    }

    internal int Density
    {
        get
        {
            var config = AndroidNative.AConfiguration_new();
            try
            {
                AndroidNative.AConfiguration_fromAssetManager(config, _activity->AssetManager);
                var d = AndroidNative.AConfiguration_getDensity(config);
                return d is <= 0 or >= 0xfffe ? 160 : d;
            }
            finally
            {
                AndroidNative.AConfiguration_delete(config);
            }
        }
    }

    // ---- main-thread side ----

    private void Post(Command command)
    {
        _commands.Enqueue(command);
        if (_looper != 0 && !_gameFinished)
        {
            AndroidNative.ALooper_wake(_looper);
        }
    }

    // The keyboard attaches only to a focused view; see AndroidJni.MakeContentFocusable. Main thread only.
    private void FocusContentView() => _mainJni.MakeContentFocusable(_activity->Clazz);

    private void PostInsets() => Post(Command.WithInsets(_mainJni.GetSystemInsets(_activity->Clazz)));

    private static string Utf8(byte* p) => p == null ? "" : Marshal.PtrToStringUTF8((nint)p) ?? "";

    private static AndroidActivityHost? Host => Current;

    [UnmanagedCallersOnly]
    private static void OnStart(nint activity) => Host?.Post(new Command(CommandKind.Start));

    [UnmanagedCallersOnly]
    private static void OnResume(nint activity)
    {
        Host?.Post(new Command(CommandKind.Resume));
        Host?.PostInsets();
    }

    [UnmanagedCallersOnly]
    private static void OnPause(nint activity) => Host?.Post(new Command(CommandKind.Pause));

    [UnmanagedCallersOnly]
    private static void OnStop(nint activity) => Host?.Post(new Command(CommandKind.Stop));

    [UnmanagedCallersOnly]
    private static void OnDestroy(nint activity)
    {
        var host = Host;
        if (host is null)
        {
            return;
        }

        host.Post(new Command(CommandKind.Destroy));
        host._thread?.Join(3000);
        Current = null;
    }

    [UnmanagedCallersOnly]
    private static void OnWindowFocusChanged(nint activity, int hasFocus)
    {
        Host?.Post(new Command(CommandKind.Focus) { A = hasFocus });
        Host?.PostInsets();
    }

    [UnmanagedCallersOnly]
    private static void OnNativeWindowCreated(nint activity, nint window)
    {
        AndroidNative.ANativeWindow_acquire(window);
        Host?.Post(new Command(CommandKind.WindowCreated) { Pointer = window });
        Host?._firstSurface.Set();
        Host?.PostInsets();
        Host?.FocusContentView();
    }

    [UnmanagedCallersOnly]
    private static void OnNativeWindowResized(nint activity, nint window)
    {
        Host?.Post(new Command(CommandKind.WindowResized) { Pointer = window });
        Host?.PostInsets();
    }

    [UnmanagedCallersOnly]
    private static void OnNativeWindowRedrawNeeded(nint activity, nint window)
    {
    }

    // The window must not be used once this returns: wait (briefly) until the game thread has dropped its swapchain.
    [UnmanagedCallersOnly]
    private static void OnNativeWindowDestroyed(nint activity, nint window)
    {
        var host = Host;
        if (host is not null && !host._gameFinished)
        {
            host._surfaceLossAck.Reset();
            host.Post(new Command(CommandKind.WindowDestroyed) { Pointer = window });
            host._surfaceLossAck.Wait(1000);
        }

        AndroidNative.ANativeWindow_release(window);
    }

    [UnmanagedCallersOnly]
    private static void OnInputQueueCreated(nint activity, nint queue)
    {
        var host = Host;
        if (host is null)
        {
            return;
        }

        host._looperReady.Wait(2000);
        lock (host._inputLock)
        {
            host._inputQueue = queue;
            AndroidNative.AInputQueue_attachLooper(queue, host._looper, AndroidNative.LooperIdentInput, 0, 0);
        }
    }

    [UnmanagedCallersOnly]
    private static void OnInputQueueDestroyed(nint activity, nint queue)
    {
        var host = Host;
        if (host is null)
        {
            return;
        }

        lock (host._inputLock)
        {
            AndroidNative.AInputQueue_detachLooper(queue);
            host._inputQueue = 0;
        }
    }

    [UnmanagedCallersOnly]
    private static void OnContentRectChanged(nint activity, int* rect) => Host?.PostInsets();

    [UnmanagedCallersOnly]
    private static void OnConfigurationChanged(nint activity)
    {
        Host?.Post(new Command(CommandKind.ConfigurationChanged));
        Host?.PostInsets();
    }

    [UnmanagedCallersOnly]
    private static void OnLowMemory(nint activity) => Host?.Post(new Command(CommandKind.LowMemory));

    internal enum CommandKind : byte
    {
        Start,
        Resume,
        Pause,
        Stop,
        Destroy,
        Focus,
        WindowCreated,
        WindowResized,
        WindowDestroyed,
        ConfigurationChanged,
        Insets,
        LowMemory,
    }

    internal readonly record struct Command(CommandKind Kind)
    {
        public int A { get; init; }

        public nint Pointer { get; init; }

        public EdgeInsets Insets { get; init; }

        public static Command WithInsets(EdgeInsets insets) => new(CommandKind.Insets) { Insets = insets };
    }

    // A manual-reset flag with a timed wait. (Monitor based, so the host owns nothing that needs disposing.)
    private sealed class Signal
    {
        private readonly object _gate = new();
        private bool _set;

        public void Set()
        {
            lock (_gate)
            {
                _set = true;
                Monitor.PulseAll(_gate);
            }
        }

        public void Reset()
        {
            lock (_gate)
            {
                _set = false;
            }
        }

        public bool Wait(int milliseconds)
        {
            lock (_gate)
            {
                if (!_set)
                {
                    Monitor.Wait(_gate, milliseconds);
                }

                return _set;
            }
        }
    }

    // Standard output as the platform sees it: lines go to logcat, and everything to a file a tool can read back.
    private sealed class AndroidConsole : TextWriter
    {
        private readonly StreamWriter _file;
        private readonly StringBuilder _line = new();
        private readonly object _gate = new();

        public AndroidConsole(string path)
        {
            _file = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite), new UTF8Encoding(false)) { AutoFlush = true };
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            lock (_gate)
            {
                _file.Write(value);
                if (value == '\n')
                {
                    AndroidNative.WriteLog(AndroidNative.LogInfo, "onegame", _line.ToString().TrimEnd('\r'));
                    _line.Clear();
                }
                else
                {
                    _line.Append(value);
                }
            }
        }

        public override void Write(string? value)
        {
            if (value is null)
            {
                return;
            }

            lock (_gate)
            {
                foreach (var c in value)
                {
                    Write(c);
                }
            }
        }

        public override void Flush()
        {
            lock (_gate)
            {
                _file.Flush();
            }
        }
    }
}
