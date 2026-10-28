[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# IosAppHost class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public sealed class IosAppHost
```

The iOS app: UIKit on the process's main thread, the game on a thread of its own. The main thread runs `UIApplicationMain`; its delegate callbacks (scene lifecycle, touches, key presses, text input, layout) only queue `IosEvent`s. The game thread owns the [`PlatformApp`](PlatformApp.md) and turns the queue into platform events in `IosBackend`'s pump, so game code never touches UIKit and UIKit never calls into game code.

Command-line arguments are the process arguments (`xcrun simctl launch <device> <bundle id> --headless --frames 3`). Standard output and error are written to the console and to `Documents/onegame-stdout.txt` in the app's data folder; the exit code of `Main` is written to `Documents/onegame-exit.txt` when it returns, and the process then exits. That marker is how a tool knows a headless run ended.

## Methods

| Member | Description |
| --- | --- |
| [`Run(string[], Func<string[], int>)`](#m-run-string-func-string-int) | Runs the app. |

### <a id="m-run-string-func-string-int"></a>Run(string\[\], Func&lt;string\[\], int&gt;)

```csharp
public static int Run(string[] args, Func<string[], int> main);
```

Runs the app. Call it from the program's entry point; it returns only if UIKit stops the process loop.

**Parameters**

- `args` (`string[]`): The process arguments.
- `main` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)`<string[], int>`): The game's `Main`; it runs on its own thread once the window exists and its result is the exit code.

**Returns:** `int` — The exit code of `UIApplicationMain`.
