[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# ListenConfig record

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ListenConfig`](ListenConfig.md)&gt;

```csharp
public sealed record ListenConfig : IEquatable<ListenConfig>
```

One address the server listens on for clients.

## Constructors

| Member | Description |
| --- | --- |
| [`ListenConfig(string, int, string, int)`](#m-ctor-string-int-string-int) | One address the server listens on for clients. |

### <a id="m-ctor-string-int-string-int"></a>ListenConfig(string, int, string, int)

```csharp
public ListenConfig(string Transport, int Port, string Address, int MaxSendQueueBytes = 0);
```

One address the server listens on for clients.

**Parameters**

- `Transport` (`string`): `udp` (reliable UDP) or `ws` (WebSocket).
- `Port` (`int`): The port; 0 lets the system pick one (the bound port is reported by the host).
- `Address` (`string`): The local address to bind; `0.0.0.0` by default.
- `MaxSendQueueBytes` (`int`): Most message bytes queued per client on this listener (unacknowledged data included) before the client is closed with `SendQueueFull`; 0 uses [`ServerHost.MaxSendQueueBytes`](ServerHost.md#m-maxsendqueuebytes), and that the transport default (8 MiB). A relayed or first snapshot travels in 256 KiB parts but all of it is queued at once, so a scope whose snapshot approaches this needs it raised.

## Properties

| Member | Description |
| --- | --- |
| [`Address`](#m-address) | The local address to bind; `0.0.0.0` by default. |
| [`MaxSendQueueBytes`](#m-maxsendqueuebytes) | Most message bytes queued per client on this listener (unacknowledged data included) before the client is closed with `SendQueueFull`; 0 uses [`ServerHost.MaxSendQueueBytes`](ServerHost.md#m-maxsendqueuebytes), and that the transport default (8 MiB). |
| [`Port`](#m-port) | The port; 0 lets the system pick one (the bound port is reported by the host). |
| [`Transport`](#m-transport) | `udp` (reliable UDP) or `ws` (WebSocket). |

### <a id="m-address"></a>Address

```csharp
public string Address { get; init; }
```

The local address to bind; `0.0.0.0` by default.

**Type:** `string`

### <a id="m-maxsendqueuebytes"></a>MaxSendQueueBytes

```csharp
public int MaxSendQueueBytes { get; init; }
```

Most message bytes queued per client on this listener (unacknowledged data included) before the client is closed with `SendQueueFull`; 0 uses [`ServerHost.MaxSendQueueBytes`](ServerHost.md#m-maxsendqueuebytes), and that the transport default (8 MiB). A relayed or first snapshot travels in 256 KiB parts but all of it is queued at once, so a scope whose snapshot approaches this needs it raised.

**Type:** `int`

### <a id="m-port"></a>Port

```csharp
public int Port { get; init; }
```

The port; 0 lets the system pick one (the bound port is reported by the host).

**Type:** `int`

### <a id="m-transport"></a>Transport

```csharp
public string Transport { get; init; }
```

`udp` (reliable UDP) or `ws` (WebSocket).

**Type:** `string`
