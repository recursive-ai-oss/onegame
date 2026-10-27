using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.UI;
using OneGame.UI.Kit;

namespace Solitaire.Views;

/// <summary>
/// The table screen: a bar with the score and clock, the board with its piles and 52 card elements, a row of buttons, and the pause and
/// win overlays. The cards are ordinary UI elements with SVG backgrounds, placed from the game's piles every frame; moving a card is a
/// tween on its place, drawn by the 2D renderer like everything else. The class only reads the table and sends commands.
/// </summary>
[View]
internal sealed class TableView
{
    internal const float HudUnits = 44;
    internal const float FooterUnits = 54;

    private static readonly string[] Ranks = ["A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K"];
    private static readonly string[] SuitArt = ["suit-spade", "suit-heart", "suit-club", "suit-diamond"];
    private static readonly string[] PileIds = ["stock", "waste", "found1", "found2", "found3", "found4", "col1", "col2", "col3", "col4", "col5", "col6", "col7"];

    private readonly Session _s;
    private readonly GameInstance _game;
    private readonly TableGeometry _geo = new();
    private readonly CardSlot[] _slots = new CardSlot[Cards.Count];
    private readonly float[] _curX = new float[Cards.Count];
    private readonly float[] _curY = new float[Cards.Count];
    private readonly bool[] _placed = new bool[Cards.Count];
    private readonly bool[] _moving = new bool[Cards.Count];
    private readonly Element[] _piles = new Element[Piles.Count];
    private Element _board = null!;
    private Element _focusRing = null!;
    private Element _selectRing = null!;
    private float _lastW = -1;
    private float _lastH = -1;
    private long _lastFrame = -1;

    // Dragging.
    private bool _pending;
    private bool _dragging;
    private int _dragPile;
    private int _dragIndex;
    private int _dragCount;
    private float _grabX;
    private float _grabY;
    private float _downX;
    private float _downY;
    private float _dragX;
    private float _dragY;

    // Rings (board coordinates).
    private (float X, float Y, float W, float H) _focusRect;
    private bool _focusShown;
    private Color _focusColor;
    private (float X, float Y, float W, float H) _selectRect;
    private bool _selectShown;

    private TableView(GameInstance game)
    {
        _game = game;
        _s = Session.For(game);
    }

    private Table T => _s.Table;

    /// <summary>Builds the table screen.</summary>
    public static Element Build(GameInstance game)
    {
        SettingsPage.Hide(game);
        return new TableView(game).Make();
    }

    private Element Make()
    {
        _board = new Element("Board").Style(st => st.Grow(1).Shrink(1).MinHeight(Length.Px(0)).Overflow(OverflowMode.Hidden));
        _board.OnUpdate(_ => Update());
        _board.On(UiEventKind.PointerDown, OnPointerDown);
        _board.On(UiEventKind.PointerMove, OnPointerMove);
        _board.On(UiEventKind.PointerUp, OnPointerUp);
        _board.On(UiEventKind.Navigate, (_, _) => Keyboard());
        _board.On(UiEventKind.KeyDown, (_, _) => Keyboard());

        for (var p = 0; p < Piles.Count; p++)
        {
            _board.Add(MakePile(p));
        }

        for (var id = 0; id < Cards.Count; id++)
        {
            _board.Add(MakeCard(id));
        }

        _focusRing = MakeRing(4).BindVisible(() => _focusShown).BindStyle(st => PlaceRing(st, _focusRect, _focusColor));
        _selectRing = MakeRing(5).BindVisible(() => _selectShown).BindStyle(st => PlaceRing(st, _selectRect, Theme.Accent));
        _board.Add(_focusRing);
        _board.Add(_selectRing);

        var footer = MakeFooter();
        var root = Ui.Column(MakeBar(), _board, footer, MakePause(), MakeWin())
            .Class("screen")
            .Named("table")
            .Width(Length.Percent(100))
            .Height(Length.Percent(100));
        root.On(UiEventKind.Cancel, (_, _) =>
        {
            if (T.Phase == GamePhase.Won)
            {
                return false;
            }

            if (T.Paused || _s.Selected < 0)
            {
                _s.TogglePause();
            }
            else
            {
                _s.ClearSelection();
            }

            return true;
        });
        return _s.Kit.AttachTo(root);
    }

