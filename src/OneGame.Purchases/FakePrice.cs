using System.Globalization;
using System.Text;

namespace OneGame.Purchases;

/// <summary>
/// Formats and parses the prices the fake store reports. The real stores format a price for the player's locale; the fake store
/// always uses one fixed English-style format (symbol before the amount, <c>,</c> between thousands, <c>.</c> before the decimals) so a
/// scenario or a store screenshot looks the same on every machine, whatever its culture.
/// </summary>
public static class FakePrice
{
    // ISO 4217 minor units that are not 2. Anything not listed has two.
    private static readonly Dictionary<string, int> MinorDigits = new(StringComparer.Ordinal)
    {
        ["BIF"] = 0, ["CLP"] = 0, ["DJF"] = 0, ["GNF"] = 0, ["ISK"] = 0, ["JPY"] = 0, ["KMF"] = 0, ["KRW"] = 0, ["PYG"] = 0,
        ["RWF"] = 0, ["UGX"] = 0, ["VND"] = 0, ["VUV"] = 0, ["XAF"] = 0, ["XOF"] = 0, ["XPF"] = 0,
        ["BHD"] = 3, ["IQD"] = 3, ["JOD"] = 3, ["KWD"] = 3, ["LYD"] = 3, ["OMR"] = 3, ["TND"] = 3,
    };

    private static readonly Dictionary<string, string> Symbols = new(StringComparer.Ordinal)
    {
        ["USD"] = "$", ["EUR"] = "\u20AC", ["GBP"] = "\u00A3", ["JPY"] = "\u00A5", ["KRW"] = "\u20A9", ["INR"] = "\u20B9",
        ["CNY"] = "CN\u00A5", ["CAD"] = "CA$", ["AUD"] = "A$", ["MXN"] = "MX$", ["BRL"] = "R$", ["NZD"] = "NZ$",
    };

    /// <summary>How many decimal digits the currency shows (ISO 4217 minor units): 0 for JPY, 3 for KWD, otherwise 2.</summary>
    /// <param name="currency">ISO 4217 code such as <c>EUR</c>.</param>
    public static int MinorUnits(string currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        return MinorDigits.TryGetValue(currency.ToUpperInvariant(), out var d) ? d : 2;
    }

    /// <summary>
    /// The display string for an amount: <c>$4.99</c>, <c>€4.99</c>, <c>¥1,200</c>. Currencies without a known symbol show their code and a space, such as <c>CHF 4.50</c>.
    /// The amount is rounded to the currency's minor units, halves away from zero.
    /// </summary>
    /// <param name="micros">The amount in millionths of the currency (4.99 is 4,990,000).</param>
    /// <param name="currency">ISO 4217 code.</param>
    public static string Format(long micros, string currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        var code = currency.ToUpperInvariant();
        var digits = MinorUnits(code);
        var negative = micros < 0;

        // Unsigned arithmetic so long.MinValue does not overflow.
        var abs = negative ? (ulong)(-(micros + 1)) + 1UL : (ulong)micros;
        var unit = 1UL;
        for (var i = 0; i < 6 - digits; i++)
        {
            unit *= 10;
        }

        var scaled = (abs / unit) + ((abs % unit) * 2 >= unit ? 1UL : 0UL);
        var divisor = 1UL;
        for (var i = 0; i < digits; i++)
        {
            divisor *= 10;
        }

        var whole = (scaled / divisor).ToString(CultureInfo.InvariantCulture);
        var sb = new StringBuilder();
        if (negative && scaled != 0)
        {
            sb.Append('-');
        }

        if (Symbols.TryGetValue(code, out var symbol))
        {
            sb.Append(symbol);
        }
        else if (code.Length > 0)
        {
            sb.Append(code).Append(' ');
        }

        for (var i = 0; i < whole.Length; i++)
        {
            if (i > 0 && (whole.Length - i) % 3 == 0)
            {
                sb.Append(',');
            }

            sb.Append(whole[i]);
        }

        if (digits > 0)
        {
            sb.Append('.').Append((scaled % divisor).ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0'));
        }

        return sb.ToString();
    }

    /// <summary>Reads an amount such as <c>4.99</c> or <c>1200</c> (digits, an optional <c>.</c> and at most 6 decimals, culture-independent) into millionths.</summary>
    /// <param name="text">The amount.</param>
    /// <param name="micros">The amount in millionths when this returns true.</param>
    /// <returns>False when the text is not a non-negative decimal amount that fits.</returns>
    public static bool TryParseAmount(string? text, out long micros)
    {
        micros = 0;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var dot = text.IndexOf('.', StringComparison.Ordinal);
        var whole = dot < 0 ? text : text[..dot];
        var frac = dot < 0 ? string.Empty : text[(dot + 1)..];
        if (whole.Length is 0 or > 12 || frac.Length > 6 || (dot >= 0 && frac.Length == 0) || !whole.All(char.IsAsciiDigit) || !frac.All(char.IsAsciiDigit))
        {
            return false;
        }

        micros = (long.Parse(whole, CultureInfo.InvariantCulture) * 1_000_000L) + (frac.Length == 0 ? 0 : long.Parse(frac.PadRight(6, '0'), CultureInfo.InvariantCulture));
        return true;
    }
}
