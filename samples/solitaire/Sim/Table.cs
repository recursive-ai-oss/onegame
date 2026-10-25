using OneGame.Core;

namespace Solitaire;

/// <summary>Whether the game is still being played.</summary>
public enum GamePhase
{
    /// <summary>Cards are in play.</summary>
    Playing,

    /// <summary>All four foundations are complete.</summary>
    Won,
}

/// <summary>
/// The whole game of Klondike: the thirteen piles, the score, the move counter, the timer and the undo history. It is one entity,
/// so a save is one snapshot and a replay is the log of the commands sent to it. Everything here is simulation code: the table
/// view reads it and sends commands, and never changes it.
/// </summary>
[Entity]
public partial class Table : Entity
{
    private const int HistoryLimit = 4096;

    // History record layout (a long): kind 2 bits, from 4, to 4, count 6, flipped 1, score before the move 16.
    private const int KindDraw = 0;
    private const int KindRecycle = 1;
    private const int KindMove = 2;

    /// <summary>The face-down draw pile; the top is the last element.</summary>
    [Synced] public SyncList<int> Stock { get; } = new();

    /// <summary>The cards drawn from the stock, face up; the top is the last element.</summary>
    [Synced] public SyncList<int> Waste { get; } = new();

    /// <summary>Foundation for spades.</summary>
    [Synced] public SyncList<int> Foundation0 { get; } = new();

    /// <summary>Foundation for hearts.</summary>
    [Synced] public SyncList<int> Foundation1 { get; } = new();

    /// <summary>Foundation for clubs.</summary>
    [Synced] public SyncList<int> Foundation2 { get; } = new();

    /// <summary>Foundation for diamonds.</summary>
    [Synced] public SyncList<int> Foundation3 { get; } = new();

    /// <summary>Tableau column 1; the first element is the bottom of the column.</summary>
    [Synced] public SyncList<int> Tableau0 { get; } = new();

    /// <summary>Tableau column 2.</summary>
    [Synced] public SyncList<int> Tableau1 { get; } = new();

    /// <summary>Tableau column 3.</summary>
    [Synced] public SyncList<int> Tableau2 { get; } = new();

    /// <summary>Tableau column 4.</summary>
    [Synced] public SyncList<int> Tableau3 { get; } = new();

    /// <summary>Tableau column 5.</summary>
    [Synced] public SyncList<int> Tableau4 { get; } = new();

    /// <summary>Tableau column 6.</summary>
    [Synced] public SyncList<int> Tableau5 { get; } = new();

    /// <summary>Tableau column 7.</summary>
    [Synced] public SyncList<int> Tableau6 { get; } = new();

    /// <summary>One packed record per move that can be undone, oldest first.</summary>
    [Synced] public SyncList<long> History { get; } = new();

    /// <summary>Cards turned from the stock at a time: 1 or 3.</summary>
    [Synced] public partial int DrawMode { get; private set; }

    /// <summary>The score under the usual Windows Klondike rules.</summary>
    [Synced] public partial int Score { get; private set; }

    /// <summary>Moves made, counting draws.</summary>
    [Synced] public partial int Moves { get; private set; }

    /// <summary>Simulation ticks since the first move; the timer never reads a clock.</summary>
    [Synced] public partial int Elapsed { get; private set; }

    /// <summary>Playing or won.</summary>
    [Synced] public partial GamePhase Phase { get; private set; }

    /// <summary>True while the pause overlay is up: the timer stops and moves are refused.</summary>
    [Synced] public partial bool Paused { get; private set; }

    /// <summary>Times the waste was turned back over into the stock.</summary>
    [Synced] public partial int Recycles { get; private set; }

    /// <summary>Commands refused since the deal, so a view can react to a bad move.</summary>
    [Synced] public partial int Refused { get; private set; }

    /// <summary>Number of the game since the program started or the world was created; the first deal is 1.</summary>
    [Synced] public partial int Deal { get; private set; }

