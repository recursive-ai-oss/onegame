using System.Globalization;

namespace OneGame.Purchases;

/// <summary>What the fake store does with the next purchase of a product.</summary>
public enum FakeOutcome
{
    /// <summary>The player pays: a <see cref="TransactionState.Purchased"/> transaction.</summary>
    Succeed,

    /// <summary>The player backs out: <see cref="TransactionState.Cancelled"/>.</summary>
    Cancel,

    /// <summary>The payment waits on approval: <see cref="TransactionState.Pending"/>, settled by <see cref="FakeStore.ResolvePending"/>.</summary>
    Pend,

    /// <summary>The store fails the purchase: <see cref="TransactionState.Failed"/> with the scripted error.</summary>
    Fail,
}

/// <summary>
/// A deterministic, scriptable store that runs in-process on every platform, so a game and its scenarios can test purchase flows with
/// no store account. It follows the rules the real stores share: a non-consumable can be bought once, a consumable cannot be bought
/// again until the last purchase is finished, unfinished purchases come back on a restore, and answers arrive as events in the
/// order the requests were made. Transaction ids are <c>fake-1</c>, <c>fake-2</c>, and so on.
/// </summary>
public sealed class FakeStore : IPurchaseBackend
{
    private sealed class Ledger(string id, string product, ProductKind kind, int number)
    {
        public string Id { get; } = id;

        public string Product { get; } = product;

        public ProductKind Kind { get; } = kind;

        public int Number { get; } = number;

        public TransactionState State { get; set; } = TransactionState.Purchased;

        public bool Finished { get; set; }
    }

    private readonly StoreEventBuffer _queue = new();
    private readonly object _lock = new();
    private readonly Dictionary<string, StoreProduct> _catalog = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Queue<(FakeOutcome Outcome, StoreError Error)>> _scripts = new(StringComparer.Ordinal);
    private readonly List<Ledger> _ledger = [];
    private readonly Dictionary<string, Ledger> _pendingByProduct = new(StringComparer.Ordinal);
    private int _next = 1;
    private bool _connected;

    /// <summary>Creates a store selling the given products.</summary>
    public FakeStore(IEnumerable<StoreProduct> products)
    {
        ArgumentNullException.ThrowIfNull(products);
        foreach (var p in products)
        {
            _catalog[p.Id] = p;
        }
    }

    /// <summary>A store that sells every given definition, with generated titles and prices of 0.99 and up in USD.</summary>
    public static FakeStore For(IEnumerable<ProductDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        var n = 0;
        return new FakeStore(definitions.Select(d =>
        {
            n++;
            var micros = (990_000L * n);
            return new StoreProduct(d.Id, d.Kind, "Product " + d.Id, "Fake " + d.Kind, "$" + (micros / 1_000_000.0).ToString("0.00", CultureInfo.InvariantCulture), micros, "USD");
        }));
    }

    /// <inheritdoc/>
    public string Name => "fake";

    /// <inheritdoc/>
    public bool RestoreIsComplete => true;

    /// <summary>When set, the next <see cref="Connect"/> fails with this error (and the one after succeeds again unless it is set again).</summary>
    public StoreError? ConnectFailure { get; set; }

    /// <summary>Queues the outcome of a later purchase of the product. Outcomes are used in order, one per purchase; after the script runs out a purchase succeeds.</summary>
    /// <param name="productId">The product.</param>
    /// <param name="outcome">What happens.</param>
    /// <param name="failure">For <see cref="FakeOutcome.Fail"/>: the error. Defaults to <see cref="StoreErrorKind.Failed"/>.</param>
    public void Script(string productId, FakeOutcome outcome, StoreError? failure = null)
    {
        lock (_lock)
        {
            if (!_scripts.TryGetValue(productId, out var q))
            {
                _scripts[productId] = q = new Queue<(FakeOutcome, StoreError)>();
            }

            q.Enqueue((outcome, failure ?? new StoreError(StoreErrorKind.Failed, "scripted failure")));
        }
    }

