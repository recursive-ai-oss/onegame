[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# AppLifecycleState enum

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public enum AppLifecycleState : byte
```

What [`PlatformEventKind.AppLifecycle`](PlatformEventKind.md#m-applifecycle) reports.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-resumed"></a>`Resumed` | `0` | The app is in the foreground and interactive. |
| <a id="m-paused"></a>`Paused` | `1` | The app left the foreground. Stop simulating and playing sound; state stays in memory. |
| <a id="m-lowmemory"></a>`LowMemory` | `2` | The system wants memory back: drop caches that can be rebuilt. |
