[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# ListenEndpoint record

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ListenEndpoint`](ListenEndpoint.md)&gt;

```csharp
public sealed record ListenEndpoint : IEquatable<ListenEndpoint>
```

A bound listening address.

## Constructors

| Member | Description |
| --- | --- |
| [`ListenEndpoint(string, string, int)`](#m-ctor-string-string-int) | A bound listening address. |

### <a id="m-ctor-string-string-int"></a>ListenEndpoint(string, string, int)

```csharp
public ListenEndpoint(string Transport, string Address, int Port);
```

A bound listening address.

**Parameters**

- `Transport` (`string`): `udp` or `ws`.
- `Address` (`string`): The address text the listener bound.
- `Port` (`int`): The port; for a configured port of 0, the one the system picked.

## Properties

| Member | Description |
| --- | --- |
| [`Address`](#m-address) | The address text the listener bound. |
| [`Port`](#m-port) | The port; for a configured port of 0, the one the system picked. |
| [`Transport`](#m-transport) | `udp` or `ws`. |

### <a id="m-address"></a>Address

```csharp
public string Address { get; init; }
```

The address text the listener bound.

**Type:** `string`

### <a id="m-port"></a>Port

```csharp
public int Port { get; init; }
```

The port; for a configured port of 0, the one the system picked.

**Type:** `int`

### <a id="m-transport"></a>Transport

```csharp
public string Transport { get; init; }
```

`udp` or `ws`.

**Type:** `string`
