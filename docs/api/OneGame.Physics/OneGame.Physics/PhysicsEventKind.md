[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# PhysicsEventKind enum

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)

```csharp
public enum PhysicsEventKind : byte
```

What happened between two bodies during a step.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-contactbegin"></a>`ContactBegin` | `0` | Two bodies started touching. |
| <a id="m-contactstay"></a>`ContactStay` | `1` | Two bodies are still touching (recorded only with [`PhysicsOptions.StayEvents`](PhysicsOptions.md#m-stayevents)). |
| <a id="m-contactend"></a>`ContactEnd` | `2` | Two bodies stopped touching. |
| <a id="m-contacthit"></a>`ContactHit` | `3` | Two bodies met at speed (recorded only with [`PhysicsOptions.HitEvents`](PhysicsOptions.md#m-hitevents)); [`PhysicsEvent.Speed`](PhysicsEvent.md#m-speed) is the approach speed. |
| <a id="m-triggerenter"></a>`TriggerEnter` | `4` | A body started overlapping a trigger. [`PhysicsEvent.A`](PhysicsEvent.md#m-a) is the trigger. |
| <a id="m-triggerexit"></a>`TriggerExit` | `5` | A body stopped overlapping a trigger. [`PhysicsEvent.A`](PhysicsEvent.md#m-a) is the trigger. |
| <a id="m-sleep"></a>`Sleep` | `6` | A dynamic body fell asleep. [`PhysicsEvent.A`](PhysicsEvent.md#m-a) is the body. |
| <a id="m-wake"></a>`Wake` | `7` | A dynamic body woke up. [`PhysicsEvent.A`](PhysicsEvent.md#m-a) is the body. |
