[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# ActionMap class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public sealed class ActionMap
```

A game's actions: named buttons, axes and 2D vectors, with default bindings on keyboard, mouse, gamepad and touch, grouped in contexts a screen switches on and off. View code asks [`ActionMap.Pressed()`](ActionMap.md#m-pressed-string), [`ActionMap.Held()`](ActionMap.md#m-held-string), [`ActionMap.Released()`](ActionMap.md#m-released-string), [`ActionMap.Value()`](ActionMap.md#m-value-string) and [`ActionMap.Vector()`](ActionMap.md#m-vector-string) and sends commands; actions never touch synced state. A hosted game gets one as `GameInstance.Actions`, declared in `GameDefinition.DefineActions`, and it is updated once a frame. Outside the host, call [`ActionMap.Observe()`](ActionMap.md#m-observe-platformevent) for every event and [`ActionMap.Update()`](ActionMap.md#m-update-inputstate-int-int) once per frame.

## Constructors

| Member | Description |
| --- | --- |
| [`ActionMap()`](#m-ctor) | Creates an empty map. |

### <a id="m-ctor"></a>ActionMap()

```csharp
public ActionMap();
```

Creates an empty map. It has one extra source, [`ActionMap.Harness`](ActionMap.md#m-harness), for scenarios and live control.

## Fields

| Member | Description |
| --- | --- |
| [`FileHeader`](#m-fileheader) | The first line of a bindings file. |

### <a id="m-fileheader"></a>FileHeader

```csharp
public const string FileHeader = "onegame-bindings 1";
```

The first line of a bindings file.

**Type:** `string`

## Properties

| Member | Description |
| --- | --- |
| [`Actions`](#m-actions) | The declared actions in declaration order. |
| [`Contexts`](#m-contexts) | The contexts in declaration order. |
| [`FollowsScreens`](#m-followsscreens) | True when some context names screens, so the host switches contexts as screens open. |
| [`Harness`](#m-harness) | The source the harness drives (the `action:` scenario step and the `actions` control operation). |
| [`KeyboardCaptured`](#m-keyboardcaptured) | True while key sources are ignored because a text field has the keyboard (the host sets this). |
| [`Rebinding`](#m-rebinding) | The pending rebinding capture, or null. |
| [`Version`](#m-version) | Increases whenever a binding changes, so a list or glyph table knows to refresh. |
| [`this[string]`](#m-item-string) | Looks an action up. |

### <a id="m-actions"></a>Actions

```csharp
public IReadOnlyList<InputAction> Actions { get; }
```

The declared actions in declaration order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`InputAction`](InputAction.md)&gt;

### <a id="m-contexts"></a>Contexts

```csharp
public IReadOnlyList<ActionContext> Contexts { get; }
```

The contexts in declaration order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ActionContext`](ActionContext.md)&gt;

### <a id="m-followsscreens"></a>FollowsScreens

```csharp
public bool FollowsScreens { get; }
```

True when some context names screens, so the host switches contexts as screens open.

**Type:** `bool`

### <a id="m-harness"></a>Harness

```csharp
public ActionSource Harness { get; }
```

The source the harness drives (the `action:` scenario step and the `actions` control operation).

**Type:** [`ActionSource`](ActionSource.md)

### <a id="m-keyboardcaptured"></a>KeyboardCaptured

```csharp
public bool KeyboardCaptured { get; set; }
```

True while key sources are ignored because a text field has the keyboard (the host sets this).

**Type:** `bool`

### <a id="m-rebinding"></a>Rebinding

```csharp
public RebindRequest? Rebinding { get; }
```

The pending rebinding capture, or null.

**Type:** [`RebindRequest`](RebindRequest.md)?

### <a id="m-version"></a>Version

```csharp
public int Version { get; }
```

Increases whenever a binding changes, so a list or glyph table knows to refresh.

**Type:** `int`

### <a id="m-item-string"></a>this\[string\]

```csharp
public InputAction this[string name] { get; }
```

Looks an action up. A typo throws instead of silently reading zero.

**Parameters**

- `name` (`string`): The action's name.

**Type:** [`InputAction`](InputAction.md)

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): No such action.

## Events

