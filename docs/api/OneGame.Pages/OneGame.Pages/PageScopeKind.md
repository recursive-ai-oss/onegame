[API reference](../../index.md) › [OneGame.Pages](../index.md) › OneGame.Pages

# PageScopeKind enum

**Namespace:** `OneGame.Pages`  
**Package:** [`RecursiveAI.OneGame.Pages`](../index.md)

```csharp
public enum PageScopeKind : int
```

What a host-scope number means to the page renderer.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-none"></a>`None` | `0` | The scope is drawn as if it were not there. |
| <a id="m-layer"></a>`Layer` | `1` | The scope's content is drawn when its layer is drawn, not with layer 0. |
| <a id="m-additive"></a>`Additive` | `2` | The scope's content is drawn apart and added to what is below (a glow). |
