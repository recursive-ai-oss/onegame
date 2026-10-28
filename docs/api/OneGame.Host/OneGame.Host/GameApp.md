[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# GameApp static class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public static class GameApp
```

The entry point of a game: parse the command line, create the instance, run it.

**Example**

```csharp
internal static class Program
{
    // The exit code is one of ; return it so scripts and CI see failures.
    internal static int Main(string[] args) => GameApp.Run(new MyGame(), args);
}
```

## Properties

| Member | Description |
| --- | --- |
| [`DevTools`](#m-devtools) | Set by development builds before [`GameApp.Run()`](GameApp.md#m-run-gamedefinition-string) so the extra options work. |

### <a id="m-devtools"></a>DevTools

```csharp
public static Func<IDevTools>? DevTools { get; set; }
```

Set by development builds before [`GameApp.Run()`](GameApp.md#m-run-gamedefinition-string) so the extra options work. A release build leaves it null, and those options then fail with [`ExitCodes.NoDevTools`](ExitCodes.md#m-nodevtools).

**Type:** [`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)&lt;[`IDevTools`](IDevTools.md)&gt;?

## Methods

| Member | Description |
| --- | --- |
| [`Fail(bool, TextWriter, TextWriter, int, string, string)`](#m-fail-bool-textwriter-textwriter-int-string-string) | Prints a structured error (a JSON object with `ok:false`) or a plain message, and returns the exit code. |
| [`Run(GameDefinition, string[])`](#m-run-gamedefinition-string) | Runs a game. |
| [`Run(GameDefinition, string[], TextWriter, TextWriter)`](#m-run-gamedefinition-string-textwriter-textwriter) | Runs a game with explicit output streams, so a test or tool can capture what a command line run would print. |

### <a id="m-fail-bool-textwriter-textwriter-int-string-string"></a>Fail(bool, TextWriter, TextWriter, int, string, string)

```csharp
public static int Fail(bool json, TextWriter output, TextWriter error, int exitCode, string code, string message);
```

Prints a structured error (a JSON object with `ok:false`) or a plain message, and returns the exit code.

**Parameters**

- `json` (`bool`): True to print a JSON object to `output`; false to print plain text to `error`.
- `output` ([`TextWriter`](https://learn.microsoft.com/dotnet/api/system.io.textwriter)): The result stream.
- `error` ([`TextWriter`](https://learn.microsoft.com/dotnet/api/system.io.textwriter)): The diagnostics stream.
- `exitCode` (`int`): The code to return, from [`ExitCodes`](ExitCodes.md).
- `code` (`string`): A short machine-readable error name.
- `message` (`string`): What went wrong and what to do.

**Returns:** `int` — `exitCode`.

### <a id="m-run-gamedefinition-string"></a>Run(GameDefinition, string\[\])

```csharp
public static int Run(GameDefinition game, string[] args);
```

Runs a game. Options: `--headless`, `--frames N` (stop after N frames), `--size 1280x720`, `--scale 1.5` (headless UI scale), `--seed N`, `--gpu auto|off|required`, `--backend wayland|x11|cocoa` and `--display NAME` (which window system and display a windowed run opens on; the environment decides without them), `--present vsync|lowlatency|immediate`, `--json` (print one result object), `--connect udp://host:port|ws://host:port` (play on a server: the world is a replica of its scope), with `--scope N`, `--name N` and `--net latency=100,loss=0.02` (network conditions on this client's traffic). `--realtime` makes a headless run follow the clock instead of running its frames as fast as it can, and `--until-tick N` (a connected client; implies `--realtime`) stops once the replica reaches tick N or the connection closes; a release build has no live control, so this is how a connected client is run and checked. `--data FOLDER` keeps saves and settings in that folder, and `--persist-data` keeps them in the platform's persistent store even when headless.

**Parameters**

- `game` ([`GameDefinition`](GameDefinition.md)): The game to run.
- `args` (`string[]`): The command line, normally the `args` of `Main`.

**Returns:** `int` — An exit code from [`ExitCodes`](ExitCodes.md): 0 when the run completed.

### <a id="m-run-gamedefinition-string-textwriter-textwriter"></a>Run(GameDefinition, string\[\], TextWriter, TextWriter)

```csharp
public static int Run(GameDefinition game, string[] args, TextWriter output, TextWriter error);
```

Runs a game with explicit output streams, so a test or tool can capture what a command line run would print.

**Parameters**

- `game` ([`GameDefinition`](GameDefinition.md)): The game to run.
- `args` (`string[]`): The command line; see [`GameApp.Run()`](GameApp.md#m-run-gamedefinition-string) for the options.
- `output` ([`TextWriter`](https://learn.microsoft.com/dotnet/api/system.io.textwriter)): Receives results (and the `--json` object).
- `error` ([`TextWriter`](https://learn.microsoft.com/dotnet/api/system.io.textwriter)): Receives diagnostics.

**Returns:** `int` — An exit code from [`ExitCodes`](ExitCodes.md).
