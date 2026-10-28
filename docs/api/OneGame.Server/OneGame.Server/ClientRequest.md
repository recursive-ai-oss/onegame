[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# ClientRequest record

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ClientRequest`](ClientRequest.md)&gt;

```csharp
public sealed record ClientRequest : IEquatable<ClientRequest>
```

A client that has just connected, as the authentication hook sees it.

## Constructors

| Member | Description |
| --- | --- |
| [`ClientRequest(string, string, string)`](#m-ctor-string-string-string) | A client that has just connected, as the authentication hook sees it. |

### <a id="m-ctor-string-string-string"></a>ClientRequest(string, string, string)

```csharp
public ClientRequest(string Name, string Transport, string Address);
```

A client that has just connected, as the authentication hook sees it.

**Parameters**

- `Name` (`string`): The name the connection will have in reports (transport and remote address).
- `Transport` (`string`): `udp` or `ws`.
- `Address` (`string`): The client's remote address as text.

## Properties

| Member | Description |
| --- | --- |
| [`Address`](#m-address) | The client's remote address as text. |
| [`Name`](#m-name) | The name the connection will have in reports (transport and remote address). |
| [`Transport`](#m-transport) | `udp` or `ws`. |

### <a id="m-address"></a>Address

```csharp
public string Address { get; init; }
```

The client's remote address as text.

**Type:** `string`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The name the connection will have in reports (transport and remote address).

**Type:** `string`

### <a id="m-transport"></a>Transport

```csharp
public string Transport { get; init; }
```

`udp` or `ws`.

**Type:** `string`
