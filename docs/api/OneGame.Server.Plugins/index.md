[API reference](../index.md) › OneGame.Server.Plugins

# OneGame.Server.Plugins

Loads server plugins into collectible load contexts. CoreCLR only: it uses reflection and assembly loading, so NativeAOT hosts leave it out.

**Package:** `RecursiveAI.OneGame.Server.Plugins`

```sh
dotnet add package RecursiveAI.OneGame.Server.Plugins
```

## OneGame.Server.Plugins

| Type | Kind | Description |
| --- | --- | --- |
| [`PluginLoader`](OneGame.Server.Plugins/PluginLoader.md) | class | Loads server plugins into collectible [`AssemblyLoadContext`](https://learn.microsoft.com/dotnet/api/system.runtime.loader.assemblyloadcontext)s. |
