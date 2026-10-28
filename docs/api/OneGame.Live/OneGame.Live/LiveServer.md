[API reference](../../index.md) › [OneGame.Live](../index.md) › OneGame.Live

# LiveServer class

**Namespace:** `OneGame.Live`  
**Package:** [`RecursiveAI.OneGame.Live`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class LiveServer : IDisposable
```

The listener of a development build: a local socket that speaks MCP (newline-delimited JSON-RPC, the protocol's stdio framing) to one agent at a time or several in turn. Connections are read on background threads, but every request runs on the thread that calls [`LiveServer.Pump()`](LiveServer.md#m-pump), the game's own, so the simulation never sees a second thread. Only development builds contain it; a game started with `onegame run --live` already hosts one.

**Example**

Hosting live control in a custom game loop:

```csharp
using var live = new LiveServer(new ControlSurface(game));
live.Start(LiveServer.DefaultPath());
while (running)
{
    live.Pump();            // run requests that arrived, on this thread
    game.Step(1);
    live.WaitForWork(5);    // or sleep until the next frame
}
```

## Constructors

| Member | Description |
| --- | --- |
| [`LiveServer(ControlSurface)`](#m-ctor-controlsurface) | Creates a server for a game's control surface. |
| [`LiveServer(IControlEndpoint)`](#m-ctor-icontrolendpoint) | Creates a server for any control endpoint, such as a game server's. |

### <a id="m-ctor-controlsurface"></a>LiveServer(ControlSurface)

```csharp
public LiveServer(ControlSurface surface);
```

Creates a server for a game's control surface. Nothing listens until [`LiveServer.Start()`](LiveServer.md#m-start-string).

**Parameters**

- `surface` ([`ControlSurface`](../../OneGame.Control/OneGame.Control/ControlSurface.md)): The game's control surface.

### <a id="m-ctor-icontrolendpoint"></a>LiveServer(IControlEndpoint)

```csharp
public LiveServer(IControlEndpoint endpoint);
```

Creates a server for any control endpoint, such as a game server's. Nothing listens until [`LiveServer.Start()`](LiveServer.md#m-start-string).

**Parameters**

- `endpoint` ([`IControlEndpoint`](../../OneGame.Control/OneGame.Control/IControlEndpoint.md)): The endpoint whose operations are served.

**Exceptions**

- [`ArgumentNullException`](https://learn.microsoft.com/dotnet/api/system.argumentnullexception): `endpoint` is null.

## Fields

| Member | Description |
| --- | --- |
| [`MaxLineBytes`](#m-maxlinebytes) | Longest request line accepted, in bytes. |

### <a id="m-maxlinebytes"></a>MaxLineBytes

```csharp
public const int MaxLineBytes = 8388608;
```

Longest request line accepted, in bytes.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`Path`](#m-path) | The socket path being listened on. |

### <a id="m-path"></a>Path

```csharp
public string? Path { get; }
```

The socket path being listened on.

**Type:** `string?`

## Methods

| Member | Description |
| --- | --- |
| [`DefaultPath()`](#m-defaultpath) | A path in a private per-user folder for a game process to listen on. |
| [`Dispose()`](#m-dispose) |  |
| [`Pump()`](#m-pump) | Runs the requests that have arrived. |
| [`Start(string)`](#m-start-string) | Starts listening on a Unix domain socket at `path`. |
| [`WaitForWork(int)`](#m-waitforwork-int) | Waits up to `milliseconds` for a request to arrive, so a loop can sleep without adding latency. |

### <a id="m-defaultpath"></a>DefaultPath()

```csharp
public static string DefaultPath();
```

A path in a private per-user folder for a game process to listen on.

**Returns:** `string`

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Implements [`IDisposable.Dispose()`](https://learn.microsoft.com/dotnet/api/system.idisposable.dispose).

### <a id="m-pump"></a>Pump()

```csharp
public int Pump();
```

Runs the requests that have arrived. Call it from the game's thread, between frames. Returns how many ran.

**Returns:** `int`

### <a id="m-start-string"></a>Start(string)

```csharp
public void Start(string path);
```

Starts listening on a Unix domain socket at `path`.

**Parameters**

- `path` (`string`): The socket file; see [`LiveServer.DefaultPath()`](LiveServer.md#m-defaultpath). An old file at the path is replaced. `tcp:HOST:PORT` listens on TCP instead, for a game on a device; that listener has no authentication, so use it on test devices only.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): `path` is empty.
- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): The server is already listening.
- [`IOException`](https://learn.microsoft.com/dotnet/api/system.io.ioexception): The socket could not be created.

### <a id="m-waitforwork-int"></a>WaitForWork(int)

```csharp
public bool WaitForWork(int milliseconds);
```

Waits up to `milliseconds` for a request to arrive, so a loop can sleep without adding latency.

**Parameters**

- `milliseconds` (`int`): The longest wait.

**Returns:** `bool` — True when a request arrived.
