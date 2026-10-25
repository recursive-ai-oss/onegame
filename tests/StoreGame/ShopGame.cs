using System;
using System.IO;
using System.Linq;
using OneGame.Core;
using OneGame.Host;
using OneGame.Purchases;
using OneGame.UI;
using OneGame.UI.Kit;

namespace StoreGame;

/// <summary>
/// A tiny shop: one consumable (coins), one unlock (remove ads), a restore button and a status line. It shows the whole purchase loop
/// (connect, load, buy, grant through a command, finish) and drives the store checks on a device or against the fake store.
/// </summary>
public sealed class ShopGame : GameDefinition
{
    /// <summary>The products this game sells, as named in the stores.</summary>
    public static readonly ProductDefinition[] Catalog =
    [
        new("coins.100", ProductKind.Consumable),
        new("remove_ads", ProductKind.NonConsumable),
    ];

    private readonly PurchaseService _store;
    private GameInstance? _game;
    private bool _started;
    private string _status = "starting";

    /// <summary>A shop over the platform's store, or the fake store when ONEGAME_STORE=fake.</summary>
    public ShopGame()
        : this(PurchaseBackends.CreateService(Catalog))
    {
    }

    /// <summary>A shop over the given service.</summary>
    public ShopGame(PurchaseService store)
    {
        _store = store;
        _store.Event += OnStoreEvent;
    }

    public override string Name => "StoreGame";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.StoreGame.EntityTypes.Register(registry);

    public override void Setup(World world) => world.Add(new Wallet());

    public override void DefineFonts(OneGame.Text.FontLibrary fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        fonts.Add(Read("fonts/NotoSans-Regular.ttf"));
    }

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", game => Screen(game));

    private Element Screen(GameInstance game)
    {
        Hook(game);
        return Ui.Column(
        Ui.Label(() => "Coins: " + Wallet(game).Coins).Named("coins"),
        Ui.Label(() => Wallet(game).AdsRemoved ? "Ads: removed" : "Ads: shown").Named("ads"),
        Ui.Label(() => "Store: " + _store.Name + " " + _store.Describe().Connection).Named("store"),
        Ui.Label(() => _status).Named("status"),
        UiKit.Button(() => "Buy 100 coins " + PriceOf("coins.100"), () => Report(_store.Buy("coins.100")), "buy-coins"),
        UiKit.Button(() => "Remove ads " + PriceOf("remove_ads"), () => Report(_store.Buy("remove_ads")), "buy-ads"),
        UiKit.Button("Restore purchases", () => Report(_store.Restore()), "restore"))
        .Gap(Length.Ui(8)).Padding(Length.Ui(16)).TextColor(Color.White);
    }

    // A game on its own (no server) hears about time through TickCompleted; store answers are delivered there, on the game thread.
    private void Hook(GameInstance game)
    {
        if (ReferenceEquals(_game, game))
        {
            return;
        }

        _game = game;
        game.TickCompleted += _ =>
        {
            if (!_started)
            {
                _started = true;
                _store.Connect();
            }

            _store.Pump();
        };
    }

    private static Wallet Wallet(GameInstance game) => (Wallet)game.World.Entities[0];

    private string PriceOf(string id) => _store.Products.FirstOrDefault(p => p.Id == id)?.Price ?? string.Empty;

    private void Report(StoreError error)
    {
        if (error.IsError)
        {
            _status = "not started: " + error.Kind;
        }
    }

    private void OnStoreEvent(StoreEvent e)
    {
        switch (e.Kind)
        {
            case StoreEventKind.Connected when e.Error.IsError:
                _status = "store unavailable: " + e.Error.Kind;
                break;
            case StoreEventKind.Connected:
                _status = "connected";
                _store.LoadProducts();
                break;
            case StoreEventKind.ProductsLoaded:
                _status = e.Error.IsError ? "products failed: " + e.Error.Kind : "ready: " + e.Products.Count + " products";
                break;
            case StoreEventKind.Transaction when e.Transaction is { } t:
                OnTransaction(t);
                break;
            case StoreEventKind.RestoreFinished:
                _status = e.Error.IsError ? "restore failed: " + e.Error.Kind : "restore finished";
                break;
            default:
                break;
        }
    }

    private void OnTransaction(PurchaseTransaction t)
    {
        var wallet = _game is null ? null : Wallet(_game);
        switch (t.State)
        {
            case TransactionState.Purchased:
            case TransactionState.Restored:
                // Grant first, finish after: if the game stops in between, the store hands the purchase over again.
                if (t.ProductId == "coins.100")
                {
                    wallet?.Send.Credit(100 * t.Quantity);
                }
                else if (t.ProductId == "remove_ads")
                {
                    wallet?.Send.SetAdsRemoved(true);
                }

                _store.Finish(t);
                _status = (t.State == TransactionState.Restored ? "restored " : "bought ") + t.ProductId;
                break;
            case TransactionState.Pending:
                _status = "waiting for approval: " + t.ProductId;
                break;
            case TransactionState.Cancelled:
                _status = "cancelled " + t.ProductId;
                break;
            case TransactionState.Failed:
                _status = "failed " + t.ProductId + ": " + t.Error.Kind;
                break;
            case TransactionState.Revoked:
                if (t.ProductId == "remove_ads")
                {
                    wallet?.Send.SetAdsRemoved(false);
                }

                _status = "refunded " + t.ProductId;
                break;
            default:
                break;
        }
    }

    private static byte[] Read(string name)
    {
        using var stream = typeof(ShopGame).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"The program has no embedded file '{name}'.");
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }
}
