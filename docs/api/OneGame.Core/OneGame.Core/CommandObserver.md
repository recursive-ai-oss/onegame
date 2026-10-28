[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# CommandObserver delegate

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public delegate void CommandObserver(long tick, EntityId entity, uint commandId, ReadOnlySpan<byte> payload, CommandStatus status);
```

Receives every command a world executes, in order, for logging and replay.

**Parameters**

- `tick`: The tick the command ran in.
- `entity`: The target entity.
- `commandId`: The command's id.
- `payload`: The encoded arguments; valid only during the call.
- `status`: The outcome.
