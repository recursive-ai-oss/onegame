namespace OneGame.Purchases.Tests;

public class FakeStoreTests
{
    private static readonly ProductDefinition[] Catalog =
    [
        new("coins.100", ProductKind.Consumable),
        new("remove_ads", ProductKind.NonConsumable),
        new("pass.monthly", ProductKind.Subscription),
    ];

    private static (PurchaseService Service, FakeStore Store, List<StoreEvent> Events) Connected()
    {
        var store = FakeStore.For(Catalog);
        var service = new PurchaseService(store, Catalog);
        var events = new List<StoreEvent>();
        service.Event += events.Add;
        service.Connect();
        service.Pump();
        Assert.Equal(StoreConnection.Connected, service.Connection);
        Assert.False(service.LoadProducts().IsError);
        service.Pump();
        events.Clear();
        return (service, store, events);
    }

    private static PurchaseTransaction Single(List<StoreEvent> events, TransactionState state) =>
        Assert.Single(events, e => e.Kind == StoreEventKind.Transaction && e.Transaction!.State == state).Transaction!;

    [Fact]
    public void ConnectAndLoadReportProductsAndUnknownIds()
    {
        var store = FakeStore.For(Catalog.Take(2));
        using var service = new PurchaseService(store, Catalog);
        var events = new List<StoreEvent>();
        service.Event += events.Add;
        Assert.Equal(StoreConnection.Disconnected, service.Connection);
        Assert.True(service.LoadProducts().IsError); // not connected yet
        service.Connect();
        Assert.Equal(StoreConnection.Connecting, service.Connection);
        service.Pump();
        Assert.Equal(StoreConnection.Connected, service.Connection);
        service.LoadProducts();
        service.Pump();

        var loaded = Assert.Single(events, e => e.Kind == StoreEventKind.ProductsLoaded);
        Assert.Equal(["coins.100", "remove_ads"], loaded.Products.Select(p => p.Id));
        Assert.Equal(["pass.monthly"], loaded.UnknownProductIds);
        Assert.Equal(ProductKind.NonConsumable, service.Products.Single(p => p.Id == "remove_ads").Kind);
        Assert.Equal(990_000, service.Products[0].PriceMicros);
        Assert.Equal("$0.99", service.Products[0].Price);
    }

    [Fact]
    public void ProductsKeepTheirDefaultPricesUntilOneIsSet()
    {
        var (service, _, _) = Connected();
        // Products come back in id order: coins.100, pass.monthly, remove_ads.
        Assert.Equal([("$0.99", 990_000L, "USD"), ("$2.97", 2_970_000L, "USD"), ("$1.98", 1_980_000L, "USD")], service.Products.Select(p => (p.Price, p.PriceMicros, p.Currency)));
    }

    [Fact]
    public void ASetPriceReachesTheGameOnTheNextLoadInEachCurrency()
    {
        var (service, store, _) = Connected();
        Assert.True(store.SetPrice("coins.100", 4_990_000, "eur"));
        Assert.True(store.SetPrice("remove_ads", 1_200_000_000, "JPY"));
        Assert.True(store.SetPrice("pass.monthly", 3_000_000, "CHF", display: "3 francs"));
        Assert.False(store.SetPrice("nope", 1, "USD"));
        Assert.Equal("$0.99", service.Products.Single(p => p.Id == "coins.100").Price); // not reloaded yet
        service.LoadProducts();
        service.Pump();
        var coins = service.Products.Single(p => p.Id == "coins.100");
        Assert.Equal(("\u20ac4.99", 4_990_000L, "EUR"), (coins.Price, coins.PriceMicros, coins.Currency));
        var ads = service.Products.Single(p => p.Id == "remove_ads");
        Assert.Equal(("\u00a51,200", 1_200_000_000L, "JPY"), (ads.Price, ads.PriceMicros, ads.Currency));
        Assert.Equal("3 francs", service.Products.Single(p => p.Id == "pass.monthly").Price);
    }

    // A culture that swaps the separators, as German does; the format must ignore it.
    private static System.Globalization.CultureInfo SwappedSeparators()
    {
        var c = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.InvariantCulture.Clone();
        c.NumberFormat.NumberDecimalSeparator = ",";
        c.NumberFormat.NumberGroupSeparator = ".";
        return c;
    }

