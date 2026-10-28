[API reference](../index.md) › OneGame.Platform

# OneGame.Platform

Windows, input, controllers and the app lifecycle per operating system.

**Package:** `RecursiveAI.OneGame.Platform`

```sh
dotnet add package RecursiveAI.OneGame.Platform
```

## OneGame.Platform

| Type | Kind | Description |
| --- | --- | --- |
| [`ActionBinding`](OneGame.Platform/ActionBinding.md) | struct | What one slot of an action listens to: a single source or a composite (two keys making an axis, WASD making a vector, a stick). |
| [`ActionContext`](OneGame.Platform/ActionContext.md) | class | Declares the actions of one context. |
| [`ActionKind`](OneGame.Platform/ActionKind.md) | enum | What shape of value an action produces. |
| [`ActionMap`](OneGame.Platform/ActionMap.md) | class | A game's actions: named buttons, axes and 2D vectors, with default bindings on keyboard, mouse, gamepad and touch, grouped in contexts a screen switches on and off. |
| [`ActionSource`](OneGame.Platform/ActionSource.md) | class | Another producer of action values next to the devices: Steam Input, the harness, a replay. |
| [`ActionVector`](OneGame.Platform/ActionVector.md) | struct | A 2D action value. |
| [`AndroidActivityHost`](OneGame.Platform/AndroidActivityHost.md) | class | The program side of an Android native activity. |
| [`AppLifecycleState`](OneGame.Platform/AppLifecycleState.md) | enum | What [`PlatformEventKind.AppLifecycle`](OneGame.Platform/PlatformEventKind.md#m-applifecycle) reports. |
| [`Bind`](OneGame.Platform/Bind.md) | static class | Short ways to write bindings: `Bind.Key(Key.Space)`, `Bind.Wasd`, `Bind.LeftStick`. |
| [`BindingShape`](OneGame.Platform/BindingShape.md) | enum | The arrangement of an [`ActionBinding`](OneGame.Platform/ActionBinding.md). |
| [`BindingsLoadResult`](OneGame.Platform/BindingsLoadResult.md) | record | What [`ActionMap.LoadText()`](OneGame.Platform/ActionMap.md#m-loadtext-string) did. |
| [`BrowserFiles`](OneGame.Platform/BrowserFiles.md) | static class | The browser's persistent file area, a flat map of names to bytes kept in IndexedDB. |
| [`BrowserPage`](OneGame.Platform/BrowserPage.md) | static class | The page-level services a browser host needs around the platform backend: the animation-frame loop. |
| [`BrowserWebSocket`](OneGame.Platform/BrowserWebSocket.md) | class | A binary WebSocket made with the browser's own `WebSocket`. |
| [`ClipboardSelection`](OneGame.Platform/ClipboardSelection.md) | enum | Which system selection a clipboard call talks to. |
| [`ConflictPolicy`](OneGame.Platform/ConflictPolicy.md) | enum | What to do with other actions that already use a source being assigned. |
| [`EdgeInsets`](OneGame.Platform/EdgeInsets.md) | struct | Distances in pixels from each edge of a window. |
| [`GamepadAxis`](OneGame.Platform/GamepadAxis.md) | enum | An analog axis on the standard gamepad layout. |
| [`GamepadButton`](OneGame.Platform/GamepadButton.md) | enum | A button on the standard gamepad layout. |
| [`GamepadManager`](OneGame.Platform/GamepadManager.md) | class | Finds gamepads, maps them onto the standard layout, and plays rumble. |
| [`GamepadMapping`](OneGame.Platform/GamepadMapping.md) | class | One entry of a gamepad mapping database in the community text format: a device GUID, a name, and comma-separated `target:source` pairs such as `a:b0,leftx:a0,dpup:h0.1`. |
| [`GamepadMappingDatabase`](OneGame.Platform/GamepadMappingDatabase.md) | class | A set of [`GamepadMapping`](OneGame.Platform/GamepadMapping.md) entries looked up by device GUID. |
| [`GamepadState`](OneGame.Platform/GamepadState.md) | class | Current state of one gamepad on the standard layout. |
| [`InputAction`](OneGame.Platform/InputAction.md) | class | One declared action and its state for the current frame. |
| [`InputInjector`](OneGame.Platform/InputInjector.md) | class | Makes input events without hardware: for the harness, tests and agents driving a running game. |
| [`InputSource`](OneGame.Platform/InputSource.md) | struct | One physical control an action can listen to: a key, a mouse button, a pad button or axis, the wheel, or a touch region. |
| [`InputSourceKind`](OneGame.Platform/InputSourceKind.md) | enum | What kind of control an [`InputSource`](OneGame.Platform/InputSource.md) reads. |
| [`InputState`](OneGame.Platform/InputState.md) | class | What is currently held: keys, pointer, buttons and gamepads. |
| [`IosAppHost`](OneGame.Platform/IosAppHost.md) | class | The iOS app: UIKit on the process's main thread, the game on a thread of its own. |
| [`Key`](OneGame.Platform/Key.md) | enum | A physical key, named by its position on a US keyboard, not by the character the current layout gives it. |
| [`KeyModifiers`](OneGame.Platform/KeyModifiers.md) | enum | Modifier keys held while an event happened. |
| [`MouseButton`](OneGame.Platform/MouseButton.md) | enum | A mouse or pointer button. |
| [`NativeWindowHandle`](OneGame.Platform/NativeWindowHandle.md) | struct | The raw handles a graphics API needs to create a presentable surface for a window. |
| [`NativeWindowKind`](OneGame.Platform/NativeWindowKind.md) | enum | The kind of native window system handles in a [`NativeWindowHandle`](OneGame.Platform/NativeWindowHandle.md). |
| [`PlatformApp`](OneGame.Platform/PlatformApp.md) | class | The entry point of the platform layer: owns the connection to the windowing system, the windows, the gamepads and the single event queue. |
| [`PlatformBackendKind`](OneGame.Platform/PlatformBackendKind.md) | enum | Which windowing system a [`PlatformApp`](OneGame.Platform/PlatformApp.md) talks to. |
| [`PlatformClipboard`](OneGame.Platform/PlatformClipboard.md) | class | Text on the system clipboard. |
| [`PlatformEvent`](OneGame.Platform/PlatformEvent.md) | struct | One input or lifecycle event. |
| [`PlatformEventKind`](OneGame.Platform/PlatformEventKind.md) | enum | What happened. |
| [`PlatformException`](OneGame.Platform/PlatformException.md) | class | Thrown when the platform layer cannot do what was asked. |
| [`PlatformModule`](OneGame.Platform/PlatformModule.md) | static class | Identifies the Platform assembly; a placeholder until its first feature lands. |
| [`PlatformOptions`](OneGame.Platform/PlatformOptions.md) | record | Options for [`PlatformApp.Create()`](OneGame.Platform/PlatformApp.md#m-create-platformoptions). |
| [`RebindRequest`](OneGame.Platform/RebindRequest.md) | record | A rebinding waiting for the player's next input. |
| [`RebindResult`](OneGame.Platform/RebindResult.md) | record | The outcome of a rebinding capture. |
| [`RunningOn`](OneGame.Platform/RunningOn.md) | static class | Which kind of system the game is running on. |
| [`TouchRegion`](OneGame.Platform/TouchRegion.md) | enum | The part of the window a touch binding listens to. |
| [`Window`](OneGame.Platform/Window.md) | class | An application window. |
| [`WindowOptions`](OneGame.Platform/WindowOptions.md) | record | Options for [`PlatformApp.CreateWindow()`](OneGame.Platform/PlatformApp.md#m-createwindow-windowoptions). |
