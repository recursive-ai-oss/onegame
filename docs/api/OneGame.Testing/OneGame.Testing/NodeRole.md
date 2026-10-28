[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# NodeRole enum

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)

```csharp
public enum NodeRole : int
```

What a node of a process cluster is.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-server"></a>`Server` | `0` | A dedicated server: it listens and owns scopes. |
| <a id="m-client"></a>`Client` | `1` | A game (a development build, headless) connected to a server. |
