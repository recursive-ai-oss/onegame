using OneGame.Core;
using OneGame.Host;
using OneGame.UI;
using OneGame.UI.Kit;

namespace Lanterns.Views;

/// <summary>
/// The game screen: a bar with what I hold, the island with its players and lanterns (plain boxes, placed every frame from the world), the
/// leaderboard, and the swap dialog. The class reads the session and sends commands through it.
/// </summary>
[View]
internal sealed class IslandView
{
    private const float BarUnits = 44;
    private const float SideUnits = 250;
    private const int MaxLanterns = 24;
    private const int MaxPlayers = 16;
    private const int Rows = 8;
    private const int Partners = 6;

    private readonly Session _s;
    private readonly GameInstance _game;
    private readonly bool _swapScreen;
    private float _ppu = 1;
    private float _ox;
    private float _oy;
    private uint _scope = Rules.IslandA;
    private Lantern[] _lanterns = [];
    private Player[] _players = [];
    private (uint Id, string Name, int Total, uint Island)[] _board = [];
    private (uint Id, string Name, int Total, uint Island)[] _partners = [];

    private IslandView(GameInstance game, bool swap)
    {
        _game = game;
        _s = Session.For(game);
        _swapScreen = swap;
    }

    public static Element Build(GameInstance game, bool swap) => new IslandView(game, swap).Make();

    private Element Make()
    {
        if (_swapScreen)
        {
            _s.SwapOpen = true;
        }

        var board = new Element("Board").Style(st => st.Grow(1).Shrink(1).MinWidth(Length.Px(0)).Overflow(OverflowMode.Hidden));
        board.OnUpdate(_ => Update());
        board.Add(Ui.Box().Named("island").Style(st => st.Position(PositionMode.Absolute).Background(Theme.Sand).Radius(Length.Ui(10)))
            .BindStyle(st => st.Left(_ox).Top(_oy).Width(Rules.Width * _ppu).Height(Rules.Height * _ppu)));
        board.Add(Ui.Box().Named("bridge").Style(st => st.Position(PositionMode.Absolute).Background(Theme.Bridge))
            .BindStyle(st =>
            {
                var x = _scope == Rules.IslandA ? _ox + (Rules.Width * _ppu) : _ox - (24 * _ppu);
                st.Left(x).Top(_oy + (Rules.BridgeTop * _ppu)).Width(24 * _ppu).Height((Rules.BridgeBottom - Rules.BridgeTop) * _ppu);
            }));
        for (var i = 0; i < MaxLanterns; i++)
        {
            board.Add(MakeLantern(i));
        }

        for (var i = 0; i < MaxPlayers; i++)
        {
            board.Add(MakePlayer(i));
        }

        var side = Ui.Column(
            Ui.Label("Leaderboard").Class("heading"),
            Rows0(),
            Ui.Spacer(),
            Ui.Button("Swap lanterns", () => _s.SwapOpen = true).Named("swap-open"))
            .Class("side").Width(Length.Ui(SideUnits)).Shrink(0);
        var root = Ui.Column(MakeBar(), Ui.Row(board, side).Style(st => st.Grow(1).MinHeight(Length.Px(0))), MakeSwapDialog())
            .Class("screen").Named("island-screen").Width(Length.Percent(100)).Height(Length.Percent(100));
        return _s.Kit.AttachTo(root);
    }

    private Element MakeBar() =>
        Ui.Row(
            Ui.Label(string.Empty).Class("stat").Named("where").BindText(() => _s.Scope, s => s == Rules.IslandA ? "North island" : "South island"),
            Ui.Label(string.Empty).Named("mine").BindText(() => _s.Me is { } m ? Hash(m) : 0, v => v == 0 ? "Looking for you..." : Holding()),
            Ui.Spacer(),
            Ui.Label(string.Empty).Class("dim").Named("swap-status").BindText(() => _s.MyAccount?.Status ?? -1, StatusText))
            .Class("bar").Height(Length.Ui(BarUnits)).Shrink(0);

