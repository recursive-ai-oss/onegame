[API reference](../index.md) › OneGame.Server

# OneGame.Server

The headless server host: fixed-step scopes, YAML configuration, logging, and the plugin API.

**Package:** `RecursiveAI.OneGame.Server`

```sh
dotnet add package RecursiveAI.OneGame.Server
```

## OneGame.Server

| Type | Kind | Description |
| --- | --- | --- |
| [`ClientInfo`](OneGame.Server/ClientInfo.md) | record | One connected client, for reports. |
| [`ClientRequest`](OneGame.Server/ClientRequest.md) | record | A client that has just connected, as the authentication hook sees it. |
| [`ClientTrustConfig`](OneGame.Server/ClientTrustConfig.md) | record | What clients may do on this server. |
| [`IPluginLoader`](OneGame.Server/IPluginLoader.md) | interface | Loads plugin assemblies. |
| [`InProcessPluginImage`](OneGame.Server/InProcessPluginImage.md) | class | An image for plugins already linked into the process (a game that bundles its own, or tests). |
| [`ListenConfig`](OneGame.Server/ListenConfig.md) | record | One address the server listens on for clients. |
| [`ListenEndpoint`](OneGame.Server/ListenEndpoint.md) | record | A bound listening address. |
| [`LogEntry`](OneGame.Server/LogEntry.md) | record | One line of the server log. |
| [`PluginApi`](OneGame.Server/PluginApi.md) | static class | The version of the plugin API a host speaks. |
| [`PluginConfig`](OneGame.Server/PluginConfig.md) | record | One plugin the server loads at start. |
| [`PluginContext`](OneGame.Server/PluginContext.md) | class | What a plugin sees while it loads: its settings, the scopes it is bound to, and the places to register types and systems. |
| [`PluginException`](OneGame.Server/PluginException.md) | class | A plugin could not be loaded, reloaded or unloaded, or a setting was wrong. |
| [`PluginFault`](OneGame.Server/PluginFault.md) | record | What went wrong inside a plugin. |
| [`PluginImage`](OneGame.Server/PluginImage.md) | class | One loaded copy of a plugin: the entry object plus the means to find which exceptions came from it and to release it. |
| [`PluginInfo`](OneGame.Server/PluginInfo.md) | record | A snapshot of one loaded plugin. |
| [`PluginLoadFailure`](OneGame.Server/PluginLoadFailure.md) | record | A plugin that was configured but could not be loaded. |
| [`PluginScope`](OneGame.Server/PluginScope.md) | record | One scope a plugin is bound to. |
| [`PluginState`](OneGame.Server/PluginState.md) | enum | Whether a loaded plugin is running. |
| [`ReloadResult`](OneGame.Server/ReloadResult.md) | record | The result of a hot reload. |
| [`ScopeConfig`](OneGame.Server/ScopeConfig.md) | record | One scope the server hosts. |
| [`ScopeHashAt`](OneGame.Server/ScopeHashAt.md) | record | The recorded hash of one scope at one tick. |
| [`ScopeInfo`](OneGame.Server/ScopeInfo.md) | record | A snapshot of one scope. |
| [`SecurityConfig`](OneGame.Server/SecurityConfig.md) | record | How the server secures its client links (the `security:` section). |
| [`ServerConfig`](OneGame.Server/ServerConfig.md) | class | The server configuration. |
| [`ServerConfigException`](OneGame.Server/ServerConfigException.md) | class | The server configuration file is not valid: the message names the file, the line and what to change. |
| [`ServerHost`](OneGame.Server/ServerHost.md) | class | A headless game server: one node hosting one or more authority scopes on a fixed-step driver, with no window, GPU or audio. |
| [`ServerLog`](OneGame.Server/ServerLog.md) | class | The server log: lines go to a writer (standard error for the runner) and the last few hundred are kept for the control surface. |
| [`ServerPlugin`](OneGame.Server/ServerPlugin.md) | class | A server plugin. |
| [`ServerPluginAttribute`](OneGame.Server/ServerPluginAttribute.md) | attribute | Marks a plugin assembly and names its entry class. |
| [`ServerScope`](OneGame.Server/ServerScope.md) | class | One hosted scope. |
| [`ServerSetup`](OneGame.Server/ServerSetup.md) | class | What a game adds to a server that has no plugins, or in addition to them. |
| [`ServerState`](OneGame.Server/ServerState.md) | enum | Where a server is in its life. |
| [`UnloadResult`](OneGame.Server/UnloadResult.md) | record | The result of an unload. |
