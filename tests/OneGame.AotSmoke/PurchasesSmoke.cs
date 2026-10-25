using OneGame.Purchases;

namespace OneGame.AotSmoke;

/// <summary>The purchase service over the fake store: connect, load, buy, cancel, refund and restore, plus the platform choice (which on this host is the "no store" backend, so the native code paths are only linked).</summary>
internal static class PurchasesSmoke
{
    public static string? Run()
    {
        ProductDefinition[] catalog = [new("coins", ProductKind.Consumable), new("unlock", ProductKind.NonConsumable)];
        var fake = FakeStore.For(catalog);
        using var service = new PurchaseService(fake, catalog);
        service.Connect();
        service.Pump();
        service.LoadProducts();
        service.Pump();
        if (service.Products.Count != 2)
        {
            return "the fake store did not describe both products";
        }

        fake.Script("coins", FakeOutcome.Cancel);
        service.Buy("coins");
        service.Buy("unlock");
        service.Pump();
        if (!service.IsOwned("unlock") || service.Unfinished.Count != 1)
        {
            return "the unlock was not owned after a purchase";
        }

        service.Finish(service.Unfinished[0]);
        fake.Revoke("unlock");
        service.Pump();
        if (service.IsOwned("unlock"))
        {
            return "a refund did not take the unlock away";
        }

        using var native = new PurchaseService(PurchaseBackends.CreateNative(), catalog);
        native.Connect();
        native.Pump();
        return native.Connection == StoreConnection.Failed && native.ConnectError.Kind == StoreErrorKind.Unavailable ? null : "the platform without a store did not report it";
    }
}