    [Theory]
    [InlineData(4_990_000L, "USD", "$4.99")]
    [InlineData(4_990_000L, "EUR", "\u20ac4.99")]
    [InlineData(1_234_567_890_000L, "USD", "$1,234,567.89")]
    [InlineData(1_500_000L, "JPY", "\u00a52")]
    [InlineData(1_499_999L, "JPY", "\u00a51")]
    [InlineData(999_999_999L, "JPY", "\u00a51,000")]
    [InlineData(1_234_000L, "KWD", "KWD 1.234")]
    [InlineData(4_500_000L, "CHF", "CHF 4.50")]
    [InlineData(0L, "USD", "$0.00")]
    [InlineData(5_000L, "USD", "$0.01")]
    [InlineData(4_999L, "USD", "$0.00")]
    public void PricesFormatTheSameWhateverTheCulture(long micros, string currency, string expected)
    {
        var before = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = SwappedSeparators();
            Assert.Equal(expected, FakePrice.Format(micros, currency));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = before;
        }
    }

    [Theory]
    [InlineData("4.99", true, 4_990_000L)]
    [InlineData("1200", true, 1_200_000_000L)]
    [InlineData("0.000001", true, 1L)]
    [InlineData("4,99", false, 0L)]
    [InlineData("-1", false, 0L)]
    [InlineData("4.", false, 0L)]
    [InlineData("1.0000001", false, 0L)]
    [InlineData("", false, 0L)]
    public void AmountsParseWithADotOnly(string text, bool ok, long micros)
    {
        Assert.Equal(ok, FakePrice.TryParseAmount(text, out var got));
        Assert.Equal(micros, got);
    }

    [Fact]
    public void ConnectFailureIsAValueAndTheNextAttemptCanSucceed()
    {
        var store = FakeStore.For(Catalog);
        using var service = new PurchaseService(store, Catalog);
        store.ConnectFailure = new StoreError(StoreErrorKind.Network, "offline");
        service.Connect();
        service.Pump();
        Assert.Equal(StoreConnection.Failed, service.Connection);
        Assert.Equal(StoreErrorKind.Network, service.ConnectError.Kind);
        Assert.Equal(StoreErrorKind.Unavailable, service.Buy("coins.100").Kind);

        service.Connect();
        service.Pump();
        Assert.Equal(StoreConnection.Connected, service.Connection);
        Assert.False(service.ConnectError.IsError);
    }

    [Fact]
    public void ABoughtNonConsumableIsOwnedFinishedAndRestoredLater()
    {
        var (service, store, events) = Connected();
        Assert.False(service.Buy("remove_ads").IsError);
        service.Pump();
        var bought = Single(events, TransactionState.Purchased);
        Assert.Equal("fake-1", bought.TransactionId);
        Assert.True(bought.NeedsFinish);
        Assert.True(service.IsOwned("remove_ads"));
        Assert.Single(service.Unfinished);

        events.Clear();
        Assert.False(service.Finish(bought).IsError);
        service.Pump();
        Assert.Equal(StoreEventKind.Finished, Assert.Single(events).Kind);
        Assert.Empty(service.Unfinished);

        // A second purchase is refused before the store is asked.
        Assert.Equal(StoreErrorKind.AlreadyOwned, service.Buy("remove_ads").Kind);

        // A fresh install: a new service over the same store gets the purchase back as restored.
        using var second = new PurchaseService(store, Catalog);
        var seen = new List<StoreEvent>();
        second.Event += seen.Add;
        second.Connect();
        second.Pump();
        second.LoadProducts();
        second.Pump();
        Assert.False(second.IsOwned("remove_ads"));
        seen.Clear();
        Assert.False(second.Restore().IsError);
        second.Pump();
        var restored = Single(seen, TransactionState.Restored);
        Assert.Equal("remove_ads", restored.ProductId);
        Assert.Equal(StoreEventKind.RestoreFinished, seen[^1].Kind);
        Assert.True(second.IsOwned("remove_ads"));
        service.Dispose();
    }

    [Fact]
    public void AConsumableIsNeverOwnedAndCannotBeBoughtAgainUntilFinished()
    {
        var (service, store, events) = Connected();
        service.Buy("coins.100");
        service.Pump();
        var first = Single(events, TransactionState.Purchased);
        Assert.False(service.IsOwned("coins.100"));

        events.Clear();
        service.Buy("coins.100");
        service.Pump();
        var refused = Single(events, TransactionState.Failed);
        Assert.Equal(StoreErrorKind.AlreadyOwned, refused.Error.Kind);

        events.Clear();
        service.Finish(first);
        service.Pump();
        service.Buy("coins.100");
        service.Pump();
        Assert.Equal("fake-2", Single(events, TransactionState.Purchased).TransactionId);
        Assert.Empty(store.Owned());
    }

    [Fact]
    public void AnUnfinishedConsumableComesBackOnRestore()
    {
        var (service, _, events) = Connected();
        service.Buy("coins.100");
        service.Pump();
        events.Clear();
        service.Restore();
        service.Pump();
        Assert.Equal("coins.100", Single(events, TransactionState.Restored).ProductId);
    }

    [Fact]
    public void CancelIsNotAnError()
    {
        var (service, store, events) = Connected();
        store.Script("remove_ads", FakeOutcome.Cancel);
        service.Buy("remove_ads");
        service.Pump();
        var t = Single(events, TransactionState.Cancelled);
        Assert.False(t.Error.IsError);
        Assert.False(service.IsOwned("remove_ads"));
        Assert.False(t.NeedsFinish);

        // The script is used up: the next purchase succeeds.
        events.Clear();
        service.Buy("remove_ads");
        service.Pump();
        Single(events, TransactionState.Purchased);
    }

    [Fact]
    public void PendingPurchasesSettleWhenApproved()
    {
        var (service, store, events) = Connected();
        store.Script("remove_ads", FakeOutcome.Pend);
        service.Buy("remove_ads");
        service.Pump();
        Single(events, TransactionState.Pending);
        Assert.True(service.IsPending("remove_ads"));
        Assert.False(service.IsOwned("remove_ads"));
        Assert.Empty(service.Unfinished);

        events.Clear();
        Assert.True(store.ResolvePending("remove_ads", approve: true));
        Assert.False(store.ResolvePending("remove_ads", approve: true));
        service.Pump();
        Single(events, TransactionState.Purchased);
        Assert.False(service.IsPending("remove_ads"));
        Assert.True(service.IsOwned("remove_ads"));
    }

    [Fact]
    public void PendingPurchasesFailWhenDeclinedAndCanBeTriedAgain()
    {
        var (service, store, events) = Connected();
        store.Script("remove_ads", FakeOutcome.Pend);
        service.Buy("remove_ads");
        service.Pump();
        events.Clear();
        store.ResolvePending("remove_ads", approve: false);
        service.Pump();
        var failed = Single(events, TransactionState.Failed);
        Assert.Equal(StoreErrorKind.NotAllowed, failed.Error.Kind);
        Assert.False(service.IsPending("remove_ads"));

        events.Clear();
        service.Buy("remove_ads");
        service.Pump();
        Single(events, TransactionState.Purchased);
    }

    [Theory]
    [InlineData(StoreErrorKind.Network)]
    [InlineData(StoreErrorKind.Timeout)]
    [InlineData(StoreErrorKind.NotAllowed)]
    [InlineData(StoreErrorKind.Failed)]
    public void ScriptedFailuresCarryTheirError(StoreErrorKind kind)
    {
        var (service, store, events) = Connected();
        store.Script("coins.100", FakeOutcome.Fail, new StoreError(kind, "scripted " + kind));
        service.Buy("coins.100");
        service.Pump();
        var t = Single(events, TransactionState.Failed);
        Assert.Equal(kind, t.Error.Kind);
        Assert.Equal("scripted " + kind, t.Error.Message);
        Assert.Empty(service.Unfinished);
    }

    [Fact]
    public void ARefundRevokesWhatWasOwned()
    {
        var (service, store, events) = Connected();
        service.Buy("remove_ads");
        service.Pump();
        events.Clear();

        Assert.True(store.Revoke("remove_ads"));
        Assert.False(store.Revoke("remove_ads"));
        service.Pump();
        Single(events, TransactionState.Revoked);
        Assert.False(service.IsOwned("remove_ads"));
        Assert.Empty(service.Unfinished);
        Assert.Empty(store.Owned());

        // Bought again after the refund.
        events.Clear();
        Assert.False(service.Buy("remove_ads").IsError);
        service.Pump();
        Single(events, TransactionState.Purchased);
    }

    [Fact]
    public void ARestoreThatNoLongerListsAPurchaseRevokesIt()
    {
        // The real store refunded without the game hearing; the fake is told only after the game stopped listening.
        var (service, store, events) = Connected();
        service.Buy("pass.monthly");
        service.Pump();
        Assert.True(service.IsOwned("pass.monthly"));
        store.Revoke("pass.monthly");
        while (store.TryTake(out _))
        {
            // The revocation event is lost on purpose.
        }

        events.Clear();
        service.Restore();
        service.Pump();
        var revoked = Single(events, TransactionState.Revoked);
        Assert.Equal("pass.monthly", revoked.ProductId);
        Assert.False(service.IsOwned("pass.monthly"));
    }

    [Fact]
    public void ArgumentErrorsComeBackAsValuesWithoutTouchingTheStore()
    {
        var (service, _, events) = Connected();
        Assert.Equal(StoreErrorKind.ProductNotFound, service.Buy("nope").Kind);
        Assert.Equal(StoreErrorKind.ProductNotFound, service.Buy(null!).Kind);
        Assert.Equal(StoreErrorKind.InvalidRequest, service.Finish(new PurchaseTransaction("x", "coins.100", TransactionState.Cancelled, 0, 0, "", "", "", default)).Kind);
        service.Pump();
        Assert.Empty(events);
        Assert.Throws<ArgumentException>(() => new PurchaseService(FakeStore.For([]), [new("a", ProductKind.Consumable), new("a", ProductKind.Consumable)]));
        Assert.Throws<ArgumentException>(() => new PurchaseService(FakeStore.For([]), [new("", ProductKind.Consumable)]));
    }

    [Fact]
    public void BuyBeforeProductsAreLoadedIsRefusedWithAReason()
    {
        var store = FakeStore.For(Catalog);
        using var service = new PurchaseService(store, Catalog);
        service.Connect();
        service.Pump();
        var error = service.Buy("coins.100");
        Assert.Equal(StoreErrorKind.ProductNotFound, error.Kind);
        Assert.Contains("LoadProducts", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ADroppedConnectionIsReportedAndPurchasesAreRefused()
    {
        var (service, store, events) = Connected();
        store.Disconnect();
        service.Pump();
        Assert.Equal(StoreEventKind.Disconnected, Assert.Single(events).Kind);
        Assert.Equal(StoreConnection.Disconnected, service.Connection);
        Assert.Equal(StoreErrorKind.Unavailable, service.Buy("coins.100").Kind);
    }

    [Fact]
    public void TwoRunsOfTheSameScriptProduceTheSameEvents()
    {
        static string Run()
        {
            var (service, store, events) = Connected();
            store.Script("remove_ads", FakeOutcome.Cancel);
            store.Script("remove_ads", FakeOutcome.Pend);
            service.Buy("remove_ads");
            service.Pump();
            service.Buy("remove_ads");
            service.Pump();
            store.ResolvePending("remove_ads", true);
            service.Buy("coins.100");
            service.Pump();
            return string.Join('|', events.Select(e => e.Kind + ":" + e.Transaction?.TransactionId + ":" + e.Transaction?.State));
        }

        Assert.Equal(Run(), Run());
    }

    [Fact]
    public void UnavailableStoreFailsEveryCallWithItsReason()
    {
        var catalog = new[] { new ProductDefinition("a", ProductKind.Consumable) };
        using var service = new PurchaseService(new UnavailableStore("no store here"), catalog);
        service.Connect();
        service.Pump();
        Assert.Equal(StoreConnection.Failed, service.Connection);
        Assert.Equal("no store here", service.ConnectError.Message);
        Assert.Equal(StoreErrorKind.Unavailable, service.Buy("a").Kind);
        Assert.Equal(StoreErrorKind.Unavailable, service.Restore().Kind);
    }

    [Fact]
    public void EventsFromOtherThreadsAreDeliveredOnlyByPump()
    {
        var buffer = new StoreEventBuffer();
        var threads = Enumerable.Range(0, 8).Select(i => new Thread(() =>
        {
            for (var n = 0; n < 200; n++)
            {
                buffer.Post(new StoreEvent(StoreEventKind.Finished));
            }
        })).ToList();
        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());
        var count = 0;
        while (buffer.TryTake(out _))
        {
            count++;
        }

        Assert.Equal(1600, count);
    }

    [Fact]
    public void ABufferThatNobodyDrainsStaysBounded()
    {
        var buffer = new StoreEventBuffer();
        for (var i = 0; i < 10_000; i++)
        {
            buffer.Post(new StoreEvent(StoreEventKind.Finished));
        }

        var count = 0;
        while (buffer.TryTake(out _))
        {
            count++;
        }

        Assert.Equal(4096, count);
    }

    [Fact]
    public void DescribeListsTheState()
    {
        var (service, _, _) = Connected();
        service.Buy("remove_ads");
        service.Pump();
        var d = service.Describe();
        Assert.Equal("fake", d.Store);
        Assert.Equal("connected", d.Connection);
        Assert.Equal(["remove_ads"], d.Owned);
        Assert.Equal(["fake-1:remove_ads:purchased"], d.Unfinished);
        Assert.Contains(d.Recent, r => r.StartsWith("transaction remove_ads purchased", StringComparison.Ordinal));
        service.Dispose();
    }
}
