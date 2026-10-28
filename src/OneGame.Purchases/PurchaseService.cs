using System.Globalization;

namespace OneGame.Purchases;

/// <summary>Where the connection to the store stands.</summary>
public enum StoreConnection
{
    /// <summary><see cref="PurchaseService.Connect"/> has not been called, or the store went away.</summary>
    Disconnected,

    /// <summary>Waiting for the store to answer.</summary>
    Connecting,

    /// <summary>Ready: queries and purchases can be made.</summary>
    Connected,

    /// <summary>The last attempt failed; <see cref="PurchaseService.ConnectError"/> says why.</summary>
    Failed,
}

/// <summary>A snapshot of the service for logs and the harness.</summary>
/// <param name="Store">Backend name.</param>
/// <param name="Connection">Connection state as text.</param>
/// <param name="ConnectError">The connect failure, or null.</param>
/// <param name="Products">Product ids the store described.</param>
/// <param name="Owned">Product ids the player owns (non-consumables and subscriptions).</param>
/// <param name="Unfinished">Transactions waiting for <see cref="PurchaseService.Finish"/> as <c>id:product:state</c>.</param>
/// <param name="Pending">Product ids with a purchase waiting on approval.</param>
/// <param name="Recent">The latest events, oldest first, as short text.</param>
public sealed record PurchaseStatus(string Store, string Connection, string? ConnectError, string[] Products, string[] Owned, string[] Unfinished, string[] Pending, string[] Recent);

/// <summary>
/// In-app purchases for a game: one API over the platform's store. A game declares its products, connects, loads them, buys and finishes.
/// Everything the store says arrives as <see cref="StoreEvent"/>s from <see cref="Pump"/>, which the game calls once per frame on its
/// game thread; requests may be made from that thread too. Pick the backend with <see cref="PurchaseBackends"/>.
/// </summary>
/// <remarks>
/// Purchases are view-side and asynchronous: never call this from sim code. Grant the purchased item through a game command, wait
/// until it is saved, and only then call <see cref="Finish"/>; the stores refund purchases that are never finished.
/// </remarks>
/// <example>
/// <code>
/// var catalog = new[] { new ProductDefinition("coins_100", ProductKind.Consumable), new ProductDefinition("no_ads", ProductKind.NonConsumable) };
/// using var store = PurchaseBackends.CreateService(catalog);   // ONEGAME_STORE=fake selects the in-process fake
/// store.Event += e =&gt;
/// {
///     if (e.Kind == StoreEventKind.Connected) { store.LoadProducts(); }
///     if (e.Kind == StoreEventKind.Transaction &amp;&amp; e.Transaction is { NeedsFinish: true } t)
///     {
///         // grant the item with a command, save, then:
///         store.Finish(t);
///     }
/// };
/// store.Connect();
/// // Once per frame on the game thread: store.Pump();
/// // From a button: var error = store.Buy("coins_100");
/// </code>
/// </example>
public sealed class PurchaseService : IDisposable
{
    private const int RecentCapacity = 32;

    private readonly IPurchaseBackend _backend;
    private readonly Dictionary<string, ProductDefinition> _catalog = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StoreProduct> _products = new(StringComparer.Ordinal);
    private readonly SortedSet<string> _owned = new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, PurchaseTransaction> _unfinished = new(StringComparer.Ordinal);
    private readonly SortedSet<string> _pending = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _lastTransaction = new(StringComparer.Ordinal);
    private readonly Queue<string> _recent = new();
    private readonly object _lock = new();
    private HashSet<string>? _restoreSeen;
    private StoreConnection _connection;
    private StoreError _connectError;
    private bool _disposed;

    /// <summary>The most recently created service that has not been disposed; the harness uses it when a game does nothing special.</summary>
    public static PurchaseService? Active { get; private set; }

    /// <summary>Creates a service over a backend. The catalog lists every product the game sells; ids must be unique and non-empty.</summary>
    /// <exception cref="ArgumentException">An id is empty, too long, or listed twice.</exception>
    public PurchaseService(IPurchaseBackend backend, IEnumerable<ProductDefinition> catalog)
    {
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(catalog);
        _backend = backend;
        foreach (var p in catalog)
        {
            if (string.IsNullOrEmpty(p.Id) || p.Id.Length > StoreLimits.MaxTextChars)
            {
                throw new ArgumentException("A product id must be 1 to " + StoreLimits.MaxTextChars.ToString(CultureInfo.InvariantCulture) + " characters.", nameof(catalog));
            }

            if (!_catalog.TryAdd(p.Id, p))
            {
                throw new ArgumentException("Product '" + p.Id + "' is listed twice.", nameof(catalog));
            }
        }

        Active = this;
    }

    /// <summary>Raised from <see cref="Pump"/> for every store event, after the service updated its own state.</summary>
    public event Action<StoreEvent>? Event;

    /// <summary>The backend's short name.</summary>
    public string Name => _backend.Name;

    /// <summary>The backend, for tests that script a fake store.</summary>
    public IPurchaseBackend Backend => _backend;

