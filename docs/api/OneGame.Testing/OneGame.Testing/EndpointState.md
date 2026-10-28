[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# EndpointState record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`EndpointState`](EndpointState.md)&gt;

```csharp
public sealed record EndpointState : IEquatable<EndpointState>
```

A listening address of a server node.

## Constructors

| Member | Description |
| --- | --- |
| [`EndpointState(string, string, int)`](#m-ctor-string-string-int) | A listening address of a server node. |

### <a id="m-ctor-string-string-int"></a>EndpointState(string, string, int)

```csharp
public EndpointState(string Transport, string Address, int Port);
```

A listening address of a server node.

**Parameters**

- `Transport` (`string`): `udp` or `ws`.
- `Address` (`string`): The bound address.
- `Port` (`int`): The bound port.

## Properties

| Member | Description |
| --- | --- |
| [`Address`](#m-address) | The bound address. |
| [`Port`](#m-port) | The bound port. |
| [`Transport`](#m-transport) | `udp` or `ws`. |

### <a id="m-address"></a>Address

```csharp
public string Address { get; init; }
```

The bound address.

**Type:** `string`

### <a id="m-port"></a>Port

```csharp
public int Port { get; init; }
```

The bound port.

**Type:** `int`

### <a id="m-transport"></a>Transport

```csharp
public string Transport { get; init; }
```

`udp` or `ws`.

**Type:** `string`
