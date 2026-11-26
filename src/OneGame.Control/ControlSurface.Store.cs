using OneGame.Purchases;

namespace OneGame.Control;

public sealed partial class ControlSurface
{
    /// <summary>The game's purchase service, for the <c>store</c> operation. Defaults to the most recently created <see cref="PurchaseService"/>, so a game needs to do nothing; set it to point at another.</summary>
    public PurchaseService? Purchases { get; set; }

    private PurchaseService PurchasesOrThrow() =>
        Purchases ?? PurchaseService.Active ?? throw new ControlException("no_store", "This game has no purchase service running (create a PurchaseService).");

    private static FakeStore FakeOrThrow(PurchaseService service) =>
        service.Backend as FakeStore ?? throw new ControlException("rejected", $"'{service.Name}' is a real store. The script, resolve, revoke, disconnect and fail-connect actions drive only the fake store (set ONEGAME_STORE=fake).");

    private StoreProductsStatus StoreProducts() =>
        new([.. PurchasesOrThrow().Products.Select(p => new StoreProductInfo(p.Id, p.Kind.ToString(), p.Title, p.Price, p.PriceMicros, p.Currency))]);

    private PurchaseStatus StoreAction(Args a)
    {
        var service = PurchasesOrThrow();
        var action = a.Str("action") ?? "describe";
        var product = a.Str("product");
        string Product() => product ?? throw new ControlException("bad_args", $"'{action}' needs a product id.");

        static ControlException Failed(StoreError e) => new("store_" + e.Kind.ToString().ToLowerInvariant(), e.Message);

        switch (action)
        {
            case "describe":
                break;
            case "connect":
                service.Connect();
                break;
            case "load":
                ThrowIf(service.LoadProducts());
                break;
            case "buy":
                ThrowIf(service.Buy(Product()));
                break;
            case "finish":
                // Finishes the unfinished transaction of the product, or every unfinished one when no product is given.
                var open = service.Unfinished.Where(t => product is null || t.ProductId == product).ToList();
                if (open.Count == 0 && product is not null)
                {
                    throw new ControlException("not_found", $"No unfinished transaction for '{product}'.");
                }

                foreach (var t in open)
                {
                    ThrowIf(service.Finish(t));
                }

                break;
            case "restore":
                ThrowIf(service.Restore());
                break;
            case "script":
                FakeOrThrow(service).Script(Product(), ParseOutcome(a.Str("outcome")), ParseFailure(a.Str("error")));
                break;
            case "resolve":
                if (!FakeOrThrow(service).ResolvePending(Product(), a.Bool("approve", true)))
                {
                    throw new ControlException("not_found", $"No pending purchase of '{product}'.");
                }

                break;
            case "revoke":
                if (!FakeOrThrow(service).Revoke(Product()))
                {
                    throw new ControlException("not_found", $"The player has no purchase of '{product}' to take back.");
                }

                break;
            case "price":
                // price: product, amount ("4.99"), currency ("EUR"), optional display text. Products are reloaded so the game sees it.
                if (!FakePrice.TryParseAmount(a.Str("amount"), out var micros))
                {
                    throw new ControlException("bad_args", "price needs an amount such as \"4.99\" (digits and at most 6 decimals).");
                }

                var currency = a.Str("currency") ?? "USD";
                if (currency.Length != 3 || !currency.All(char.IsAsciiLetter))
                {
                    throw new ControlException("bad_args", $"currency is a 3-letter ISO 4217 code, got '{currency}'.");
                }

                if (!FakeOrThrow(service).SetPrice(Product(), micros, currency, a.Str("display")))
                {
                    throw new ControlException("not_found", $"The fake store does not sell '{product}'.");
                }

                if (service.Connection == StoreConnection.Connected)
                {
                    ThrowIf(service.LoadProducts());
                }

                break;
            case "disconnect":
                FakeOrThrow(service).Disconnect();
                break;
            case "fail-connect":
                FakeOrThrow(service).ConnectFailure = ParseFailure(a.Str("error") ?? "unavailable") ?? new StoreError(StoreErrorKind.Unavailable, "scripted");
                break;
            default:
                throw new ControlException("bad_args", $"Unknown store action '{action}'. Actions: describe, connect, load, buy, finish, restore, script, resolve, revoke, price, disconnect, fail-connect, products.");
        }

        // The answers to a request are events; delivering them here lets a scenario step see the result of the step before it.
        service.Pump();
        return service.Describe();

        void ThrowIf(StoreError e)
        {
            if (e.IsError)
            {
                throw Failed(e);
            }
        }
    }

    private static FakeOutcome ParseOutcome(string? text) => text switch
    {
        "succeed" or "success" or null => FakeOutcome.Succeed,
        "cancel" => FakeOutcome.Cancel,
        "pend" or "pending" => FakeOutcome.Pend,
        "fail" or "failure" => FakeOutcome.Fail,
        _ => throw new ControlException("bad_args", $"outcome is succeed, cancel, pend or fail, got '{text}'."),
    };

    private static StoreError? ParseFailure(string? text)
    {
        if (text is null)
        {
            return null;
        }

        return Enum.TryParse<StoreErrorKind>(text.Replace("-", string.Empty, StringComparison.Ordinal), ignoreCase: true, out var kind) && kind != StoreErrorKind.None
            ? new StoreError(kind, "scripted " + kind)
            : throw new ControlException("bad_args", $"error is one of {string.Join(", ", Enum.GetNames<StoreErrorKind>().Where(n => n != nameof(StoreErrorKind.None)))}, got '{text}'.");
    }
}

/// <summary>One product as the store described it, for the <c>store</c> operation's <c>products</c> action.</summary>
/// <param name="Id">Product id.</param>
/// <param name="Kind">Consumable, NonConsumable or Subscription.</param>
/// <param name="Title">Display title.</param>
/// <param name="Price">The price as shown to the player.</param>
/// <param name="PriceMicros">The amount in millionths of <paramref name="Currency"/>.</param>
/// <param name="Currency">ISO 4217 code, or empty.</param>
public sealed record StoreProductInfo(string Id, string Kind, string Title, string Price, long PriceMicros, string Currency);

/// <summary>The products a purchase service has loaded, in id order.</summary>
/// <param name="Products">The products.</param>
public sealed record StoreProductsStatus(StoreProductInfo[] Products);
