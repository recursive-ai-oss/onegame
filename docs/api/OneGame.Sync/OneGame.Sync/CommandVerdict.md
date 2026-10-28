[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# CommandVerdict enum

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public enum CommandVerdict : byte
```

How a command sent from a replica ended.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-executed"></a>`Executed` | `0` | The authority ran the handler and it accepted. |
| <a id="m-rejected"></a>`Rejected` | `1` | The handler rejected the command; [`CommandResult.Reason`](CommandResult.md#m-reason) says why when it gave a reason. |
| <a id="m-refused"></a>`Refused` | `2` | The authority refused the command before running it; [`CommandResult.Refusal`](CommandResult.md#m-refusal) says why. |
| <a id="m-unknownentity"></a>`UnknownEntity` | `3` | The target entity does not exist on the authority. |
| <a id="m-unknowncommand"></a>`UnknownCommand` | `4` | The entity has no such command. |
| <a id="m-malformed"></a>`Malformed` | `5` | The arguments were not valid for the command. |
| <a id="m-dropped"></a>`Dropped` | `6` | The authority never ran the command (its state was replaced by a load, or the replica stopped waiting). |
| <a id="m-timedout"></a>`TimedOut` | `7` | No answer came in time ([`ClusterOptions.CommandTimeoutMs`](ClusterOptions.md#m-commandtimeoutms)), or the authority went silent ([`ClusterOptions.SilenceTimeoutMs`](ClusterOptions.md#m-silencetimeoutms)). The command may or may not have run; a late answer is ignored. |
