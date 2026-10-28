[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# ServerState enum

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)

```csharp
public enum ServerState : int
```

Where a server is in its life.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-created"></a>`Created` | `0` | Built, not started. |
| <a id="m-running"></a>`Running` | `1` | Started and ticking. |
| <a id="m-draining"></a>`Draining` | `2` | Stopping: no more ticks, plugins being released. |
| <a id="m-stopped"></a>`Stopped` | `3` | Stopped. |
