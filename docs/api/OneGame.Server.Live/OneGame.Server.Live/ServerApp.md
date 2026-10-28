[API reference](../../index.md) › [OneGame.Server.Live](../index.md) › OneGame.Server.Live

# ServerApp static class

**Namespace:** `OneGame.Server.Live`  
**Package:** [`RecursiveAI.OneGame.Server.Live`](../index.md)

```csharp
public static class ServerApp
```

The whole `Main` of a dedicated server: parse the command line, build a [`ServerHost`](../../OneGame.Server/OneGame.Server/ServerHost.md), listen for clients, serve live control, run until stopped. A game's server project calls `ServerApp.Run(setup, args)` with the game's entity types; the `onegame-server` tool calls it with a plugin loader and no game types.

## Methods

| Member | Description |
| --- | --- |
| [`Run(ServerSetup?, string[], IPluginLoader?)`](#m-run-serversetup-string-ipluginloader) | Runs a server process and returns its exit code. |
| [`Run(ServerSetup?, string[], IPluginLoader?, TextWriter, TextWriter)`](#m-run-serversetup-string-ipluginloader-textwriter-textwriter) | Runs a server process with explicit output streams. |

### <a id="m-run-serversetup-string-ipluginloader"></a>Run(ServerSetup?, string\[\], IPluginLoader?)

```csharp
public static int Run(ServerSetup? setup, string[] args, IPluginLoader? loader = null);
```

Runs a server process and returns its exit code.

**Parameters**

- `setup` ([`ServerSetup`](../../OneGame.Server/OneGame.Server/ServerSetup.md)?): The game's entity types and world setup, or null for a server that only runs plugins.
- `args` (`string[]`): The command line.
- `loader` ([`IPluginLoader`](../../OneGame.Server/OneGame.Server/IPluginLoader.md)?): Loads plugins; null for a server that cannot (a NativeAOT build).

**Returns:** `int`

### <a id="m-run-serversetup-string-ipluginloader-textwriter-textwriter"></a>Run(ServerSetup?, string\[\], IPluginLoader?, TextWriter, TextWriter)

```csharp
public static int Run(ServerSetup? setup, string[] args, IPluginLoader? loader, TextWriter output, TextWriter error);
```

Runs a server process with explicit output streams.

**Parameters**

- `setup` ([`ServerSetup`](../../OneGame.Server/OneGame.Server/ServerSetup.md)?)
- `args` (`string[]`)
- `loader` ([`IPluginLoader`](../../OneGame.Server/OneGame.Server/IPluginLoader.md)?)
- `output` ([`TextWriter`](https://learn.microsoft.com/dotnet/api/system.io.textwriter))
- `error` ([`TextWriter`](https://learn.microsoft.com/dotnet/api/system.io.textwriter))

**Returns:** `int`
