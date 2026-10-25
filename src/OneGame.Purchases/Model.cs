using System.Runtime.CompilerServices;

// Every native call in this assembly passes blittable values, so the runtime never generates marshalling stubs.
[assembly: DisableRuntimeMarshalling]

namespace OneGame.Purchases;

/// <summary>What a product is. It decides how a purchase is finished and whether the player keeps it.</summary>
public enum ProductKind
{
    /// <summary>Used up and bought again (coins, extra lives). Finishing it consumes it; it is never "owned".</summary>
    Consumable,

    /// <summary>Bought once and kept (an unlock). Finishing it acknowledges it; restores bring it back.</summary>
    NonConsumable,

    /// <summary>Renews until cancelled. Owned while the store lists it as active.</summary>
    Subscription,
}

/// <summary>A product the game sells, declared by the game and looked up in the store by <see cref="Id"/>.</summary>
/// <param name="Id">The store's product id (the same text in App Store Connect and Play Console).</param>
/// <param name="Kind">Consumable, non-consumable or subscription.</param>
public readonly record struct ProductDefinition(string Id, ProductKind Kind);

/// <summary>A product as the store describes it to this player: localized text and a price already formatted for their country.</summary>
/// <param name="Id">The store's product id.</param>
/// <param name="Kind">The kind the game declared.</param>
/// <param name="Title">Localized title.</param>
/// <param name="Description">Localized description.</param>
/// <param name="Price">The price as it should be shown, such as <c>$4.99</c>.</param>
/// <param name="PriceMicros">The price in millionths of <paramref name="Currency"/>, for sorting and comparisons; 0 when unknown.</param>
/// <param name="Currency">ISO 4217 currency code, or empty when unknown.</param>
public sealed record StoreProduct(string Id, ProductKind Kind, string Title, string Description, string Price, long PriceMicros, string Currency);

/// <summary>Where a transaction stands.</summary>
public enum TransactionState
{
    /// <summary>The store took the payment. The game grants the item, then calls <see cref="PurchaseService.Finish"/>.</summary>
    Purchased,

    /// <summary>Payment is waiting on something outside the game (a parent's approval, a cash payment). Nothing is granted yet; a later update settles it.</summary>
    Pending,

    /// <summary>The player backed out.</summary>
    Cancelled,

    /// <summary>The purchase did not happen; <see cref="PurchaseTransaction.Error"/> says why.</summary>
    Failed,

    /// <summary>The store took the purchase back (a refund or a revoked family share). The game removes what it granted.</summary>
    Revoked,

    /// <summary>A purchase that already exists, delivered again by a restore or on start. Handled like <see cref="Purchased"/>.</summary>
    Restored,
}

/// <summary>The reasons a store operation fails, in words a game can branch on.</summary>
public enum StoreErrorKind
{
    /// <summary>No error.</summary>
    None,

    /// <summary>The store is not there: not connected, not signed in, purchases switched off, or the library is missing.</summary>
    Unavailable,

    /// <summary>The store could not be reached. Trying again later may work.</summary>
    Network,

    /// <summary>The store does not sell that product (misspelled id, not published for this build or country).</summary>
    ProductNotFound,

    /// <summary>The player already owns it, or has an unfinished purchase of it.</summary>
    AlreadyOwned,

    /// <summary>The player or device may not buy (parental controls, billing not supported).</summary>
    NotAllowed,

    /// <summary>The call was malformed or the game set the store up wrongly (unknown product, missing account setup).</summary>
    InvalidRequest,

    /// <summary>The store did not answer in time.</summary>
    Timeout,

    /// <summary>The store failed and gave no better reason; <see cref="StoreError.Message"/> has its text.</summary>
    Failed,
}

/// <summary>A failure value: what went wrong and the store's own message for logs.</summary>
/// <param name="Kind">The category.</param>
/// <param name="Message">The store's text, bounded in length. May be empty.</param>
public readonly record struct StoreError(StoreErrorKind Kind, string Message)
{
    /// <summary>No error.</summary>
    public static StoreError None => default;

    /// <summary>True when something went wrong.</summary>
    public bool IsError => Kind != StoreErrorKind.None;
}