    /// <summary>The pile with the given number (see <see cref="Piles"/>).</summary>
    public SyncList<int> Pile(int pile) => pile switch
    {
        Piles.Stock => Stock,
        Piles.Waste => Waste,
        Piles.Foundation => Foundation0,
        Piles.Foundation + 1 => Foundation1,
        Piles.Foundation + 2 => Foundation2,
        Piles.Foundation + 3 => Foundation3,
        Piles.Tableau => Tableau0,
        Piles.Tableau + 1 => Tableau1,
        Piles.Tableau + 2 => Tableau2,
        Piles.Tableau + 3 => Tableau3,
        Piles.Tableau + 4 => Tableau4,
        Piles.Tableau + 5 => Tableau5,
        Piles.Tableau + 6 => Tableau6,
        _ => throw new ArgumentOutOfRangeException(nameof(pile), "A pile number is 0 to 12."),
    };

    /// <summary>True when every foundation holds its thirteen cards.</summary>
    public bool IsWon => Foundation0.Count + Foundation1.Count + Foundation2.Count + Foundation3.Count == Cards.Count;

    /// <summary>
    /// True when no tableau card is face down and the stock is empty, so the rest of the game is only a matter of moving cards up
    /// to the foundations (<see cref="AutoFinish"/>).
    /// </summary>
    public bool CanAutoFinish
    {
        get
        {
            if (Phase != GamePhase.Playing || Stock.Count > 0)
            {
                return false;
            }

            for (var p = Piles.Tableau; p < Piles.Count; p++)
            {
                var column = Pile(p);
                for (var i = 0; i < column.Count; i++)
                {
                    if (!Cards.IsFaceUp(column[i]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    /// <summary>The move that would put a pile's top card on its foundation, as a destination pile, or -1 when there is none.</summary>
    public int FoundationTargetFor(int pile)
    {
        if (pile != Piles.Waste && !Piles.IsTableau(pile))
        {
            return -1;
        }

        var source = Pile(pile);
        if (source.Count == 0)
        {
            return -1;
        }

        var target = Piles.Foundation + Cards.Suit(source[source.Count - 1]);
        return CanMove(pile, target, 1) ? target : -1;
    }

    /// <summary>Whether moving the top <paramref name="count"/> cards of <paramref name="from"/> onto <paramref name="to"/> is legal right now.</summary>
    public bool CanMove(int from, int to, int count)
    {
        if (Phase != GamePhase.Playing || Paused || from == to || count < 1)
        {
            return false;
        }

        if (from < Piles.Waste || from >= Piles.Count || to < Piles.Foundation || to >= Piles.Count)
        {
            return false;
        }

        var source = Pile(from);
        if (count > source.Count || ((from == Piles.Waste || Piles.IsFoundation(from)) && count != 1))
        {
            return false;
        }

        var first = source.Count - count;
        for (var i = first; i < source.Count; i++)
        {
            if (!Cards.IsFaceUp(source[i]) || (i > first && !Cards.Stacks(source[i - 1], source[i])))
            {
                return false;
            }
        }

        var moving = source[first];
        var target = Pile(to);
        if (Piles.IsFoundation(to))
        {
            return count == 1 && Cards.Suit(moving) == to - Piles.Foundation && Cards.Rank(moving) == target.Count + 1;
        }

        if (target.Count == 0)
        {
            return Cards.Rank(moving) == 13;
        }

        var top = target[target.Count - 1];
        return Cards.IsFaceUp(top) && Cards.Stacks(top, moving);
    }

    /// <summary>
    /// Shuffles a fresh deck with the world's <c>deal</c> random stream and deals seven columns of one to seven cards, the last of
    /// each face up, with the other 24 cards in the stock.
    /// </summary>
    /// <param name="drawMode">Cards turned from the stock at a time: 1 or 3. Other values are refused.</param>
    [Command]
    public bool NewGame(int drawMode)
    {
        if (drawMode is not (1 or 3))
        {
            Refused++;
            return false;
        }

        for (var p = 0; p < Piles.Count; p++)
        {
            Pile(p).Clear();
        }

        History.Clear();
        var rng = World!.Rng["deal"];
        var deck = new int[Cards.Count];
        for (var i = 0; i < deck.Length; i++)
        {
            deck[i] = i;
        }

        for (var i = deck.Length - 1; i > 0; i--)
        {
            var j = rng.NextInt(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }

        var next = 0;
        for (var column = 0; column < 7; column++)
        {
            var pile = Pile(Piles.Tableau + column);
            for (var row = 0; row <= column; row++)
            {
                var card = deck[next++];
                pile.Add(row == column ? card | Cards.FaceUp : card);
            }
        }

        while (next < deck.Length)
        {
            Stock.Add(deck[next++]);
        }

        DrawMode = drawMode;
        Score = 0;
        Moves = 0;
        Elapsed = 0;
        Phase = GamePhase.Playing;
        Paused = false;
        Recycles = 0;
        Refused = 0;
        Deal++;
        return true;
    }

    /// <summary>Turns cards from the stock onto the waste (one or three), or, when the stock is empty, turns the waste back over.</summary>
    [Command]
    public bool Draw()
    {
        if (Phase != GamePhase.Playing || Paused || (Stock.Count == 0 && Waste.Count == 0))
        {
            Refused++;
            return false;
        }

        var before = Score;
        if (Stock.Count > 0)
        {
            var n = Stock.Count < DrawMode ? Stock.Count : DrawMode;
            for (var i = 0; i < n; i++)
            {
                var card = Stock[Stock.Count - 1];
                Stock.RemoveAt(Stock.Count - 1);
                Waste.Add(card | Cards.FaceUp);
            }

            Record(KindDraw, Piles.Stock, Piles.Waste, n, false, before);
        }
        else
        {
            var n = Waste.Count;
            for (var i = 0; i < n; i++)
            {
                var card = Waste[Waste.Count - 1];
                Waste.RemoveAt(Waste.Count - 1);
                Stock.Add(card & ~Cards.FaceUp);
            }

            Recycles++;
            AddScore(DrawMode == 1 ? -100 : -20);
            Record(KindRecycle, Piles.Waste, Piles.Stock, n, false, before);
        }

        Moves++;
        return true;
    }

    /// <summary>Moves the top <paramref name="count"/> cards of one pile onto another if the rules allow it.</summary>
    /// <param name="from">Source pile: the waste, a foundation or a tableau column.</param>
    /// <param name="to">Destination pile: a foundation or a tableau column.</param>
    /// <param name="count">How many cards, counted from the top of the source.</param>
    [Command]
    public bool Move(int from, int to, int count)
    {
        if (!CanMove(from, to, count))
        {
            Refused++;
            return false;
        }

        var before = Score;
        var delta = 0;
        if (Piles.IsFoundation(to))
        {
            delta = 10;
        }
        else if (from == Piles.Waste)
        {
            delta = 5;
        }
        else if (Piles.IsFoundation(from))
        {
            delta = -15;
        }

        Transfer(from, to, count);
        var flipped = false;
        if (Piles.IsTableau(from))
        {
            var rest = Pile(from);
            if (rest.Count > 0 && !Cards.IsFaceUp(rest[rest.Count - 1]))
            {
                rest[rest.Count - 1] |= Cards.FaceUp;
                flipped = true;
                delta += 5;
            }
        }

        AddScore(delta);
        Moves++;
        Record(KindMove, from, to, count, flipped, before);
        if (IsWon)
        {
            Phase = GamePhase.Won;
            var seconds = Elapsed / 60;
            if (seconds >= 30)
            {
                Score += 700000 / seconds;
            }
        }

        return true;
    }

    /// <summary>Sends the top card of the waste or of a tableau column to its foundation if it fits.</summary>
    [Command]
    public bool AutoMove(int from)
    {
        var target = FoundationTargetFor(from);
        if (target < 0)
        {
            Refused++;
            return false;
        }

        return Move(from, target, 1);
    }

    /// <summary>Moves every card that can go to a foundation there, one at a time, until none can. Each card is its own undo step.</summary>
    [Command]
    public bool AutoFinish()
    {
        var moved = 0;
        var again = true;
        while (again && moved < Cards.Count)
        {
            again = false;
            for (var p = Piles.Waste; p < Piles.Count && !again; p++)
            {
                var target = FoundationTargetFor(p);
                if (target >= 0 && Move(p, target, 1))
                {
                    moved++;
                    again = true;
                }
            }
        }

        if (moved == 0)
        {
            Refused++;
        }

        return moved > 0;
    }

    /// <summary>Takes back the last move exactly: cards, face-up flags, score, move counter and recycle count return to what they were.</summary>
    [Command]
    public bool Undo()
    {
        if (Phase != GamePhase.Playing || Paused || History.Count == 0)
        {
            Refused++;
            return false;
        }

        var record = History[History.Count - 1];
        History.RemoveAt(History.Count - 1);
        var kind = (int)(record & 3);
        var from = (int)((record >> 2) & 15);
        var to = (int)((record >> 6) & 15);
        var count = (int)((record >> 10) & 63);
        var flipped = ((record >> 16) & 1) != 0;
        var before = (int)((record >> 17) & 0xFFFF);
        switch (kind)
        {
            case KindDraw:
                for (var i = 0; i < count; i++)
                {
                    var card = Waste[Waste.Count - 1];
                    Waste.RemoveAt(Waste.Count - 1);
                    Stock.Add(card & ~Cards.FaceUp);
                }

                break;
            case KindRecycle:
                for (var i = 0; i < count; i++)
                {
                    var card = Stock[Stock.Count - 1];
                    Stock.RemoveAt(Stock.Count - 1);
                    Waste.Add(card | Cards.FaceUp);
                }

                Recycles--;
                break;
            default:
                if (flipped)
                {
                    var rest = Pile(from);
                    rest[rest.Count - 1] &= ~Cards.FaceUp;
                }

                Transfer(to, from, count);
                break;
        }

        Score = before;
        Moves--;
        return true;
    }

    /// <summary>Stops or restarts the timer. While paused every move is refused.</summary>
    [Command]
    public void Pause(bool paused) => Paused = paused;

    /// <summary>
    /// Sets up a game one move from the end: three suits are complete and the fourth waits for its king, with a score, move count and
    /// time to match. Tests and screens use it as a fixture.
    /// </summary>
    public void SetupNearlyWon()
    {
        for (var p = 0; p < Piles.Count; p++)
        {
            Pile(p).Clear();
        }

        History.Clear();
        for (var suit = 0; suit < 4; suit++)
        {
            var last = suit == 0 ? 11 : 12;
            for (var rank = 0; rank <= last; rank++)
            {
                Pile(Piles.Foundation + suit).Add((suit * 13) + rank + Cards.FaceUp);
            }
        }

        Tableau0.Add(12 + Cards.FaceUp);
        DrawMode = 1;
        Score = 640;
        Moves = 118;
        Elapsed = 95 * 60;
        Phase = GamePhase.Playing;
        Paused = false;
        Recycles = 0;
        Refused = 0;
        Deal++;
    }

    /// <summary>Counts the timer while a game is being played.</summary>
    protected override void OnTick()
    {
        if (Moves > 0 && Phase == GamePhase.Playing && !Paused)
        {
            Elapsed++;
        }
    }

    private void AddScore(int delta)
    {
        var next = Score + delta;
        Score = next < 0 ? 0 : next;
    }

    private void Transfer(int from, int to, int count)
    {
        var source = Pile(from);
        var target = Pile(to);
        var first = source.Count - count;
        for (var i = first; i < source.Count; i++)
        {
            target.Add(source[i]);
        }

        for (var i = 0; i < count; i++)
        {
            source.RemoveAt(source.Count - 1);
        }
    }

    private void Record(int kind, int from, int to, int count, bool flipped, int scoreBefore)
    {
        if (History.Count >= HistoryLimit)
        {
            History.RemoveAt(0);
        }

        var before = scoreBefore > 0xFFFF ? 0xFFFF : scoreBefore;
        History.Add((long)kind | ((long)from << 2) | ((long)to << 6) | ((long)count << 10) | ((flipped ? 1L : 0L) << 16) | ((long)before << 17));
    }
}
