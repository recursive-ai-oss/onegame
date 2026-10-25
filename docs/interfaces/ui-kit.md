# UI component kit

`OneGame.UI.Kit` (namespace `OneGame.UI.Kit`) is the base set of controls on top of `OneGame.UI`. It depends on `OneGame.UI` and OneFont only. The guide for people building screens is `docs/ui-guide.md`; this file is the contract for code that consumes or inspects the kit.

## Entry points

- `new KitHost(tree, theme?)`: adds the default theme sheet and a tooltip layer to the tree. Per frame call `host.Advance(seconds)` (tooltip delays; the kit reads no clock), send input through `host.Dispatch(e)` (it forwards to `tree.Dispatch` and tells `host.Glyphs` which device is in use), then `tree.Frame()`.
- `host.SetTheme(KitTheme.Create(tokens))` swaps the theme in place (`UiTree.ReplaceStyleSheet`): same elements and boxes, new colors and sizes in the display list.
- `UiKit` is the factory class: `Button`, `Toggle`, `RadioGroup<T>`, `Slider` (int and float), `NumberStepper`, `Progress`, `Card`, `Frame`, `Tabs`, `ScrollView`, `VirtualList<T>`, `TextList`, `Table<T>`, `Dropdown<T>`, `Dialog`, `ConfirmDialog`, `TextInput`, `InputGlyph`, plus `host.Tooltip(element, text)` and `FocusHelpers` (`FocusGroup`, `AsDefaultFocus`, `FocusDefault`).
- Components are controlled: they read state through `Func<T>` and report edits through `Action<T>`. A refused edit shows the old value on the next frame.
- Art (`KitArt.Assets`) is exposed as SVG bytes with nine-slice margins; the game registers them in its `ArtAtlas`. The kit has no atlas dependency.

## Inspection: ids, roles, values

Every kit element has `Role` (an ARIA-style name) and often `Value` (the human-readable current value). Both appear in `DumpJson` as `role` and `value`. A part-built control (role button, checkbox, radio, tab, option, columnheader, combobox) also reports `text`: the visible text of its parts, so a scenario `ui: {id: buy, text: Buy}` reads a Button's label, a Toggle's label, a Tab's title. Password fields never expose their text.

| Component | Root role | Parts (ids are `<id>/…`) |
|---|---|---|
| Button | `button` (value = label; inspection `text` = label) | none |
| Toggle | `checkbox` (value `on`/`off`) | none |
| RadioGroup | `radiogroup` (value = selected label) | `radio`, one per choice |
| Slider | `slider` (value = number) | thumb and track are unnamed |
| NumberStepper | `spinbutton` (value = number) | two `button` parts |
| Progress | `progressbar` (value = number) | none |
| Card / Frame | `card` (value = title) / `group` | none |
| Tabs | `group` | `tablist`, `tab` per page (class `selected`), `tabpanel` |
| ScrollView | `scrollview` | `scrollbar` tracks |
| VirtualList / TextList | `listbox` | `<id>/row/N` (N = item index, role `row`, value = N, classes `selected`, `odd`) |
| Table | `table` | `<id>/header/N` (`columnheader`, classes `sorted`, `asc`/`desc`), `<id>/body` (`listbox`), `<id>/row/N` |
| Dropdown | `combobox` (value = chosen label; class `open` while open) | `<id>/head`, `<id>/popup` (`listbox`), `<id>/option/N` |
| Dialog | `dialog` (value = title) | its buttons |
| TextInput | `textbox` (value = text, null when password) | none |
| Tooltip | `tooltip` | lives in `host.TooltipLayer` |
| InputGlyph | `img` (value = `device:name`) | none |

Rows are recycled: `RealizedRowCount` is the number of row elements that exist, not the item count.

## Theme tokens

`KitTokens` (records with `init` properties; `KitTokens.Dark` default, `KitTokens.Light`). The theme sheet declares them on `:root` as custom properties and every kit rule reads them with `var(...)`:

