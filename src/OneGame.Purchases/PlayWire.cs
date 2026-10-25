using System.Globalization;
using System.Text.Json;

namespace OneGame.Purchases;

/// <summary>A Google Play purchase as read from its JSON (the <c>originalJson</c> Play hands out).</summary>
internal sealed record PlayPurchase(string OrderId, string ProductId, string Token, long TimeMs, int Quantity);

/// <summary>
/// Reads what the Play Billing bridge reports. The bridge is our own Java class, but the text it forwards (product titles, purchase
/// JSON, tokens) comes from the store, so every length and count is checked before anything is allocated, nesting is capped, and a
/// message that breaks a rule is refused whole. Nothing here throws on any input.
/// </summary>
internal static class PlayWire
{
    private const int MaxIdChars = StoreLimits.MaxTextChars;

    // BillingClient.BillingResponseCode values (Play Billing Library reference).
    internal const int ResponseOk = 0;
    internal const int ResponseUserCanceled = 1;

    private sealed class Item
    {
        public string Id = string.Empty;
        public string Title = string.Empty;
        public string Description = string.Empty;
        public string Price = string.Empty;
        public string Currency = string.Empty;
        public long Micros;
        public string Json = string.Empty;
        public string Signature = string.Empty;
        public int State;
    }

    private sealed class Message
    {
        public string Kind = string.Empty;
        public int Code;
        public string Text = string.Empty;
        public string Product = string.Empty;
        public string Token = string.Empty;
        public readonly List<Item> Items = [];
        public readonly List<string> Unknown = [];
    }

    /// <summary>Maps a <c>BillingResponseCode</c> and its debug message to a store error. <see cref="ResponseOk"/> maps to none.</summary>
    internal static StoreError MapResponse(int code, string message)
    {
        var text = Clip(message);
        return code switch
        {
            0 => default,
            -3 => new StoreError(StoreErrorKind.Timeout, text), // SERVICE_TIMEOUT
            -2 => new StoreError(StoreErrorKind.NotAllowed, text), // FEATURE_NOT_SUPPORTED
            -1 => new StoreError(StoreErrorKind.Unavailable, text), // SERVICE_DISCONNECTED
            2 => new StoreError(StoreErrorKind.Unavailable, text), // SERVICE_UNAVAILABLE
            3 => new StoreError(StoreErrorKind.Unavailable, text), // BILLING_UNAVAILABLE
            4 => new StoreError(StoreErrorKind.ProductNotFound, text), // ITEM_UNAVAILABLE
            5 => new StoreError(StoreErrorKind.InvalidRequest, text), // DEVELOPER_ERROR
            7 => new StoreError(StoreErrorKind.AlreadyOwned, text), // ITEM_ALREADY_OWNED
            8 => new StoreError(StoreErrorKind.InvalidRequest, text), // ITEM_NOT_OWNED
            12 => new StoreError(StoreErrorKind.Network, text), // NETWORK_ERROR
            _ => new StoreError(StoreErrorKind.Failed, text), // ERROR and anything newer
        };
    }

    /// <summary>Reads one bridge message and appends the events it stands for. Returns false, adding nothing, when the message is malformed or over a limit.</summary>
    internal static bool TryParse(ReadOnlySpan<byte> utf8, List<StoreEvent> events)
    {
        if (utf8.Length == 0 || utf8.Length > StoreLimits.MaxMessageBytes || !TryRead(utf8, out var m))
        {
            return false;
        }

        switch (m.Kind)
        {
            case "connected":
                events.Add(new StoreEvent(StoreEventKind.Connected) { Error = MapResponse(m.Code, m.Text) });
                return true;
            case "disconnected":
                events.Add(new StoreEvent(StoreEventKind.Disconnected));
                return true;
            case "products":
                var products = new List<StoreProduct>(m.Items.Count);
                foreach (var i in m.Items)
                {
                    // The kind placeholder is replaced by the service with the game's declaration.
                    products.Add(new StoreProduct(i.Id, ProductKind.Consumable, i.Title, i.Description, i.Price, i.Micros, i.Currency));
                }

                events.Add(new StoreEvent(StoreEventKind.ProductsLoaded) { Products = products, UnknownProductIds = m.Unknown, Error = MapResponse(m.Code, m.Text) });
                return true;
            case "purchases":
            case "restore":
                return AddPurchases(m, events);
            case "finished":
                var tx = new PurchaseTransaction(m.Product, m.Product, TransactionState.Purchased, 1, 0, m.Token, string.Empty, string.Empty, default);
                events.Add(new StoreEvent(StoreEventKind.Finished) { Transaction = tx, Error = MapResponse(m.Code, m.Text) });
                return true;
            default:
                return false;
        }
    }

