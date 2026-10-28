[API reference](../index.md) › OneGame.Live

# OneGame.Live

Development-build tools of a running game: live control over MCP, scenario runs, captures and replays. Release builds do not reference this project.

**Package:** `RecursiveAI.OneGame.Live`

```sh
dotnet add package RecursiveAI.OneGame.Live
```

## OneGame.Live

| Type | Kind | Description |
| --- | --- | --- |
| [`Dev`](OneGame.Live/Dev.md) | static class | Turns on the development tools of a game. |
| [`LiveServer`](OneGame.Live/LiveServer.md) | class | The listener of a development build: a local socket that speaks MCP (newline-delimited JSON-RPC, the protocol's stdio framing) to one agent at a time or several in turn. |
| [`ParentWatch`](OneGame.Live/ParentWatch.md) | class | Watches the process that started this one. |
