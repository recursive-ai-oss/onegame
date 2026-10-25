using System.Text;
using OneGame.Platform;

namespace OneGame.Purchases;

/// <summary>
/// Google Play Billing on Android. The Billing Library is Java, so the app carries one small class of ours (<c>dev.onegame.store.PlayBridge</c>)
/// that owns the <c>BillingClient</c> and queues every answer as a line of JSON; this class calls it through JNI and reads the lines when
/// the game pumps. Requests and <see cref="TryTake"/> must run on the game thread, the one the platform layer attached to the Java VM.
/// The export step adds the class, the Billing Library and the manifest entries when the game opts in (see <c>docs/interfaces/store.md</c>).
/// </summary>
public sealed class PlayBillingBackend : IPurchaseBackend
{
    private const string BridgeClass = "dev.onegame.store.PlayBridge";
    private const int MaxMessagesPerPump = 64;

    private readonly StoreEventBuffer _queue = new();
    private readonly Dictionary<string, PurchaseTransaction> _finishing = new(StringComparer.Ordinal);
    private readonly List<StoreEvent> _scratch = [];
    private readonly AndroidJni? _jni;
    private readonly nint _activity;
    private readonly nint _class;
    private readonly nint _connect, _query, _buy, _finish, _restore, _poll, _end;
    private readonly StoreError _initError;

    /// <summary>Creates the backend and the Play billing client.</summary>
    /// <exception cref="PlatformNotSupportedException">Not running as an Android app.</exception>
    public PlayBillingBackend()
    {
        if (AndroidActivityHost.Current is not { GameJni: { } jni } host)
        {
            throw new PlatformNotSupportedException("Play Billing needs an Android app started through the platform layer.");
        }

        _jni = jni;
        _activity = host.JavaActivity;
        // A global reference: the class is used on whichever thread calls the backend, and a local one would die with its thread.
        _class = jni.ToGlobal(jni.LoadAppClass(_activity, BridgeClass));
        if (_class == 0)
        {
            _initError = new StoreError(StoreErrorKind.Unavailable, "The app does not contain " + BridgeClass + "; export with Play Billing enabled (AndroidPlayBilling).");
            return;
        }

        _connect = jni.GetStaticMethodId(_class, "connect", "()V");
        _query = jni.GetStaticMethodId(_class, "queryProducts", "(Ljava/lang/String;Ljava/lang/String;)V");
        _buy = jni.GetStaticMethodId(_class, "buy", "(Ljava/lang/String;)V");
        _finish = jni.GetStaticMethodId(_class, "finish", "(Ljava/lang/String;Z)V");
        _restore = jni.GetStaticMethodId(_class, "restore", "()V");
        _poll = jni.GetStaticMethodId(_class, "poll", "()Ljava/lang/String;");
        _end = jni.GetStaticMethodId(_class, "end", "()V");
        var init = jni.GetStaticMethodId(_class, "init", "(Landroid/app/Activity;)Ljava/lang/String;");
        if (init == 0 || _connect == 0 || _query == 0 || _buy == 0 || _finish == 0 || _restore == 0 || _poll == 0 || _end == 0)
        {
            _initError = new StoreError(StoreErrorKind.Failed, "PlayBridge does not have the expected methods.");
            return;
        }

        var error = jni.CallStaticObject(_class, init, _activity);
        var text = jni.GetStringBounded(error, 4096, out _);
        jni.DeleteLocal(error);
        if (!string.IsNullOrEmpty(text))
        {
            _initError = new StoreError(StoreErrorKind.Unavailable, text);
        }
    }

    /// <inheritdoc/>
    public string Name => "play";

    /// <inheritdoc/>
    public bool RestoreIsComplete => true;

    /// <inheritdoc/>
    public void Connect()
    {
        if (_initError.IsError || _jni is null)
        {
            _queue.Post(new StoreEvent(StoreEventKind.Connected) { Error = _initError.IsError ? _initError : new StoreError(StoreErrorKind.Unavailable, "No Java VM.") });
            return;
        }

        _jni.CallStaticVoid(_class, _connect);
    }

