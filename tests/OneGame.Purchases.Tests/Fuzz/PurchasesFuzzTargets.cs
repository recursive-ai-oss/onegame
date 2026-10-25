using System.Text;

namespace OneGame.Purchases.Tests;

/// <summary>One fuzz target: a function of bytes that returns normally for any input (true when the input was accepted).</summary>
public sealed class PurchasesFuzzTarget(string name, Func<byte[], bool> run, Func<IEnumerable<byte[]>> seeds)
{
    /// <summary>The target's name; also the folder of its committed inputs.</summary>
    public string Name { get; } = name;

    /// <summary>Runs the target. Anything thrown is a finding.</summary>
    public bool Run(byte[] input) => run(input);

    /// <summary>Valid inputs to mutate.</summary>
    public IEnumerable<byte[]> Seeds() => seeds();
}

/// <summary>The purchase bridge's untrusted-text parsers as functions of bytes: the Play bridge messages and Play's purchase JSON.</summary>
public static class PurchasesFuzzTargets
{
    /// <summary>Every target.</summary>
    public static IReadOnlyList<PurchasesFuzzTarget> All { get; } =
    [
        new PurchasesFuzzTarget("purchases-wire", b => PlayWire.TryParse(b, []), WireSeeds),
        new PurchasesFuzzTarget("purchases-json", b => PlayWire.TryParsePurchase(b, out _), PurchaseSeeds),
    ];

    /// <summary>Bridge messages of every kind, as the Java side produces them.</summary>
    public static IEnumerable<byte[]> WireSeeds()
    {
        var one = PlayMessages.Item(PlayMessages.PurchaseJson());
        var two = PlayMessages.Item(PlayMessages.PurchaseJson("coins.100", "tok-2", "GPA.2", 3), 2);
        string[] messages =
        [
            """{"e":"connected","code":0,"msg":""}""",
            """{"e":"connected","code":3,"msg":"Billing API version is not supported for the type requested"}""",
            """{"e":"disconnected"}""",
            """{"e":"products","code":0,"msg":"","product":"","items":[{"id":"coins.100","title":"100 coins (Game)","desc":"A pile of coins","price":"$0.99","micros":990000,"cur":"USD"},{"id":"remove_ads","title":"No ads","desc":"","price":"4,99 €","micros":4990000,"cur":"EUR"}],"unknown":["old.product"]}""",
            $$"""{"e":"purchases","code":0,"msg":"","product":"remove_ads","items":[{{one}},{{two}}]}""",
            """{"e":"purchases","code":1,"msg":"User pressed back","product":"remove_ads"}""",
            $$"""{"e":"restore","code":0,"msg":"","product":"","items":[{{one}}]}""",
            """{"e":"finished","code":0,"msg":"","product":"","token":"tok-1"}""",
        ];
        return messages.Select(Encoding.UTF8.GetBytes);
    }

    /// <summary>Play purchase JSON in its old and new shapes.</summary>
    public static IEnumerable<byte[]> PurchaseSeeds() =>
    [
        Encoding.UTF8.GetBytes(PlayMessages.PurchaseJson()),
        Encoding.UTF8.GetBytes("""{"orderId":"GPA.3300","packageName":"dev.game","productIds":["a","b"],"purchaseTime":1700000000000,"purchaseState":0,"purchaseToken":"opaque-token","quantity":2,"acknowledged":true,"autoRenewing":true,"obfuscatedAccountId":"x"}"""),
    ];
}