    /// <summary>Settles a pending purchase: approved becomes <see cref="TransactionState.Purchased"/>, declined becomes <see cref="TransactionState.Failed"/>.</summary>
    /// <returns>False when no purchase of that product was pending.</returns>
    public bool ResolvePending(string productId, bool approve)
    {
        lock (_lock)
        {
            if (!_pendingByProduct.Remove(productId, out var entry))
            {
                return false;
            }

            if (approve)
            {
                entry.State = TransactionState.Purchased;
                Post(Transaction(entry, TransactionState.Purchased));
            }
            else
            {
                _ledger.Remove(entry);
                Post(Transaction(entry, TransactionState.Failed, new StoreError(StoreErrorKind.NotAllowed, "declined")));
            }

            return true;
        }
    }

    /// <summary>Takes a purchase back, as a refund does: the player no longer owns the product and the game is told with a <see cref="TransactionState.Revoked"/> transaction.</summary>
    /// <returns>False when the player has no purchase of it.</returns>
    public bool Revoke(string productId)
    {
        lock (_lock)
        {
            var entry = _ledger.LastOrDefault(l => l.Product == productId && l.State == TransactionState.Purchased);
            if (entry is null)
            {
                return false;
            }

            entry.State = TransactionState.Revoked;
            Post(Transaction(entry, TransactionState.Revoked));
            return true;
        }
    }

    /// <summary>Drops the connection, as a store service that died would.</summary>
    public void Disconnect()
    {
        lock (_lock)
        {
            _connected = false;
            Post(new StoreEvent(StoreEventKind.Disconnected));
        }
    }

    /// <summary>The product ids the player owns now (non-consumables and subscriptions that were bought and not revoked), in id order.</summary>
    public IReadOnlyList<string> Owned()
    {
        lock (_lock)
        {
            return [.. _ledger.Where(l => l.Kind != ProductKind.Consumable && l.State == TransactionState.Purchased).Select(l => l.Product).Distinct().Order(StringComparer.Ordinal)];
        }
    }

    /// <inheritdoc/>
    public void Connect()
    {
        lock (_lock)
        {
            if (ConnectFailure is { } failure)
            {
                ConnectFailure = null;
                _connected = false;
                Post(new StoreEvent(StoreEventKind.Connected) { Error = failure });
                return;
            }

            _connected = true;
            Post(new StoreEvent(StoreEventKind.Connected));
        }
    }

    /// <inheritdoc/>
    public void LoadProducts(IReadOnlyList<ProductDefinition> products)
    {
        lock (_lock)
        {
            if (!_connected)
            {
                Post(new StoreEvent(StoreEventKind.ProductsLoaded) { Error = Unavailable() });
                return;
            }

            var found = new List<StoreProduct>();
            var unknown = new List<string>();
            foreach (var d in products)
            {
                if (_catalog.TryGetValue(d.Id, out var p))
                {
                    found.Add(p with { Kind = d.Kind });
                }
                else
                {
                    unknown.Add(d.Id);
                }
            }

            Post(new StoreEvent(StoreEventKind.ProductsLoaded) { Products = found, UnknownProductIds = unknown });
        }
    }

