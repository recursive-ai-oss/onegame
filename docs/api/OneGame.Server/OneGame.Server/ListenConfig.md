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
| [`ListenConfig(string, int, string)`](#m-ctor-string-int-string) | One address the server listens on for clients. |

### <a id="m-ctor-string-int-string"></a>ListenConfig(string, int, string)

```csharp
public ListenConfig(string Transport, int Port, string Address);
```

One address the server listens on for clients.

**Parameters**

- `Transport` (`string`): `udp` (reliable UDP) or `ws` (WebSocket).
- `Port` (`int`): The port; 0 lets the system pick one (the bound port is reported by the host).
- `Address` (`string`): The local address to bind; `0.0.0.0` by default.

## Properties

| Member | Description |
| --- | --- |
| [`Address`](#m-address) | The local address to bind; `0.0.0.0` by default. |
| [`Port`](#m-port) | The port; 0 lets the system pick one (the bound port is reported by the host). |
| [`Transport`](#m-transport) | `udp` (reliable UDP) or `ws` (WebSocket). |

### <a id="m-address"></a>Address

```csharp
public string Address { get; init; }
```

The local address to bind; `0.0.0.0` by default.

**Type:** `string`

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
