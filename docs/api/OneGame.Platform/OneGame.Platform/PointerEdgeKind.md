[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# PointerEdgeKind enum

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public enum PointerEdgeKind : byte
```

What a [`PointerEdge`](PointerEdge.md) reports.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-down"></a>`Down` | `0` | A button or finger went down. |
| <a id="m-move"></a>`Move` | `1` | The pointer or a finger moved. |
| <a id="m-up"></a>`Up` | `2` | A button or finger went up. |
| <a id="m-cancel"></a>`Cancel` | `3` | The system took a finger away (a call, a gesture): treat it as lifted without a tap. |
