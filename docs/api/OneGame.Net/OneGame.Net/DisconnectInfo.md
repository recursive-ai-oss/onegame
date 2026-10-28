[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# DisconnectInfo struct

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`DisconnectInfo`](DisconnectInfo.md)&gt;

```csharp
public readonly struct DisconnectInfo : IEquatable<DisconnectInfo>
```

How and why a connection closed.

## Constructors

| Member | Description |
| --- | --- |
| [`DisconnectInfo(DisconnectReason, bool, ushort)`](#m-ctor-disconnectreason-bool-ushort) | How and why a connection closed. |

### <a id="m-ctor-disconnectreason-bool-ushort"></a>DisconnectInfo(DisconnectReason, bool, ushort)

```csharp
public DisconnectInfo(DisconnectReason Reason, bool ByRemote, ushort Code = 0);
```

How and why a connection closed.

**Parameters**

- `Reason` ([`DisconnectReason`](DisconnectReason.md)): The reason. For a remote close this is what the peer said.
- `ByRemote` (`bool`): True if the peer closed it, false if this side did (including timeouts detected here).
- `Code` (`ushort`): Transport detail: the application byte of a UDP disconnect, or the WebSocket close code.

## Properties

| Member | Description |
| --- | --- |
| [`ByRemote`](#m-byremote) | True if the peer closed it, false if this side did (including timeouts detected here). |
| [`Code`](#m-code) | Transport detail: the application byte of a UDP disconnect, or the WebSocket close code. |
| [`Reason`](#m-reason) | The reason. |

### <a id="m-byremote"></a>ByRemote

```csharp
public bool ByRemote { get; init; }
```

True if the peer closed it, false if this side did (including timeouts detected here).

**Type:** `bool`

### <a id="m-code"></a>Code

```csharp
public ushort Code { get; init; }
```

Transport detail: the application byte of a UDP disconnect, or the WebSocket close code.

**Type:** `ushort`

### <a id="m-reason"></a>Reason

```csharp
public DisconnectReason Reason { get; init; }
```

The reason. For a remote close this is what the peer said.

**Type:** [`DisconnectReason`](DisconnectReason.md)
