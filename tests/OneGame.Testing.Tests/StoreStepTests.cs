using System.Text.Json;
using OneGame.Control;
using OneGame.Host;
using OneGame.Purchases;

namespace OneGame.Testing.Tests;

/// <summary>The scenario steps and expectations for in-app purchases, run over the fake store through the control layer.</summary>
public class StoreStepTests
{
    private static readonly ProductDefinition[] Catalog = [new("coins", ProductKind.Consumable), new("unlock", ProductKind.NonConsumable)];

    private static (TestCluster Cluster, PurchaseService Service) Start()
    {
        var cluster = TestCluster.Start(new ShopGame(), new TestOptions { Width = 320, Height = 200, Gpu = GpuMode.Off });
        var service = new PurchaseService(FakeStore.For(Catalog), Catalog);
        cluster.Client.Control.Purchases = service;
        return (cluster, service);
    }

    [Fact]
    public void AScenarioBuysCancelsAndRestoresAndChecksTheService()
    {
        var (cluster, service) = Start();
        using (cluster)
        using (service)
        {
            var scenario = ScenarioParser.Parse("""
                steps:
                  - store: connect
                  - store: load
                  - expect: {store: {connection: connected, products: "2"}}
                  - store: {action: buy, product: unlock}
                  - expect: {store: {owned: unlock, unfinished: "1", recent: "transaction unlock purchased"}}
                  - store: {action: finish, product: unlock}
                  - expect: {store: {unfinished: "0"}}
                  - store: {action: script, product: coins, outcome: cancel}
                  - store: {action: buy, product: coins}
                  - expect: {store: {not-owned: coins, recent: "transaction coins cancelled"}}
                  - store: {action: script, product: coins, outcome: fail, error: network}
                  - store: {action: buy, product: coins}
                  - expect: {store: {recent: "transaction coins failed error Network"}}
                  - store: {action: revoke, product: unlock}
                  - expect: {store: {not-owned: unlock}}
                  - store: restore
                  - expect: {store: {recent: "restorefinished", not-owned: unlock}}
                """, "inline.yaml", "store");
            var result = cluster.Run(scenario);
            Assert.True(result.Passed, JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult));
        }
    }

    [Fact]
    public void AFailedStoreCheckNamesWhatItSawAndABadStepStopsTheScenario()
    {
        var (cluster, service) = Start();
        using (cluster)
        using (service)
        {
            var wrong = cluster.Run(ScenarioParser.Parse("steps:\n  - store: connect\n  - expect: {store: {owned: unlock}}\n", "inline.yaml", "wrong"));
            Assert.False(wrong.Passed);
            Assert.Contains("store.owned is", wrong.Expectations.Single(e => !e.Passed).Message, StringComparison.Ordinal);

            var bad = cluster.Run(ScenarioParser.Parse("steps:\n  - store: {action: buy, product: nope}\n", "inline.yaml", "bad"));
            Assert.False(bad.Passed);
            Assert.Contains("store_productnotfound", bad.Steps.Single(s => !s.Ok).Error, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ScriptingARealStoreIsRejectedAndNoServiceIsReported()
    {
        using var cluster = TestCluster.Start(new ShopGame(), new TestOptions { Width = 320, Height = 200, Gpu = GpuMode.Off });
        using var service = new PurchaseService(new UnavailableStore("test"), Catalog);
        cluster.Client.Control.Purchases = service;
        var ex = Assert.Throws<ControlException>(() => cluster.Client.Call("store", """{"action":"script","product":"coins","outcome":"cancel"}"""));
        Assert.Equal("rejected", ex.Code);
        Assert.Throws<ScenarioException>(() => ScenarioParser.Parse("steps:\n  - store: {action: buy, bogus: 1}\n", "inline.yaml", "x"));
    }
}
