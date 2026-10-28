[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# PluginState enum

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)

```csharp
public enum PluginState : int
```

Whether a loaded plugin is running.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-loaded"></a>`Loaded` | `0` | Running normally. |
| <a id="m-faulted"></a>`Faulted` | `1` | It threw while ticking or seeding. Its entities were removed and its systems stopped; it stays loaded so it can be inspected, reloaded or unloaded. |