    /// <summary>Connection state.</summary>
    public StoreConnection Connection
    {
        get
        {
            lock (_lock)
            {
                return _connection;
            }
        }
    }

    /// <summary>Why the last connect failed; <see cref="StoreError.None"/> otherwise.</summary>
    public StoreError ConnectError
    {
        get
        {
            lock (_lock)
            {
                return _connectError;
            }
        }
    }

    /// <summary>The products the store described, in id order.</summary>
    public IReadOnlyList<StoreProduct> Products
    {
        get
        {
            lock (_lock)
            {
                return [.. _products.Values.OrderBy(p => p.Id, StringComparer.Ordinal)];
            }
        }
    }

    /// <summary>Transactions the game has been told about but not finished, in transaction id order.</summary>
    public IReadOnlyList<PurchaseTransaction> Unfinished
    {
        get
        {
            lock (_lock)
            {
                return [.. _unfinished.Values];
            }
        }
    }

    /// <summary>True when a non-consumable or subscription is owned: a purchase or restore arrived and no revocation since.</summary>
    /// <param name="productId">A product id from the catalog.</param>
    public bool IsOwned(string productId)
    {
        lock (_lock)
        {
            return _owned.Contains(productId);
        }
    }

    /// <summary>True when a purchase of the product is waiting on approval outside the game.</summary>
    public bool IsPending(string productId)
    {
        lock (_lock)
        {
            return _pending.Contains(productId);
        }
    }

    /// <summary>Starts connecting to the store. The answer is a <see cref="StoreEventKind.Connected"/> event.</summary>
    public void Connect()
    {
        lock (_lock)
        {
            if (_connection == StoreConnection.Connecting)
            {
                return;
            }

            _connection = StoreConnection.Connecting;
            _connectError = default;
        }

        _backend.Connect();
    }

    /// <summary>Asks the store about every product in the catalog.</summary>
    /// <returns>An error when the request was not sent; <see cref="StoreError.None"/> when the answer will come as an event.</returns>
    public StoreError LoadProducts()
    {
        if (Connection != StoreConnection.Connected)
        {
            return new StoreError(StoreErrorKind.Unavailable, "The store is not connected.");
        }

        _backend.LoadProducts([.. _catalog.Values.OrderBy(p => p.Id, StringComparer.Ordinal)]);
        return default;
    }

    /// <summary>Starts a purchase. The result arrives as a <see cref="StoreEventKind.Transaction"/> event.</summary>
    /// <param name="productId">A product id from the catalog.</param>
    /// <returns>An error when the purchase was not started: not connected, not in the catalog, or not loaded from the store yet. <see cref="StoreError.None"/> when it started.</returns>
    public StoreError Buy(string productId)
    {
        if (!_catalog.TryGetValue(productId ?? string.Empty, out var definition))
        {
            return new StoreError(StoreErrorKind.ProductNotFound, "'" + productId + "' is not in this game's catalog.");
        }

        lock (_lock)
        {
            if (_connection != StoreConnection.Connected)
            {
                return new StoreError(StoreErrorKind.Unavailable, "The store is not connected.");
            }

            if (!_products.ContainsKey(productId!))
            {
                return new StoreError(StoreErrorKind.ProductNotFound, "The store has not described '" + productId + "'; call LoadProducts and wait for ProductsLoaded.");
            }

            if (definition.Kind != ProductKind.Consumable && _owned.Contains(productId!))
            {
                return new StoreError(StoreErrorKind.AlreadyOwned, "The player already owns '" + productId + "'.");
            }
        }

        _backend.Buy(definition);
        return default;
    }

    /// <summary>Tells the store the game granted a purchased or restored transaction. Call it once the item is saved; the store refunds unfinished purchases after a few days. The result is a <see cref="StoreEventKind.Finished"/> event.</summary>
    /// <returns>An error when the call was not sent.</returns>
    public StoreError Finish(PurchaseTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        if (!transaction.NeedsFinish)
        {
            return new StoreError(StoreErrorKind.InvalidRequest, "Only purchased or restored transactions are finished.");
        }

        if (!_catalog.TryGetValue(transaction.ProductId, out var definition))
        {
            return new StoreError(StoreErrorKind.ProductNotFound, "'" + transaction.ProductId + "' is not in this game's catalog.");
        }

        if (Connection != StoreConnection.Connected)
        {
            return new StoreError(StoreErrorKind.Unavailable, "The store is not connected.");
        }

        _backend.Finish(transaction, definition.Kind);
        return default;
    }

    /// <summary>Asks the store for the player's existing purchases. Each arrives as a <see cref="TransactionState.Restored"/> transaction, then <see cref="StoreEventKind.RestoreFinished"/>. On Apple platforms this may ask the player to sign in, so call it from a "restore purchases" button.</summary>
    /// <returns>An error when the request was not sent.</returns>
    public StoreError Restore()
    {
        lock (_lock)
        {
            if (_connection != StoreConnection.Connected)
            {
                return new StoreError(StoreErrorKind.Unavailable, "The store is not connected.");
            }

            _restoreSeen = new HashSet<string>(StringComparer.Ordinal);
        }

        _backend.Restore();
        return default;
    }