    private static int Hash(Player p) => HashCode.Combine(p.Red, p.Blue, p.Gold, p.Total) | 1;

    private string Holding() => _s.Me is { } m ? $"Red {m.Red}  Blue {m.Blue}  Gold {m.Gold}  ({m.Total})" : string.Empty;

    private static string StatusText(int status) => status switch
    {
        SwapStatus.Offered => "Swap offered",
        SwapStatus.Incoming => "Swap offer for you",
        SwapStatus.Trading => "Swapping...",
        SwapStatus.Done => "Swap done",
        SwapStatus.Failed => "Swap did not happen",
        _ => string.Empty,
    };

    private Element Rows0()
    {
        var list = Ui.Column().Gap(Length.Ui(4)).Named("leaderboard");
        for (var i = 0; i < Rows; i++)
        {
            var row = i;
            list.Add(Ui.Label(string.Empty).Named("rank-" + (row + 1).ToString(CultureInfo.InvariantCulture))
                .BindText(() => _board.Length > row ? _board[row].Total + (_board[row].Id * 1000L) : -1, _ => RowText(row))
                .BindVisible(() => _board.Length > row));
        }

        return list;
    }

    private string RowText(int row)
    {
        if (row >= _board.Length)
        {
            return string.Empty;
        }

        var b = _board[row];
        return $"{(row + 1).ToString(CultureInfo.InvariantCulture)}. {b.Name}  {b.Total.ToString(CultureInfo.InvariantCulture)}";
    }

    private Element MakeLantern(int i) =>
        Ui.Box().Named("lantern-" + i.ToString(CultureInfo.InvariantCulture))
            .Style(st => st.Position(PositionMode.Absolute).Radius(Length.Ui(8)).PointerEvents(PointerEvents.None))
            .BindVisible(() => i < _lanterns.Length)
            .BindStyle(st =>
            {
                if (i >= _lanterns.Length)
                {
                    return;
                }

                var l = _lanterns[i];
                var size = 10 * _ppu;
                st.Left(_ox + ((l.X - 5) * _ppu)).Top(_oy + ((l.Y - 5) * _ppu)).Width(size).Height(size).Background(Theme.Lantern[l.Color % Rules.Colors]);
            });

    private Element MakePlayer(int i)
    {
        var el = Ui.Box().Named("player-" + i.ToString(CultureInfo.InvariantCulture))
            .Style(st => st.Position(PositionMode.Absolute).Radius(Length.Ui(12)).PointerEvents(PointerEvents.None).ZIndex(5))
            .BindVisible(() => i < _players.Length)
            .BindStyle(st =>
            {
                if (i >= _players.Length)
                {
                    return;
                }

                var p = _players[i];
                var size = 14 * _ppu;
                st.Left(_ox + ((p.X - 7) * _ppu)).Top(_oy + ((p.Y - 7) * _ppu)).Width(size).Height(size).Background(p.Mine ? Theme.Me : Theme.Other);
            });
        el.Add(Ui.Label(string.Empty).Style(st => st.Position(PositionMode.Absolute).FontSize(Length.Ui(11)).Top(Length.Ui(-16)).Left(Length.Ui(-6)))
            .BindText(() => i < _players.Length ? _players[i].Owner : 0, _ => i < _players.Length ? $"{_players[i].Name} ({_players[i].Total.ToString(CultureInfo.InvariantCulture)})" : string.Empty));
        return el;
    }

    // ---- the swap dialog ---------------------------------------------------------------------------------------------

