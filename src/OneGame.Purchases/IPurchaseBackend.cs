namespace OneGame.Purchases;

/// <summary>
/// What a purchase backend implements. Requests are made from the game thread; answers may be produced on any thread and are posted to
/// a <see cref="StoreEventBuffer"/>, which <see cref="PurchaseService.Pump"/> drains on the game thread. A request never blocks on the store.
/// </summary>
public interface IPurchaseBackend : IDisposable
{
    /// <summary>Short store name: <c>fake</c>, <c>storekit</c>, <c>play</c>.</summary>
    string Name { get; }

    /// <summary>True when a restore lists every purchase the player still owns (Google Play), so anything owned before and missing now was refunded. False when a restore only replays past purchases (StoreKit).</summary>
    bool RestoreIsComplete { get; }

    /// <summary>Starts connecting. Ends with a <see cref="StoreEventKind.Connected"/> event, successful or not.</summary>
    void Connect();

    /// <summary>Asks the store about the products. Ends with a <see cref="StoreEventKind.ProductsLoaded"/> event.</summary>
    void LoadProducts(IReadOnlyList<ProductDefinition> products);

    /// <summary>Starts the store's purchase flow for a product the store has described. Ends with a <see cref="StoreEventKind.Transaction"/> event.</summary>
    void Buy(ProductDefinition product);

    /// <summary>Tells the store the game granted a transaction: consumes it (consumables) or acknowledges it (the rest). Ends with a <see cref="StoreEventKind.Finished"/> event.</summary>
    void Finish(PurchaseTransaction transaction, ProductKind kind);

    /// <summary>Asks for the player's existing purchases. Each arrives as a <see cref="StoreEventKind.Transaction"/> event, then <see cref="StoreEventKind.RestoreFinished"/>.</summary>
    void Restore();

    /// <summary>Takes the next event the store produced, if any.</summary>
    bool TryTake(out StoreEvent storeEvent);
}

/// <summary>A thread-safe, bounded queue that carries events from a store's threads to the game thread.</summary>
public sealed class StoreEventBuffer
{
    private const int Capacity = 4096;
    private readonly Queue<StoreEvent> _events = new();
    private readonly object _lock = new();

    /// <summary>Adds an event. When the game stops pumping and the queue is full, the oldest event is dropped, so a stalled game cannot grow memory.</summary>
    public void Post(StoreEvent storeEvent)
    {
        ArgumentNullException.ThrowIfNull(storeEvent);
        lock (_lock)
        {
            if (_events.Count >= Capacity)
            {
                _events.Dequeue();
            }

            _events.Enqueue(storeEvent);
        }
    }

    /// <summary>Takes the oldest event.</summary>
    public bool TryTake(out StoreEvent storeEvent)
    {
        lock (_lock)
        {
            return _events.TryDequeue(out storeEvent!);
        }
    }
}
