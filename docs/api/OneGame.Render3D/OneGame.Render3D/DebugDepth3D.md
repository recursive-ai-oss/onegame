[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# DebugDepth3D enum

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public enum DebugDepth3D : int
```

Whether scene surfaces hide debug geometry.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-depthtested"></a>`DepthTested` | `0` | Test scene depth, without writing depth. |
| <a id="m-alwaysontop"></a>`AlwaysOnTop` | `1` | Draw after all depth-tested geometry, ignoring depth. |
