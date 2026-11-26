namespace OneGame.Control;

public static partial class ControlOps
{
    private static ControlOp[] StoreOps =>
    [
        new("store",
            "Drive and inspect the game's in-app purchases. action: describe (default; connection, products, owned, unfinished, pending, recent events), connect, load (ask the store about the catalog), buy (product), finish (product, or every unfinished transaction), restore. Against the fake store (ONEGAME_STORE=fake) it also scripts outcomes: script (product, outcome = succeed|cancel|pend|fail, error = a StoreErrorKind for fail) applies to that product's next purchase; resolve (product, approve) settles a pending purchase; revoke (product) takes a purchase back like a refund; price (product, amount such as \"4.99\", currency = ISO 4217 code, default USD, optional display text) sets what the store reports as that product's price, formatted for the currency unless display is given, and reloads the products; disconnect; fail-connect (error) makes the next connect fail. The products action (any store) lists the loaded products with price text, micros and currency. Every action delivers the store's answers before it returns, and returns the same status as describe. Fails with 'no_store' when the game has no purchase service.",
            """{"type":"object","properties":{"action":{"type":"string","enum":["describe","connect","load","buy","finish","restore","script","resolve","revoke","price","products","disconnect","fail-connect"]},"product":{"type":"string"},"outcome":{"type":"string","enum":["succeed","cancel","pend","fail"]},"error":{"type":"string"},"approve":{"type":"boolean"},"amount":{"type":"string"},"currency":{"type":"string"},"display":{"type":"string"}}}""", true),
    ];
}