    private static bool AddPurchases(Message m, List<StoreEvent> events)
    {
        var restore = m.Kind == "restore";
        var error = MapResponse(m.Code, m.Text);
        var staged = new List<StoreEvent>();
        if (error.IsError)
        {
            if (restore)
            {
                staged.Add(new StoreEvent(StoreEventKind.RestoreFinished) { Error = error });
            }
            else
            {
                // A cancelled flow is not a failure; every other code is, and names the product the flow was started for.
                var state = m.Code == ResponseUserCanceled ? TransactionState.Cancelled : TransactionState.Failed;
                staged.Add(new StoreEvent(StoreEventKind.Transaction)
                {
                    Transaction = new PurchaseTransaction(string.Empty, m.Product, state, 0, 0, string.Empty, string.Empty, string.Empty, state == TransactionState.Cancelled ? default : error),
                });
            }

            events.AddRange(staged);
            return true;
        }

        foreach (var item in m.Items)
        {
            // Purchase.PurchaseState: 1 purchased, 2 pending, 0 unspecified (ignored).
            if (item.State is not (1 or 2) || !TryParsePurchase(System.Text.Encoding.UTF8.GetBytes(item.Json), out var p))
            {
                if (item.State is 1 or 2)
                {
                    return false;
                }

                continue;
            }

            var state = item.State == 2 ? TransactionState.Pending : restore ? TransactionState.Restored : TransactionState.Purchased;
            var id = p.OrderId.Length > 0 ? p.OrderId : "token:" + p.Token[..Math.Min(p.Token.Length, 24)];
            staged.Add(new StoreEvent(StoreEventKind.Transaction)
            {
                Transaction = new PurchaseTransaction(id, p.ProductId, state, p.Quantity, p.TimeMs, p.Token, item.Json, item.Signature, default),
            });
        }

        if (restore)
        {
            staged.Add(new StoreEvent(StoreEventKind.RestoreFinished));
        }

        events.AddRange(staged);
        return true;
    }

    /// <summary>Reads Play's purchase JSON: order id, product id (or the first of <c>productIds</c>), purchase token, time and quantity. The token and a product id are required.</summary>
    internal static bool TryParsePurchase(ReadOnlySpan<byte> json, out PlayPurchase purchase)
    {
        purchase = default!;
        if (json.Length == 0 || json.Length > StoreLimits.MaxProofChars * 2)
        {
            return false;
        }

        var reader = new Utf8JsonReader(json, Options);
        string orderId = string.Empty, productId = string.Empty, token = string.Empty;
        long time = 0;
        var quantity = 1;
        try
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                return false;
            }

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                var name = reader.GetString();
                if (!reader.Read())
                {
                    return false;
                }

                switch (name)
                {
                    case "orderId" when reader.TokenType == JsonTokenType.String:
                        if (!ReadString(ref reader, MaxIdChars, out orderId))
                        {
                            return false;
                        }

                        break;
                    case "productId" when reader.TokenType == JsonTokenType.String:
                        if (!ReadString(ref reader, MaxIdChars, out productId))
                        {
                            return false;
                        }

                        break;
                    case "productIds" when reader.TokenType == JsonTokenType.StartArray:
                        var first = true;
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            if (first && reader.TokenType == JsonTokenType.String && productId.Length == 0)
                            {
                                if (!ReadString(ref reader, MaxIdChars, out productId))
                                {
                                    return false;
                                }
                            }
                            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                            {
                                reader.Skip();
                            }

                            first = false;
                        }

                        break;
                    case "purchaseToken" when reader.TokenType == JsonTokenType.String:
                        if (!ReadString(ref reader, StoreLimits.MaxProofChars, out token))
                        {
                            return false;
                        }

                        break;
                    case "purchaseTime" when reader.TokenType == JsonTokenType.Number:
                        if (!reader.TryGetInt64(out time) || time < 0)
                        {
                            return false;
                        }