| Member | Description |
| --- | --- |
| [`RebindFinished`](#m-rebindfinished) | Raised when a rebinding capture finishes or is cancelled. |
| [`Updating`](#m-updating) | Raised at the start of [`ActionMap.Update()`](ActionMap.md#m-update-inputstate-int-int), so a source that polls a device (Steam Input) can set its values for this frame. |

### <a id="m-rebindfinished"></a>RebindFinished

```csharp
public event Action<RebindResult>? RebindFinished;
```

Raised when a rebinding capture finishes or is cancelled.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`RebindResult`](RebindResult.md)&gt;?

### <a id="m-updating"></a>Updating

```csharp
public event Action? Updating;
```

Raised at the start of [`ActionMap.Update()`](ActionMap.md#m-update-inputstate-int-int), so a source that polls a device (Steam Input) can set its values for this frame.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action)?

## Methods

| Member | Description |
| --- | --- |
| [`Activate(string)`](#m-activate-string) | Switches one context on. |
| [`ActivateForScreen(string?)`](#m-activateforscreen-string) | Follows the open screen: contexts that name screens are on only while one of them is open; contexts that name none stay on. |
| [`BeginRebind(string, int, int, ConflictPolicy)`](#m-beginrebind-string-int-int-conflictpolicy) | Waits for the player's next key, mouse button, pad button, pad axis push or wheel turn and puts it into a binding slot. |
| [`CancelRebind()`](#m-cancelrebind) | Stops a pending capture without changing anything. |
| [`Context(string, Action<ActionContext>)`](#m-context-string-action-actioncontext) | Declares a context and its actions. |
| [`CreateSource(string)`](#m-createsource-string) | Makes an extra source other code can drive: Steam Input, a replay, an on-screen stick. |
| [`Deactivate(string)`](#m-deactivate-string) | Switches one context off. |
| [`DefaultPath(string)`](#m-defaultpath-string) | A per-user bindings file for a game: `bindings.txt` under the user's local application data folder in a folder named after the game. |
| [`FindConflicts(string, InputSource)`](#m-findconflicts-string-inputsource) | Other actions in the same context that use `source`. |
| [`Held(string)`](#m-held-string) | True while the button is down. |
| [`IsContextActive(string)`](#m-iscontextactive-string) | Whether a context is switched on. |
| [`Load(string)`](#m-load-string) | Reads a bindings file. |
| [`LoadText(string)`](#m-loadtext-string) | Resets every action to its declared bindings, then applies a bindings text. |
| [`Observe(in PlatformEvent)`](#m-observe-platformevent) | Takes one platform event in: latches button presses shorter than a frame, sums wheel notches and feeds a pending rebinding capture. |
| [`Pressed(string)`](#m-pressed-string) | True when the button went down this frame. |
| [`Released(string)`](#m-released-string) | True when the button went up this frame. |
| [`RemoveBinding(string, int)`](#m-removebinding-string-int) | Removes one binding slot. |
| [`ResetBindings(string?)`](#m-resetbindings-string) | Puts one action, or every action, back to the declared bindings. |
| [`Save(string)`](#m-save-string) | Writes [`ActionMap.ToText()`](ActionMap.md#m-totext) to a file, creating the folder, through a temporary file so a crash never leaves half a file. |
| [`SetActive(string[])`](#m-setactive-string) | Switches exactly the named contexts on. |
| [`SetBinding(string, int, ActionBinding, ConflictPolicy)`](#m-setbinding-string-int-actionbinding-conflictpolicy) | Replaces one binding slot. |
| [`ToText()`](#m-totext) | Writes the bindings that differ from the declared ones as text (one line per changed action). |
| [`TryGet(string, out InputAction)`](#m-tryget-string-inputaction) | Looks an action up without throwing. |
| [`Update(InputState, int, int)`](#m-update-inputstate-int-int) | Computes every action's state for this frame from the devices and the extra sources. |
| [`Value(string)`](#m-value-string) | A button's strength, an axis's value, or a vector's x. |
| [`Vector(string)`](#m-vector-string) | A vector action's value. |

### <a id="m-activate-string"></a>Activate(string)

```csharp
public void Activate(string context);
```

Switches one context on.

**Parameters**

- `context` (`string`): The context's name.

### <a id="m-activateforscreen-string"></a>ActivateForScreen(string?)

```csharp
public void ActivateForScreen(string? screen);
```

Follows the open screen: contexts that name screens are on only while one of them is open; contexts that name none stay on.

**Parameters**

- `screen` (`string?`): The open screen's name.

### <a id="m-beginrebind-string-int-int-conflictpolicy"></a>BeginRebind(string, int, int, ConflictPolicy)

```csharp
public void BeginRebind(string action, int slot = 0, int part = 0, ConflictPolicy policy = 0);
```

Waits for the player's next key, mouse button, pad button, pad axis push or wheel turn and puts it into a binding slot. Escape cancels. While waiting, [`ActionMap.Observe()`](ActionMap.md#m-observe-platformevent) swallows input events so the UI does not act on them. Progress and the result come through [`ActionMap.Rebinding`](ActionMap.md#m-rebinding) and [`ActionMap.RebindFinished`](ActionMap.md#m-rebindfinished).

**Parameters**

- `action` (`string`): The action's name.
- `slot` (`int`): The slot to replace, or the binding count to add one.
- `part` (`int`): For a pair or quad binding, which source: 0..1 or 0..3 (left, right, up, down).
- `policy` ([`ConflictPolicy`](ConflictPolicy.md)): What to do with other actions using the captured source.

### <a id="m-cancelrebind"></a>CancelRebind()

```csharp
public void CancelRebind();
```

Stops a pending capture without changing anything.

### <a id="m-context-string-action-actioncontext"></a>Context(string, Action&lt;ActionContext&gt;)

```csharp
public ActionMap Context(string name, Action<ActionContext> build);
```

Declares a context and its actions.

**Parameters**

- `name` (`string`): Letters, digits and underscores, starting with a letter.
- `build` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`ActionContext`](ActionContext.md)&gt;): Declares the context's actions.

**Returns:** [`ActionMap`](ActionMap.md) — This map.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The name is taken or invalid, or an action is declared twice.

### <a id="m-createsource-string"></a>CreateSource(string)

```csharp
public ActionSource CreateSource(string name);
```

Makes an extra source other code can drive: Steam Input, a replay, an on-screen stick.

**Parameters**

- `name` (`string`): A label for reports.

**Returns:** [`ActionSource`](ActionSource.md) — The source.

### <a id="m-deactivate-string"></a>Deactivate(string)

```csharp
public void Deactivate(string context);
```

Switches one context off.

**Parameters**

- `context` (`string`): The context's name.

### <a id="m-defaultpath-string"></a>DefaultPath(string)

```csharp
public static string DefaultPath(string gameName);
```

A per-user bindings file for a game: `bindings.txt` under the user's local application data folder in a folder named after the game.

**Parameters**

- `gameName` (`string`): The game's name.

**Returns:** `string`

### <a id="m-findconflicts-string-inputsource"></a>FindConflicts(string, InputSource)

```csharp
public IReadOnlyList<string> FindConflicts(string action, InputSource source);
```

Other actions in the same context that use `source`.

**Parameters**

- `action` (`string`): The action being bound (excluded).
- `source` ([`InputSource`](InputSource.md)): The source.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>` — The names of the conflicting actions.

### <a id="m-held-string"></a>Held(string)

```csharp
public bool Held(string action);
```

True while the button is down.

**Parameters**

- `action` (`string`): The action's name.

**Returns:** `bool`

### <a id="m-iscontextactive-string"></a>IsContextActive(string)

```csharp
public bool IsContextActive(string context);
```

Whether a context is switched on.

**Parameters**

- `context` (`string`): The context's name.

**Returns:** `bool`

### <a id="m-load-string"></a>Load(string)

```csharp
public BindingsLoadResult Load(string path);
```

Reads a bindings file. A missing file is not an error: the declared bindings stay.

**Parameters**

- `path` (`string`): The file.

**Returns:** [`BindingsLoadResult`](BindingsLoadResult.md)

### <a id="m-loadtext-string"></a>LoadText(string)

```csharp
public BindingsLoadResult LoadText(string text);
```

Resets every action to its declared bindings, then applies a bindings text. Lines for unknown actions or with bindings the action cannot take are skipped and reported; nothing throws on bad input. Text over 64 KiB is rejected whole.

**Parameters**

- `text` (`string`): The text [`ActionMap.ToText()`](ActionMap.md#m-totext) wrote.

**Returns:** [`BindingsLoadResult`](BindingsLoadResult.md)

### <a id="m-observe-platformevent"></a>Observe(in PlatformEvent)

```csharp
public bool Observe(in PlatformEvent e);
```

Takes one platform event in: latches button presses shorter than a frame, sums wheel notches and feeds a pending rebinding capture.

**Parameters**

- `e` ([`PlatformEvent`](PlatformEvent.md)): The event.

**Returns:** `bool` — True when a pending rebinding capture swallowed the event, so nothing else should handle it.

### <a id="m-pressed-string"></a>Pressed(string)

```csharp
public bool Pressed(string action);
```

True when the button went down this frame.

**Parameters**

- `action` (`string`): The action's name.

**Returns:** `bool`

### <a id="m-released-string"></a>Released(string)

```csharp
public bool Released(string action);
```

True when the button went up this frame.

**Parameters**

- `action` (`string`): The action's name.

**Returns:** `bool`

### <a id="m-removebinding-string-int"></a>RemoveBinding(string, int)

```csharp
public void RemoveBinding(string action, int slot);
```

Removes one binding slot.

**Parameters**

- `action` (`string`): The action's name.
- `slot` (`int`): The slot.

### <a id="m-resetbindings-string"></a>ResetBindings(string?)

```csharp
public void ResetBindings(string? action = null);
```

Puts one action, or every action, back to the declared bindings.

**Parameters**

- `action` (`string?`): The action's name, or null for all.

### <a id="m-save-string"></a>Save(string)

```csharp
public void Save(string path);
```

Writes [`ActionMap.ToText()`](ActionMap.md#m-totext) to a file, creating the folder, through a temporary file so a crash never leaves half a file.

**Parameters**

- `path` (`string`): The file; [`ActionMap.DefaultPath()`](ActionMap.md#m-defaultpath-string) gives the usual place.

### <a id="m-setactive-string"></a>SetActive(string\[\])

```csharp
public void SetActive(params string[] contexts);
```

Switches exactly the named contexts on. Actions of the others read as not held, with no edges.

**Parameters**

- `contexts` (`string[]`): Context names.

### <a id="m-setbinding-string-int-actionbinding-conflictpolicy"></a>SetBinding(string, int, ActionBinding, ConflictPolicy)

```csharp
public IReadOnlyList<string> SetBinding(string action, int slot, ActionBinding binding, ConflictPolicy policy = 0);
```

Replaces one binding slot.

**Parameters**

- `action` (`string`): The action's name.
- `slot` (`int`): The slot, or the binding count to add one.
- `binding` ([`ActionBinding`](ActionBinding.md)): The new binding; its shape must suit the action's kind.
- `policy` ([`ConflictPolicy`](ConflictPolicy.md)): What to do with other actions using the same sources.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>` — The conflicting actions found, before the policy was applied.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): Wrong shape for the kind, or an unknown action.
- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The slot does not exist.

### <a id="m-totext"></a>ToText()

```csharp
public string ToText();
```

Writes the bindings that differ from the declared ones as text (one line per changed action). Same state gives the same text.

**Returns:** `string`

### <a id="m-tryget-string-inputaction"></a>TryGet(string, out InputAction)

```csharp
public bool TryGet(string name, out InputAction action);
```

Looks an action up without throwing.

**Parameters**

- `name` (`string`): The action's name.
- `action` ([`InputAction`](InputAction.md)): The action.

**Returns:** `bool` — True when it exists.

### <a id="m-update-inputstate-int-int"></a>Update(InputState, int, int)

```csharp
public void Update(InputState input, int width, int height);
```

Computes every action's state for this frame from the devices and the extra sources. Call once per frame, after the frame's events were observed.

**Parameters**

- `input` ([`InputState`](InputState.md)): The platform's input state.
- `width` (`int`): The window width in pixels, for touch regions.
- `height` (`int`): The window height in pixels, for touch regions.

### <a id="m-value-string"></a>Value(string)

```csharp
public float Value(string action);
```

A button's strength, an axis's value, or a vector's x.

**Parameters**

- `action` (`string`): The action's name.

**Returns:** `float`

### <a id="m-vector-string"></a>Vector(string)

```csharp
public ActionVector Vector(string action);
```

A vector action's value.

**Parameters**

- `action` (`string`): The action's name.

**Returns:** [`ActionVector`](ActionVector.md)
