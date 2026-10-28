[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# ResourceKind enum

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public enum ResourceKind : int
```

Kind of a shader resource binding.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-constantbuffer"></a>`ConstantBuffer` | `0` | Constant (uniform) buffer. |
| <a id="m-storagebuffer"></a>`StorageBuffer` | `1` | Read-write storage buffer. |
| <a id="m-readonlystoragebuffer"></a>`ReadOnlyStorageBuffer` | `2` | Storage buffer the shader only reads. |
| <a id="m-sampledtexture"></a>`SampledTexture` | `3` | Sampled texture (separate from its sampler). |
| <a id="m-sampler"></a>`Sampler` | `4` | Sampler state. |
| <a id="m-storagetexture"></a>`StorageTexture` | `5` | Read-write storage texture. |
