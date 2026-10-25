using System.Runtime.InteropServices;
using OneGame.Platform;

namespace OneGame.Purchases;

/// <summary>
/// The App Store on iOS and macOS through StoreKit's Objective-C API (the original, transaction-queue StoreKit), reached with the
/// Objective-C runtime like the rest of the Apple platform code. StoreKit 2 is Swift-only, so using it would mean building and
/// shipping a Swift library next to a NativeAOT game; the original API is deprecated for new features but still ships in every system
/// and covers everything a game sells: products, purchases, restores, unfinished transactions and the app receipt for server checks.
/// Requests run on the main thread, which is also where StoreKit calls back.
/// </summary>
public sealed unsafe class StoreKitBackend : IPurchaseBackend
{
    private const string FrameworkPath = "/System/Library/Frameworks/StoreKit.framework/StoreKit";
    private const string ObserverClass = "OneGameStoreObserver";

    // SKPaymentTransactionState (StoreKit reference).
    private const int StatePurchasing = 0;
    private const int StatePurchased = 1;
    private const int StateFailed = 2;
    private const int StateRestored = 3;
    private const int StateDeferred = 4;

    // SKError.Code values the game can act on (StoreKit reference, "SKError.Code").
    private const int ErrorClientInvalid = 1;
    private const int ErrorPaymentCancelled = 2;
    private const int ErrorPaymentInvalid = 3;
    private const int ErrorPaymentNotAllowed = 4;
    private const int ErrorStoreProductNotAvailable = 5;
    private const int ErrorCloudServiceNetworkConnectionFailed = 7;

    private static StoreKitBackend? s_current;
    private static nint s_observerClass;

    private readonly StoreEventBuffer _queue = new();
    private readonly Dictionary<string, nint> _products = new(StringComparer.Ordinal);
    private readonly Dictionary<string, nint> _open = new(StringComparer.Ordinal);
    private readonly List<nint> _requests = [];
    private readonly object _lock = new();
    private nint _observer;
    private bool _observing;

    /// <summary>Creates the backend and registers the observer class with the Objective-C runtime.</summary>
    /// <exception cref="PlatformNotSupportedException">Not running on an Apple platform, or StoreKit could not be loaded.</exception>
    public StoreKitBackend()
    {
        if (!(OperatingSystem.IsIOS() || OperatingSystem.IsMacOS()) || !NativeLibrary.TryLoad(FrameworkPath, out _))
        {
            throw new PlatformNotSupportedException("StoreKit is available on iOS and macOS only.");
        }

        lock (typeof(StoreKitBackend))
        {
            if (s_observerClass == 0)
            {
                s_observerClass = IosObjc.DefineClass(ObserverClass, "NSObject", ["SKPaymentTransactionObserver", "SKProductsRequestDelegate"],
                [
                    new("paymentQueue:updatedTransactions:", (nint)(delegate* unmanaged<nint, nint, nint, nint, void>)&OnUpdated, "v@:@@"),
                    new("paymentQueueRestoreCompletedTransactionsFinished:", (nint)(delegate* unmanaged<nint, nint, nint, void>)&OnRestoreFinished, "v@:@"),
                    new("paymentQueue:restoreCompletedTransactionsFailedWithError:", (nint)(delegate* unmanaged<nint, nint, nint, nint, void>)&OnRestoreFailed, "v@:@@"),
                    new("productsRequest:didReceiveResponse:", (nint)(delegate* unmanaged<nint, nint, nint, nint, void>)&OnProducts, "v@:@@"),
                    new("request:didFailWithError:", (nint)(delegate* unmanaged<nint, nint, nint, nint, void>)&OnRequestFailed, "v@:@@"),
                ]);
            }
        }

        s_current = this;
    }

    /// <inheritdoc/>
    public string Name => "storekit";

    /// <inheritdoc/>
    public bool RestoreIsComplete => false;

    /// <inheritdoc/>
    public void Connect() => IosObjc.OnMain(() =>
    {
        // StoreKit has no connection to make; what can fail is the device refusing purchases (parental controls, restrictions).
        if (!IosObjc.Flag(IosObjc.Cls("SKPaymentQueue"), IosObjc.Sel("canMakePayments")))
        {
            _queue.Post(new StoreEvent(StoreEventKind.Connected) { Error = new StoreError(StoreErrorKind.NotAllowed, "Purchases are turned off on this device.") });
            return;
        }

        _queue.Post(new StoreEvent(StoreEventKind.Connected));
        if (!_observing)
        {
            _observing = true;
            _observer = IosObjc.Obj(IosObjc.Alloc(s_observerClass), IosObjc.Sel("init"));
            // Adding the observer makes the queue deliver transactions the game has not finished yet (a purchase the app was closed during).
            IosObjc.Void(Queue(), IosObjc.Sel("addTransactionObserver:"), _observer);
        }
    });