                        break;
                    case "quantity" when reader.TokenType == JsonTokenType.Number:
                        if (!reader.TryGetInt32(out quantity) || quantity is < 1 or > 10_000)
                        {
                            return false;
                        }

                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            if (reader.TokenType != JsonTokenType.EndObject || productId.Length == 0 || token.Length == 0)
            {
                return false;
            }
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        purchase = new PlayPurchase(orderId, productId, token, time, quantity);
        return true;
    }

    private static readonly JsonReaderOptions Options = new() { MaxDepth = StoreLimits.MaxJsonDepth, CommentHandling = JsonCommentHandling.Disallow, AllowTrailingCommas = false };

    private static bool TryRead(ReadOnlySpan<byte> utf8, out Message m)
    {
        m = new Message();
        var reader = new Utf8JsonReader(utf8, Options);
        try
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                return false;
            }

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                var name = reader.GetString();
                if (!reader.Read())
                {
                    return false;
                }

                var ok = true;
                switch (name)
                {
                    case "e":
                        ok = ReadString(ref reader, 32, out m.Kind);
                        break;
                    case "code":
                        ok = reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out m.Code);
                        break;
                    case "msg":
                        ok = ReadString(ref reader, 4096, out m.Text);
                        break;
                    case "product":
                        ok = ReadString(ref reader, MaxIdChars, out m.Product);
                        break;
                    case "token":
                        ok = ReadString(ref reader, StoreLimits.MaxProofChars, out m.Token);
                        break;
                    case "items":
                        ok = reader.TokenType == JsonTokenType.StartArray && ReadItems(ref reader, m.Items);
                        break;
                    case "unknown":
                        ok = reader.TokenType == JsonTokenType.StartArray && ReadStrings(ref reader, m.Unknown);
                        break;
                    default:
                        reader.Skip();
                        break;
                }

                if (!ok)
                {
                    return false;
                }
            }

            return reader.TokenType == JsonTokenType.EndObject && m.Kind.Length > 0;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool ReadStrings(ref Utf8JsonReader reader, List<string> into)
    {
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType != JsonTokenType.String || into.Count >= StoreLimits.MaxItems || !ReadString(ref reader, MaxIdChars, out var s))
            {
                return false;
            }

            into.Add(s);
        }

        return reader.TokenType == JsonTokenType.EndArray;
    }

    private static bool ReadItems(ref Utf8JsonReader reader, List<Item> into)
    {
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType != JsonTokenType.StartObject || into.Count >= StoreLimits.MaxItems)
            {
                return false;
            }

            var item = new Item();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                var name = reader.GetString();
                if (!reader.Read())
                {
                    return false;
                }

                var ok = true;
                switch (name)
                {
                    case "id":
                        ok = ReadString(ref reader, MaxIdChars, out item.Id);
                        break;
                    case "title":
                        ok = ReadString(ref reader, MaxIdChars, out item.Title);
                        break;
                    case "desc":
                        ok = ReadString(ref reader, 4096, out item.Description);
                        break;
                    case "price":
                        ok = ReadString(ref reader, 128, out item.Price);
                        break;
                    case "cur":
                        ok = ReadString(ref reader, 16, out item.Currency);
                        break;
                    case "micros":
                        ok = reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out item.Micros) && item.Micros >= 0;
                        break;
                    case "json":
                        ok = ReadString(ref reader, StoreLimits.MaxProofChars, out item.Json);
                        break;
                    case "sig":
                        ok = ReadString(ref reader, 4096, out item.Signature);
                        break;
                    case "state":
                        ok = reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out item.State);
                        break;
                    default:
                        reader.Skip();
                        break;
                }

                if (!ok)
                {
                    return false;
                }
            }

            if (reader.TokenType != JsonTokenType.EndObject)
            {
                return false;
            }

            into.Add(item);
        }

        return reader.TokenType == JsonTokenType.EndArray;
    }

    // The raw token is checked before it is decoded: a character takes at most 6 bytes of JSON text (\uXXXX), so a token longer than
    // that cannot fit the limit and is refused without allocating its string.
    private static bool ReadString(ref Utf8JsonReader reader, int maxChars, out string value)
    {
        value = string.Empty;
        if (reader.TokenType != JsonTokenType.String || reader.ValueSpan.Length > maxChars * 6L)
        {
            return false;
        }

        var s = reader.GetString() ?? string.Empty;
        if (s.Length > maxChars)
        {
            return false;
        }

        value = s;
        return true;
    }

    private static string Clip(string s) => s.Length <= 512 ? s : s[..512];

    /// <summary>For tests: formats a number the way the bridge's JSON does.</summary>
    internal static string Number(long n) => n.ToString(CultureInfo.InvariantCulture);
}
