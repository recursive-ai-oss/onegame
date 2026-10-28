[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam.Input

# SteamOrigins static class

**Namespace:** `OneGame.Steam.Input`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public static class SteamOrigins
```

Maps Steam's action origins to the kit's gamepad glyph names. Steam extends the origin enum with every new controller, so the table covers one family (the Xbox One layout, origins 114 to 147) and callers translate other pads to it first with `TranslateActionOrigin`.

## Fields

| Member | Description |
| --- | --- |
| [`XboxOneFirst`](#m-xboxonefirst) | The first origin number of the Xbox One family (its A button). |

### <a id="m-xboxonefirst"></a>XboxOneFirst

```csharp
public const int XboxOneFirst = 114;
```

The first origin number of the Xbox One family (its A button).

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`KitGlyphName(int)`](#m-kitglyphname-int) | The kit glyph name for an Xbox One family origin. |

### <a id="m-kitglyphname-int"></a>KitGlyphName(int)

```csharp
public static string? KitGlyphName(int xboxOneOrigin);
```

The kit glyph name for an Xbox One family origin.

**Parameters**

- `xboxOneOrigin` (`int`): An origin number from 114 to 147.

**Returns:** `string?` — A name accepted by `InputGlyphs.Bind` (`a`, `lb`, `dpad-up`...), or null when the origin is outside the family or has no kit picture (grips, share).
