[API reference](../index.md) › OneGame.Text

# OneGame.Text

Text for the UI: font library, shaping, measurement and the glyph atlas.

**Package:** `RecursiveAI.OneGame.Text`

```sh
dotnet add package RecursiveAI.OneGame.Text
```

## OneGame.Text

| Type | Kind | Description |
| --- | --- | --- |
| [`ArtAtlasSizes`](OneGame.Text/ArtAtlasSizes.md) | class | Lets the UI ask an [`ArtAtlas`](../OneGame.Atlas/OneGame.Atlas/ArtAtlas.md) how big its art is: assign it to [`UiTree.ArtSizes`](../OneGame.UI/OneGame.UI/UiTree.md#m-artsizes). |
| [`FontLibrary`](OneGame.Text/FontLibrary.md) | class | The fonts of a game: faces loaded from asset bytes, grouped by family, resolved per `font-family` list, weight and slant into a fallback chain. |
| [`FontNotFoundException`](OneGame.Text/FontNotFoundException.md) | class | A font could not be found: the library holds no faces, or a face id is unknown. |
| [`FontTextMeasurer`](OneGame.Text/FontTextMeasurer.md) | class | The UI's text measurer, backed by the font library: shapes with OneFont (bidi, fallback, ligatures, kerning), breaks lines at the opportunities of UAX \#14 and measures with the same advances the glyphs are drawn with. |
| [`GlyphAtlas`](OneGame.Text/GlyphAtlas.md) | class | Glyph coverage atlas. |
| [`GlyphInfo`](OneGame.Text/GlyphInfo.md) | struct | Where one rasterized glyph sits in the atlas, and how to place it. |
| [`GlyphLimits`](OneGame.Text/GlyphLimits.md) | class | Budgets for glyph rasterization. |
| [`GlyphQuad`](OneGame.Text/GlyphQuad.md) | struct | One textured rectangle of a glyph, in screen pixels, ready for the renderer. |
| [`PositionedGlyph`](OneGame.Text/PositionedGlyph.md) | struct | One glyph with its pen position, ready to look up in the glyph atlas. |
| [`TextLimitException`](OneGame.Text/TextLimitException.md) | class | A text or font request broke a budget: nothing was shaped or allocated for it. |
| [`TextQuadBuilder`](OneGame.Text/TextQuadBuilder.md) | class | Turns the text runs of a display list into glyph quads. |
