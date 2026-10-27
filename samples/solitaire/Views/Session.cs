using System.Runtime.CompilerServices;
using OneGame.Core;
using OneGame.Storage;
using OneGame.Host;
using OneGame.Platform;
using OneGame.UI;
using OneGame.UI.Kit;

namespace Solitaire.Views;

/// <summary>
/// Everything the player does that is not a rule: which pile is selected, what is being dragged, which screen came before, the
/// settings and statistics, the saved game and the sounds. One session belongs to one running game. It reads the table and sends it
/// commands; it never changes the table itself, and the <see cref="ViewAttribute"/> makes the compiler hold it to that.
/// </summary>
[View]
internal sealed class Session
{
    private const string SaveSlot = "continue";
    private const int DoubleClickFrames = 24;
    private const float DragStartDistance = 6;

    private static readonly ConditionalWeakTable<GameInstance, Session> Sessions = new();

    private readonly int[] _depth = new int[Piles.Count];
    private World? _world;
    private int _deal;
    private bool _counted;
    private bool _ended;
    private int _lastMoves;
    private int _lastRefused;
    private int _lastStock;
    private int _savedSignature = -1;
    private long _noticeUntil;
    private int _lastClickPile = -1;
    private long _lastClickFrame = -100;
    private long _pointerFrame = -100;
    private bool _suppressClick;
    private (int Score, int Moves, int Ticks, int DrawMode) _live;
    private int _resets;

    private Session(GameInstance game)
    {
        Game = game;
        Definition = (SolitaireGame)game.Definition;
        Data = game.Storage.Files;
        Sound = Definition.Services.Sounds;
        Profile = Data.TryRead(Profile.FileName, 16 * 1024, out var bytes) && Profile.TryRead(bytes, out var loaded) ? loaded : new Profile();
        HasSave = game.Storage.Slots.Exists(SaveSlot);
        Kit = Kits.For(game.Ui);
        Kit.Glyphs
            .Bind("confirm", keyboard: " ", mouse: "left", gamepad: "a")
            .Bind("undo", keyboard: "u", gamepad: "x")
            .Bind("foundation", keyboard: "f", mouse: "right", gamepad: "y")
            .Bind("back", keyboard: "Escape", gamepad: "b");
        ApplyVolumes();
        game.TickCompleted += OnTick;
        _resets = Definition.Resets;
        game.WorldReplaced += OnWorldReplaced;
        Resync(game.World);
    }

    public GameInstance Game { get; }

    /// <summary>The component kit's host for this game's UI tree: theme, tooltips and input glyphs.</summary>
    public KitHost Kit { get; }

    public SolitaireGame Definition { get; }

    public Profile Profile { get; }

    public IByteStore Data { get; }

    public ISoundSink Sound { get; }

    public bool HasSave { get; private set; }

    /// <summary>The pile holding the selected cards, or -1.</summary>
    public int Selected { get; private set; } = -1;

    /// <summary>How many cards from the top of the selected pile are selected.</summary>
    public int SelectedCount { get; private set; }

    /// <summary>The screen to return to from statistics and settings.</summary>
    public string Previous { get; private set; } = "main";

    /// <summary>True once a controller button has been used, for the automatic glyph choice.</summary>
    public bool UsedGamepad { get; private set; }

    /// <summary>True while the last input was a pointer, so the keyboard focus ring stays hidden.</summary>
    public bool PointerMode => _pointerMode;

    private bool _pointerMode;

    /// <summary>A short message for the hint line, or null.</summary>
    public string? Notice => Game.Frame < _noticeUntil ? _notice : null;

    private string? _notice;

    public Table Table => (Table)Game.World.Entities[0];

    public bool InProgress => Table.Moves > 0 && Table.Phase == GamePhase.Playing;

    public bool CanContinue => InProgress || HasSave;

    public static Session For(GameInstance game) => Sessions.GetValue(game, static g => new Session(g));

    // ---- screens -----------------------------------------------------------------------------------------------------

    public void Open(string screen)
    {
        if (Game.CurrentScreen is { } current && current != screen && screen is "stats" or "settings")
        {
            Previous = current;
        }

        Game.OpenScreen(screen);
        FocusHelpers.FocusDefault(Game.Ui, Game.Ui.Root);
        Game.Ui.Update();
    }

    public void Back() => Open(Previous);

    public void StartGame(int drawMode)
    {
        Profile.DrawMode = drawMode;
        SaveProfile();
        Selected = -1;
        Table.Send.NewGame(drawMode);
        Open("table");
    }

