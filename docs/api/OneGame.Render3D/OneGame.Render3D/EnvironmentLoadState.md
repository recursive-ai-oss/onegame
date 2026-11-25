[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# EnvironmentLoadState enum

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public enum EnvironmentLoadState : int
```

Where an [`EnvironmentLoad`](EnvironmentLoad.md) is.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-converting"></a>`Converting` | `0` | The panorama is being converted to half floats (on a worker thread, or in slices on the render thread). |
| <a id="m-uploading"></a>`Uploading` | `1` | The converted panorama is being copied to the GPU in bands. |
| <a id="m-filtering"></a>`Filtering` | `2` | The filtering passes are being added to the frames a few at a time. |
| <a id="m-ready"></a>`Ready` | `3` | Every pass has been added; [`EnvironmentLoad.Environment`](EnvironmentLoad.md#m-environment) is ready to use. |
| <a id="m-failed"></a>`Failed` | `4` | The load stopped on an error, see [`EnvironmentLoad.Error`](EnvironmentLoad.md#m-error). |
| <a id="m-canceled"></a>`Canceled` | `5` | The load was cancelled or disposed before it finished. |
