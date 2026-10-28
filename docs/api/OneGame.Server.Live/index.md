[API reference](../index.md) › OneGame.Server.Live

# OneGame.Server.Live

Live control of a running game server over the same MCP surface as a development game: plugin and scope operations.

**Package:** `RecursiveAI.OneGame.Server.Live`

```sh
dotnet add package RecursiveAI.OneGame.Server.Live
```

## OneGame.Server.Live

| Type | Kind | Description |
| --- | --- | --- |
| [`ServerApp`](OneGame.Server.Live/ServerApp.md) | static class | The whole `Main` of a dedicated server: parse the command line, build a [`ServerHost`](../OneGame.Server/OneGame.Server/ServerHost.md), listen for clients, serve live control, run until stopped. |
| [`ServerAppJson`](OneGame.Server.Live/ServerAppJson.md) | class | Source-generated JSON for the lines a server process prints. |
| [`ServerControl`](OneGame.Server.Live/ServerControl.md) | class | The control surface of a game server: the operations an agent or the command line uses to look at a running server and to load, reload and unload its plugins. |
| [`ServerInfoResult`](OneGame.Server.Live/ServerInfoResult.md) | record | Result of `server_info`. |
| [`ServerJson`](OneGame.Server.Live/ServerJson.md) | class | Source-generated JSON metadata for server control results. |
| [`ServerReadyLine`](OneGame.Server.Live/ServerReadyLine.md) | record | The line printed on standard output once the server is up, so a launcher can find its live socket and listening ports. |
| [`ServerSendResult`](OneGame.Server.Live/ServerSendResult.md) | record | Result of `command_send`. |
| [`ServerStateResult`](OneGame.Server.Live/ServerStateResult.md) | record | Result of `hash` and `step`. |
| [`ServerStoppedLine`](OneGame.Server.Live/ServerStoppedLine.md) | record | The line printed when the server has stopped. |
| [`ShutdownResult`](OneGame.Server.Live/ShutdownResult.md) | record | Result of `shutdown`. |