    // ---- the bar, the buttons and the dialogs ------------------------------------------------------------------------

    private Element MakeBar() =>
        Ui.Row(
            Ui.Label("Score").Class("dim"),
            Ui.Label(string.Empty).Class("stat").Named("score").BindText(() => T.Score, v => v.ToString(CultureInfo.InvariantCulture)),
            Ui.Label("Moves").Class("dim"),
            Ui.Label(string.Empty).Class("stat").Named("moves").BindText(() => T.Moves, v => v.ToString(CultureInfo.InvariantCulture)),
            Ui.Label("Time").Class("dim"),
            Ui.Label(string.Empty).Class("stat").Named("time").BindText(() => T.Elapsed / 60, FormatClock),
            Ui.Spacer(),
            Ui.Label(string.Empty).Class("dim").Named("game-info").BindText(() => (T.Deal * 10) + T.DrawMode, v => "Game " + (v / 10).ToString(CultureInfo.InvariantCulture) + " - draw " + (v % 10).ToString(CultureInfo.InvariantCulture)))
            .Class("bar")
            .Height(Length.Ui(HudUnits))
            .Shrink(0);

    internal static string FormatClock(int seconds) =>
        (seconds / 60).ToString(CultureInfo.InvariantCulture) + ":" + (seconds % 60).ToString("00", CultureInfo.InvariantCulture);

    private Element MakeFooter() =>
        Ui.Row(
            UiKit.Button("Undo", () => _s.Undo(), "undo").BindEnabled(() => T.History.Count > 0 && T.Phase == GamePhase.Playing),
            UiKit.Button("Auto finish", () => _s.AutoFinish(), "auto").BindEnabled(() => _s.Profile.AutoFinish && T.CanAutoFinish),
            UiKit.Button("Menu", () => _s.TogglePause(), "menu"),
            Ui.Label(string.Empty).Class("dim").Named("notice").Grow().BindText(() => _s.Notice ?? string.Empty),
            Prompt("confirm", "Pick up / place"),
            Prompt("undo", "Undo"),
            Prompt("foundation", "To foundation"),
            Prompt("back", "Menu"))
            .Class("bar")
            .Height(Length.Ui(FooterUnits))
            .Shrink(0);

    private Element Prompt(string action, string text) =>
        Ui.Row(UiKit.InputGlyph(_s.Kit.Glyphs, action, "glyph-" + action), Ui.Label(text).Class("dim")).Gap(Length.Ui(6)).Align(Align.Center);

    private DialogView MakePause()
    {
        var dialog = UiKit.Dialog("Paused", Ui.Label("Take your time.").Class("dim"), "pause", onCancel: () => _s.TogglePause());
        dialog.AddButton("Resume", () => _s.TogglePause(), ButtonKind.Primary, isDefault: true, id: "resume");
        dialog.AddButton("New game", () => _s.StartGame(_s.Profile.DrawMode), id: "pause-new");
        dialog.AddButton("Statistics", () => _s.Open("stats"), id: "pause-stats");
        dialog.AddButton("Settings", () => _s.Open("settings"), id: "pause-settings");
        dialog.AddButton("Main menu", () => _s.Open("main"), id: "pause-menu");
        dialog.Buttons.Column().Gap(Length.Ui(8));
        return dialog.BindOpen(() => T.Paused && T.Phase == GamePhase.Playing);
    }

    private DialogView MakeWin()
    {
        var body = Ui.Column(
            Ui.Label(string.Empty).Class("heading").Named("win-score").BindText(() => T.Score, v => "Score " + v.ToString(CultureInfo.InvariantCulture)),
            Ui.Label(string.Empty).Named("win-detail").BindText(() => T.Moves + (T.Elapsed * 100000L), WinDetail),
            Ui.Label(string.Empty).Class("best").Named("win-best").BindText(() => _s.Profile.BestScore == T.Score && T.Phase == GamePhase.Won, v => v ? "New best score" : string.Empty))
            .Gap(Length.Ui(6));
        var dialog = UiKit.Dialog("You won!", body, "win", onCancel: () => _s.Open("main"));
        dialog.AddButton("New game", () => _s.StartGame(_s.Profile.DrawMode), ButtonKind.Primary, isDefault: true, id: "win-new");
        dialog.AddButton("Statistics", () => _s.Open("stats"), id: "win-stats");
        dialog.AddButton("Main menu", () => _s.Open("main"), id: "win-menu");
        dialog.Buttons.Column().Gap(Length.Ui(8));
        return dialog.BindOpen(() => T.Phase == GamePhase.Won);
    }

