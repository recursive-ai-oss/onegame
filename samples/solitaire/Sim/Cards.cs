namespace Solitaire;

/// <summary>
/// How a card is stored: a number from 0 to 51 for the card, plus <see cref="FaceUp"/> when it is turned face up. Suit is
/// <c>id / 13</c> (0 spades, 1 hearts, 2 clubs, 3 diamonds, so odd suits are red) and rank is <c>id % 13 + 1</c> (1 = ace, 13 = king).
/// </summary>
public static class Cards
{
    /// <summary>The bit that marks a card as face up.</summary>
    public const int FaceUp = 64;

    /// <summary>Number of cards in the deck.</summary>
    public const int Count = 52;

    /// <summary>The card number without the face-up bit.</summary>
    public static int Id(int card) => card & 63;

    /// <summary>The rank, 1 (ace) to 13 (king).</summary>
    public static int Rank(int card) => ((card & 63) % 13) + 1;

    /// <summary>The suit, 0 to 3.</summary>
    public static int Suit(int card) => (card & 63) / 13;

    /// <summary>True for hearts and diamonds.</summary>
    public static bool IsRed(int card) => (Suit(card) & 1) == 1;

    /// <summary>True when the card is turned face up.</summary>
    public static bool IsFaceUp(int card) => (card & FaceUp) != 0;

    /// <summary>
    /// True when <paramref name="lower"/> may rest on <paramref name="upper"/> in a tableau column: one rank lower and the other colour.
    /// </summary>
    public static bool Stacks(int upper, int lower) => Rank(upper) == Rank(lower) + 1 && IsRed(upper) != IsRed(lower);

    /// <summary>The card's name for the harness and the status line, as a rank letter then a suit letter: <c>AS</c> is the ace of spades, <c>TH</c> the ten of hearts.</summary>
    public static string Name(int card)
    {
        const string ranks = "A23456789TJQK";
        const string suits = "SHCD";
        return string.Concat(ranks[Rank(card) - 1], suits[Suit(card)]);
    }
}

/// <summary>The thirteen piles of the table, numbered so a move is three small integers.</summary>
public static class Piles
{
    /// <summary>The face-down draw pile.</summary>
    public const int Stock = 0;

    /// <summary>The face-up cards drawn from the stock.</summary>
    public const int Waste = 1;

    /// <summary>The first foundation; foundation <c>Foundation + s</c> collects suit <c>s</c>.</summary>
    public const int Foundation = 2;

    /// <summary>The first of the seven tableau columns.</summary>
    public const int Tableau = 6;

    /// <summary>Number of piles.</summary>
    public const int Count = 13;

    /// <summary>True for the four foundations.</summary>
    public static bool IsFoundation(int pile) => pile is >= Foundation and < Tableau;

    /// <summary>True for the seven tableau columns.</summary>
    public static bool IsTableau(int pile) => pile is >= Tableau and < Count;

    /// <summary>A short name for the harness and messages.</summary>
    public static string Name(int pile) => pile switch
    {
        Stock => "stock",
        Waste => "waste",
        >= Foundation and < Tableau => "foundation " + (pile - Foundation + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
        >= Tableau and < Count => "column " + (pile - Tableau + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
        _ => "none",
    };
}
