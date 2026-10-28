[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# CommandReply struct

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public readonly struct CommandReply
```

What a command handler answers: accepted, or rejected with a reason that travels back to whoever sent the command. A handler that returns [`Boolean`](https://learn.microsoft.com/dotnet/api/system.boolean) converts to this, with no reason.

## Properties

| Member | Description |
| --- | --- |
| [`Accepted`](#m-accepted) | True when the handler accepted the command. |
| [`Ok`](#m-ok) | The command was accepted. |
| [`Reason`](#m-reason) | Why the handler rejected the command, or null. |

### <a id="m-accepted"></a>Accepted

```csharp
public bool Accepted { get; }
```

True when the handler accepted the command.

**Type:** `bool`

### <a id="m-ok"></a>Ok

```csharp
public static CommandReply Ok { get; }
```

The command was accepted.

**Type:** [`CommandReply`](CommandReply.md)

### <a id="m-reason"></a>Reason

```csharp
public string? Reason { get; }
```

Why the handler rejected the command, or null. Not part of the synced state; only the sender sees it.

**Type:** `string?`

## Methods

| Member | Description |
| --- | --- |
| [`Reject(string)`](#m-reject-string) | The command is refused; `reason` is sent to the submitter (long text is cut at 200 characters). |

### <a id="m-reject-string"></a>Reject(string)

```csharp
public static CommandReply Reject(string reason);
```

The command is refused; `reason` is sent to the submitter (long text is cut at 200 characters).

**Parameters**

- `reason` (`string`)

**Returns:** [`CommandReply`](CommandReply.md)

## Operators

| Member | Description |
| --- | --- |
| [`implicit operator CommandReply(bool)`](#m-implicit-operator-commandreply-bool) | `true` accepts and `false` rejects without a reason. |

### <a id="m-implicit-operator-commandreply-bool"></a>implicit operator CommandReply(bool)

```csharp
public static implicit operator CommandReply(bool accepted);
```

`true` accepts and `false` rejects without a reason.

**Parameters**

- `accepted` (`bool`)

**Returns:** [`CommandReply`](CommandReply.md)
