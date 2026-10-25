using System.Collections.Concurrent;

namespace OneGame.Platform.Tests;

/// <summary>
/// A platform app that lives on its own thread and keeps pumping events, like a second program would. Clipboard owners must answer
/// requests while another connection reads, which a single test thread cannot do.
/// </summary>
internal sealed class AppThread : IDisposable
{
    private readonly Thread _thread;
    private readonly BlockingCollection<Action> _work = [];
    private PlatformApp? _app;
    private Exception? _startError;

    public AppThread(PlatformOptions options)
    {
        using var ready = new ManualResetEventSlim();
        _thread = new Thread(() =>
        {
            try
            {
                _app = PlatformApp.Create(options);
            }
            catch (Exception e)
            {
                _startError = e;
                ready.Set();
                return;
            }

            ready.Set();
            while (!_work.IsAddingCompleted)
            {
                while (_work.TryTake(out var job))
                {
                    job();
                }

                _app.PumpEvents(5);
                while (_app.TryGetEvent(out _))
                {
                }
            }

            _app.Dispose();
        })
        { IsBackground = true };
        _thread.Start();
        ready.Wait();
        if (_startError is not null)
        {
            throw new InvalidOperationException("could not start the app thread", _startError);
        }
    }

    public T Run<T>(Func<PlatformApp, T> func)
    {
        T? result = default;
        Exception? error = null;
        using var done = new ManualResetEventSlim();
        _work.Add(() =>
        {
            try
            {
                result = func(_app!);
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                done.Set();
            }
        });
        Assert.True(done.Wait(10_000), "the app thread did not answer");
        return error is null ? result! : throw new InvalidOperationException("action failed on the app thread", error);
    }

    public void Dispose()
    {
        _work.CompleteAdding();
        _thread.Join(3000);
    }
}
