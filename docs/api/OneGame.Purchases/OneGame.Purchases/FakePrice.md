[API reference](../../index.md) › [OneGame.Purchases](../index.md) › OneGame.Purchases

# FakePrice static class

**Namespace:** `OneGame.Purchases`  
**Package:** [`RecursiveAI.OneGame.Purchases`](../index.md)

```csharp
public static class FakePrice
```

Formats and parses the prices the fake store reports. The real stores format a price for the player's locale; the fake store always uses one fixed English-style format (symbol before the amount, `,` between thousands, `.` before the decimals) so a scenario or a store screenshot looks the same on every machine, whatever its culture.

## Methods

| Member | Description |
| --- | --- |
| [`Format(long, string)`](#m-format-long-string) | The display string for an amount: `$4.99`, `€4.99`, `¥1,200`. |
| [`MinorUnits(string)`](#m-minorunits-string) | How many decimal digits the currency shows (ISO 4217 minor units): 0 for JPY, 3 for KWD, otherwise 2. |
| [`TryParseAmount(string?, out long)`](#m-tryparseamount-string-long) | Reads an amount such as `4.99` or `1200` (digits, an optional `.` and at most 6 decimals, culture-independent) into millionths. |

### <a id="m-format-long-string"></a>Format(long, string)

```csharp
public static string Format(long micros, string currency);
```

The display string for an amount: `$4.99`, `€4.99`, `¥1,200`. Currencies without a known symbol show their code and a space, such as `CHF 4.50`. The amount is rounded to the currency's minor units, halves away from zero.

**Parameters**

- `micros` (`long`): The amount in millionths of the currency (4.99 is 4,990,000).
- `currency` (`string`): ISO 4217 code.

**Returns:** `string`

### <a id="m-minorunits-string"></a>MinorUnits(string)

```csharp
public static int MinorUnits(string currency);
```

How many decimal digits the currency shows (ISO 4217 minor units): 0 for JPY, 3 for KWD, otherwise 2.

**Parameters**

- `currency` (`string`): ISO 4217 code such as `EUR`.

**Returns:** `int`

### <a id="m-tryparseamount-string-long"></a>TryParseAmount(string?, out long)

```csharp
public static bool TryParseAmount(string? text, out long micros);
```

Reads an amount such as `4.99` or `1200` (digits, an optional `.` and at most 6 decimals, culture-independent) into millionths.

**Parameters**

- `text` (`string?`): The amount.
- `micros` (`long`): The amount in millionths when this returns true.

**Returns:** `bool` — False when the text is not a non-negative decimal amount that fits.
