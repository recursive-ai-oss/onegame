# store: in-app purchases

Owner: `src/OneGame.Purchases` (the API, the fake store and the two backends), `src/OneGame.Control/ControlSurface.Store.cs` (the `store` operation), the `store:` scenario step and expectation in `src/OneGame.Testing`, and the Play Billing part of the Android export (`src/OneGame.Cli/Verbs/PlayBillingKit.cs`, `src/OneGame.Purchases/Java`). Example game: `tests/StoreGame`. Steam is a different thing (`steam.md`: achievements, cloud saves, ownership) and does not sell items through this API.

## The loop

```csharp
static readonly ProductDefinition[] Catalog = [new("coins.100", ProductKind.Consumable), new("remove_ads", ProductKind.NonConsumable)];

var store = PurchaseBackends.CreateService(Catalog);   // App Store, Google Play, or the fake store when ONEGAME_STORE=fake
store.Event += e => { /* react: see below */ };
store.Connect();                                        // answer: StoreEvent Connected (Error set on failure)
// every frame, on the game thread:
store.Pump();                                           // delivers what the store reported since the last call
```

1. `Connect()` → `Connected`. On failure `ConnectError.Kind` says why (`Unavailable`: no store here, not signed in, library missing; `NotAllowed`: purchases are off on the device; `Network`).
2. `LoadProducts()` → `ProductsLoaded` with `Products` (title, description, formatted `Price`, `PriceMicros`, `Currency`) and `UnknownProductIds`. Show these strings; never build prices yourself.
3. `Buy(id)` → `Transaction` events: `Purchased`, `Pending` (waiting on someone's approval; grant nothing), `Cancelled` (not an error), `Failed` (`Error.Kind`), later `Purchased`/`Failed` for a pending one.
4. On `Purchased` or `Restored` (`NeedsFinish`): **grant the item as a command on a synced entity, then call `Finish(transaction)`**. Finish consumes a consumable and acknowledges the rest. Play refunds purchases left unacknowledged after three days; Apple re-delivers an unfinished transaction at every start, so a game that stops between granting and finishing gets the purchase again (make granting idempotent per `TransactionId`).
5. `Restore()` → `Restored` transactions (everything the player owns), then `RestoreFinished`. Apple may ask the player to sign in, so call it from a button. Google Play lists everything owned and reports unacknowledged purchases again after every connect by itself.
6. `Revoked` (refund, revoked family share): remove what was granted. `IsOwned(id)` is true from a purchase/restore of a non-consumable or subscription until a revocation. On a store whose restore is complete (Play, the fake: `RestoreIsComplete`), an owned product missing from a finished restore is reported as `Revoked` too. StoreKit has no refund event in its original API: refunds reach the game only through your server (App Store Server Notifications) or the receipt.

Requests return a `StoreError` at once when they cannot even be sent (not connected, product not in the catalog, not loaded, already owned); `StoreError.None` means the answer will come as an event. Nothing throws for store conditions.

**Threads.** Requests and `Pump()` belong to the game thread. Backends receive store callbacks on their own threads and queue them in a bounded `StoreEventBuffer` (4096 events, oldest dropped); handlers run only inside `Pump()`. The Play backend must be used from the thread the platform layer attached to the Java VM (the game thread).

**Server verification.** `PurchaseTransaction.ServerProof` is what your server sends to the store: on Apple the base64 app receipt (empty on a simulator without one), on Google Play the purchase token (`SignedData` and `Signature` also allow a local signature check against your Play public key). Treat the client's word as a hint: grant anything valuable only after the server checked it. All of it is untrusted text, truncated or refused at the limits in `StoreLimits`.

## Backends

| Backend | Where | Notes |
|---|---|---|
| `FakeStore` | everywhere | deterministic and scriptable, see below. `ONEGAME_STORE=fake` makes `CreateService` use it. |
| `StoreKitBackend` | iOS, macOS | StoreKit's original Objective-C API through the runtime (`IosObjc`), observer class registered at run time, all requests on the main thread. |
| `PlayBillingBackend` | Android | Google Play Billing Library 9.1.0 through one Java class of ours and JNI. |
| `UnavailableStore` | other platforms, or when a real backend cannot start | every call fails `Unavailable` with the reason. |

`PurchaseBackends.CreateNative()` picks the platform's real store.

**Why the original StoreKit API.** StoreKit 2 exists only as a Swift API; using it from a NativeAOT C# game means building a Swift library, shipping it and bridging async Swift. The original API is deprecated for new features but still ships in every system release and covers the loop above. Its gaps are no refund notification, no JWS transaction data (the receipt carries the same facts) and no `Transaction.currentEntitlements`. If one of those matters, a small Swift shim is the next step; the neutral API does not change.

**Play Billing in the APK.** A game opts in with `<AndroidPlayBilling>true</AndroidPlayBilling>` in its project file. The export then adds `classes.dex` (about 280 KiB: our class `dev.onegame.store.PlayBridge`, the Billing Library 9.1.0 and the few androidx and datatransport classes it reaches, shrunk by R8 with the library's own keep rules), `hasCode="true"`, the `com.android.vending.BILLING` permission, the `<queries>` entries for Play's billing service and Play's two purchase activities. The libraries are build inputs: six files fetched once from Google's Maven repository, checked against pinned SHA-256 values and cached in `ONEGAME_PLAY_BILLING_DIR` (default `~/.local/share/onegame/play-billing`); `javac`, `java` and the SDK's `d8.jar` (which holds R8) do the build, so a JDK is already needed for signing. Nothing else changes: the game's own code still runs as NativeAOT in the native activity. `PlayBridge` queues every Play callback as one line of JSON; the C# side polls it (one JNI call per frame) and parses it with bounds checks (`PlayWire`), so no callback ever enters native code.

## The fake store

`FakeStore.For(catalog)` sells every product. It follows the rules the real stores share: a non-consumable can be bought once; a consumable cannot be bought again until the last purchase is finished; unfinished purchases come back on a restore; events arrive in request order; transaction ids are `fake-1`, `fake-2`, ….

- `Script(product, outcome, error?)` queues what the next purchase of a product does: `Succeed` (default), `Cancel`, `Pend`, `Fail` with a `StoreError`.
- `ResolvePending(product, approve)`, `Revoke(product)` (a refund), `Disconnect()`, `ConnectFailure`, `Owned()`.

## In the harness

Control operation `store` (needs a `PurchaseService`; the most recent one is used unless `ControlSurface.Purchases` is set). Actions: `describe` (connection, products, owned, unfinished, pending and the latest events as text), `connect`, `load`, `buy`, `finish`, `restore`, and for the fake store `script`, `resolve`, `revoke`, `disconnect`, `fail-connect`. Each action delivers the store's answers before it returns and returns the same status as `describe`. A request that cannot be sent fails with `store_<kind>` (`store_productnotfound`, `store_alreadyowned`, …). Scripting a real store fails with `rejected`.

Scenario step `store: {action: buy, product: remove_ads}` (a bare `store: connect` works for actions without arguments; keys `action`, `product`, `outcome`, `error`, `approve`) and expectation `expect: {store: {owned: remove_ads, not-owned: x, pending: id|none, connection: connected, unfinished: "0", products: "2", recent: "transaction coins.100 purchased"}}`. A game must call `Pump()` itself; a single-player game hears about time through `GameInstance.TickCompleted` (see `tests/StoreGame/ShopGame.cs`). `tests/StoreGame/scenarios/01-buy-cancel-pend-restore-refund.yaml` buys, cancels, waits for approval, restores, refunds and fails a purchase, on desktop and on the iOS simulator with the same final state hash.

## Trying real purchases (owner steps)

None of this needs the engine to change; it needs accounts.

- **Apple:** an Apple Developer account, the app's bundle id registered, products created in App Store Connect with the ids in the catalog (the screens' "Paid Applications" agreement signed for sandbox), and a sandbox tester under Users and Access. Run on a device signed into the sandbox account (`onegame export --target ios`, sign and install). Without products the simulator answers `ProductsLoaded` with every id unknown, which is what the binding was run against. `tests/StoreGame/StoreKit/Products.storekit` is a StoreKit configuration for Xcode's local testing (an Xcode scheme run); it was written from Apple's documented format and has not been opened in Xcode.
- **Google:** a Play Console developer account, the app uploaded to an internal test track with the same package name and a release signing key (a debug-signed APK cannot buy), products created in Play Console with the catalog ids, and the tester's Google account added as a licence tester. The device needs the Play Store app; emulator images labelled "Google Play" provide it, "Google APIs" images do not.

## Not covered

Subscriptions are bought with the first base plan and offer Play lists (no offer choice, upgrade or downgrade); no promotional offers, introductory-offer eligibility, family sharing queries, price-change consent, `appAccountToken` or obfuscated account ids; no pending-purchase updates for Apple's "ask to buy" after the app restarts beyond what the queue redelivers; no StoreKit 2; macOS compiles the same backend as iOS but it has not been run on a Mac (the iOS simulator is the only place it ran), and there is no Mac App Store signing.
