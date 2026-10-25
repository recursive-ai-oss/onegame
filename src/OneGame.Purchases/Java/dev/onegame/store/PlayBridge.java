package dev.onegame.store;

import android.app.Activity;
import com.android.billingclient.api.AcknowledgePurchaseParams;
import com.android.billingclient.api.BillingClient;
import com.android.billingclient.api.BillingClientStateListener;
import com.android.billingclient.api.BillingFlowParams;
import com.android.billingclient.api.BillingResult;
import com.android.billingclient.api.ConsumeParams;
import com.android.billingclient.api.PendingPurchasesParams;
import com.android.billingclient.api.ProductDetails;
import com.android.billingclient.api.Purchase;
import com.android.billingclient.api.PurchasesUpdatedListener;
import com.android.billingclient.api.QueryProductDetailsParams;
import com.android.billingclient.api.QueryPurchasesParams;
import com.android.billingclient.api.UnfetchedProduct;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentLinkedQueue;
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

/**
 * The whole Java side of the engine's Google Play Billing support. The game's native code calls the static methods through JNI and
 * reads results with {@link #poll()}: every answer from Play is turned into one line of JSON and queued here, so no callback ever
 * crosses back into native code and native code never has to implement a Java interface. The message shapes are read by
 * PlayWire.cs in the purchases library.
 */
public final class PlayBridge implements BillingClientStateListener, PurchasesUpdatedListener {
    private static final ConcurrentLinkedQueue<String> QUEUE = new ConcurrentLinkedQueue<>();
    private static final Map<String, ProductDetails> DETAILS = new HashMap<>();
    private static PlayBridge instance;
    private static BillingClient client;
    private static Activity activity;
    // The product the open purchase flow was started for, so a cancel or a failure can say what it was about.
    private static String flowProduct = "";

    private PlayBridge() {
    }

    /** Creates the billing client. Returns an empty string, or the reason it could not be created. */
    public static synchronized String init(Activity host) {
        try {
            activity = host;
            if (client == null) {
                instance = new PlayBridge();
                client = BillingClient.newBuilder(host.getApplicationContext())
                        .setListener(instance)
                        .enablePendingPurchases(PendingPurchasesParams.newBuilder().enableOneTimeProducts().build())
                        .build();
            }
            return "";
        } catch (Throwable t) {
            return String.valueOf(t);
        }
    }

    /** The next queued message, or null. */
    public static String poll() {
        return QUEUE.poll();
    }

    /** Starts connecting; the answer is a "connected" message. */
    public static void connect() {
        if (client == null) {
            post(message("connected", 3, "The billing client was not created.").toString());
            return;
        }
        if (client.isReady()) {
            post(message("connected", 0, "").toString());
            return;
        }
        client.startConnection(instance);
    }

    /** Ends the connection. */
    public static void end() {
        if (client != null) {
            client.endConnection();
        }
    }

    /** Queries product details: ids of one-time products and of subscriptions, each comma separated. */
    public static void queryProducts(String inapp, String subs) {
        List<QueryProductDetailsParams.Product> products = new ArrayList<>();
        addProducts(products, inapp, BillingClient.ProductType.INAPP);
        addProducts(products, subs, BillingClient.ProductType.SUBS);
        if (products.isEmpty()) {
            post(message("products", 0, "").toString());
            return;
        }
        QueryProductDetailsParams params = QueryProductDetailsParams.newBuilder().setProductList(products).build();
        client.queryProductDetailsAsync(params, (result, details) -> {
            JSONObject m = message("products", result.getResponseCode(), result.getDebugMessage());
            try {
                JSONArray items = new JSONArray();
                for (ProductDetails d : details.getProductDetailsList()) {
                    DETAILS.put(d.getProductId(), d);
                    items.put(describe(d));
                }
                JSONArray unknown = new JSONArray();
                for (UnfetchedProduct u : details.getUnfetchedProductList()) {
                    unknown.put(u.getProductId());
                }
                m.put("items", items);
                m.put("unknown", unknown);
            } catch (JSONException e) {
                // The message stays valid without the lists; native code treats a short list as "nothing found".
            }
            post(m.toString());
        });
    }

    /** Opens Play's purchase flow for a product described by an earlier {@link #queryProducts}. The answer is a "purchases" message. */
    public static void buy(String productId) {
        final ProductDetails details = DETAILS.get(productId);
        if (details == null || client == null) {
            post(message("purchases", 4, "Product details for '" + productId + "' were not loaded.", productId).toString());
            return;
        }
        flowProduct = productId;
        BillingFlowParams.ProductDetailsParams.Builder one = BillingFlowParams.ProductDetailsParams.newBuilder().setProductDetails(details);
        List<ProductDetails.SubscriptionOfferDetails> offers = details.getSubscriptionOfferDetails();
        if (offers != null && !offers.isEmpty()) {
            one.setOfferToken(offers.get(0).getOfferToken());
        }
        final List<BillingFlowParams.ProductDetailsParams> list = new ArrayList<>();
        list.add(one.build());
        final BillingFlowParams flow = BillingFlowParams.newBuilder().setProductDetailsParamsList(list).build();
        // Play's documentation asks for the flow to be launched from the main thread.
        activity.runOnUiThread(() -> {
            BillingResult r = client.launchBillingFlow(activity, flow);
            if (r.getResponseCode() != 0) {
                post(message("purchases", r.getResponseCode(), r.getDebugMessage(), productId).toString());
            }
        });
    }

