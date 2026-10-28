[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# AdapterKind enum

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public enum AdapterKind : int
```

Kind of graphics adapter.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-other"></a>`Other` | `0` | Unknown. |
| <a id="m-discrete"></a>`Discrete` | `1` | Discrete GPU with its own memory. |
| <a id="m-integrated"></a>`Integrated` | `2` | GPU integrated with the CPU. |
| <a id="m-virtual"></a>`Virtual` | `3` | Virtualized GPU. |
| <a id="m-software"></a>`Software` | `4` | Software rasterizer running on the CPU. |