    public void Continue()
    {
        if (InProgress)
        {
            Open("table");
            return;
        }

        var loaded = Game.Storage.Load(SaveSlot);
        if (loaded.Status == SlotStatus.NoSuchSlot)
        {
            HasSave = false;
            Say("There is no saved game.");
            return;
        }

        if (!loaded.Ok)
        {
            DropSave("The saved game is damaged and was removed.");
            return;
        }

        if (Game.World.Entities.Count != 1 || Game.World.Entities[0] is not Solitaire.Table || !IsSane(Table))
        {
            Game.Reset();
            DropSave("The saved game is damaged and was removed.");
            return;
        }

        Open("table");
    }

    // Starts watching a world that was just put in place (a reset, a fixture, a loaded save). The game in it was counted before, if it has moves.
    private void Resync(World world)
    {
        if (world.Entities.Count == 0 || world.Entities[0] is not Solitaire.Table table)
        {
            return;
        }

        _world = world;
        _deal = table.Deal;
        _counted = table.Moves > 0;
        _ended = table.Phase == GamePhase.Won;
        _lastMoves = table.Moves;
        _lastRefused = table.Refused;
        _lastStock = table.Stock.Count;
        _savedSignature = Signature(table);
    }

    private void OnWorldReplaced(World world)
    {
        Selected = -1;
        Resync(world);
        if (Definition.Resets == _resets)
        {
            return;
        }

        // The harness reset the game. In a run whose files live in memory that means a clean slate: no profile, no save, the keyboard glyphs.
        _resets = Definition.Resets;
        if (Data is MemoryByteStore)
        {
            Profile.ResetAll();
            Data.Delete(Profile.FileName);
            Game.Storage.Slots.Delete(SaveSlot);
            HasSave = false;
            Kit.Glyphs.SetDevice(InputDevice.Keyboard);
            _notice = null;
        }
    }

    private void DropSave(string message)
    {
        Game.Storage.Slots.Delete(SaveSlot);
        HasSave = false;
        Say(message);
    }

    /// <summary>True when the table holds each of the 52 cards exactly once and the draw mode is one the game deals.</summary>
    public static bool IsSane(Table table)
    {
        if (table.DrawMode is not (1 or 3))
        {
            return false;
        }

        var seen = new bool[Cards.Count];
        for (var p = 0; p < Piles.Count; p++)
        {
            foreach (var card in table.Pile(p))
            {
                var id = Cards.Id(card);
                if (id >= Cards.Count || seen[id])
                {
                    return false;
                }

                seen[id] = true;
            }
        }

        return seen.All(s => s);
    }

    // ---- settings and statistics -------------------------------------------------------------------------------------

    public void SaveProfile() => Data.Write(Profile.FileName, Profile.Write());

    public void Say(string message)
    {
        _notice = message;
        _noticeUntil = Game.Frame + 240;
    }

    public void ApplyVolumes() => Sound.SetVolumes(Profile.MusicVolume / 10f, Profile.EffectsVolume / 10f);

    public void Play(SoundEvent sound) => Sound.Play(sound, Profile.EffectsVolume / 10f);

    // ---- the table: selection, clicks, drags -------------------------------------------------------------------------

    /// <summary>The first index of the run that can be moved from a pile, counting from its top; -1 when nothing can move.</summary>
    public int RunStart(int pile)
    {
        var cards = Table.Pile(pile);
        if (pile == Piles.Stock || cards.Count == 0)
        {
            return -1;
        }

        if (!Piles.IsTableau(pile))
        {
            return cards.Count - 1;
        }

        var i = cards.Count - 1;
        if (!Cards.IsFaceUp(cards[i]))
        {
            return -1;
        }

        while (i > 0 && Cards.IsFaceUp(cards[i - 1]) && Cards.Stacks(cards[i - 1], cards[i]))
        {
            i--;
        }

        return i;
    }

    public int Depth(int pile) => Math.Clamp(_depth[pile], 0, Math.Max(0, RunLength(pile) - 1));

    public int RunLength(int pile)
    {
        var start = RunStart(pile);
        return start < 0 ? 0 : Table.Pile(pile).Count - start;
    }

    public void ResetDepth(int pile) => _depth[pile] = 0;

    /// <summary>Moves the keyboard cursor up or down a tableau run. Returns false at the end of the run.</summary>
    public bool StepDepth(int pile, int delta)
    {
        if (!Piles.IsTableau(pile))
        {
            return false;
        }

        var next = _depth[pile] + delta;
        if (next < 0 || next >= RunLength(pile))
        {
            return false;
        }

        _depth[pile] = next;
        return true;
    }

    public void ClearSelection() => Selected = -1;