    /// <inheritdoc/>
    public void LoadProducts(IReadOnlyList<ProductDefinition> products)
    {
        var ids = products.Select(p => p.Id).ToArray();
        IosObjc.OnMain(() =>
        {
            var set = IosObjc.Obj(IosObjc.Cls("NSMutableSet"), IosObjc.Sel("set"));
            foreach (var id in ids)
            {
                IosObjc.Void(set, IosObjc.Sel("addObject:"), IosObjc.NSString(id));
            }

            var request = IosObjc.Obj(IosObjc.Alloc(IosObjc.Cls("SKProductsRequest")), IosObjc.Sel("initWithProductIdentifiers:"), set);
            if (_observer == 0)
            {
                _observer = IosObjc.Obj(IosObjc.Alloc(s_observerClass), IosObjc.Sel("init"));
            }

            IosObjc.Void(request, IosObjc.Sel("setDelegate:"), _observer);
            lock (_lock)
            {
                _requests.Add(request); // owned until the request answers
            }

            IosObjc.Void(request, IosObjc.Sel("start"));
        });
    }

    /// <inheritdoc/>
    public void Buy(ProductDefinition product)
    {
        IosObjc.OnMain(() =>
        {
            nint skProduct;
            lock (_lock)
            {
                _products.TryGetValue(product.Id, out skProduct);
            }

            if (skProduct == 0)
            {
                _queue.Post(Failure(product.Id, new StoreError(StoreErrorKind.ProductNotFound, "The store has not described '" + product.Id + "'.")));
                return;
            }

            var payment = IosObjc.Obj(IosObjc.Cls("SKPayment"), IosObjc.Sel("paymentWithProduct:"), skProduct);
            IosObjc.Void(Queue(), IosObjc.Sel("addPayment:"), payment);
        });
    }

    /// <inheritdoc/>
    public void Finish(PurchaseTransaction transaction, ProductKind kind)
    {
        // StoreKit has one way to finish a transaction whatever the product is: it leaves the queue and is not delivered again.
        IosObjc.OnMain(() =>
        {
            nint tx;
            lock (_lock)
            {
                _open.Remove(transaction.TransactionId, out tx);
            }

            if (tx == 0)
            {
                _queue.Post(new StoreEvent(StoreEventKind.Finished) { Transaction = transaction, Error = new StoreError(StoreErrorKind.InvalidRequest, "No open transaction '" + transaction.TransactionId + "'.") });
                return;
            }

            IosObjc.Void(Queue(), IosObjc.Sel("finishTransaction:"), tx);
            IosObjc.objc_release(tx);
            _queue.Post(new StoreEvent(StoreEventKind.Finished) { Transaction = transaction });
        });
    }

    /// <inheritdoc/>
    public void Restore() => IosObjc.OnMain(() => IosObjc.Void(Queue(), IosObjc.Sel("restoreCompletedTransactions")));

    /// <inheritdoc/>
    public bool TryTake(out StoreEvent storeEvent) => _queue.TryTake(out storeEvent);

    /// <inheritdoc/>
    public void Dispose()
    {
        if (ReferenceEquals(s_current, this))
        {
            s_current = null;
        }

        IosObjc.OnMain(() =>
        {
            if (_observing && _observer != 0)
            {
                IosObjc.Void(Queue(), IosObjc.Sel("removeTransactionObserver:"), _observer);
            }
        });
    }

    private static nint Queue() => IosObjc.Obj(IosObjc.Cls("SKPaymentQueue"), IosObjc.Sel("defaultQueue"));

    private static StoreEvent Failure(string product, StoreError error) =>
        new(StoreEventKind.Transaction) { Transaction = new PurchaseTransaction(string.Empty, product, TransactionState.Failed, 0, 0, string.Empty, string.Empty, string.Empty, error) };

    private static string Text(nint ns, int max)
    {
        var s = IosObjc.ToManaged(ns);
        return s.Length <= max ? s : s[..max];
    }

    // ---- observer callbacks (main thread) ----

    [UnmanagedCallersOnly]
    private static void OnUpdated(nint self, nint cmd, nint queue, nint transactions) => s_current?.Updated(transactions);

