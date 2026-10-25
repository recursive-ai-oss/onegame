# UI input and inspection

## In: input the UI consumes

One entry point, one struct. A platform adapter translates its own events into `UiEvent` and calls `ui.Dispatch(e)`; the harness uses the same call for injected input, and a recording of `UiEvent`s replays a session.

```csharp
bool handled = ui.Dispatch(UiEvent.PointerMove(x, y));
```

All positions are screen pixels. `Dispatch` first brings the UI up to date (bindings, style, layout), so hit tests see current boxes.

| Event (factory) | Fields | Notes |
|---|---|---|
| `PointerMove(x, y)` | position | Updates hover (`:hover`, enter/leave), then bubbles to the element under the pointer. |
| `PointerDown(x, y, button)` / `PointerUp(...)` | position, `Button` 0 primary, 1 secondary, 2 middle | Button 0 sets `:active` and focuses the nearest focusable ancestor (or clears focus). A click goes to the nearest common ancestor of the press and release targets. |
| `PointerLeftWindow()` | none | Clears hover. |
| `Wheel(x, y, dx, dy)` | position, pixel deltas (positive `dy` reveals content below) | Handlers first, then the nearest scrollable ancestor that can scroll that way. Platform adapters convert line or notch scrolling to pixels. |
| `KeyDown(key, modifiers, repeat)` / `KeyUp(key, modifiers)` | `Key` is the DOM `KeyboardEvent.key` string: `"Enter"`, `"Escape"`, `"Tab"`, `" "`, `"ArrowLeft"`, `"a"`... | Sent to the focused element, bubbling. Unhandled defaults: Tab / Shift+Tab move focus, Enter and Space click the focused element, arrows move focus (`ArrowKeysNavigate`), Escape is a Cancel. |
| `TextInput(text)` | committed text (IME results included) | Sent to the focused element, bubbling. A text field handles it; nothing else does. |
| `Composition(text, cursorStart, cursorEnd)` | the input method's pre-edit string (empty ends it), the highlighted part | Sent to the focused element, bubbling. A text field shows the pre-edit text, usually underlined, until the finished text arrives as `TextInput`. |
| `Navigate(direction)` | `Up Down Left Right Next Previous` | Gamepad d-pad / stick (as discrete steps; repeat is the adapter's job) and bumpers for Next/Previous. The focused element sees it first (a slider can take Left/Right), otherwise focus moves. |
| `Activate()` | none | The confirm button: clicks the focused element. |
| `Cancel()` | none | The back button: bubbles from the focused element (or the root) as a `Cancel` event. |

Return value: true when a handler or a default action consumed the event, so an adapter can pass unconsumed input to the game (for example, a click on empty UI that should fire a weapon).

**Text fields.** An element that takes typed text sets `AcceptsText = true` and may keep `CaretArea` (screen pixels) current. `ui.TextInputTarget` is the focused element when it accepts text, `ui.TextInputArea` its caret or bounds; the host uses them to switch the platform's input method on and place its candidate window.

**Element events.** Elements register handlers with `On(kind, handler)` or the sugar (`OnClick`, `OnPointerEnter`, `OnFocus`, `OnCancel`...). A handler returns true to stop bubbling and suppress the default action. Disabled elements (and their descendants) receive no input; `:disabled` matches them.

**Focus.** Elements are focusable with `Focusable()`. Tab order is positive `TabIndex` first, then document order. Directional moves pick the focusable element nearest in that direction by edge distance, favoring ones that overlap the current row or column; nothing moves at an edge. `TrapFocus()` confines Next/Previous/directional moves to a subtree while it is shown (dialogs). Focusing scrolls scrollable ancestors so the element is visible. `ui.Focus(el)`, `ui.MoveFocus(dir)`, `ui.Focused`.

**Cursor.** `ui.Cursor` is the `cursor` style of the hovered element; the platform adapter applies it.

## Out: inspection for the harness

All of it works on a headless `UiTree`; none of it needs a renderer.

- `ui.DumpJson(from?, allStyles, children)`: the tree as JSON. Per element: `kind`, `id`, `classes`, `states` (`hover active focus disabled hidden focusable`), `text`, `bounds` (relative to the parent), `screen` (absolute, scroll applied), `scroll` (offset and content size, scrolling elements only), `style` (computed values in CSS spelling; properties equal to their initial value are omitted unless `allStyles`), `children`. The document root carries `width`, `height`, `scale` and `focused`.
- `ui.Find(id)`: first element with that id. `ui.Query(selector)`: all matches in document order, using the style-rule selector syntax (`Kind`, `#id`, `.class`, `:hover :focus :active :disabled :root :first-child :last-child :nth-child(odd|even|N)`, descendant and `>` combinators).
- `ui.HitTest(x, y)`: the element a pointer event at that position would target (clipping, scroll, transforms, z-order, `pointer-events`, `visibility` honored).
- `ui.Invoke(el)`: click an element without a pointer (fails on disabled ones). Together with `Dispatch` this is how a harness presses buttons by id: `ui.Invoke(ui.Find("buy")!)`.
- `el.Computed.Get("flex-direction")`: one computed property. `ComputedStyle.PropertyNames` lists them.
- `ui.DisplayList.ToText()`: what would be drawn (see `ui-render.md`).

The harness maps these onto its own verbs (query, patch, click, capture); the UI exposes no transport.

## Authoring contract (bindings)

Bindings are lambdas evaluated once per `Frame()` or `Update()`, parents before children, before styles resolve: `BindText`, `BindClass`, `BindVisible`, `BindEnabled`, `BindStyle`, `OnUpdate`, `BindChildren` (`Ui.Each`). Each compares its result with the last one and touches the element only on a change, so a frame where nothing changed costs the lambda calls and allocates nothing as long as the lambdas themselves do not allocate: return cached strings, or use `BindText(source, format)` where `format` runs only when the value changed. Nothing is re-evaluated outside a frame; a game state change shows up in the next frame.
