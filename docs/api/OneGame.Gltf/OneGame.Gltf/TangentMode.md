[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# TangentMode enum

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public enum TangentMode : int
```

When tangents are generated.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-whennormalmapped"></a>`WhenNormalMapped` | `0` | For primitives whose material has a normal texture and that ship no tangents. The default. |
| <a id="m-always"></a>`Always` | `1` | For every triangle primitive with UVs, replacing shipped tangents. |
| <a id="m-never"></a>`Never` | `2` | Never; only shipped tangents are kept. |
