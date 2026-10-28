[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# LoadOp enum

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public enum LoadOp : int
```

What happens to an attachment's contents when a pass begins.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-load"></a>`Load` | `0` | Keep the existing contents. |
| <a id="m-clear"></a>`Clear` | `1` | Clear to the given value. |
| <a id="m-dontcare"></a>`DontCare` | `2` | Contents are undefined; the pass overwrites everything. |
