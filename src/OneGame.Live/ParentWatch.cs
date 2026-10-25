using System.Diagnostics;

namespace OneGame.Live;

/// <summary>
/// Watches the process that started this one. Harnesses that launch several game processes pass <c>--parent &lt;pid&gt;</c>, so a test
/// that crashes or is killed does not leave servers and clients running: when the parent is gone the watcher asks the process to stop,
/// and exits it outright if that has not worked a few seconds later.
/// </summary>
public sealed class ParentWatch : IDisposable
{
    private readonly Timer _timer;
    private readonly int _pid;
    private readonly Action _stop;
    private readonly Process? _parent;
    private int _fired;

    /// <summary>Starts watching.</summary>
    /// <param name="pid">The process to watch.</param>
    /// <param name="stop">Called once, on a background thread, when the process is gone: it should make the main loop finish.</param>
    public ParentWatch(int pid, Action stop)
    {
        _pid = pid;
        _stop = stop;
        try
        {
            _parent = Process.GetProcessById(pid);
        }
        catch (ArgumentException)
        {
            // Already gone: fire at once.
        }

        _timer = new Timer(_ => Check(), null, 250, 250);
    }

    private void Check()
    {
        var gone = _parent is null || _parent.HasExited;
        if (!gone || Interlocked.Exchange(ref _fired, 1) == 1)
        {
            return;
        }

        Console.Error.WriteLine($"parent process {_pid} is gone; stopping");
        try
        {
            _stop();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Console.Error.WriteLine("stop failed: " + ex.Message);
        }

        // The main loop should end by itself; this is the backstop for one that is stuck.
        Thread.Sleep(4000);
        Environment.Exit(3);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _timer.Dispose();
        _parent?.Dispose();
    }
}
