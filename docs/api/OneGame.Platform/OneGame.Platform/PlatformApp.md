[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# PlatformApp class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class PlatformApp : IDisposable
```

The entry point of the platform layer: owns the connection to the windowing system, the windows, the gamepads and the single event queue. Use it from one thread (normally the one that runs the game loop).

## Properties

| Member | Description |
| --- | --- |
| [`Backend`](#m-backend) | Which backend was chosen. |
| [`Clipboard`](#m-clipboard) | Text on the system clipboard. |
| [`DroppedEvents`](#m-droppedevents) | How many events were dropped because the queue was full (the game stopped polling). |
| [`Gamepads`](#m-gamepads) | Gamepad devices and rumble. |
| [`Injector`](#m-injector) | Synthetic input. |
| [`Input`](#m-input) | What is currently held. |
| [`Options`](#m-options) | The options this application was created with. |
| [`QuitRequested`](#m-quitrequested) | True once a quit was requested. |
| [`Windows`](#m-windows) | The open windows. |

### <a id="m-backend"></a>Backend

```csharp
public PlatformBackendKind Backend { get; }
```

Which backend was chosen.

**Type:** [`PlatformBackendKind`](PlatformBackendKind.md)

### <a id="m-clipboard"></a>Clipboard

```csharp
public PlatformClipboard Clipboard { get; }
```

Text on the system clipboard.

**Type:** [`PlatformClipboard`](PlatformClipboard.md)

### <a id="m-droppedevents"></a>DroppedEvents

```csharp
public int DroppedEvents { get; }
```

How many events were dropped because the queue was full (the game stopped polling).

**Type:** `int`

### <a id="m-gamepads"></a>Gamepads

```csharp
public GamepadManager Gamepads { get; }
```

Gamepad devices and rumble.

**Type:** [`GamepadManager`](GamepadManager.md)

### <a id="m-injector"></a>Injector

```csharp
public InputInjector Injector { get; }
```

Synthetic input. Events it makes are queued exactly like real ones.

**Type:** [`InputInjector`](InputInjector.md)

### <a id="m-input"></a>Input

```csharp
public InputState Input { get; }
```

What is currently held.

**Type:** [`InputState`](InputState.md)

### <a id="m-options"></a>Options

```csharp
public PlatformOptions Options { get; }
```

The options this application was created with.

**Type:** [`PlatformOptions`](PlatformOptions.md)

### <a id="m-quitrequested"></a>QuitRequested

```csharp
public bool QuitRequested { get; }
```

True once a quit was requested.

**Type:** `bool`

### <a id="m-windows"></a>Windows

```csharp
public IReadOnlyList<Window> Windows { get; }
```

The open windows.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`Window`](Window.md)&gt;

## Methods

| Member | Description |
| --- | --- |
| [`Create(PlatformOptions?)`](#m-create-platformoptions) | Connects to the windowing system. |
| [`CreateWindow(WindowOptions?)`](#m-createwindow-windowoptions) | Creates a window. |
| [`Dispose()`](#m-dispose) | Closes every window and disconnects. |
| [`NativeCommand(string, string?)`](#m-nativecommand-string-string) | Runs a command that only one backend understands, for the harness and tests: on iOS `describe`, `orientation`, `text_insert`, `text_mark`, `text_unmark` and `text_delete` (see docs/interfaces/ios.md). |
| [`PumpEvents(int)`](#m-pumpevents-int) | Reads everything the operating system and the gamepads have to say and queues it. |
| [`RequestQuit()`](#m-requestquit) | Asks the application to quit; the game sees [`PlatformEventKind.QuitRequested`](PlatformEventKind.md#m-quitrequested). |
| [`TryGetEvent(out PlatformEvent)`](#m-trygetevent-platformevent) | Takes the next queued event, oldest first. |

### <a id="m-create-platformoptions"></a>Create(PlatformOptions?)

```csharp
public static PlatformApp Create(PlatformOptions? options = null);
```

Connects to the windowing system.

**Parameters**

- `options` ([`PlatformOptions`](PlatformOptions.md)?)

**Returns:** [`PlatformApp`](PlatformApp.md)

**Exceptions**

- [`PlatformException`](PlatformException.md): The requested backend is not available here.

### <a id="m-createwindow-windowoptions"></a>CreateWindow(WindowOptions?)

```csharp
public Window CreateWindow(WindowOptions? options = null);
```

Creates a window.

**Parameters**

- `options` ([`WindowOptions`](WindowOptions.md)?)

**Returns:** [`Window`](Window.md)

**Exceptions**

- [`PlatformException`](PlatformException.md): The window system refused.

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Closes every window and disconnects.

### <a id="m-nativecommand-string-string"></a>NativeCommand(string, string?)

```csharp
public string NativeCommand(string action, string? argument = null);
```

Runs a command that only one backend understands, for the harness and tests: on iOS `describe`, `orientation`, `text_insert`, `text_mark`, `text_unmark` and `text_delete` (see docs/interfaces/ios.md). Returns the backend's answer.

**Parameters**

- `action` (`string`)
- `argument` (`string?`)

**Returns:** `string`

**Exceptions**

- [`PlatformException`](PlatformException.md): The backend has no such command.

### <a id="m-pumpevents-int"></a>PumpEvents(int)

```csharp
public void PumpEvents(int timeoutMilliseconds = 0);
```

Reads everything the operating system and the gamepads have to say and queues it. Call it once per frame.

**Parameters**

- `timeoutMilliseconds` (`int`): 0 returns at once; a positive value waits up to that long for the first event.

### <a id="m-requestquit"></a>RequestQuit()

```csharp
public void RequestQuit();
```

Asks the application to quit; the game sees [`PlatformEventKind.QuitRequested`](PlatformEventKind.md#m-quitrequested).

### <a id="m-trygetevent-platformevent"></a>TryGetEvent(out PlatformEvent)

```csharp
public bool TryGetEvent(out PlatformEvent platformEvent);
```

Takes the next queued event, oldest first.

**Parameters**

- `platformEvent` ([`PlatformEvent`](PlatformEvent.md)): The event when one was waiting.

**Returns:** `bool` — False when the queue is empty.
