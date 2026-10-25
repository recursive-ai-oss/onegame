using System.Numerics;

namespace Solitaire.Views;

/// <summary>Where one card is drawn: its pile, its place in the pile, and its corner on the board.</summary>
internal struct CardSlot
{
    /// <summary>True when the card is in some pile (a damaged save may leave cards out).</summary>
    public bool Present;

    public int Pile;
    public int Index;
    public bool FaceUp;
    public float X;
    public float Y;
    public int Z;
}

/// <summary>
/// The size and place of everything on the board, computed from the board's pixel size alone. The view reads the game's piles and asks
/// this where each card goes; the game knows nothing of it.
/// </summary>
internal sealed class TableGeometry
{
    private const float GapRatio = 0.16f;
    private const float AspectRatio = 1.4f;

    private readonly float[] _pileX = new float[Piles.Count];
    private readonly float[] _pileY = new float[Piles.Count];

    public float BoardWidth { get; private set; }

    public float BoardHeight { get; private set; }

    public float CardWidth { get; private set; }

    public float CardHeight { get; private set; }

    public float Margin { get; private set; }

    public float DownStep { get; private set; }

    public float UpStep { get; private set; }

    public float WasteStep { get; private set; }

    public float TopY { get; private set; }

    public float TableauY { get; private set; }

    public float ColumnPitch { get; private set; }

    /// <summary>Sets the board size and recomputes the layout.</summary>
    public void Resize(float width, float height)
    {
        BoardWidth = width;
        BoardHeight = height;
        Margin = MathF.Max(8, MathF.Round(MathF.Min(width, height) * 0.03f));
        var rowGap = Margin * 1.2f;
        var byWidth = (width - (2 * Margin)) / (7 + (6 * GapRatio));
        var byHeight = (height - (2 * Margin) - rowGap) / ((2 * AspectRatio) + 1.7f);
        CardWidth = MathF.Floor(MathF.Max(24, MathF.Min(byWidth, byHeight)));
        CardHeight = MathF.Round(CardWidth * AspectRatio);
        ColumnPitch = CardWidth + MathF.Max(4, (width - (2 * Margin) - (7 * CardWidth)) / 6);
        var left = MathF.Round((width - (CardWidth + (6 * ColumnPitch))) / 2);
        TopY = Margin;
        TableauY = Margin + CardHeight + rowGap;
        DownStep = MathF.Round(CardHeight * 0.12f);
        UpStep = MathF.Round(CardHeight * 0.27f);
        WasteStep = MathF.Round(CardWidth * 0.24f);
        for (var c = 0; c < 7; c++)
        {
            _pileX[Piles.Tableau + c] = left + (c * ColumnPitch);
            _pileY[Piles.Tableau + c] = TableauY;
        }

        _pileX[Piles.Stock] = left;
        _pileX[Piles.Waste] = left + ColumnPitch;
        for (var f = 0; f < 4; f++)
        {
            _pileX[Piles.Foundation + f] = left + ((3 + f) * ColumnPitch);
        }

        for (var p = 0; p < Piles.Tableau; p++)
        {
            _pileY[p] = TopY;
        }
    }

    /// <summary>The rectangle a pile's first card occupies.</summary>
    public (float X, float Y, float W, float H) BaseRect(int pile) => (_pileX[pile], _pileY[pile], CardWidth, CardHeight);

    /// <summary>How much a column's steps are scaled so that its last card ends inside the board.</summary>
    public float ColumnSqueeze(IReadOnlyList<int> cards)
    {
        var need = 0f;
        for (var i = 0; i + 1 < cards.Count; i++)
        {
            need += Cards.IsFaceUp(cards[i]) ? UpStep : DownStep;
        }

        var room = BoardHeight - Margin - TableauY - CardHeight;
        return need <= room || need <= 0 ? 1f : MathF.Max(0.25f, room / need);
    }

    /// <summary>The corner of card number <paramref name="index"/> in a pile.</summary>
    public Vector2 CardCorner(Table table, int pile, int index)
    {
        var cards = table.Pile(pile);
        var x = _pileX[pile];
        var y = _pileY[pile];
        if (Piles.IsTableau(pile))
        {
            var squeeze = ColumnSqueeze(cards);
            for (var i = 0; i < index && i < cards.Count; i++)
            {
                y += (Cards.IsFaceUp(cards[i]) ? UpStep : DownStep) * squeeze;
            }
        }
        else if (pile == Piles.Waste && table.DrawMode == 3)
        {
            // Draw three shows the last three cards fanned to the right; earlier ones lie under the first.
            var fromTop = cards.Count - 1 - index;
            var shown = Math.Min(3, cards.Count);
            x += fromTop < 3 ? WasteStep * (shown - 1 - fromTop) : 0;
        }

        return new Vector2(x, y);
    }

    /// <summary>Fills in the slot of every card for the current piles.</summary>
    public void Arrange(Table table, CardSlot[] slots)
    {
        Array.Clear(slots);
        for (var pile = 0; pile < Piles.Count; pile++)
        {
            var cards = table.Pile(pile);
            for (var i = 0; i < cards.Count; i++)
            {
                var id = Cards.Id(cards[i]);
                if (id >= Cards.Count)
                {
                    continue;
                }

                var corner = CardCorner(table, pile, i);
                slots[id] = new CardSlot { Present = true, Pile = pile, Index = i, FaceUp = Cards.IsFaceUp(cards[i]), X = corner.X, Y = corner.Y, Z = 10 + i };
            }
        }
    }

    /// <summary>The rectangle that covers a pile and all its cards, for focus and pointer hits.</summary>
    public (float X, float Y, float W, float H) Footprint(Table table, int pile)
    {
        var (x, y, w, h) = BaseRect(pile);
        var count = table.Pile(pile).Count;
        if (Piles.IsTableau(pile) && count > 1)
        {
            h += CardCorner(table, pile, count - 1).Y - y;
        }
        else if (pile == Piles.Waste && table.DrawMode == 3)
        {
            w += WasteStep * 2;
        }

        return (x, y, w, h);
    }

    /// <summary>
    /// The card under a board position as a pile and an index; an empty pile whose footprint holds the point as (pile, -1); or (-1, -1).
    /// </summary>
    public (int Pile, int Index) Hit(Table table, float x, float y)
    {
        for (var pile = 0; pile < Piles.Count; pile++)
        {
            var (px, py, pw, ph) = Footprint(table, pile);
            if (x < px || x >= px + pw || y < py || y >= py + ph)
            {
                continue;
            }

            var cards = table.Pile(pile);
            for (var i = cards.Count - 1; i >= 0; i--)
            {
                var c = CardCorner(table, pile, i);
                if (x >= c.X && x < c.X + CardWidth && y >= c.Y && y < c.Y + CardHeight)
                {
                    return (pile, i);
                }
            }

            return (pile, -1);
        }

        return (-1, -1);
    }
}
