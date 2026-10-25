using System.Text.Json;

namespace OneGame.Purchases.Tests;

/// <summary>Builders for Play bridge messages and purchase JSON, shaped like the real ones, shared by the tests and the fuzz seeds.</summary>
public static class PlayMessages
{
    /// <summary>A JSON string literal.</summary>
    public static string Json(string s) => JsonSerializer.Serialize(s);

    /// <summary>Play's purchase JSON.</summary>
    public static string PurchaseJson(string product = "remove_ads", string token = "tok-1", string order = "GPA.1111-2222", int quantity = 1) =>
        $$"""{"orderId":"{{order}}","packageName":"dev.game","productId":"{{product}}","purchaseTime":1700000000123,"purchaseState":0,"purchaseToken":"{{token}}","quantity":{{quantity}},"acknowledged":false}""";

    /// <summary>One purchase item of a bridge message.</summary>
    public static string Item(string purchaseJson, int state = 1) => $$"""{"json":{{Json(purchaseJson)}},"sig":"c2lnbmF0dXJl","state":{{state}}}""";
}
