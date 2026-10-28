[API reference](../../index.md) › [OneGame.Purchases](../index.md) › OneGame.Purchases

# ProductKind enum

**Namespace:** `OneGame.Purchases`  
**Package:** [`RecursiveAI.OneGame.Purchases`](../index.md)

```csharp
public enum ProductKind : int
```

What a product is. It decides how a purchase is finished and whether the player keeps it.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-consumable"></a>`Consumable` | `0` | Used up and bought again (coins, extra lives). Finishing it consumes it; it is never "owned". |
| <a id="m-nonconsumable"></a>`NonConsumable` | `1` | Bought once and kept (an unlock). Finishing it acknowledges it; restores bring it back. |
| <a id="m-subscription"></a>`Subscription` | `2` | Renews until cancelled. Owned while the store lists it as active. |