    /// <inheritdoc/>
    public void LoadProducts(IReadOnlyList<ProductDefinition> products)
    {
        if (_jni is null || _initError.IsError)
        {
            _queue.Post(new StoreEvent(StoreEventKind.ProductsLoaded) { Error = Unavailable() });
            return;
        }

        var inapp = string.Join(',', products.Where(p => p.Kind != ProductKind.Subscription).Select(p => p.Id));
        var subs = string.Join(',', products.Where(p => p.Kind == ProductKind.Subscription).Select(p => p.Id));
        var a = _jni.NewString(inapp);
        var b = _jni.NewString(subs);
        _jni.CallStaticVoid(_class, _query, a, b);
        _jni.DeleteLocal(a);
        _jni.DeleteLocal(b);
    }

    /// <inheritdoc/>
    public void Buy(ProductDefinition product)
    {
        if (_jni is null || _initError.IsError)
        {
            _queue.Post(new StoreEvent(StoreEventKind.Transaction) { Transaction = Failed(product.Id, Unavailable()) });
            return;
        }

        var id = _jni.NewString(product.Id);
        _jni.CallStaticVoid(_class, _buy, id);
        _jni.DeleteLocal(id);
    }

    /// <inheritdoc/>
    public void Finish(PurchaseTransaction transaction, ProductKind kind)
    {
        if (_jni is null || _initError.IsError || transaction.ServerProof.Length == 0)
        {
            _queue.Post(new StoreEvent(StoreEventKind.Finished) { Transaction = transaction, Error = Unavailable() });
            return;
        }

        _finishing[transaction.ServerProof] = transaction;
        var token = _jni.NewString(transaction.ServerProof);
        _jni.CallStaticVoid(_class, _finish, token, kind == ProductKind.Consumable ? 1 : 0);
        _jni.DeleteLocal(token);
    }

    /// <inheritdoc/>
    public void Restore()
    {
        if (_jni is null || _initError.IsError)
        {
            _queue.Post(new StoreEvent(StoreEventKind.RestoreFinished) { Error = Unavailable() });
            return;
        }

        _jni.CallStaticVoid(_class, _restore);
    }

    /// <inheritdoc/>
    public bool TryTake(out StoreEvent storeEvent)
    {
        if (_jni is not null && !_initError.IsError)
        {
            Drain();
        }

        return _queue.TryTake(out storeEvent);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_jni is not null && !_initError.IsError)
        {
            _jni.CallStaticVoid(_class, _end);
        }
    }

    private void Drain()
    {
        var events = _scratch;
        for (var i = 0; i < MaxMessagesPerPump; i++)
        {
            var line = _jni!.CallStaticObject(_class, _poll);
            if (line == 0)
            {
                break;
            }

            var text = _jni.GetStringBounded(line, StoreLimits.MaxMessageBytes, out var tooLong);
            _jni.DeleteLocal(line);
            events.Clear();
            if (text is not null && PlayWire.TryParse(Encoding.UTF8.GetBytes(text), events))
            {
                foreach (var e in events)
                {
                    _queue.Post(Reattach(e));
                }
            }
            else
            {
                Console.Error.WriteLine(tooLong ? "purchases: a billing message over the size limit was dropped" : "purchases: a malformed billing message was dropped");
            }
        }
    }

    // The bridge answers a finish with the token only; the game gets back the transaction it finished.
    private StoreEvent Reattach(StoreEvent e)
    {
        if (e.Kind == StoreEventKind.Finished && e.Transaction is { } t && _finishing.Remove(t.ServerProof, out var original))
        {
            return e with { Transaction = original };
        }

        return e;
    }

    private static StoreError Unavailable() => new(StoreErrorKind.Unavailable, "Google Play Billing is not available in this app.");

    private static PurchaseTransaction Failed(string product, StoreError error) =>
        new(string.Empty, product, TransactionState.Failed, 0, 0, string.Empty, string.Empty, string.Empty, error);
}
