using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

namespace OneGame.Platform.Tests;

/// <summary>
/// Runs code on the process's main thread, which AppKit insists on for windows and the event loop. On a Mac the test runner moves to a
/// worker thread and the main thread serves this queue; elsewhere everything runs where it is called.
/// </summary>
internal static class MainThread
{
    private static readonly BlockingCollection<Action> Queue = [];

    /// <summary>Runs <paramref name="run"/> (the test runner) and returns its exit code. On macOS the main thread serves <see cref="Run(Action)"/> meanwhile.</summary>
    public static int Host(string[] args, Func<string[], int> run)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return run(args);
        }

        var code = 0;
        var runner = new Thread(() =>
        {
            try
            {
                code = run(args);
            }
            finally
            {
                Queue.CompleteAdding();
            }
        })
        { Name = "test runner" };
        runner.Start();
        foreach (var work in Queue.GetConsumingEnumerable())
        {
            work();
        }

        runner.Join();
        return code;
    }

    /// <summary>Runs a test body on the main thread and waits for it; exceptions come back to the caller unchanged.</summary>
    public static void Run(Action body)
    {
        if (!OperatingSystem.IsMacOS())
        {
            body();
            return;
        }

        ExceptionDispatchInfo? failure = null;
        using var done = new ManualResetEventSlim();
        Queue.Add(() =>
        {
            try
            {
                body();
            }
            catch (Exception e)
            {
                failure = ExceptionDispatchInfo.Capture(e);
            }
            finally
            {
                done.Set();
            }
        });
        done.Wait();
        failure?.Throw();
    }
}
