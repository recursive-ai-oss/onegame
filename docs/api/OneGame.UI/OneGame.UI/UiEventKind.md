[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# UiEventKind enum

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public enum UiEventKind : int
```

What happened. The first group is input fed to the UI; the second is delivered to elements; some kinds are both.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-pointermove"></a>`PointerMove` | `0` | The pointer moved to (X, Y) in screen pixels. Input and element event. |
| <a id="m-pointerdown"></a>`PointerDown` | `1` | A pointer button went down at (X, Y). Input and element event. |
| <a id="m-pointerup"></a>`PointerUp` | `2` | A pointer button was released at (X, Y). Input and element event. |
| <a id="m-pointerleave"></a>`PointerLeave` | `3` | Input: the pointer left the window. Element event: the pointer left this element. |
| <a id="m-pointerenter"></a>`PointerEnter` | `4` | Element event: the pointer entered this element (not sent to ancestors that were already hovered). |
| <a id="m-wheel"></a>`Wheel` | `5` | Scroll wheel or touchpad scroll at (X, Y) by (DeltaX, DeltaY) pixels. Positive Y scrolls content up (reveals what is below). Input and element event. |
| <a id="m-keydown"></a>`KeyDown` | `6` | A key went down; [`UiEvent.Key`](UiEvent.md#m-key) is a DOM key name. Input and element event (sent to the focused element). |
| <a id="m-keyup"></a>`KeyUp` | `7` | A key was released. Input and element event. |
| <a id="m-textinput"></a>`TextInput` | `8` | Committed text from the keyboard or an input method. Input and element event (sent to the focused element). |
| <a id="m-composition"></a>`Composition` | `9` | Text an input method is still composing (a pre-edit string). Input and element event (sent to the focused element). [`UiEvent.Text`](UiEvent.md#m-text) is the whole pre-edit text, empty when the composition ended, and [`UiEvent.CursorStart`](UiEvent.md#m-cursorstart)/[`UiEvent.CursorEnd`](UiEvent.md#m-cursorend) mark the highlighted part. The finished text arrives as [`UiEventKind.TextInput`](UiEventKind.md#m-textinput). |
| <a id="m-navigate"></a>`Navigate` | `10` | A directional or tab-like focus move from a gamepad, d-pad or keyboard. Input; the focused element sees it first and may handle it. |
| <a id="m-activate"></a>`Activate` | `11` | The confirm button (gamepad A, Enter): clicks the focused element. Input. |
| <a id="m-cancel"></a>`Cancel` | `12` | The back button (gamepad B, Escape). Input; delivered to the focused element and its ancestors. |
| <a id="m-click"></a>`Click` | `13` | Element event: a press and release on the same element, or an activation of the focused one. |
| <a id="m-focus"></a>`Focus` | `14` | Element event: the element gained focus. |
| <a id="m-blur"></a>`Blur` | `15` | Element event: the element lost focus. |
