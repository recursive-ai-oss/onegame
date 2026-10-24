using OneGame.Steam.Native;

namespace OneGame.Steam;

/// <summary>Delivers Steam's callbacks and async call results to typed handlers. It drives the library's manual-dispatch loop, so
/// everything runs on whichever thread calls <see cref="Pump"/>, and nothing runs between calls.</summary>
public sealed class SteamCallbacks
{
    /// <summary>Largest async call result the loop will fetch. Steam results are small structs; a size above this is treated as a
    /// corrupt message and skipped instead of allocating for it.</summary>
    internal const int MaxResultBytes = 64 * 1024;

    private readonly ISteamNative _native;
    private readonly SteamPacking _packing;
    private readonly List<Subscription> _subscriptions = [];
    private readonly List<PendingCall> _pending = [];
    private byte[] _scratch = new byte[512];

    internal SteamCallbacks(ISteamNative native, SteamPacking packing)
    {
        _native = native;
        _packing = packing;
    }

    /// <summary>Number of async calls still waiting for their result.</summary>
    public int PendingCalls => _pending.Count;

    /// <summary>Calls <paramref name="handler"/> from <see cref="Pump"/> every time Steam broadcasts a <typeparamref name="T"/>.</summary>
    public void Subscribe<T>(Action<T> handler)
        where T : struct, ISteamCallbackData<T>
    {
        ArgumentNullException.ThrowIfNull(handler);
        var packing = _packing;
        _subscriptions.Add(new Subscription(T.CallbackId, data =>
        {
            if (T.TryRead(data, packing, out var value))
            {
                handler(value);
            }
        }));
    }

    /// <summary>Waits for the result of an async call. <paramref name="handler"/> runs once, from <see cref="Pump"/>, with the result
    /// and whether the call failed at the I/O level (in which case the value is default).</summary>
    /// <param name="call">The handle the async function returned. Zero means the call was refused, and the handler runs at once as failed.</param>
    /// <param name="handler">Receives the result.</param>
    public void Track<T>(ulong call, Action<T, bool> handler)
        where T : struct, ISteamCallbackData<T>
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (call == 0)
        {
            handler(default, true);
            return;
        }

        var packing = _packing;
        _pending.Add(new PendingCall(call, T.CallbackId, (data, failed) =>
        {
            if (!failed && T.TryRead(data, packing, out var value))
            {
                handler(value, false);
            }
            else
            {
                handler(default, true);
            }
        }));
    }

    /// <summary>Runs the library's dispatch loop and invokes handlers for everything that arrived since the last call.</summary>
    public void Pump()
    {
        _native.RunFrame();
        while (_native.TryGetNextCallback(out var id, out var data))
        {
            try
            {
                if (id == 703)
                {
                    Complete(data);
                }
                else
                {
                    // Index loop: a handler may subscribe another handler while we iterate.
                    for (var i = 0; i < _subscriptions.Count; i++)
                    {
                        if (_subscriptions[i].CallbackId == id)
                        {
                            _subscriptions[i].Handler(data);
                        }
                    }
                }
            }
            finally
            {
                _native.FreeLastCallback();
            }
        }
    }

    private void Complete(ReadOnlySpan<byte> data)
    {
        if (!Read(data, out var done))
        {
            return;
        }

        var index = _pending.FindIndex(p => p.Call == done.Call);
        if (index < 0)
        {
            return; // somebody else's call (for example one the library started itself)
        }

        var pending = _pending[index];
        _pending.RemoveAt(index);

        if (done.ResultSize > MaxResultBytes)
        {
            pending.Handler(default, true);
            return;
        }

        var size = (int)done.ResultSize;
        if (_scratch.Length < size)
        {
            _scratch = new byte[size];
        }

        var ok = _native.GetCallResult(done.Call, _scratch.AsSpan(0, size), pending.CallbackId, out var failed);
        pending.Handler(_scratch.AsSpan(0, size), failed || !ok);
    }

    private bool Read(ReadOnlySpan<byte> data, out SteamApiCallCompleted done) =>
        ReadAs<SteamApiCallCompleted>(data, out done);

    private bool ReadAs<T>(ReadOnlySpan<byte> data, out T value)
        where T : struct, ISteamCallbackData<T> =>
        T.TryRead(data, _packing, out value);

    private delegate void DataHandler(ReadOnlySpan<byte> data);

    private delegate void ResultHandler(ReadOnlySpan<byte> data, bool failed);

    private readonly record struct Subscription(int CallbackId, DataHandler Handler);

    private readonly record struct PendingCall(ulong Call, int CallbackId, ResultHandler Handler);
}
