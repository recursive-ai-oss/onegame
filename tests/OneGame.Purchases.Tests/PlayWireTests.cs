using System.Text;

namespace OneGame.Purchases.Tests;

public class PlayWireTests
{
    private static List<StoreEvent>? Parse(string message)
    {
        var events = new List<StoreEvent>();
        return PlayWire.TryParse(Encoding.UTF8.GetBytes(message), events) ? events : null;
    }

    [Fact]
    public void ConnectedMapsTheResponseCode()
    {
        Assert.False(Assert.Single(Parse("""{"e":"connected","code":0,"msg":""}""")!).Error.IsError);
        var unavailable = Assert.Single(Parse("""{"e":"connected","code":3,"msg":"Billing is unavailable"}""")!);
        Assert.Equal(StoreErrorKind.Unavailable, unavailable.Error.Kind);
        Assert.Equal("Billing is unavailable", unavailable.Error.Message);
    }

    [Theory]
    [InlineData(-3, StoreErrorKind.Timeout)]
    [InlineData(-2, StoreErrorKind.NotAllowed)]
    [InlineData(-1, StoreErrorKind.Unavailable)]
    [InlineData(2, StoreErrorKind.Unavailable)]
    [InlineData(4, StoreErrorKind.ProductNotFound)]
    [InlineData(5, StoreErrorKind.InvalidRequest)]
    [InlineData(6, StoreErrorKind.Failed)]
    [InlineData(7, StoreErrorKind.AlreadyOwned)]
    [InlineData(12, StoreErrorKind.Network)]
    [InlineData(99, StoreErrorKind.Failed)]
    public void ResponseCodesMapToErrorKinds(int code, StoreErrorKind kind) => Assert.Equal(kind, PlayWire.MapResponse(code, "m").Kind);

    [Fact]
    public void ProductsCarryPricesAndUnknownIds()
    {
        var e = Assert.Single(Parse("""{"e":"products","code":0,"msg":"","items":[{"id":"coins.100","title":"100 coins (Game)","desc":"A pile","price":"$0.99","micros":990000,"cur":"USD"}],"unknown":["gone"]}""")!);
        var p = Assert.Single(e.Products);
        Assert.Equal(("coins.100", "100 coins (Game)", "$0.99", 990000L, "USD"), (p.Id, p.Title, p.Price, p.PriceMicros, p.Currency));
        Assert.Equal(["gone"], e.UnknownProductIds);
    }

    [Fact]
    public void APurchaseUpdateBecomesATransactionWithItsProof()
    {
        var e = Assert.Single(Parse($$"""{"e":"purchases","code":0,"msg":"","product":"remove_ads","items":[{{PlayMessages.Item(PlayMessages.PurchaseJson())}}]}""")!);
        var t = e.Transaction!;
        Assert.Equal((TransactionState.Purchased, "GPA.1111-2222", "remove_ads", "tok-1", 1700000000123L, 1), (t.State, t.TransactionId, t.ProductId, t.ServerProof, t.PurchaseTimeUnixMs, t.Quantity));
        Assert.Equal("c2lnbmF0dXJl", t.Signature);
        Assert.Contains("\"purchaseToken\":\"tok-1\"", t.SignedData, StringComparison.Ordinal);
    }

    [Fact]
    public void PendingPurchasesAreNotGrantable()
    {
        var t = Assert.Single(Parse($$"""{"e":"purchases","code":0,"msg":"","items":[{{PlayMessages.Item(PlayMessages.PurchaseJson(order: ""), state: 2)}}]}""")!).Transaction!;
        Assert.Equal(TransactionState.Pending, t.State);
        Assert.False(t.NeedsFinish);
        Assert.StartsWith("token:", t.TransactionId, StringComparison.Ordinal); // a pending purchase has no order id yet
    }

    [Fact]
    public void ACancelledFlowIsCancelledAndAFailedOneNamesTheProduct()
    {
        var cancel = Assert.Single(Parse("""{"e":"purchases","code":1,"msg":"user cancelled","product":"remove_ads"}""")!).Transaction!;
        Assert.Equal((TransactionState.Cancelled, "remove_ads", false), (cancel.State, cancel.ProductId, cancel.Error.IsError));

        var failed = Assert.Single(Parse("""{"e":"purchases","code":7,"msg":"owned","product":"remove_ads"}""")!).Transaction!;
        Assert.Equal((TransactionState.Failed, StoreErrorKind.AlreadyOwned), (failed.State, failed.Error.Kind));
    }

