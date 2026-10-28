[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu.Graph

# BufferRead enum

**Namespace:** `OneGame.Gpu.Graph`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public enum BufferRead : int
```

How a pass reads a buffer.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-uniform"></a>`Uniform` | `0` | As a constant (uniform) buffer. |
| <a id="m-storage"></a>`Storage` | `1` | As a storage buffer it only reads. |
| <a id="m-vertex"></a>`Vertex` | `2` | As vertex data. |
| <a id="m-index"></a>`Index` | `3` | As index data. |
| <a id="m-indirect"></a>`Indirect` | `4` | As indirect draw or dispatch arguments. |
| <a id="m-copysource"></a>`CopySource` | `5` | As the source of a copy. |