colors `--kit-bg --kit-panel --kit-raised --kit-field --kit-text --kit-text-dim --kit-accent --kit-accent-hover --kit-accent-text --kit-danger --kit-border --kit-focus --kit-hover --kit-active --kit-select --kit-select-text --kit-scrim --kit-scrollbar`; sizes `--kit-font-size --kit-radius --kit-focus-width`.

Derived by `KitTheme.Create` (not `KitTokens` members): `--kit-accent-active --kit-danger-hover --kit-danger-active --kit-danger-text --kit-control-border --kit-disabled-fill --kit-disabled-border --kit-disabled-text`.
A token that carries text or an indicator and would miss WCAG 2.x AA against what it sits on is moved toward white or black just far enough (`KitContrast.Ensure`); passing tokens are untouched.
The rules for each state of every component are in `KitTheme`; disabled never uses opacity.

## Contrast lint

`ThemeLint.Check(KitTokens | UiTree | Func<string, Color?>)` returns a `ThemeLintReport` (`Passed`, `Checked`, `Failures` worst first, `Checks`). Each `ContrastCheck` names the pair (`Foreground`/`Background` tokens, `ForegroundColor`/`BackgroundColor`, `Ratio`, `Required`, `Kind` = `text` | `disabled-text` | `indicator`). Text needs 4.5:1; disabled text and indicators (focus ring, control outline, accent fill, scroll thumb) 3:1. A token that is not defined is reported as `missing` with ratio 0.
Faces: control operation `theme_lint` (MCP), dev option `--lint-theme` on a game, and `onegame lint-theme [--json]` (exit 1 when a pair fails). `KitContrast` has `Luminance`, `Ratio`, `Mix`, `Ensure`.

A game restyles by passing its own `KitTokens`, by adding a sheet after the theme that overrides a token on `:root`, or by adding rules for the `kit-*` classes (`kit-button`, `kit-row`, …). Rules added by the game survive a theme swap.

## Hooks the kit added to `OneGame.UI`

- `Element.Role`, `Element.Value`: inspection only; they do not affect style or layout. Printed by `DumpJson`.
- `UiTree.CapturePointer(el)` / `ReleasePointer()` / `PointerCaptor`: while captured, moves and the release go to the captor even outside its box (slider drag, text selection, scrollbar thumb). The press itself still hit-tests normally.
- `UiTree.RequestFocus(el)`: focuses at the end of the current `Update`. Use it from bindings, where `Focus` would re-enter the update.
- `UiTree.TextStyleOf(el)`: the resolved text style the engine measures with, for caret geometry.
- `UiTree.ReplaceStyleSheet(old, new)`: swaps a sheet at its cascade position.
- `UiTree.TryGetColorToken(name, out color)`: a color token as the cascade resolved it on the root (what `ThemeLint.Check(tree)` reads).
- `background-tint` (`StyleBuilder.BackgroundTint`): multiplies the background image by a color; white single-color art follows a theme token.
- `:disabled` matches an element that is disabled or inside a disabled ancestor (as CSS does for controls in a disabled fieldset).
- `UiTree.Invoke` sends `Click` with `Button = -1` (an activation without a pointer); pointer clicks carry the real button.
- Directional focus ignores candidates that overlap the source along the direction of travel when a wholly-ahead candidate exists, so Right from a wide table does not land on its own header cells.

## Limits

No double-click, no type-ahead in the dropdown, no word-wise caret movement, no blinking caret, no horizontal list virtualization. The text input sets `AcceptsText`, keeps `CaretArea` (= `CaretScreenRect`) current and draws input-method composition (`UiEvent.Composition`, `Preedit`) at the caret. The clipboard is `IClipboard`; `UiKit.DefaultClipboard` is in memory until a windowed `GameInstance` with a clipboard replaces it with the system one.
