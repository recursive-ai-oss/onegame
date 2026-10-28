[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# ConnectionState enum

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)

```csharp
public enum ConnectionState : byte
```

Where a connection is in its life.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-connecting"></a>`Connecting` | `0` | The handshake is running. Messages may be queued; they go out once connected. |
| <a id="m-connected"></a>`Connected` | `1` | Messages flow both ways. |
| <a id="m-closing"></a>`Closing` | `2` | A flushing disconnect is draining queued messages. New sends are ignored. |
| <a id="m-closed"></a>`Closed` | `3` | Over. [`INetConnection.CloseInfo`](INetConnection.md#m-closeinfo) says why. Sends are ignored. |
