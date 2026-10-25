using OneGame.Platform;
namespace OneGame.Purchases;

/// <summary>
/// A store that is not there: connecting fails with <see cref="StoreErrorKind.Unavailable"/> and says why. A game on a platform with no
/// store (a desktop build, a test run) can use the same code path as everywhere else and just see that purchases are unavailable.
/// </summary>
public sealed class UnavailableStore(string reason) : IPurchaseBackend
{
    private readonly StoreEventBuffer _queue = new();

    /// <inheritdoc/>
    public string Name => "none";

    /// <inheritdoc/>
    public bool RestoreIsComplete => false;

    /// <inheritdoc/>
    public void Connect() => _queue.Post(new StoreEvent(StoreEventKind.Connected) { Error = Error });

    /// <inheritdoc/>
    public void LoadProducts(IReadOnlyList<ProductDefinition> products) => _queue.Post(new StoreEvent(StoreEventKind.ProductsLoaded) { Error = Error });

    /// <inheritdoc/>
    public void Buy(ProductDefinition product) => _queue.Post(new StoreEvent(StoreEventKind.Transaction)
    {
        Transaction = new PurchaseTransaction(string.Empty, product.Id, TransactionState.Failed, 0, 0, string.Empty, string.Empty, string.Empty, Error),
    });

    /// <inheritdoc/>
    public void Finish(PurchaseTransaction transaction, ProductKind kind) => _queue.Post(new StoreEvent(StoreEventKind.Finished) { Transaction = transaction, Error = Error });

    /// <inheritdoc/>
    public void Restore() => _queue.Post(new StoreEvent(StoreEventKind.RestoreFinished) { Error = Error });

    /// <inheritdoc/>
    public bool TryTake(out StoreEvent storeEvent) => _queue.TryTake(out storeEvent);

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    private StoreError Error => new(StoreErrorKind.Unavailable, reason);
}

/// <summary>Chooses the purchase backend for the running platform.</summary>
public static class PurchaseBackends
{
    /// <summary>Set to <c>fake</c> to use a <see cref="FakeStore"/> everywhere (a development run or a test), whatever the platform.</summary>
    public const string BackendVariable = "ONEGAME_STORE";

    /// <summary>The platform's real store: Google Play on Android, the App Store on iOS and macOS. Anywhere else, or when the store cannot be set up, an <see cref="UnavailableStore"/> that says why.</summary>
    public static IPurchaseBackend CreateNative()
    {
        try
        {
            // NativeAOT apps run on the linux-bionic runtime, where OperatingSystem.IsAndroid() is false; the activity host is the reliable sign.
            if (OperatingSystem.IsAndroid() || AndroidActivityHost.Current is not null)
            {
                return new PlayBillingBackend();
            }

            if (OperatingSystem.IsIOS() || OperatingSystem.IsMacOS())
            {
                return new StoreKitBackend();
            }
        }
        catch (PlatformNotSupportedException e)
        {
            return new UnavailableStore(e.Message);
        }

        return new UnavailableStore("This platform has no in-app purchase store.");
    }

    /// <summary>
    /// Makes a service for a game: the fake store when <see cref="BackendVariable"/> is <c>fake</c>, otherwise <see cref="CreateNative"/>.
    /// The fake sells every product in the catalog.
    /// </summary>
    public static PurchaseService CreateService(IReadOnlyList<ProductDefinition> catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var backend = string.Equals(Environment.GetEnvironmentVariable(BackendVariable), "fake", StringComparison.OrdinalIgnoreCase)
            ? FakeStore.For(catalog)
            : CreateNative();
        return new PurchaseService(backend, catalog);
    }
}
