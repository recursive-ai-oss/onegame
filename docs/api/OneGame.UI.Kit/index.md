[API reference](../index.md) › OneGame.UI.Kit

# OneGame.UI.Kit

Base UI component kit: buttons, tables, lists, tabs, text input and more, themed with tokens.

**Package:** `RecursiveAI.OneGame.UI.Kit`

```sh
dotnet add package RecursiveAI.OneGame.UI.Kit
```

## OneGame.UI.Kit

| Type | Kind | Description |
| --- | --- | --- |
| [`ButtonKind`](OneGame.UI.Kit/ButtonKind.md) | enum | The look of a [`UiKit.Button()`](OneGame.UI.Kit/UiKit.md#m-button-string-action-string-buttonkind). |
| [`Choice<T>`](OneGame.UI.Kit/Choice-1.md) | record | One option of a radio group or drop-down: the value it stands for and the text shown. |
| [`ColumnWidth`](OneGame.UI.Kit/ColumnWidth.md) | struct | How wide a table column is: a fixed length, or a share of the space the fixed columns leave. |
| [`ContrastCheck`](OneGame.UI.Kit/ContrastCheck.md) | record | One foreground and background pair of the theme and how it fares against WCAG 2.x AA. |
| [`DialogView`](OneGame.UI.Kit/DialogView.md) | class | A modal dialog (kind `Dialog`, role `dialog`): a full-screen scrim over everything with a panel in the middle. |
| [`FocusHelpers`](OneGame.UI.Kit/FocusHelpers.md) | static class | Focus helpers: a focus group that keeps arrow keys inside a set of controls, a default focus target, and a way to give a screen its first focus. |
| [`IClipboard`](OneGame.UI.Kit/IClipboard.md) | interface | Where copied text goes. |
| [`InputDevice`](OneGame.UI.Kit/InputDevice.md) | enum | The kind of device the player last used, which decides what an [`InputGlyphs`](OneGame.UI.Kit/InputGlyphs.md) glyph shows. |
| [`InputGlyphs`](OneGame.UI.Kit/InputGlyphs.md) | class | Maps game actions to the button each device uses, and tracks which device the player used last, so a prompt such as "Press (A) to buy" can show the right picture. |
| [`KitArt`](OneGame.UI.Kit/KitArt.md) | static class | The SVG art that ships with the kit: a frame for [`UiKit.Frame()`](OneGame.UI.Kit/UiKit.md#m-frame-string-float-element), sort arrows, a drop-down chevron, a check mark, and input glyphs for keyboard, mouse and gamepad. |
| [`KitArtAsset`](OneGame.UI.Kit/KitArtAsset.md) | struct | One piece of vector art the kit uses: the asset id, the SVG source, and the nine-slice inset (0 for none) in the SVG's own pixels. |
| [`KitContrast`](OneGame.UI.Kit/KitContrast.md) | static class | Color contrast as WCAG 2.x defines it (Understanding SC 1.4.3 and 1.4.11): relative luminance of sRGB, and the ratio `(L1 + 0.05) / (L2 + 0.05)`. |
| [`KitHost`](OneGame.UI.Kit/KitHost.md) | class | Installs the kit into a [`UiTree`](../OneGame.UI/OneGame.UI/UiTree.md): adds the theme, owns the tooltip layer and the input glyph bindings, and keeps the kit's notion of time. |
| [`KitTheme`](OneGame.UI.Kit/KitTheme.md) | static class | Builds the style sheet that gives every kit component its look. |
| [`KitTokens`](OneGame.UI.Kit/KitTokens.md) | record | The values the default theme is made of. |
| [`ListView<T>`](OneGame.UI.Kit/ListView-1.md) | class | A virtualized list (kind `List`, role `listbox`): however many items the source holds, only the rows in view (plus a spare) exist as elements, and they are recycled as the list scrolls. |
| [`MemoryClipboard`](OneGame.UI.Kit/MemoryClipboard.md) | class | A clipboard that lives in memory, shared by everything that was given the same instance. |
| [`ScrollView`](OneGame.UI.Kit/ScrollView.md) | class | A scrolling container with scroll bars (kind `ScrollView`, role `scrollview`). |
| [`TabPage`](OneGame.UI.Kit/TabPage.md) | record | A page of [`UiKit.Tabs()`](OneGame.UI.Kit/UiKit.md#m-tabs-ireadonlylist-tabpage-func-int-action-int-string): the tab's title and the content shown while it is selected. |
| [`TableColumn<T>`](OneGame.UI.Kit/TableColumn-1.md) | class | One column of a [`TableView<T>`](OneGame.UI.Kit/TableView-1.md): a title, a width, and how a cell shows an item. |
| [`TableView<T>`](OneGame.UI.Kit/TableView-1.md) | class | A table (kind `Table`, role `table`) with a header row and a virtualized body, so 5,000 rows cost about as much as 30. |
| [`TextInputView`](OneGame.UI.Kit/TextInputView.md) | class | A single-line text field (kind `TextInput`, role `textbox`). |
| [`ThemeLint`](OneGame.UI.Kit/ThemeLint.md) | static class | Checks that a theme's colors stay legible. |
| [`ThemeLintReport`](OneGame.UI.Kit/ThemeLintReport.md) | record | The outcome of [`ThemeLint`](OneGame.UI.Kit/ThemeLint.md). |
| [`UiKit`](OneGame.UI.Kit/UiKit.md) | static class | The component kit: factory methods that return ready-made [`Element`](../OneGame.UI/OneGame.UI/Element.md) trees for the controls a game screen needs. |