    [UnmanagedCallersOnly]
    private static void OnRestoreFinished(nint self, nint cmd, nint queue) => s_current?._queue.Post(new StoreEvent(StoreEventKind.RestoreFinished));

    [UnmanagedCallersOnly]
    private static void OnRestoreFailed(nint self, nint cmd, nint queue, nint error) =>
        s_current?._queue.Post(new StoreEvent(StoreEventKind.RestoreFinished) { Error = MapError(error) });

    [UnmanagedCallersOnly]
    private static void OnProducts(nint self, nint cmd, nint request, nint response) => s_current?.Products(request, response);

    [UnmanagedCallersOnly]
    private static void OnRequestFailed(nint self, nint cmd, nint request, nint error)
    {
        var me = s_current;
        if (me is null)
        {
            return;
        }

        me.ReleaseRequest(request);
        me._queue.Post(new StoreEvent(StoreEventKind.ProductsLoaded) { Error = MapError(error) });
    }

    private void ReleaseRequest(nint request)
    {
        lock (_lock)
        {
            if (!_requests.Remove(request))
            {
                return;
            }
        }

        IosObjc.objc_release(request);
    }

    private void Products(nint request, nint response)
    {
        var found = new List<StoreProduct>();
        var unknown = new List<string>();
        var formatter = IosObjc.Obj(IosObjc.Alloc(IosObjc.Cls("NSNumberFormatter")), IosObjc.Sel("init"));
        IosObjc.Void(formatter, IosObjc.Sel("setNumberStyle:"), (nint)2); // NSNumberFormatterCurrencyStyle
        var items = IosObjc.Items(IosObjc.Obj(response, IosObjc.Sel("products")));
        foreach (var p in items.Take(StoreLimits.MaxItems))
        {
            var id = Text(IosObjc.Obj(p, IosObjc.Sel("productIdentifier")), StoreLimits.MaxTextChars);
            var price = IosObjc.Obj(p, IosObjc.Sel("price"));
            var locale = IosObjc.Obj(p, IosObjc.Sel("priceLocale"));
            IosObjc.Void(formatter, IosObjc.Sel("setLocale:"), locale);
            var shown = Text(IosObjc.Obj(formatter, IosObjc.Sel("stringFromNumber:"), price), 128);
            var micros = (long)Math.Round(IosObjc.Dbl(price, IosObjc.Sel("doubleValue")) * 1_000_000d);
            var currency = locale == 0 ? string.Empty : Text(IosObjc.Obj(locale, IosObjc.Sel("currencyCode")), 16);
            lock (_lock)
            {
                if (_products.Remove(id, out var old))
                {
                    IosObjc.objc_release(old);
                }

                _products[id] = IosObjc.objc_retain(p);
            }

            found.Add(new StoreProduct(
                id,
                ProductKind.Consumable,
                Text(IosObjc.Obj(p, IosObjc.Sel("localizedTitle")), StoreLimits.MaxTextChars),
                Text(IosObjc.Obj(p, IosObjc.Sel("localizedDescription")), 4096),
                shown,
                micros < 0 ? 0 : micros,
                currency));
        }

        IosObjc.objc_release(formatter);
        foreach (var bad in IosObjc.Items(IosObjc.Obj(response, IosObjc.Sel("invalidProductIdentifiers"))).Take(StoreLimits.MaxItems))
        {
            unknown.Add(Text(bad, StoreLimits.MaxTextChars));
        }

        ReleaseRequest(request);
        _queue.Post(new StoreEvent(StoreEventKind.ProductsLoaded) { Products = found, UnknownProductIds = unknown });
    }