    private static string WinDetail(long packed) =>
        (packed % 100000).ToString(CultureInfo.InvariantCulture) + " moves in " + FormatClock((int)(packed / 100000 / 60));

    // ---- piles and cards ---------------------------------------------------------------------------------------------

    private Element MakePile(int pile)
    {
        var el = new Element("Pile").Named(PileIds[pile]).Focusable();
        if (pile == Piles.Stock)
        {
            el.AsDefaultFocus();
        }

        el.Tag = new PileTag(pile);
        _piles[pile] = el;
        el.BindStyle(st =>
        {
            var (x, y, w, h) = _geo.Footprint(T, pile);
            st.Position(PositionMode.Absolute).Left(x).Top(y).Width(w).Height(h).ZIndex(1);
        });
        var baseArt = Ui.Box().Style(st => st.Position(PositionMode.Absolute).Left(0).Top(0).BackgroundImage("slot").PointerEvents(PointerEvents.None))
            .BindStyle(st => st.Width(_geo.CardWidth).Height(_geo.CardHeight));
        el.Add(baseArt);
        if (pile == Piles.Stock)
        {
            el.Add(Ui.Image("recycle", 48, 48).BindVisible(() => T.Stock.Count == 0 && T.Waste.Count > 0)
                .Style(st => st.Position(PositionMode.Absolute).PointerEvents(PointerEvents.None).Opacity(0.8f))
                .BindStyle(st => CenterIcon(st, 0.5f)));
        }
        else if (Piles.IsFoundation(pile))
        {
            el.Add(Ui.Image(SuitArt[pile - Piles.Foundation], 48, 48).BindVisible(() => T.Pile(pile).Count == 0)
                .Style(st => st.Position(PositionMode.Absolute).PointerEvents(PointerEvents.None).Opacity(0.22f))
                .BindStyle(st => CenterIcon(st, 0.5f)));
        }

        el.On(UiEventKind.Click, (_, e) =>
        {
            _s.ClickPile(pile, ClickIndex(pile, e));
            return true;
        });
        el.OnFocus(() => _s.ResetDepth(pile));
        el.On(UiEventKind.Navigate, (_, e) =>
        {
            _s.MarkPointer(false);
            return e.Direction switch
            {
                NavDirection.Up => _s.StepDepth(pile, +1),
                NavDirection.Down => _s.StepDepth(pile, -1),
                _ => false,
            };
        });
        return el;
    }

    private int ClickIndex(int pile, in UiEvent click)
    {
        if (!_s.PointerThisFrame)
        {
            return Session.KeyboardClick;
        }

        var (x, y) = Local(click);
        var (hitPile, index) = _geo.Hit(T, x, y);
        return hitPile == pile ? index : -1;
    }

    private void CenterIcon(StyleBuilder st, float fraction)
    {
        var size = _geo.CardWidth * fraction;
        st.Width(size).Height(size).Left((_geo.CardWidth - size) / 2).Top((_geo.CardHeight - size) / 2);
    }

