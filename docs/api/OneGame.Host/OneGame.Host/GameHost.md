[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# GameHost class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class GameHost : IDisposable
```

The parts of a windowed game that are costly to build and need not die with one [`GameInstance`](GameInstance.md): the connection to the window system, the window, the graphics device and the swapchain. A process that runs several games one after another (a menu loop, then a match, then the menu again) lets them share one host, so the later ones start drawing at once instead of paying for a new window and device each time. [`GameApp.Run()`](GameApp.md#m-run-gamedefinition-string) uses [`GameHost.Shared`](GameHost.md#m-shared) unless [`GameApp.ReuseWindow`](GameApp.md#m-reusewindow) is switched off.

What survives a run: the window (position, size and focus as the player left them), the device and the swapchain. What does not: the world, the session, the UI tree, the input actions, the render stage with its renderers and every texture or model the game uploaded. Those belong to the instance and are rebuilt, so nothing one run did is visible to the next. A host serves one running instance at a time and must be used from one thread. See `docs/interfaces/host-lifetime.md`.

**Example**

```csharp
using var host = new GameHost();
for (var i = 0; i < 3; i++)
{
    using var game = GameInstance.Create(new MyGame(), new GameOptions { Host = host });
    game.RunRealtime(300);
}   // the window and device are still open here; disposing the host closes them
```

## Constructors

| Member | Description |
| --- | --- |
| [`GameHost()`](#m-ctor) |  |

### <a id="m-ctor"></a>GameHost()

```csharp
public GameHost();
```

## Properties

| Member | Description |
| --- | --- |
| [`HasDevice`](#m-hasdevice) | True while a graphics device is held for the next run. |
| [`HasWindow`](#m-haswindow) | True while a window is open and waiting for the next run. |
| [`Runs`](#m-runs) | How many instances have used this host. |
| [`Shared`](#m-shared) | The host [`GameApp.Run()`](GameApp.md#m-run-gamedefinition-string) uses. |

### <a id="m-hasdevice"></a>HasDevice

```csharp
public bool HasDevice { get; }
```

True while a graphics device is held for the next run.

**Type:** `bool`

### <a id="m-haswindow"></a>HasWindow

```csharp
public bool HasWindow { get; }
```

True while a window is open and waiting for the next run.

**Type:** `bool`

### <a id="m-runs"></a>Runs

```csharp
public int Runs { get; }
```

How many instances have used this host.

**Type:** `int`

### <a id="m-shared"></a>Shared

```csharp
public static GameHost Shared { get; }
```

The host [`GameApp.Run()`](GameApp.md#m-run-gamedefinition-string) uses. It is closed when the process exits, or earlier with [`GameHost.Release()`](GameHost.md#m-release).

**Type:** [`GameHost`](GameHost.md)

## Methods

| Member | Description |
| --- | --- |
| [`Dispose()`](#m-dispose) | Closes the window and the device. |
| [`Pump()`](#m-pump) | Keeps the held window answering the window system while the game does something else between runs (a long load, a sign-in). |
| [`Release()`](#m-release) | Closes the shared host's window and device now, if one is open and no game is running on it. |

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Closes the window and the device. Call it only when no game is running on the host.

**Exceptions**

- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): A game is still running on the host.

### <a id="m-pump"></a>Pump()

```csharp
public void Pump();
```

Keeps the held window answering the window system while the game does something else between runs (a long load, a sign-in). Without it a window manager may mark the window as not responding. Does nothing when there is no window or a game is running on it.

### <a id="m-release"></a>Release()

```csharp
public static void Release();
```

Closes the shared host's window and device now, if one is open and no game is running on it. The next run opens new ones.