    /** Consumes (consumable) or acknowledges a purchase by its token. The answer is a "finished" message carrying the token. */
    public static void finish(String token, boolean consume) {
        if (consume) {
            client.consumeAsync(ConsumeParams.newBuilder().setPurchaseToken(token).build(), (r, t) -> post(finished(r, token)));
        } else {
            client.acknowledgePurchase(AcknowledgePurchaseParams.newBuilder().setPurchaseToken(token).build(), r -> post(finished(r, token)));
        }
    }

    /** Lists what the player owns now (one-time products, then subscriptions). The answer is one "restore" message. */
    public static void restore() {
        queryOwned("restore", false);
    }

    // After a connection, owned purchases are reported once as ordinary purchase updates, so a purchase that was paid for but never
    // acknowledged (the game stopped first) reaches the game again. Play refunds unacknowledged purchases after three days.
    private static void queryOwned(final String kind, final boolean onlyOpen) {
        final JSONArray items = new JSONArray();
        final BillingResult[] failure = new BillingResult[1];
        client.queryPurchasesAsync(QueryPurchasesParams.newBuilder().setProductType(BillingClient.ProductType.INAPP).build(), (r1, p1) -> {
            collect(r1, p1, items, failure, onlyOpen);
            client.queryPurchasesAsync(QueryPurchasesParams.newBuilder().setProductType(BillingClient.ProductType.SUBS).build(), (r2, p2) -> {
                collect(r2, p2, items, failure, onlyOpen);
                BillingResult r = failure[0];
                JSONObject m = message(kind, r == null ? 0 : r.getResponseCode(), r == null ? "" : r.getDebugMessage());
                try {
                    m.put("items", items);
                } catch (JSONException e) {
                    // See queryProducts.
                }
                post(m.toString());
            });
        });
    }

    private static void collect(BillingResult r, List<Purchase> purchases, JSONArray into, BillingResult[] failure, boolean onlyOpen) {
        if (r.getResponseCode() != 0) {
            if (failure[0] == null) {
                failure[0] = r;
            }
            return;
        }
        for (Purchase p : purchases) {
            // Acknowledged purchases are settled; only the ones still waiting for the game matter after a reconnect.
            if (onlyOpen && p.getPurchaseState() == Purchase.PurchaseState.PURCHASED && p.isAcknowledged()) {
                continue;
            }
            into.put(describe(p));
        }
    }

    @Override
    public void onBillingSetupFinished(BillingResult result) {
        post(message("connected", result.getResponseCode(), result.getDebugMessage()).toString());
        if (result.getResponseCode() == 0) {
            queryOwned("purchases", true);
        }
    }

    @Override
    public void onBillingServiceDisconnected() {
        post("{\"e\":\"disconnected\"}");
    }

    @Override
    public void onPurchasesUpdated(BillingResult result, List<Purchase> purchases) {
        JSONObject m = message("purchases", result.getResponseCode(), result.getDebugMessage(), flowProduct);
        try {
            JSONArray items = new JSONArray();
            if (purchases != null) {
                for (Purchase p : purchases) {
                    items.put(describe(p));
                }
            }
            m.put("items", items);
        } catch (JSONException e) {
            // See queryProducts.
        }
        post(m.toString());
    }

    private static void addProducts(List<QueryProductDetailsParams.Product> into, String csv, String type) {
        if (csv == null || csv.isEmpty()) {
            return;
        }
        for (String id : csv.split(",")) {
            if (!id.isEmpty()) {
                into.add(QueryProductDetailsParams.Product.newBuilder().setProductId(id).setProductType(type).build());
            }
        }
    }

    private static JSONObject describe(ProductDetails d) throws JSONException {
        JSONObject o = new JSONObject();
        o.put("id", d.getProductId());
        o.put("title", d.getTitle());
        o.put("desc", d.getDescription());
        ProductDetails.OneTimePurchaseOfferDetails one = d.getOneTimePurchaseOfferDetails();
        if (one != null) {
            o.put("price", one.getFormattedPrice());
            o.put("micros", one.getPriceAmountMicros());
            o.put("cur", one.getPriceCurrencyCode());
        } else if (d.getSubscriptionOfferDetails() != null && !d.getSubscriptionOfferDetails().isEmpty()) {
            List<ProductDetails.PricingPhase> phases = d.getSubscriptionOfferDetails().get(0).getPricingPhases().getPricingPhaseList();
            ProductDetails.PricingPhase last = phases.get(phases.size() - 1);
            o.put("price", last.getFormattedPrice());
            o.put("micros", last.getPriceAmountMicros());
            o.put("cur", last.getPriceCurrencyCode());
        }
        return o;
    }

    private static JSONObject describe(Purchase p) {
        JSONObject o = new JSONObject();
        try {
            o.put("json", p.getOriginalJson());
            o.put("sig", p.getSignature());
            o.put("state", p.getPurchaseState());
        } catch (JSONException e) {
            // Cannot happen for plain values.
        }
        return o;
    }

    private static String finished(BillingResult r, String token) {
        JSONObject m = message("finished", r.getResponseCode(), r.getDebugMessage());
        try {
            m.put("token", token);
        } catch (JSONException e) {
            // Cannot happen for plain values.
        }
        return m.toString();
    }

    private static JSONObject message(String kind, int code, String text) {
        return message(kind, code, text, "");
    }

    private static JSONObject message(String kind, int code, String text, String product) {
        JSONObject m = new JSONObject();
        try {
            m.put("e", kind);
            m.put("code", code);
            m.put("msg", text == null ? "" : text);
            m.put("product", product == null ? "" : product);
        } catch (JSONException e) {
            // Cannot happen for plain values.
        }
        return m;
    }

    private static void post(String line) {
        // A game that stops polling must not grow memory without bound.
        if (QUEUE.size() < 4096) {
            QUEUE.add(line);
        }
    }
}
