[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# TestClient class

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class TestClient : IDisposable
```

A headless game under test, driven through the same control layer a live game and an MCP client use: click, press, type, send a command, step frames, read state, capture. Every driving call returns the client, so a test reads as one chain. Get one from [`TestCluster.Client`](TestCluster.md#m-client). Calls fail with [`ControlException`](../../OneGame.Control/OneGame.Control/ControlException.md) (with a stable `Code`) when the element, entity, command or key does not exist.

**Example**

```csharp
using var cluster = TestCluster.Start(new BricksGame(), new TestOptions { Width = 540, Height = 960 });
var client = cluster.Client;
client.Click("play").Step(2);
client.Send("Court", "Launch").Step(20);
Assert.Equal("2", client.Get("Court", "Phase"));
```

## Properties

| Member | Description |
| --- | --- |
| [`Control`](#m-control) | The control layer over the game. |
| [`Game`](#m-game) | The running game, for what the control layer does not cover: its platform injector, UI tree and world. |
| [`Hash`](#m-hash) | The hash of all synced state at this tick. |
| [`Tick`](#m-tick) | The world's tick: how many simulation ticks have run since the world was created or its snapshot was taken. |

### <a id="m-control"></a>Control

```csharp
public ControlSurface Control { get; }
```

The control layer over the game.

**Type:** [`ControlSurface`](../../OneGame.Control/OneGame.Control/ControlSurface.md)

### <a id="m-game"></a>Game

```csharp
public GameInstance Game { get; }
```

The running game, for what the control layer does not cover: its platform injector, UI tree and world.

**Type:** [`GameInstance`](../../OneGame.Host/OneGame.Host/GameInstance.md)

### <a id="m-hash"></a>Hash

```csharp
public ulong Hash { get; }
```

The hash of all synced state at this tick. Two runs that do the same things have the same hash on every tick; compare it to prove a change did not alter behaviour.

**Type:** `ulong`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; }
```

The world's tick: how many simulation ticks have run since the world was created or its snapshot was taken.

**Type:** `long`

## Methods

| Member | Description |
| --- | --- |
| [`Call(string, string?)`](#m-call-string-string) | Runs any control operation by name (the same ones live control lists) and returns its JSON result. |
| [`Capture(string, float)`](#m-capture-string-float) | Renders the current state offscreen, with no window or display. |
| [`Click(string)`](#m-click-string) | Clicks a UI element by its id, with real pointer move, down and up events at the centre of its box, so hover, press and click handlers run as for a player. |
| [`ClickAt(double, double)`](#m-clickat-double-double) | Clicks a window position given in pixels from the top-left corner of the window (before UI scale). |
| [`Dispose()`](#m-dispose) |  |
| [`Find(string)`](#m-find-string) | Finds a UI element by id after bringing styles and layout up to date, or null when there is none. |
| [`Get(string, string)`](#m-get-string-string) | Reads one synced property as text (strings as they are, everything else as its JSON form). |
| [`Press(string)`](#m-press-string) | Presses and releases a key by name (`Enter`, `Escape`, `Space`, `a`). |
| [`Send(string, string, string[])`](#m-send-string-string-string) | Sends a command to an entity as a player would; it is validated and runs on the next tick, so call [`TestClient.Step()`](TestClient.md#m-step-int) before reading the result. |
| [`Step(int)`](#m-step-int) | Runs frames: each is one fixed simulation tick plus input, UI update and layout, as in the running game. |
| [`Type(string)`](#m-type-string) | Types text into the focused text input, as committed characters (what an IME or keyboard produces). |

### <a id="m-call-string-string"></a>Call(string, string?)

```csharp
public string Call(string op, string? argsJson = null);
```

Runs any control operation by name (the same ones live control lists) and returns its JSON result. Use it for what the typed calls below do not cover.

**Parameters**

- `op` (`string`): The operation, for example `state_query`.
- `argsJson` (`string?`): The operation's arguments as a JSON object, or null for none.

**Returns:** `string`

**Exceptions**

- [`ControlException`](../../OneGame.Control/OneGame.Control/ControlException.md): The operation is unknown or its arguments are wrong.

### <a id="m-capture-string-float"></a>Capture(string, float)

```csharp
public CapturedFrame Capture(string size = "320x180", float scale = 1f);
```

Renders the current state offscreen, with no window or display.

**Parameters**

- `size` (`string`): The resolution as `WIDTHxHEIGHT`, for example `1280x720`.
- `scale` (`float`): The UI scale for this capture.

**Returns:** [`CapturedFrame`](../../OneGame.Host/OneGame.Host/CapturedFrame.md) — The frame's RGBA pixels, size and the renderer that drew them; compare with [`ImageCompare`](ImageCompare.md).

### <a id="m-click-string"></a>Click(string)

```csharp
public TestClient Click(string id);
```

Clicks a UI element by its id, with real pointer move, down and up events at the centre of its box, so hover, press and click handlers run as for a player. Follow with [`TestClient.Step()`](TestClient.md#m-step-int) so the effect shows.

**Parameters**

- `id` (`string`): The element's `Id`.

**Returns:** [`TestClient`](TestClient.md)

**Exceptions**

- [`ControlException`](../../OneGame.Control/OneGame.Control/ControlException.md): No visible element has that id.

### <a id="m-clickat-double-double"></a>ClickAt(double, double)

```csharp
public TestClient ClickAt(double x, double y);
```

Clicks a window position given in pixels from the top-left corner of the window (before UI scale).

**Parameters**

- `x` (`double`)
- `y` (`double`)

**Returns:** [`TestClient`](TestClient.md)

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Implements [`IDisposable.Dispose()`](https://learn.microsoft.com/dotnet/api/system.idisposable.dispose).

### <a id="m-find-string"></a>Find(string)

```csharp
public Element? Find(string id);
```

Finds a UI element by id after bringing styles and layout up to date, or null when there is none. Read its computed box, text or state.

**Parameters**

- `id` (`string`)

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)?

### <a id="m-get-string-string"></a>Get(string, string)

```csharp
public string Get(string entity, string property);
```

Reads one synced property as text (strings as they are, everything else as its JSON form).

**Parameters**

- `entity` (`string`): The entity, as for [`TestClient.Send()`](TestClient.md#m-send-string-string-string).
- `property` (`string`): The property name; a path such as `Balls[0].Y` or `Balls.count` reaches into collections.

**Returns:** `string`

**Exceptions**

- [`ControlException`](../../OneGame.Control/OneGame.Control/ControlException.md): The entity or property does not exist.

### <a id="m-press-string"></a>Press(string)

```csharp
public TestClient Press(string key);
```

Presses and releases a key by name (`Enter`, `Escape`, `Space`, `a`). It goes through the game's input path, so focus, shortcuts and action maps see it.

**Parameters**

- `key` (`string`)

**Returns:** [`TestClient`](TestClient.md)

**Exceptions**

- [`ControlException`](../../OneGame.Control/OneGame.Control/ControlException.md): The key name is not known.

### <a id="m-send-string-string-string"></a>Send(string, string, string\[\])

```csharp
public TestClient Send(string entity, string command, params string[] args);
```

Sends a command to an entity as a player would; it is validated and runs on the next tick, so call [`TestClient.Step()`](TestClient.md#m-step-int) before reading the result.

**Parameters**

- `entity` (`string`): The entity: its type name (when there is one), an id, or `Type#n`.
- `command` (`string`): The command method's name.
- `args` (`string[]`): The arguments as text, in the command's parameter order; numbers use the invariant culture.

**Returns:** [`TestClient`](TestClient.md)

**Exceptions**

- [`ControlException`](../../OneGame.Control/OneGame.Control/ControlException.md): The entity or command does not exist or the arguments do not parse.

### <a id="m-step-int"></a>Step(int)

```csharp
public TestClient Step(int frames = 1);
```

Runs frames: each is one fixed simulation tick plus input, UI update and layout, as in the running game. Time only passes when you call this.

**Parameters**

- `frames` (`int`): How many frames to run; 1 by default.

**Returns:** [`TestClient`](TestClient.md)

### <a id="m-type-string"></a>Type(string)

```csharp
public TestClient Type(string text);
```

Types text into the focused text input, as committed characters (what an IME or keyboard produces).

**Parameters**

- `text` (`string`)

**Returns:** [`TestClient`](TestClient.md)