    private DialogView MakeSwapDialog()
    {
        var body = Ui.Column(
            Ui.Label("Swap one lantern with:").Class("dim"),
            Partner(),
            Ui.Label("You give").Class("dim"),
            ColorRow("give", () => _s.SwapGive, c => _s.SwapGive = c),
            Ui.Label("You get").Class("dim"),
            ColorRow("want", () => _s.SwapWant, c => _s.SwapWant = c),
            Ui.Label(string.Empty).Named("swap-note").BindText(() => _s.Notice.Length + (_s.MyAccount?.Status ?? 0), _ => Note()),
            Ui.Row(
                Ui.Button("Accept", () => _s.Accept()).Named("swap-accept"),
                Ui.Button("Decline", () => _s.Decline()).Named("swap-decline")).Gap(Length.Ui(8))
                .BindVisible(() => _s.MyAccount?.Status == SwapStatus.Incoming))
            .Gap(Length.Ui(6));
        var dialog = UiKit.Dialog("Swap lanterns", body, "swap", onCancel: () => Close());
        dialog.AddButton("Offer swap", () => _s.SendSwap(), ButtonKind.Primary, isDefault: true, id: "swap-offer");
        dialog.AddButton("Close", () => Close(), id: "swap-close");
        return dialog.BindOpen(() => _s.SwapOpen);
    }

    private void Close()
    {
        _s.SwapOpen = false;
        if (_swapScreen)
        {
            _game.OpenScreen("main");
        }
    }

    private string Note()
    {
        if (_s.MyAccount is { Status: SwapStatus.Incoming } a)
        {
            return $"{NameOf(a.Partner)} offers {Rules.ColorName(a.Give)} for your {Rules.ColorName(a.Want)}.";
        }

        return _s.Notice;
    }

    private string NameOf(uint id) => _board.FirstOrDefault(b => b.Id == id).Name ?? "Someone";

    private Element Partner()
    {
        var col = Ui.Column().Gap(Length.Ui(4)).Named("partners");
        for (var i = 0; i < Partners; i++)
        {
            var n = i;
            col.Add(Ui.Button(string.Empty, () =>
            {
                if (n < _partners.Length)
                {
                    _s.SwapTarget = _partners[n].Id;
                }
            }).Named("partner-" + (n + 1).ToString(CultureInfo.InvariantCulture))
                .BindText(() => n < _partners.Length ? _partners[n].Id + (_s.SwapTarget == _partners[n].Id ? 1UL << 40 : 0) : 0, _ => PartnerText(n))
                .BindVisible(() => n < _partners.Length));
        }

        return col;
    }

    private string PartnerText(int n)
    {
        if (n >= _partners.Length)
        {
            return string.Empty;
        }

        var p = _partners[n];
        return (_s.SwapTarget == p.Id ? "> " : "  ") + p.Name + (p.Island == 0 ? string.Empty : p.Island == Rules.IslandA ? " (north)" : " (south)");
    }

    private static Element ColorRow(string name, Func<int> get, Action<int> set)
    {
        var row = Ui.Row().Gap(Length.Ui(6));
        for (var c = 0; c < Rules.Colors; c++)
        {
            var color = c;
            row.Add(Ui.Button(string.Empty, () => set(color)).Named(name + "-" + Rules.ColorName(color).ToLowerInvariant())
                .BindText(() => get() == color ? 1 : 0, v => (v == 1 ? "[" : " ") + Rules.ColorName(color) + (v == 1 ? "]" : " ")));
        }

        return row;
    }

    // ---- per-frame update --------------------------------------------------------------------------------------------

    private void Update()
    {
        var ui = _game.Ui;
        var bw = Math.Max(1, ui.Width - (SideUnits * ui.Scale));
        var bh = Math.Max(1, ui.Height - (BarUnits * ui.Scale));
        var margin = 28f;
        _ppu = Math.Min(bw / (Rules.Width + (2 * margin)), bh / (Rules.Height + (2 * margin)));
        _ox = (bw - (Rules.Width * _ppu)) / 2;
        _oy = (bh - (Rules.Height * _ppu)) / 2;
        _scope = _s.Scope;
        _lanterns = [.. _s.LanternsHere.OrderBy(l => l.Id.Value).Take(MaxLanterns)];
        _players = [.. _s.PlayersHere.Where(p => p.Dest == 0).OrderBy(p => p.Id.Value).Take(MaxPlayers)];
        _board = [.. _s.Board().Take(Rows)];
        var me = _s.Me?.Owner ?? 0;
        _partners = [.. _s.Board().Where(b => b.Id != me).Take(Partners)];
    }
}