    public void MarkPointer(bool pointer)
    {
        _pointerMode = pointer;
        if (pointer)
        {
            _pointerFrame = Game.Frame;
        }
    }

    public void MarkGamepad() => UsedGamepad = true;

    public void SuppressNextClick() => _suppressClick = true;

    public void AllowClick() => _suppressClick = false;

    /// <summary>Passed to <see cref="ClickPile"/> for an activation that did not come from the pointer.</summary>
    public const int KeyboardClick = -2;

    /// <summary>True when a pointer event arrived in the frame being handled, which is how a click is told from a key or button activation.</summary>
    public bool PointerThisFrame => _pointerFrame == Game.Frame;

    /// <summary>A click or an activation on a pile. <paramref name="index"/> is the card under the pointer, -1 for an empty pile, or <see cref="KeyboardClick"/>.</summary>
    public void ClickPile(int pile, int index)
    {
        if (_suppressClick)
        {
            _suppressClick = false;
            return;
        }

        var table = Table;
        if (table.Phase != GamePhase.Playing || table.Paused)
        {
            return;
        }

        var fromPointer = index != KeyboardClick;
        var doubleClick = fromPointer && _lastClickPile == pile && Game.Frame - _lastClickFrame <= DoubleClickFrames;
        _lastClickPile = pile;
        _lastClickFrame = Game.Frame;
        if (pile == Piles.Stock)
        {
            Selected = -1;
            table.Send.Draw();
            return;
        }

        if (doubleClick && table.FoundationTargetFor(pile) >= 0)
        {
            Selected = -1;
            table.Send.AutoMove(pile);
            return;
        }

        if (Selected < 0)
        {
            Select(pile, index);
            return;
        }

        if (Selected == pile)
        {
            Selected = -1;
            return;
        }

        if (table.CanMove(Selected, pile, SelectedCount))
        {
            table.Send.Move(Selected, pile, SelectedCount);
            Selected = -1;
        }
        else
        {
            Play(SoundEvent.Refuse);
            Say("That move is not allowed.");
        }
    }

    private void Select(int pile, int index)
    {
        var start = RunStart(pile);
        if (start < 0 || (index >= 0 && index < start))
        {
            return; // an empty pile, or a face-down card
        }

        var cards = Table.Pile(pile);
        var first = index >= 0 ? index : Piles.IsTableau(pile) ? cards.Count - 1 - Depth(pile) : start;
        Selected = pile;
        SelectedCount = cards.Count - first;
    }

    public void AutoMove(int pile)
    {
        var table = Table;
        if (table.FoundationTargetFor(pile) >= 0)
        {
            Selected = -1;
            table.Send.AutoMove(pile);
        }
        else
        {
            Play(SoundEvent.Refuse);
        }
    }

    public void Undo()
    {
        Selected = -1;
        if (Table.History.Count > 0)
        {
            Table.Send.Undo();
        }
    }

    public void AutoFinish()
    {
        if (Table.CanAutoFinish)
        {
            Selected = -1;
            Table.Send.AutoFinish();
        }
    }

    public void TogglePause()
    {
        if (Table.Phase == GamePhase.Playing)
        {
            Selected = -1;
            Table.Send.Pause(!Table.Paused);
        }
    }

    /// <summary>Drops dragged cards: sends the move to the legal pile they overlap most, if any.</summary>
    public bool Drop(int from, int count, float x, float y, TableGeometry geo)
    {
        var table = Table;
        var best = -1;
        var bestArea = geo.CardWidth * geo.CardHeight * 0.25f;
        var blocked = false;
        for (var to = Piles.Foundation; to < Piles.Count; to++)
        {
            if (to == from)
            {
                continue;
            }

            var (bx, by, bw, bh) = geo.BaseRect(to);
            var next = geo.CardCorner(table, to, table.Pile(to).Count);
            var left = Math.Min(bx, next.X);
            var top = Math.Min(by, next.Y);
            var area = Overlap(x, y, geo.CardWidth, geo.CardHeight, left, top, bw, (next.Y - by) + bh);
            if (area <= 0)
            {
                continue;
            }

            if (table.CanMove(from, to, count))
            {
                if (area > bestArea)
                {
                    bestArea = area;
                    best = to;
                }
            }
            else if (area > geo.CardWidth * geo.CardHeight * 0.25f)
            {
                blocked = true;
            }
        }

        if (best >= 0)
        {
            table.Send.Move(from, best, count);
            Selected = -1;
            return true;
        }

        if (blocked)
        {
            Play(SoundEvent.Refuse);
            Say("That move is not allowed.");
        }

        return false;
    }

