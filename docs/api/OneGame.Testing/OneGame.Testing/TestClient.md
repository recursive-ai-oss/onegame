[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# TestClient class

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class TestClient : IDisposable
```

A headless game under test, driven through the control layer like a live one. One node for now.

## Properties

| Member | Description |
| --- | --- |
| [`Control`](#m-control) | The control layer over the game. |
| [`Game`](#m-game) | The running game. |
| [`Hash`](#m-hash) | The world's state hash. |
| [`Tick`](#m-tick) | The world's tick. |

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

The running game.

**Type:** [`GameInstance`](../../OneGame.Host/OneGame.Host/GameInstance.md)

### <a id="m-hash"></a>Hash

```csharp
public ulong Hash { get; }
```

The world's state hash.

**Type:** `ulong`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; }
```

The world's tick.

**Type:** `long`

## Methods

| Member | Description |
| --- | --- |
| [`Call(string, string?)`](#m-call-string-string) | Runs a control operation and returns its JSON. |
| [`Capture(string, float)`](#m-capture-string-float) | Renders the current state offscreen. |
| [`Click(string)`](#m-click-string) | Clicks an element by id with pointer events. |
| [`ClickAt(double, double)`](#m-clickat-double-double) | Clicks a window position. |
| [`Dispose()`](#m-dispose) |  |
| [`Find(string)`](#m-find-string) | Finds a UI element by id. |
| [`Get(string, string)`](#m-get-string-string) | Reads one synced property as text. |
| [`Press(string)`](#m-press-string) | Presses a key by name (`Enter`, `Escape`, `a`). |
| [`Send(string, string, string[])`](#m-send-string-string-string) | Sends a command to an entity (id, type name or Type\#n) as a player would; it runs on the next tick. |
| [`Step(int)`](#m-step-int) | Runs frames. |
| [`Type(string)`](#m-type-string) | Types text. |

### <a id="m-call-string-string"></a>Call(string, string?)

```csharp
public string Call(string op, string? argsJson = null);
```

Runs a control operation and returns its JSON.

**Parameters**

- `op` (`string`)
- `argsJson` (`string?`)

**Returns:** `string`

### <a id="m-capture-string-float"></a>Capture(string, float)

```csharp
public CapturedFrame Capture(string size = "320x180", float scale = 1f);
```

Renders the current state offscreen.

**Parameters**

- `size` (`string`)
- `scale` (`float`)

**Returns:** [`CapturedFrame`](../../OneGame.Host/OneGame.Host/CapturedFrame.md)

### <a id="m-click-string"></a>Click(string)

```csharp
public TestClient Click(string id);
```

Clicks an element by id with pointer events.

**Parameters**

- `id` (`string`)

**Returns:** [`TestClient`](TestClient.md)

### <a id="m-clickat-double-double"></a>ClickAt(double, double)

```csharp
public TestClient ClickAt(double x, double y);
```

Clicks a window position.

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

Finds a UI element by id.

**Parameters**

- `id` (`string`)

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)?

### <a id="m-get-string-string"></a>Get(string, string)

```csharp
public string Get(string entity, string property);
```

Reads one synced property as text.

**Parameters**

- `entity` (`string`)
- `property` (`string`)

**Returns:** `string`

### <a id="m-press-string"></a>Press(string)

```csharp
public TestClient Press(string key);
```

Presses a key by name (`Enter`, `Escape`, `a`).

**Parameters**

- `key` (`string`)

**Returns:** [`TestClient`](TestClient.md)

### <a id="m-send-string-string-string"></a>Send(string, string, string\[\])

```csharp
public TestClient Send(string entity, string command, params string[] args);
```

Sends a command to an entity (id, type name or Type\#n) as a player would; it runs on the next tick.

**Parameters**

- `entity` (`string`)
- `command` (`string`)
- `args` (`string[]`)

**Returns:** [`TestClient`](TestClient.md)

### <a id="m-step-int"></a>Step(int)

```csharp
public TestClient Step(int frames = 1);
```

Runs frames.

**Parameters**

- `frames` (`int`)

**Returns:** [`TestClient`](TestClient.md)

### <a id="m-type-string"></a>Type(string)

```csharp
public TestClient Type(string text);
```

Types text.

**Parameters**

- `text` (`string`)

**Returns:** [`TestClient`](TestClient.md)