    [Fact]
    public void RestoreListsRestoredTransactionsThenFinishes()
    {
        var events = Parse($$"""{"e":"restore","code":0,"msg":"","items":[{{PlayMessages.Item(PlayMessages.PurchaseJson("a", "t1", "o1"))}},{{PlayMessages.Item(PlayMessages.PurchaseJson("b", "t2", "o2"), 0)}}]}""")!;
        Assert.Equal([StoreEventKind.Transaction, StoreEventKind.RestoreFinished], events.Select(e => e.Kind));
        Assert.Equal(TransactionState.Restored, events[0].Transaction!.State); // the unspecified one is skipped
        var failed = Assert.Single(Parse("""{"e":"restore","code":12,"msg":"offline"}""")!);
        Assert.Equal((StoreEventKind.RestoreFinished, StoreErrorKind.Network), (failed.Kind, failed.Error.Kind));
    }

    [Fact]
    public void FinishedCarriesTheToken()
    {
        var e = Assert.Single(Parse("""{"e":"finished","code":0,"msg":"","token":"tok-1"}""")!);
        Assert.Equal((StoreEventKind.Finished, "tok-1"), (e.Kind, e.Transaction!.ServerProof));
    }

    [Fact]
    public void UnknownFieldsAreIgnoredButUnknownKindsAndDamageAreRefused()
    {
        Assert.NotNull(Parse("""{"e":"disconnected","extra":{"a":[1,2,{"b":null}]}}"""));
        Assert.Null(Parse("""{"e":"mystery"}"""));
        Assert.Null(Parse("""{"code":0}"""));
        Assert.Null(Parse("""[1,2,3]"""));
        Assert.Null(Parse("""{"e":"connected","code":"zero"}"""));
        Assert.Null(Parse("""{"e":"connected","code":0"""));
        Assert.Null(Parse(""));
        Assert.Null(Parse("not json"));
        Assert.Null(Parse("""{"e":"products","items":[1]}"""));
        // A purchase that cannot be read is refused whole, not half-delivered.
        Assert.Null(Parse($$"""{"e":"restore","code":0,"items":[{{PlayMessages.Item(PlayMessages.PurchaseJson("a", "t1", "o1"))}},{{PlayMessages.Item("{\"productId\":\"x\"}")}}]}"""));
    }

    [Fact]
    public void LimitsAreEnforcedBeforeAnythingIsKept()
    {
        Assert.Null(Parse("""{"e":"products","items":[{"id":"%s"}]}""".Replace("%s", new string('x', StoreLimits.MaxTextChars + 1), StringComparison.Ordinal)));
        Assert.NotNull(Parse("""{"e":"products","items":[{"id":"%s"}]}""".Replace("%s", new string('x', StoreLimits.MaxTextChars), StringComparison.Ordinal)));

        var many = string.Join(',', Enumerable.Range(0, StoreLimits.MaxItems + 1).Select(i => $$"""{"id":"p{{i}}"}"""));
        Assert.Null(Parse($$"""{"e":"products","items":[{{many}}]}"""));

        var deep = new string('[', 200) + new string(']', 200);
        Assert.Null(Parse($$"""{"e":"products","x":{{deep}}}"""));

        var big = new byte[StoreLimits.MaxMessageBytes + 1];
        Array.Fill(big, (byte)' ');
        Assert.False(PlayWire.TryParse(big, []));

        Assert.Null(Parse($$"""{"e":"restore","items":[{{PlayMessages.Item(PlayMessages.PurchaseJson(quantity: 0))}}]}""")); // quantity out of range
        Assert.Null(Parse($$"""{"e":"restore","items":[{{PlayMessages.Item(PlayMessages.PurchaseJson(token: new string('t', StoreLimits.MaxProofChars + 1)))}}]}"""));
    }

