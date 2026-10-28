[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# CommandStatus enum

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public enum CommandStatus : int
```

What happened to a command.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-executed"></a>`Executed` | `0` | The handler ran and accepted the command. |
| <a id="m-rejected"></a>`Rejected` | `1` | The handler ran and refused the command (it returned false). |
| <a id="m-queued"></a>`Queued` | `2` | The command was queued to run at the start of the next tick. |
| <a id="m-forwarded"></a>`Forwarded` | `3` | The command was passed on to the authority; this world is a replica. |
| <a id="m-unknownentity"></a>`UnknownEntity` | `4` | No entity has the command's entity id. |
| <a id="m-unknowncommand"></a>`UnknownCommand` | `5` | The entity has no command with that id. |
| <a id="m-malformed"></a>`Malformed` | `6` | The bytes were not a valid command: truncated, too large or with trailing data. |