    private Element MakeCard(int id)
    {
        var suit = id / 13;
        var rank = (id % 13) + 1;
        var ink = (suit & 1) == 1 ? Color.Rgb(0xC4303D) : Color.Rgb(0x1D222B);
        var card = new Element("Card").Named("card-" + Cards.Name(id));
        card.Style(st => st.PointerEvents(PointerEvents.None).Position(PositionMode.Absolute));
        card.BindVisible(() => _slots[id].Present);
        card.BindStyle(st =>
        {
            ref var slot = ref _slots[id];
            var z = slot.Z + (_moving[id] ? 400 : 0);
            st.Left(_curX[id]).Top(_curY[id]).Width(_geo.CardWidth).Height(_geo.CardHeight).ZIndex(z)
                .BackgroundImage(slot.FaceUp ? "card-face" : "card-back");
        });
        var label = Ui.Label(Ranks[rank - 1]).Style(st => st.Position(PositionMode.Absolute).FontWeight(700).TextColor(ink))
            .BindStyle(st => st.FontSize(_geo.CardWidth * (rank == 10 ? 0.25f : 0.3f)).Left(_geo.CardWidth * (rank == 10 ? 0.05f : 0.09f)).Top(_geo.CardHeight * 0.02f))
            .BindVisible(() => _slots[id].FaceUp);
        var small = Ui.Image(SuitArt[suit], 24, 24).Style(st => st.Position(PositionMode.Absolute))
            .BindStyle(st => st.Width(_geo.CardWidth * 0.2f).Height(_geo.CardWidth * 0.2f).Left(_geo.CardWidth * 0.08f).Top(_geo.CardHeight * 0.02f + (_geo.CardWidth * 0.3f * 1.35f)))
            .BindVisible(() => _slots[id].FaceUp);
        var big = Ui.Image(SuitArt[suit], 48, 48).Style(st => st.Position(PositionMode.Absolute))
            .BindStyle(st => st.Width(_geo.CardWidth * 0.5f).Height(_geo.CardWidth * 0.5f).Left(_geo.CardWidth * 0.3f).Top(_geo.CardHeight * 0.4f))
            .BindVisible(() => _slots[id].FaceUp);
        return card.With(label, small, big);
    }

    private static Element MakeRing(int z) =>
        new Element("Ring").Style(st => st.Position(PositionMode.Absolute).PointerEvents(PointerEvents.None).ZIndex(z * 1000).Radius(Length.Px(8)));

    private static void PlaceRing(StyleBuilder st, (float X, float Y, float W, float H) r, Color color) =>
        st.Left(r.X - 4).Top(r.Y - 4).Width(r.W + 8).Height(r.H + 8).Border(Length.Px(3), color);

    // ---- per-frame update --------------------------------------------------------------------------------------------

    private bool OverlayOpen => T.Paused || T.Phase == GamePhase.Won;

    private void Update()
    {
        var ui = _game.Ui;
        var w = ui.Width;
        var h = Math.Max(1, ui.Height - ((HudUnits + FooterUnits) * ui.Scale));
        var snap = false;
        if (w != _lastW || h != _lastH)
        {
            _geo.Resize(w, h);
            _lastW = w;
            _lastH = h;
            snap = true;
        }

        var table = T;
        _geo.Arrange(table, _slots);
        if (_dragging && (_dragPile >= Piles.Count || table.Pile(_dragPile).Count < _dragIndex + _dragCount))
        {
            _dragging = false;
            _pending = false;
        }

        if (_dragging)
        {
            var pile = table.Pile(_dragPile);
            for (var k = 0; k < _dragCount; k++)
            {
                var id = Cards.Id(pile[_dragIndex + k]);
                if (id < Cards.Count)
                {
                    _slots[id].X = _dragX;
                    _slots[id].Y = _dragY + (k * _geo.UpStep);
                    _slots[id].Z = 2000 + k;
                    _placed[id] = false;
                }
            }
        }

        var advance = _game.Frame != _lastFrame;
        _lastFrame = _game.Frame;
        for (var id = 0; id < Cards.Count; id++)
        {
            ref var slot = ref _slots[id];
            if (!slot.Present)
            {
                _placed[id] = false;
                _moving[id] = false;
                continue;
            }

            if (!_placed[id] || snap || (_dragging && slot.Z >= 2000))
            {
                _curX[id] = slot.X;
                _curY[id] = slot.Y;
                _placed[id] = !(_dragging && slot.Z >= 2000);
            }
            else if (advance)
            {
                _curX[id] = Approach(_curX[id], slot.X);
                _curY[id] = Approach(_curY[id], slot.Y);
            }

            _moving[id] = MathF.Abs(_curX[id] - slot.X) > 1 || MathF.Abs(_curY[id] - slot.Y) > 1;
        }

        UpdateRings(table);
    }

    private static float Approach(float cur, float target)
    {
        var d = target - cur;
        return MathF.Abs(d) < 0.75f ? target : cur + (d * 0.3f);
    }