    [Fact]
    public void PurchaseJsonReadsNewAndOldProductFields()
    {
        Assert.True(PlayWire.TryParsePurchase(Encoding.UTF8.GetBytes("""{"orderId":"o","productIds":["first","second"],"purchaseToken":"t","purchaseTime":5}"""), out var p));
        Assert.Equal(("first", "t", 5L, 1), (p.ProductId, p.Token, p.TimeMs, p.Quantity));
        Assert.False(PlayWire.TryParsePurchase("""{"productId":"x"}"""u8, out _)); // no token
        Assert.False(PlayWire.TryParsePurchase("""{"purchaseToken":"x"}"""u8, out _)); // no product
        Assert.False(PlayWire.TryParsePurchase("""{"productId":"x","purchaseToken":"t","purchaseTime":-1}"""u8, out _));
    }

    // A backend that replays bridge messages, so the whole path from Play's text to the game's state runs.
    private sealed class WireBackend : IPurchaseBackend
    {
        private readonly StoreEventBuffer _buffer = new();

        public string Name => "wire";

        public bool RestoreIsComplete => true;

        public void Feed(string message)
        {
            var events = new List<StoreEvent>();
            Assert.True(PlayWire.TryParse(Encoding.UTF8.GetBytes(message), events));
            events.ForEach(_buffer.Post);
        }

        public void Connect() => Feed("""{"e":"connected","code":0,"msg":""}""");

        public void LoadProducts(IReadOnlyList<ProductDefinition> products) => Feed("""{"e":"products","code":0,"items":[{"id":"remove_ads","title":"t","price":"$1","micros":1000000,"cur":"USD"}]}""");

        public void Buy(ProductDefinition product)
        {
        }

        public void Finish(PurchaseTransaction transaction, ProductKind kind) => Feed($$"""{"e":"finished","code":0,"token":"{{transaction.ServerProof}}"}""");

        public void Restore() => Feed("""{"e":"restore","code":0,"items":[]}""");

        public bool TryTake(out StoreEvent storeEvent) => _buffer.TryTake(out storeEvent);

        public void Dispose()
        {
        }
    }

    [Fact]
    public void ARefundShowsAsAnOwnedProductMissingFromTheNextRestore()
    {
        var backend = new WireBackend();
        using var service = new PurchaseService(backend, [new("remove_ads", ProductKind.NonConsumable)]);
        service.Connect();
        service.Pump();
        service.LoadProducts();
        service.Pump();
        backend.Feed($$"""{"e":"purchases","code":0,"items":[{{PlayMessages.Item(PlayMessages.PurchaseJson())}}]}""");
        service.Pump();
        Assert.True(service.IsOwned("remove_ads"));

        var revoked = new List<PurchaseTransaction>();
        service.Event += e =>
        {
            if (e.Transaction is { State: TransactionState.Revoked } t)
            {
                revoked.Add(t);
            }
        };
        service.Restore(); // the listing is empty: Play no longer has the purchase
        service.Pump();
        Assert.Equal("GPA.1111-2222", Assert.Single(revoked).TransactionId);
        Assert.False(service.IsOwned("remove_ads"));
    }

    [Fact]
    public void MutatedMessagesNeverThrow()
    {
        var rng = new Random(7);
        var seeds = PurchasesFuzzTargets.WireSeeds().Concat(PurchasesFuzzTargets.PurchaseSeeds()).ToList();
        for (var i = 0; i < 20_000; i++)
        {
            var bytes = (byte[])seeds[rng.Next(seeds.Count)].Clone();
            for (var m = rng.Next(1, 6); m > 0 && bytes.Length > 0; m--)
            {
                switch (rng.Next(3))
                {
                    case 0:
                        bytes[rng.Next(bytes.Length)] = (byte)rng.Next(256);
                        break;
                    case 1:
                        Array.Resize(ref bytes, rng.Next(bytes.Length + 1));
                        break;
                    default:
                        bytes[rng.Next(bytes.Length)] ^= (byte)(1 << rng.Next(8));
                        break;
                }
            }

            _ = PlayWire.TryParse(bytes, []);
            _ = PlayWire.TryParsePurchase(bytes, out _);
        }
    }
}
