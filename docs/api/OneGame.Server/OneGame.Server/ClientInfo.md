[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# ClientInfo record

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ClientInfo`](ClientInfo.md)&gt;

```csharp
public sealed record ClientInfo : IEquatable<ClientInfo>
```

One connected client, for reports.

## Constructors

| Member | Description |
| --- | --- |
| [`ClientInfo(int, string, string, string, string, double, double, double, long, long, long)`](#m-ctor-int-string-string-string-string-double-double-double-long-long-long) | One connected client, for reports. |

### <a id="m-ctor-int-string-string-string-string-double-double-double-long-long-long"></a>ClientInfo(int, string, string, string, string, double, double, double, long, long, long)

```csharp
public ClientInfo(int Id, string Name, string Transport, string Address, string State, double RttMs, double InboundLoss, double OutboundLoss, long Retransmits, long MessagesSent, long MessagesReceived);
```

One connected client, for reports.

**Parameters**

- `Id` (`int`): Id of the connection on the server node.
- `Name` (`string`): Name in reports.
- `Transport` (`string`): `udp` or `ws`.
- `Address` (`string`): Remote address.
- `State` (`string`): Transport state.
- `RttMs` (`double`): Smoothed round-trip time.
- `InboundLoss` (`double`): Estimated share of the client's datagrams that never arrived.
- `OutboundLoss` (`double`): Share of our reliable transmissions that were repeats.
- `Retransmits` (`long`): Reliable fragments sent again.
- `MessagesSent` (`long`): Messages sent to the client.
- `MessagesReceived` (`long`): Messages received from the client.

## Properties

| Member | Description |
| --- | --- |
| [`Address`](#m-address) | Remote address. |
| [`Id`](#m-id) | Id of the connection on the server node. |
| [`InboundLoss`](#m-inboundloss) | Estimated share of the client's datagrams that never arrived. |
| [`MessagesReceived`](#m-messagesreceived) | Messages received from the client. |
| [`MessagesSent`](#m-messagessent) | Messages sent to the client. |
| [`Name`](#m-name) | Name in reports. |
| [`OutboundLoss`](#m-outboundloss) | Share of our reliable transmissions that were repeats. |
| [`Retransmits`](#m-retransmits) | Reliable fragments sent again. |
| [`RttMs`](#m-rttms) | Smoothed round-trip time. |
| [`State`](#m-state) | Transport state. |
| [`Transport`](#m-transport) | `udp` or `ws`. |

### <a id="m-address"></a>Address

```csharp
public string Address { get; init; }
```

Remote address.

**Type:** `string`

### <a id="m-id"></a>Id

```csharp
public int Id { get; init; }
```

Id of the connection on the server node.

**Type:** `int`

### <a id="m-inboundloss"></a>InboundLoss

```csharp
public double InboundLoss { get; init; }
```

Estimated share of the client's datagrams that never arrived.

**Type:** `double`

### <a id="m-messagesreceived"></a>MessagesReceived

```csharp
public long MessagesReceived { get; init; }
```

Messages received from the client.

**Type:** `long`

### <a id="m-messagessent"></a>MessagesSent

```csharp
public long MessagesSent { get; init; }
```

Messages sent to the client.

**Type:** `long`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

Name in reports.

**Type:** `string`

### <a id="m-outboundloss"></a>OutboundLoss

```csharp
public double OutboundLoss { get; init; }
```

Share of our reliable transmissions that were repeats.

**Type:** `double`

### <a id="m-retransmits"></a>Retransmits

```csharp
public long Retransmits { get; init; }
```

Reliable fragments sent again.

**Type:** `long`

### <a id="m-rttms"></a>RttMs

```csharp
public double RttMs { get; init; }
```

Smoothed round-trip time.

**Type:** `double`

### <a id="m-state"></a>State

```csharp
public string State { get; init; }
```

Transport state.

**Type:** `string`

### <a id="m-transport"></a>Transport

```csharp
public string Transport { get; init; }
```

`udp` or `ws`.

**Type:** `string`
