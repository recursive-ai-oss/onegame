namespace OneGame.Control;

public static partial class ControlOps
{
    private static ControlOp[] StoreOps =>
    [
        new("store",
            "Drive and inspect the game's in-app purchases. action: describe (default; connection, products, owned, unfinished, pending, recent events), connect, load (ask the store about the catalog), buy (product), finish (product, or every unfinished transaction), restore. Against the fake store (ONEGAME_STORE=fake) it also scripts outcomes: script (product, outcome = succeed|cancel|pend|fail, error = a StoreErrorKind for fail) applies to that product's next purchase; resolve (product, approve) settles a pending purchase; revoke (product) takes a purchase back like a refund; disconnect; fail-connect (error) makes the next connect fail. Every action delivers the store's answers before it returns, and returns the same status as describe. Fails with 'no_store' when the game has no purchase service.",
            """{"type":"object","properties":{"action":{"type":"string","enum":["describe","connect","load","buy","finish","restore","script","resolve","revoke","disconnect","fail-connect"]},"product":{"type":"string"},"outcome":{"type":"string","enum":["succeed","cancel","pend","fail"]},"error":{"type":"string"},"approve":{"type":"boolean"}}}""", true),
    ];
}
