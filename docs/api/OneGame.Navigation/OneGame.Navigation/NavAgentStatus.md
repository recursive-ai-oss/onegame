[API reference](../../index.md) › [OneGame.Navigation](../index.md) › OneGame.Navigation

# NavAgentStatus enum

**Namespace:** `OneGame.Navigation`  
**Package:** [`RecursiveAI.OneGame.Navigation`](../index.md)

```csharp
public enum NavAgentStatus : byte
```

What an agent in a [`NavCrowd`](NavCrowd.md) is doing.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-idle"></a>`Idle` | `0` | Standing with no destination (or stopped). |
| <a id="m-walking"></a>`Walking` | `1` | Walking toward its destination. |
| <a id="m-arrived"></a>`Arrived` | `2` | Stopped at its destination. |
| <a id="m-arrivedshort"></a>`ArrivedShort` | `3` | Stopped at the nearest reachable point because the destination itself cannot be reached. |
| <a id="m-stuck"></a>`Stuck` | `4` | Gave up after making no progress through every recovery. |
| <a id="m-nopath"></a>`NoPath` | `5` | No route could be started or kept. |
