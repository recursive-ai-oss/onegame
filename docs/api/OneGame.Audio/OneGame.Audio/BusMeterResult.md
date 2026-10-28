[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# BusMeterResult enum

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)

```csharp
public enum BusMeterResult : int
```

What asking for a bus meter came to. Nothing throws for any of these.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-metered"></a>`Metered` | `0` | The bus is metered, or its meter starts at the next block. |
| <a id="m-nofreeslot"></a>`NoFreeSlot` | `1` | Every meter slot is in use by other buses ([`AudioService.FreeBusMeters`](AudioService.md#m-freebusmeters) is 0). Disable one first. |
| <a id="m-queuefull"></a>`QueueFull` | `2` | A slot is free but the mixer's command queue was full. Nothing changed; try again. |
| <a id="m-unavailable"></a>`Unavailable` | `3` | This service measures no buses: metering is off, the sample rate is too low, or [`AudioServiceOptions.MaxBusMeters`](AudioServiceOptions.md#m-maxbusmeters) is 0. |
