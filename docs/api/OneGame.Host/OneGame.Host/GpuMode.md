[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# GpuMode enum

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public enum GpuMode : int
```

How the host picks a graphics device.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-auto"></a>`Auto` | `0` | Use a device if one can be created; otherwise render captures on the CPU. |
| <a id="m-off"></a>`Off` | `1` | Never create a device. Captures are rendered on the CPU. |
| <a id="m-required"></a>`Required` | `2` | Fail if no device can be created. |
