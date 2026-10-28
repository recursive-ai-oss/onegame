[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# ShaderStage enum

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
[Flags]
public enum ShaderStage : int
```

Shader stage of a module or a resource's visibility.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-none"></a>`None` | `0` | No stage. |
| <a id="m-vertex"></a>`Vertex` | `1` | Vertex stage. |
| <a id="m-pixel"></a>`Pixel` | `2` | Pixel (fragment) stage. |
| <a id="m-compute"></a>`Compute` | `4` | Compute stage. |
| <a id="m-all"></a>`All` | `7` | Every stage. |