    /// <summary>Delivers what the store reported since the last call: updates the service's state and raises <see cref="Event"/> for each. Call once per frame from the game thread. Cheap when nothing happened.</summary>
    public void Pump()
    {
        while (_backend.TryTake(out var e))
        {
            List<StoreEvent>? extra = null;
            lock (_lock)
            {
                Apply(e, ref extra);
            }

            Event?.Invoke(e);
            if (extra is not null)
            {
                foreach (var x in extra)
                {
                    Event?.Invoke(x);
                }
            }
        }
    }

    /// <summary>A snapshot for logs and the harness.</summary>
    public PurchaseStatus Describe()
    {
        lock (_lock)
        {
            return new PurchaseStatus(
                _backend.Name,
                _connection.ToString().ToLowerInvariant(),
                _connectError.IsError ? _connectError.Kind.ToString() + ": " + _connectError.Message : null,
                [.. _products.Keys.Order(StringComparer.Ordinal)],
                [.. _owned],
                [.. _unfinished.Values.Select(t => t.TransactionId + ":" + t.ProductId + ":" + t.State.ToString().ToLowerInvariant())],
                [.. _pending],
                [.. _recent]);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (ReferenceEquals(Active, this))
        {
            Active = null;
        }

        _backend.Dispose();
    }

    private void Apply(StoreEvent e, ref List<StoreEvent>? extra)
    {
        switch (e.Kind)
        {
            case StoreEventKind.Connected:
                _connection = e.Error.IsError ? StoreConnection.Failed : StoreConnection.Connected;
                _connectError = e.Error;
                break;
            case StoreEventKind.Disconnected:
                _connection = StoreConnection.Disconnected;
                break;
            case StoreEventKind.ProductsLoaded:
                foreach (var p in e.Products)
                {
                    // The kind is the game's declaration; a store has no say in how the game treats a product.
                    var kind = _catalog.TryGetValue(p.Id, out var d) ? d.Kind : p.Kind;
                    _products[p.Id] = p with { Kind = kind };
                }

                break;
            case StoreEventKind.Transaction when e.Transaction is { } t:
                ApplyTransaction(t);
                break;
            case StoreEventKind.Finished when e.Transaction is { } done && !e.Error.IsError:
                _unfinished.Remove(done.TransactionId);
                break;
            case StoreEventKind.RestoreFinished:
                if (!e.Error.IsError && _backend.RestoreIsComplete && _restoreSeen is { } seen)
                {
                    // A complete restore is the whole truth: what was owned and is missing now was refunded or expired.
                    foreach (var id in _owned.Where(id => !seen.Contains(id)).ToList())
                    {
                        var revoked = new PurchaseTransaction(_lastTransaction.GetValueOrDefault(id, string.Empty), id, TransactionState.Revoked, 1, 0, string.Empty, string.Empty, string.Empty, default);
                        ApplyTransaction(revoked);
                        (extra ??= []).Add(new StoreEvent(StoreEventKind.Transaction) { Transaction = revoked });
                    }
                }

                _restoreSeen = null;
                break;
            default:
                break;
        }

        _recent.Enqueue(Summarize(e));
        while (_recent.Count > RecentCapacity)
        {
            _recent.Dequeue();
        }
    }

    private void ApplyTransaction(PurchaseTransaction t)
    {
        var kind = _catalog.TryGetValue(t.ProductId, out var d) ? d.Kind : ProductKind.Consumable;
        switch (t.State)
        {
            case TransactionState.Purchased:
            case TransactionState.Restored:
                _pending.Remove(t.ProductId);
                _restoreSeen?.Add(t.ProductId);
                _lastTransaction[t.ProductId] = t.TransactionId;
                if (kind != ProductKind.Consumable)
                {
                    _owned.Add(t.ProductId);
                }

                _unfinished[t.TransactionId] = t;
                break;
            case TransactionState.Pending:
                _pending.Add(t.ProductId);
                break;
            case TransactionState.Revoked:
                _owned.Remove(t.ProductId);
                _pending.Remove(t.ProductId);
                _unfinished.Remove(t.TransactionId);
                break;
            default:
                _pending.Remove(t.ProductId);
                break;
        }
    }

    private static string Summarize(StoreEvent e)
    {
        var text = e.Kind.ToString().ToLowerInvariant();
        if (e.Transaction is { } t)
        {
            text += " " + t.ProductId + " " + t.State.ToString().ToLowerInvariant();
        }

        if (e.Kind == StoreEventKind.ProductsLoaded)
        {
            text += " " + e.Products.Count.ToString(CultureInfo.InvariantCulture) + " products, " + e.UnknownProductIds.Count.ToString(CultureInfo.InvariantCulture) + " unknown";
        }

        var error = e.Error.IsError ? e.Error : e.Transaction?.Error ?? default;
        return error.IsError ? text + " error " + error.Kind : text;
    }
}