    private static float Overlap(float ax, float ay, float aw, float ah, float bx, float by, float bw, float bh)
    {
        var w = Math.Min(ax + aw, bx + bw) - Math.Max(ax, bx);
        var h = Math.Min(ay + ah, by + bh) - Math.Max(ay, by);
        return w > 0 && h > 0 ? w * h : 0;
    }

    public static float DragDistance(float dx, float dy) => MathF.Sqrt((dx * dx) + (dy * dy));

    public static bool DragStarted(float dx, float dy) => DragDistance(dx, dy) >= DragStartDistance;

    // ---- keys and buttons the UI did not take ------------------------------------------------------------------------

    public void HandleInput(in PlatformEvent e)
    {
        if (Game.CurrentScreen != "table")
        {
            return;
        }

        if (e.Kind == PlatformEventKind.KeyDown && !e.IsRepeat)
        {
            MarkPointer(false);
            switch (e.Key)
            {
                case Key.U or Key.Backspace:
                    Undo();
                    break;
                case Key.D:
                    Table.Send.Draw();
                    break;
                case Key.F:
                    AutoMoveFocused();
                    break;
                case Key.A:
                    AutoFinish();
                    break;
                case Key.P:
                    TogglePause();
                    break;
            }
        }
        else if (e.Kind == PlatformEventKind.GamepadButtonDown)
        {
            MarkGamepad();
            MarkPointer(false);
            switch (e.GamepadButton)
            {
                case GamepadButton.West:
                    Undo();
                    break;
                case GamepadButton.North:
                    if (Table.CanAutoFinish)
                    {
                        AutoFinish();
                    }
                    else
                    {
                        AutoMoveFocused();
                    }

                    break;
                case GamepadButton.Start:
                    TogglePause();
                    break;
            }
        }
    }

    private void AutoMoveFocused()
    {
        if (Game.Ui.Focused?.Tag is PileTag tag)
        {
            AutoMove(tag.Pile);
        }
    }

    // ---- watching the table ------------------------------------------------------------------------------------------

    private void OnTick(World world)
    {
        Kit.Advance(1f / GameInstance.TickRate);
        Definition.Services.Store?.Pump();
        if (Profile.Glyphs != GlyphStyle.Auto)
        {
            Kit.Glyphs.SetDevice(Profile.Glyphs == GlyphStyle.Gamepad ? InputDevice.Gamepad : InputDevice.Keyboard);
        }

        if (world.Entities.Count == 0 || world.Entities[0] is not Table table)
        {
            return;
        }

        if (!ReferenceEquals(world, _world))
        {
            Resync(world);
            return;
        }

        if (table.Deal != _deal)
        {
            if (_counted && !_ended)
            {
                Profile.Add(new GameRecord(_deal, _live.DrawMode, false, _live.Score, _live.Moves, _live.Ticks));
                SaveProfile();
            }

            _deal = table.Deal;
            _counted = false;
            _ended = false;
            Selected = -1;
        }

        if (!_counted && table.Moves > 0)
        {
            _counted = true;
            Profile.Games++;
            SaveProfile();
        }

        if (_counted && !_ended)
        {
            _live = (table.Score, table.Moves, table.Elapsed, table.DrawMode);
        }

        if (!_ended && table.Phase == GamePhase.Won)
        {
            _ended = true;
            Profile.Add(new GameRecord(table.Deal, table.DrawMode, true, table.Score, table.Moves, table.Elapsed));
            SaveProfile();
            Play(SoundEvent.Win);
        }

        if (table.Moves != _lastMoves)
        {
            Selected = -1;
            Play(table.Stock.Count != _lastStock ? SoundEvent.Draw : SoundEvent.Card);
        }

        if (table.Refused > _lastRefused)
        {
            Play(SoundEvent.Refuse);
        }

        _lastMoves = table.Moves;
        _lastRefused = table.Refused;
        _lastStock = table.Stock.Count;
        Autosave(table);
    }

    private static int Signature(Table table) => HashCode.Combine(table.Deal, table.Moves, table.Phase, table.Paused, table.History.Count);

    private void Autosave(Table table)
    {
        var signature = Signature(table);
        if (signature == _savedSignature)
        {
            return;
        }

        _savedSignature = signature;
        if (table.Phase == GamePhase.Won || table.Moves == 0)
        {
            Game.Storage.Slots.Delete(SaveSlot);
            HasSave = false;
        }
        else
        {
            HasSave = Game.Storage.Save(SaveSlot, "Deal " + table.Deal.ToString(System.Globalization.CultureInfo.InvariantCulture), SlotKind.Auto).Ok;
        }
    }
}

/// <summary>Marks a pile element so input handlers can tell which pile they belong to.</summary>
internal sealed record PileTag(int Pile);