    private void Updated(nint transactions)
    {
        foreach (var tx in IosObjc.Items(transactions).Take(StoreLimits.MaxItems * 4))
        {
            var state = (int)IosObjc.Int(tx, IosObjc.Sel("transactionState"));
            if (state == StatePurchasing)
            {
                continue;
            }

            var payment = IosObjc.Obj(tx, IosObjc.Sel("payment"));
            var product = Text(IosObjc.Obj(payment, IosObjc.Sel("productIdentifier")), StoreLimits.MaxTextChars);
            var id = Text(IosObjc.Obj(tx, IosObjc.Sel("transactionIdentifier")), StoreLimits.MaxTextChars);
            switch (state)
            {
                case StatePurchased:
                case StateRestored:
                    var quantity = (int)Math.Clamp((long)IosObjc.Int(payment, IosObjc.Sel("quantity")), 1, 10_000);
                    var date = IosObjc.Obj(tx, IosObjc.Sel("transactionDate"));
                    var ms = date == 0 ? 0 : (long)(IosObjc.Dbl(date, IosObjc.Sel("timeIntervalSince1970")) * 1000d);
                    lock (_lock)
                    {
                        if (_open.Remove(id, out var old))
                        {
                            IosObjc.objc_release(old);
                        }

                        _open[id] = IosObjc.objc_retain(tx);
                    }

                    _queue.Post(new StoreEvent(StoreEventKind.Transaction)
                    {
                        Transaction = new PurchaseTransaction(id, product, state == StateRestored ? TransactionState.Restored : TransactionState.Purchased, quantity, Math.Max(ms, 0), ReadReceipt(), string.Empty, string.Empty, default),
                    });
                    break;
                case StateFailed:
                    var error = MapError(IosObjc.Obj(tx, IosObjc.Sel("error")));
                    var cancelled = ErrorCode(IosObjc.Obj(tx, IosObjc.Sel("error"))) == ErrorPaymentCancelled;
                    // A failed transaction has nothing to grant, so it leaves the queue at once; the game only needs to know it ended.
                    IosObjc.Void(Queue(), IosObjc.Sel("finishTransaction:"), tx);
                    _queue.Post(new StoreEvent(StoreEventKind.Transaction)
                    {
                        Transaction = new PurchaseTransaction(string.Empty, product, cancelled ? TransactionState.Cancelled : TransactionState.Failed, 0, 0, string.Empty, string.Empty, string.Empty, cancelled ? default : error),
                    });
                    break;
                case StateDeferred:
                    _queue.Post(new StoreEvent(StoreEventKind.Transaction)
                    {
                        Transaction = new PurchaseTransaction(id, product, TransactionState.Pending, 1, 0, string.Empty, string.Empty, string.Empty, default),
                    });
                    break;
                default:
                    break;
            }
        }
    }

    // The app receipt is a signed blob the App Store writes next to the app. A server verifies it with Apple; this code only passes it
    // on, base64 encoded, and gives up (empty) on a receipt larger than the limit rather than copying it.
    private static string ReadReceipt()
    {
        var url = IosObjc.Obj(IosObjc.Obj(IosObjc.Cls("NSBundle"), IosObjc.Sel("mainBundle")), IosObjc.Sel("appStoreReceiptURL"));
        if (url == 0)
        {
            return string.Empty;
        }

        var data = IosObjc.Obj(IosObjc.Cls("NSData"), IosObjc.Sel("dataWithContentsOfURL:"), url);
        if (data == 0 || (long)IosObjc.Int(data, IosObjc.Sel("length")) > StoreLimits.MaxProofChars / 4 * 3)
        {
            return string.Empty;
        }

        return Text(IosObjc.Obj(data, IosObjc.Sel("base64EncodedStringWithOptions:"), (nint)0), StoreLimits.MaxProofChars);
    }

    private static int ErrorCode(nint error) => error == 0 ? 0 : (int)IosObjc.Int(error, IosObjc.Sel("code"));

    private static StoreError MapError(nint error)
    {
        if (error == 0)
        {
            return new StoreError(StoreErrorKind.Failed, string.Empty);
        }

        var text = Text(IosObjc.Obj(error, IosObjc.Sel("localizedDescription")), 512);
        // Only errors in StoreKit's own domain use SKError codes; others (network stack, for example) are plain failures.
        var domain = IosObjc.ToManaged(IosObjc.Obj(error, IosObjc.Sel("domain")));
        if (domain != "SKErrorDomain")
        {
            return new StoreError(domain == "NSURLErrorDomain" ? StoreErrorKind.Network : StoreErrorKind.Failed, text);
        }

        return ErrorCode(error) switch
        {
            ErrorClientInvalid => new StoreError(StoreErrorKind.NotAllowed, text),
            ErrorPaymentInvalid => new StoreError(StoreErrorKind.InvalidRequest, text),
            ErrorPaymentNotAllowed => new StoreError(StoreErrorKind.NotAllowed, text),
            ErrorStoreProductNotAvailable => new StoreError(StoreErrorKind.ProductNotFound, text),
            ErrorCloudServiceNetworkConnectionFailed => new StoreError(StoreErrorKind.Network, text),
            ErrorPaymentCancelled => new StoreError(StoreErrorKind.Failed, text),
            _ => new StoreError(StoreErrorKind.Failed, text),
        };
    }
}