    /// <inheritdoc/>
    public void Buy(ProductDefinition product)
    {
        lock (_lock)
        {
            if (!_connected)
            {
                Post(Failure(product.Id, Unavailable()));
                return;
            }

            if (!_catalog.ContainsKey(product.Id))
            {
                Post(Failure(product.Id, new StoreError(StoreErrorKind.ProductNotFound, "The store does not sell '" + product.Id + "'.")));
                return;
            }

            // Stores refuse a second purchase of something owned; a consumable counts as owned until it is consumed.
            var blocked = _ledger.Any(l => l.Product == product.Id && ((l.Kind != ProductKind.Consumable && l.State == TransactionState.Purchased) || (l.Kind == ProductKind.Consumable && !l.Finished && l.State != TransactionState.Revoked)));
            if (blocked || _pendingByProduct.ContainsKey(product.Id))
            {
                Post(Failure(product.Id, new StoreError(StoreErrorKind.AlreadyOwned, "'" + product.Id + "' is already owned or in progress.")));
                return;
            }

            var (outcome, error) = _scripts.TryGetValue(product.Id, out var q) && q.TryDequeue(out var next) ? next : (FakeOutcome.Succeed, default);
            switch (outcome)
            {
                case FakeOutcome.Cancel:
                    Post(new StoreEvent(StoreEventKind.Transaction)
                    {
                        Transaction = new PurchaseTransaction(string.Empty, product.Id, TransactionState.Cancelled, 0, 0, string.Empty, string.Empty, string.Empty, default),
                    });
                    break;
                case FakeOutcome.Fail:
                    Post(Failure(product.Id, error));
                    break;
                case FakeOutcome.Pend:
                    var pending = NewEntry(product);
                    pending.State = TransactionState.Pending;
                    _pendingByProduct[product.Id] = pending;
                    Post(Transaction(pending, TransactionState.Pending));
                    break;
                default:
                    var entry = NewEntry(product);
                    Post(Transaction(entry, TransactionState.Purchased));
                    break;
            }
        }
    }

    /// <inheritdoc/>
    public void Finish(PurchaseTransaction transaction, ProductKind kind)
    {
        lock (_lock)
        {
            var entry = _ledger.FirstOrDefault(l => l.Id == transaction.TransactionId);
            if (!_connected)
            {
                Post(new StoreEvent(StoreEventKind.Finished) { Transaction = transaction, Error = Unavailable() });
            }
            else if (entry is null)
            {
                Post(new StoreEvent(StoreEventKind.Finished) { Transaction = transaction, Error = new StoreError(StoreErrorKind.InvalidRequest, "Unknown transaction '" + transaction.TransactionId + "'.") });
            }
            else
            {
                entry.Finished = true;
                Post(new StoreEvent(StoreEventKind.Finished) { Transaction = transaction });
            }
        }
    }

    /// <inheritdoc/>
    public void Restore()
    {
        lock (_lock)
        {
            if (!_connected)
            {
                Post(new StoreEvent(StoreEventKind.RestoreFinished) { Error = Unavailable() });
                return;
            }

            foreach (var l in _ledger.Where(l => l.State == TransactionState.Purchased && (l.Kind != ProductKind.Consumable || !l.Finished)).OrderBy(l => l.Number))
            {
                Post(Transaction(l, TransactionState.Restored));
            }

            Post(new StoreEvent(StoreEventKind.RestoreFinished));
        }
    }

    /// <inheritdoc/>
    public bool TryTake(out StoreEvent storeEvent) => _queue.TryTake(out storeEvent);

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    private Ledger NewEntry(ProductDefinition product)
    {
        var number = _next++;
        var entry = new Ledger("fake-" + number.ToString(CultureInfo.InvariantCulture), product.Id, product.Kind, number);
        _ledger.Add(entry);
        return entry;
    }

    private static PurchaseTransaction TransactionOf(Ledger l, TransactionState state, StoreError error) =>
        new(l.Id, l.Product, state, 1, 1_700_000_000_000L + (l.Number * 1000L), string.Empty, string.Empty, string.Empty, error);

    private static StoreEvent Transaction(Ledger l, TransactionState state, StoreError error = default) =>
        new(StoreEventKind.Transaction) { Transaction = TransactionOf(l, state, error) };

    private static StoreEvent Failure(string productId, StoreError error) =>
        new(StoreEventKind.Transaction) { Transaction = new PurchaseTransaction(string.Empty, productId, TransactionState.Failed, 0, 0, string.Empty, string.Empty, string.Empty, error) };

    private static StoreError Unavailable() => new(StoreErrorKind.Unavailable, "The fake store is not connected.");

    private void Post(StoreEvent e) => _queue.Post(e);
}