    private void UpdateRings(Table table)
    {
        _selectShown = false;
        _focusShown = false;
        if (_s.Selected >= 0 && !_dragging)
        {
            var pile = table.Pile(_s.Selected);
            if (pile.Count >= _s.SelectedCount && _s.SelectedCount > 0)
            {
                _selectRect = RunRect(table, _s.Selected, pile.Count - _s.SelectedCount);
                _selectShown = true;
            }
            else
            {
                _s.ClearSelection();
            }
        }

        if (_game.Ui.Focused?.Tag is not PileTag tag || _s.PointerMode || OverlayOpen || _dragging)
        {
            return;
        }

        var p = tag.Pile;
        var cards = table.Pile(p);
        if (_s.Selected >= 0)
        {
            _focusRect = RunRect(table, p, Math.Max(0, cards.Count - 1));
            _focusColor = p == _s.Selected ? Theme.Accent : table.CanMove(_s.Selected, p, _s.SelectedCount) ? Theme.Good : Theme.Bad;
        }
        else
        {
            var first = _s.RunStart(p) >= 0 && Piles.IsTableau(p) ? cards.Count - 1 - _s.Depth(p) : Math.Max(0, cards.Count - 1);
            _focusRect = RunRect(table, p, first);
            _focusColor = Color.White;
        }

        _focusShown = true;
    }

    // The rectangle from card `first` down to the end of its pile, or the pile's base when it is empty.
    private (float X, float Y, float W, float H) RunRect(Table table, int pile, int first)
    {
        var cards = table.Pile(pile);
        var (bx, by, bw, bh) = _geo.BaseRect(pile);
        if (cards.Count == 0)
        {
            return (bx, by, bw, bh);
        }

        var a = _geo.CardCorner(table, pile, Math.Min(first, cards.Count - 1));
        var b = _geo.CardCorner(table, pile, cards.Count - 1);
        return (a.X, a.Y, _geo.CardWidth + (b.X - a.X), (b.Y - a.Y) + _geo.CardHeight);
    }

    private bool Keyboard()
    {
        _s.MarkPointer(false);
        return false;
    }

    // ---- pointer: drag and drop --------------------------------------------------------------------------------------

    private (float X, float Y) Local(in UiEvent e)
    {
        var o = _board.AbsoluteBounds;
        return (e.X - o.X, e.Y - o.Y);
    }

    private bool OnPointerDown(Element target, UiEvent e)
    {
        _s.MarkPointer(true);
        _s.AllowClick();
        _pending = false;
        _dragging = false;
        if (e.Button != 0 || OverlayOpen)
        {
            return false;
        }

        var (x, y) = Local(e);
        var (pile, index) = _geo.Hit(T, x, y);
        if (pile < 0 || index < 0 || pile == Piles.Stock)
        {
            return false;
        }

        var start = _s.RunStart(pile);
        if (start < 0 || index < start)
        {
            return false;
        }

        var corner = _geo.CardCorner(T, pile, index);
        _pending = true;
        _dragPile = pile;
        _dragIndex = index;
        _dragCount = T.Pile(pile).Count - index;
        _grabX = x - corner.X;
        _grabY = y - corner.Y;
        _downX = e.X;
        _downY = e.Y;
        return false;
    }

    private bool OnPointerMove(Element target, UiEvent e)
    {
        _s.MarkPointer(true);
        if (!_pending)
        {
            return false;
        }

        if (!_dragging)
        {
            if (!Session.DragStarted(e.X - _downX, e.Y - _downY))
            {
                return false;
            }

            _dragging = true;
            _s.ClearSelection();
        }

        var (x, y) = Local(e);
        _dragX = x - _grabX;
        _dragY = y - _grabY;
        return true;
    }

    private bool OnPointerUp(Element target, UiEvent e)
    {
        _s.MarkPointer(true);
        if (e.Button == 1 && !OverlayOpen)
        {
            var (rx, ry) = Local(e);
            var (rightPile, _) = _geo.Hit(T, rx, ry);
            if (rightPile >= 0)
            {
                _s.AutoMove(rightPile);
                _s.SuppressNextClick();
                return true;
            }

            return false;
        }

        var wasDragging = _dragging;
        var pile = _dragPile;
        var count = _dragCount;
        var dropX = _dragX;
        var dropY = _dragY;
        _pending = false;
        _dragging = false;
        if (!wasDragging)
        {
            return false;
        }

        _s.SuppressNextClick();
        _s.Drop(pile, count, dropX, dropY, _geo);
        return true;
    }
}