/// <summary>
/// One purchase as the store reports it. Everything here came from outside the process: treat the ids and proof as opaque text, and
/// check <see cref="ServerProof"/> on your own server before granting anything that matters.
/// </summary>
/// <param name="TransactionId">The store's id for this transaction. Stable across restores; empty for a purchase that never started.</param>
/// <param name="ProductId">The product bought.</param>
/// <param name="State">Where the transaction stands.</param>
/// <param name="Quantity">How many were bought (1 unless the store says otherwise).</param>
/// <param name="PurchaseTimeUnixMs">When the store says it happened, in milliseconds since 1970-01-01 UTC; 0 when unknown.</param>
/// <param name="ServerProof">What a server needs to verify the purchase with the store: the base64 app receipt on Apple, the purchase token on Google Play. Empty for the fake store.</param>
/// <param name="SignedData">Google Play: the purchase's JSON, which <see cref="Signature"/> signs. Empty elsewhere.</param>
/// <param name="Signature">Google Play: the base64 signature over <see cref="SignedData"/>. Empty elsewhere.</param>
/// <param name="Error">Why a <see cref="TransactionState.Failed"/> purchase failed.</param>
public sealed record PurchaseTransaction(
    string TransactionId,
    string ProductId,
    TransactionState State,
    int Quantity,
    long PurchaseTimeUnixMs,
    string ServerProof,
    string SignedData,
    string Signature,
    StoreError Error)
{
    /// <summary>True for states that wait for <see cref="PurchaseService.Finish"/>: <see cref="TransactionState.Purchased"/> and <see cref="TransactionState.Restored"/>.</summary>
    public bool NeedsFinish => State is TransactionState.Purchased or TransactionState.Restored;
}

/// <summary>What kind of thing happened in the store.</summary>
public enum StoreEventKind
{
    /// <summary>The connection attempt ended; <see cref="StoreEvent.Error"/> is set when it failed.</summary>
    Connected,

    /// <summary>The store went away after being connected (Play's service disconnected, for example).</summary>
    Disconnected,

    /// <summary>A product query finished: <see cref="StoreEvent.Products"/> and <see cref="StoreEvent.UnknownProductIds"/>.</summary>
    ProductsLoaded,

    /// <summary>A transaction changed: <see cref="StoreEvent.Transaction"/>. Also used for failed purchases that have no store transaction.</summary>
    Transaction,

    /// <summary>A restore finished. <see cref="StoreEvent.Error"/> is set when it failed. Every restored transaction arrived as a <see cref="Transaction"/> event before this one.</summary>
    RestoreFinished,

    /// <summary>A <see cref="PurchaseService.Finish"/> call ended: <see cref="StoreEvent.Transaction"/> is the one finished; <see cref="StoreEvent.Error"/> is set when the store refused.</summary>
    Finished,
}

/// <summary>One thing the store told the game. Events reach the game only from <see cref="PurchaseService.Pump"/>, on the thread that calls it.</summary>
public sealed record StoreEvent(StoreEventKind Kind)
{
    /// <summary>For <see cref="StoreEventKind.Transaction"/> and <see cref="StoreEventKind.Finished"/>.</summary>
    public PurchaseTransaction? Transaction { get; init; }

    /// <summary>For <see cref="StoreEventKind.ProductsLoaded"/>.</summary>
    public IReadOnlyList<StoreProduct> Products { get; init; } = [];

    /// <summary>For <see cref="StoreEventKind.ProductsLoaded"/>: ids the store does not know.</summary>
    public IReadOnlyList<string> UnknownProductIds { get; init; } = [];

    /// <summary>Set when the operation behind the event failed.</summary>
    public StoreError Error { get; init; }
}

/// <summary>Limits on everything read from a store, so a hostile or broken store cannot make the game allocate without bound.</summary>
public static class StoreLimits
{
    /// <summary>The longest store message the bridge to a native store accepts, in bytes (256 KiB).</summary>
    public const int MaxMessageBytes = 256 * 1024;

    /// <summary>The longest id, title or other short text kept, in UTF-16 characters.</summary>
    public const int MaxTextChars = 1024;

    /// <summary>The longest proof (receipt, purchase token, signed data) kept, in UTF-16 characters.</summary>
    public const int MaxProofChars = 1024 * 1024;

    /// <summary>The most products or purchases one message may list.</summary>
    public const int MaxItems = 256;

    /// <summary>The deepest JSON nesting accepted.</summary>
    public const int MaxJsonDepth = 8;
}
