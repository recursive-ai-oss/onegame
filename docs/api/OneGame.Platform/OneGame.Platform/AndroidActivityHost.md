[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# AndroidActivityHost class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public sealed class AndroidActivityHost
```

The program side of an Android native activity. The app's shared library exports `ANativeActivity_onCreate` and passes the activity here along with the game's `Main`; the game then runs on its own thread while the activity's callbacks (which arrive on the system's main thread and must return quickly) only queue what they were told. The input queue is read on the game thread.

Command-line arguments come from the launch intent's string extra `onegame.args` (for example `adb shell am start -n app/android.app.NativeActivity -e onegame.args "--headless --frames 3"`). Standard output and error go to logcat (tag `onegame`) and to `onegame-stdout.txt` in the app's files folder; the exit code of `Main` is written to `onegame-exit.txt` there when it returns, which is how a tool waits for a headless run to finish.

## Methods

| Member | Description |
| --- | --- |
| [`Start(nint, Func<string[], int>)`](#m-start-nint-func-string-int) | Call this from the game's exported `ANativeActivity_onCreate`. |

### <a id="m-start-nint-func-string-int"></a>Start(nint, Func&lt;string\[\], int&gt;)

```csharp
public static void Start(nint activity, Func<string[], int> main);
```

Call this from the game's exported `ANativeActivity_onCreate`. It installs the activity callbacks, reads the launch arguments and starts the game thread, then returns at once.

**Parameters**

- `activity` (`nint`): The `ANativeActivity*` the system passed to `ANativeActivity_onCreate`.
- `main` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)`<string[], int>`): The game's entry point: it receives the launch arguments and returns the exit code.
