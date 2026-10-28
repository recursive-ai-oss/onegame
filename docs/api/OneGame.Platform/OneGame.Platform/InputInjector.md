[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# InputInjector class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public sealed class InputInjector
```

Makes input events without hardware: for the harness, tests and agents driving a running game. Events enter the same queue and state as real ones, so game code cannot tell them apart.

## Methods

| Member | Description |
| --- | --- |
| [`CloseRequest(int)`](#m-closerequest-int) | Reports that the OS asked a window to close. |
| [`Composition(int, string, int?, int?)`](#m-composition-int-string-int-int) | Reports an input method's pre-edit string, as a real IME would while the user composes. |
| [`GamepadAxis(int, GamepadAxis, float)`](#m-gamepadaxis-int-gamepadaxis-float) | Moves a gamepad axis. |
| [`GamepadButton(int, GamepadButton, bool)`](#m-gamepadbutton-int-gamepadbutton-bool) | Presses or releases a button on a gamepad. |
| [`GamepadConnect(string)`](#m-gamepadconnect-string) | Connects a virtual gamepad that already speaks the standard layout, and returns its id. |
| [`GamepadDisconnect(int)`](#m-gamepaddisconnect-int) | Removes a virtual gamepad. |
| [`Key(int, Key, bool)`](#m-key-int-key-bool) | Presses or releases a physical key. |
| [`KeyPress(int, Key)`](#m-keypress-int-key) | Presses and releases a key. |
| [`Lifecycle(AppLifecycleState)`](#m-lifecycle-applifecyclestate) | Reports that the app moved to the foreground or background, or that memory is short. |
| [`PointerButton(int, MouseButton, bool)`](#m-pointerbutton-int-mousebutton-bool) | Presses or releases a pointer button at the current pointer position. |
| [`PointerMove(int, float, float)`](#m-pointermove-int-float-float) | Moves the pointer to a position in window pixels. |
| [`RelativeMotion(int, float, float)`](#m-relativemotion-int-float-float) | Reports raw pointer motion in pixels, as sent while the cursor is locked. |
| [`Tap(int, float, float, int)`](#m-tap-int-float-float-int) | Touches and lifts one finger at a position. |
| [`Text(int, string)`](#m-text-int-string) | Types text, as if produced by the keyboard layout. |
| [`TouchCancel(int, int)`](#m-touchcancel-int-int) | Takes a finger away without a lift, as the system does when it claims a gesture. |
| [`TouchDown(int, int, float, float)`](#m-touchdown-int-int-float-float) | Puts a finger down at a position in window pixels. |
| [`TouchMove(int, int, float, float)`](#m-touchmove-int-int-float-float) | Moves a finger that is down. |
| [`TouchUp(int, int, float, float)`](#m-touchup-int-int-float-float) | Lifts a finger. |
| [`Wheel(int, float, float)`](#m-wheel-int-float-float) | Turns the wheel; amounts are in notches. |

### <a id="m-closerequest-int"></a>CloseRequest(int)

```csharp
public void CloseRequest(int windowId);
```

Reports that the OS asked a window to close.

**Parameters**

- `windowId` (`int`)

### <a id="m-composition-int-string-int-int"></a>Composition(int, string, int?, int?)

```csharp
public void Composition(int windowId, string preedit, int? cursorStart = default, int? cursorEnd = default);
```

Reports an input method's pre-edit string, as a real IME would while the user composes. An empty string ends the composition.

**Parameters**

- `windowId` (`int`): The window.
- `preedit` (`string`): The whole pre-edit text.
- `cursorStart` (`int?`): UTF-16 index where the highlighted part starts, or -1 for no cursor. Defaults to the end of the text.
- `cursorEnd` (`int?`): UTF-16 index where it ends. Defaults to `cursorStart`.

### <a id="m-gamepadaxis-int-gamepadaxis-float"></a>GamepadAxis(int, GamepadAxis, float)

```csharp
public void GamepadAxis(int gamepadId, GamepadAxis axis, float value);
```

Moves a gamepad axis. Values are used as given, with no dead zone.

**Parameters**

- `gamepadId` (`int`)
- `axis` ([`GamepadAxis`](GamepadAxis.md))
- `value` (`float`)

### <a id="m-gamepadbutton-int-gamepadbutton-bool"></a>GamepadButton(int, GamepadButton, bool)

```csharp
public void GamepadButton(int gamepadId, GamepadButton button, bool down);
```

Presses or releases a button on a gamepad.

**Parameters**

- `gamepadId` (`int`)
- `button` ([`GamepadButton`](GamepadButton.md))
- `down` (`bool`)

### <a id="m-gamepadconnect-string"></a>GamepadConnect(string)

```csharp
public int GamepadConnect(string name = "Virtual Gamepad");
```

Connects a virtual gamepad that already speaks the standard layout, and returns its id.

**Parameters**

- `name` (`string`)

**Returns:** `int`

### <a id="m-gamepaddisconnect-int"></a>GamepadDisconnect(int)

```csharp
public void GamepadDisconnect(int gamepadId);
```

Removes a virtual gamepad.

**Parameters**

- `gamepadId` (`int`)

### <a id="m-key-int-key-bool"></a>Key(int, Key, bool)

```csharp
public void Key(int windowId, Key key, bool down);
```

Presses or releases a physical key. Modifiers come from the modifier keys currently held.

**Parameters**

- `windowId` (`int`)
- `key` ([`Key`](Key.md))
- `down` (`bool`)

### <a id="m-keypress-int-key"></a>KeyPress(int, Key)

```csharp
public void KeyPress(int windowId, Key key);
```

Presses and releases a key.

**Parameters**

- `windowId` (`int`)
- `key` ([`Key`](Key.md))

### <a id="m-lifecycle-applifecyclestate"></a>Lifecycle(AppLifecycleState)

```csharp
public void Lifecycle(AppLifecycleState state);
```

Reports that the app moved to the foreground or background, or that memory is short.

**Parameters**

- `state` ([`AppLifecycleState`](AppLifecycleState.md))

### <a id="m-pointerbutton-int-mousebutton-bool"></a>PointerButton(int, MouseButton, bool)

```csharp
public void PointerButton(int windowId, MouseButton button, bool down);
```

Presses or releases a pointer button at the current pointer position.

**Parameters**

- `windowId` (`int`)
- `button` ([`MouseButton`](MouseButton.md))
- `down` (`bool`)

### <a id="m-pointermove-int-float-float"></a>PointerMove(int, float, float)

```csharp
public void PointerMove(int windowId, float x, float y);
```

Moves the pointer to a position in window pixels.

**Parameters**

- `windowId` (`int`)
- `x` (`float`)
- `y` (`float`)

### <a id="m-relativemotion-int-float-float"></a>RelativeMotion(int, float, float)

```csharp
public void RelativeMotion(int windowId, float deltaX, float deltaY);
```

Reports raw pointer motion in pixels, as sent while the cursor is locked.

**Parameters**

- `windowId` (`int`)
- `deltaX` (`float`)
- `deltaY` (`float`)

### <a id="m-tap-int-float-float-int"></a>Tap(int, float, float, int)

```csharp
public void Tap(int windowId, float x, float y, int touchId = 0);
```

Touches and lifts one finger at a position.

**Parameters**

- `windowId` (`int`)
- `x` (`float`)
- `y` (`float`)
- `touchId` (`int`)

### <a id="m-text-int-string"></a>Text(int, string)

```csharp
public void Text(int windowId, string text);
```

Types text, as if produced by the keyboard layout. `text` must not be empty.

**Parameters**

- `windowId` (`int`)
- `text` (`string`)

### <a id="m-touchcancel-int-int"></a>TouchCancel(int, int)

```csharp
public void TouchCancel(int windowId, int touchId);
```

Takes a finger away without a lift, as the system does when it claims a gesture.

**Parameters**

- `windowId` (`int`)
- `touchId` (`int`)

### <a id="m-touchdown-int-int-float-float"></a>TouchDown(int, int, float, float)

```csharp
public void TouchDown(int windowId, int touchId, float x, float y);
```

Puts a finger down at a position in window pixels.

**Parameters**

- `windowId` (`int`)
- `touchId` (`int`)
- `x` (`float`)
- `y` (`float`)

### <a id="m-touchmove-int-int-float-float"></a>TouchMove(int, int, float, float)

```csharp
public void TouchMove(int windowId, int touchId, float x, float y);
```

Moves a finger that is down.

**Parameters**

- `windowId` (`int`)
- `touchId` (`int`)
- `x` (`float`)
- `y` (`float`)

### <a id="m-touchup-int-int-float-float"></a>TouchUp(int, int, float, float)

```csharp
public void TouchUp(int windowId, int touchId, float x, float y);
```

Lifts a finger.

**Parameters**

- `windowId` (`int`)
- `touchId` (`int`)
- `x` (`float`)
- `y` (`float`)

### <a id="m-wheel-int-float-float"></a>Wheel(int, float, float)

```csharp
public void Wheel(int windowId, float deltaX, float deltaY);
```

Turns the wheel; amounts are in notches.

**Parameters**

- `windowId` (`int`)
- `deltaX` (`float`)
- `deltaY` (`float`)
