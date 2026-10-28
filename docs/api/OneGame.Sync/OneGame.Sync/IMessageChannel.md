[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# IMessageChannel interface

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public interface IMessageChannel
```

One end of a message pipe between two nodes. Messages are whole, ordered and reliable; a real transport fragments, retransmits and reassembles underneath this. Everything a node sends is already bytes, so the loopback and a network carry the same thing.

## Methods

| Member | Description |
| --- | --- |
| [`Send(ReadOnlySpan<byte>)`](#m-send-readonlyspan-byte) | Queues a message for the other end. |
| [`TryReceive(out ReadOnlySpan<byte>)`](#m-tryreceive-readonlyspan-byte) | Takes the next message, or returns false if none is waiting. |

### <a id="m-send-readonlyspan-byte"></a>Send(ReadOnlySpan&lt;byte&gt;)

```csharp
void Send(ReadOnlySpan<byte> message);
```

Queues a message for the other end.

**Parameters**

- `message` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The message is larger than [`Loopback.MaxMessageBytes`](Loopback.md#m-maxmessagebytes).

### <a id="m-tryreceive-readonlyspan-byte"></a>TryReceive(out ReadOnlySpan&lt;byte&gt;)

```csharp
bool TryReceive(out ReadOnlySpan<byte> message);
```

Takes the next message, or returns false if none is waiting. The span is valid until the next call to this method.

**Parameters**

- `message` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

**Returns:** `bool`
