[API reference](../../index.md) › [OneGame.Atlas](../index.md) › OneGame.Atlas

# AtlasStatus enum

**Namespace:** `OneGame.Atlas`  
**Package:** [`RecursiveAI.OneGame.Atlas`](../index.md)

```csharp
public enum AtlasStatus : int
```

The outcome of a lookup that may rasterize.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-ok"></a>`Ok` | `0` | The entry is in the atlas. |
| <a id="m-unknownresource"></a>`UnknownResource` | `1` | No resource with that id was registered. |
| <a id="m-toolarge"></a>`TooLarge` | `2` | The request exceeds the size budget; nothing was decoded or allocated for it. |
| <a id="m-full"></a>`Full` | `3` | It fits a page, but every page is in use this frame. |
| <a id="m-invalid"></a>`Invalid` | `4` | The asset could not be rasterized (it was damaged, or a limit inside the decoder stopped it). The failure is remembered. |
