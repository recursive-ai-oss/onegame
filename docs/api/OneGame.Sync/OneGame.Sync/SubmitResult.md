[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# SubmitResult enum

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public enum SubmitResult : int
```

What `Connection.SubmitCommand` did with a command.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-queued"></a>`Queued` | `0` | Accepted; it runs at the start of the scope's next tick. |
| <a id="m-nottrusted"></a>`NotTrusted` | `1` | The connection's trust does not allow commanding the scope; a [`CommandRefusal`](CommandRefusal.md) was filed. |
| <a id="m-unknownscope"></a>`UnknownScope` | `2` | The node has no authority scope with that id. |
| <a id="m-malformed"></a>`Malformed` | `3` | The bytes were too large to be a command. |
