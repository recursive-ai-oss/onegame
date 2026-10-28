[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# NodeStatusLine record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`NodeStatusLine`](NodeStatusLine.md)&gt;

```csharp
public sealed record NodeStatusLine : IEquatable<NodeStatusLine>
```

What `cluster status` reports for one node.

## Constructors

| Member | Description |
| --- | --- |
| [`NodeStatusLine(string, string, int, bool, long, string?, string)`](#m-ctor-string-string-int-bool-long-string-string) | What `cluster status` reports for one node. |

### <a id="m-ctor-string-string-int-bool-long-string-string"></a>NodeStatusLine(string, string, int, bool, long, string?, string)

```csharp
public NodeStatusLine(string Name, string Role, int Pid, bool Alive, long Tick, string? Hash, string Detail);
```

What `cluster status` reports for one node.

**Parameters**

- `Name` (`string`): The node's name.
- `Role` (`string`): `server` or `client`.
- `Pid` (`int`): The process id.
- `Alive` (`bool`): Whether the process exists.
- `Tick` (`long`): The node's tick, or -1 when it could not be asked.
- `Hash` (`string?`): The node's state hash.
- `Detail` (`string`): The node's own status JSON (`server_info` or `net_status`), or the reason it could not be read.

## Properties

| Member | Description |
| --- | --- |
| [`Alive`](#m-alive) | Whether the process exists. |
| [`Detail`](#m-detail) | The node's own status JSON (`server_info` or `net_status`), or the reason it could not be read. |
| [`Hash`](#m-hash) | The node's state hash. |
| [`Name`](#m-name) | The node's name. |
| [`Pid`](#m-pid) | The process id. |
| [`Role`](#m-role) | `server` or `client`. |
| [`Tick`](#m-tick) | The node's tick, or -1 when it could not be asked. |

### <a id="m-alive"></a>Alive

```csharp
public bool Alive { get; init; }
```

Whether the process exists.

**Type:** `bool`

### <a id="m-detail"></a>Detail

```csharp
public string Detail { get; init; }
```

The node's own status JSON (`server_info` or `net_status`), or the reason it could not be read.

**Type:** `string`

### <a id="m-hash"></a>Hash

```csharp
public string? Hash { get; init; }
```

The node's state hash.

**Type:** `string?`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The node's name.

**Type:** `string`

### <a id="m-pid"></a>Pid

```csharp
public int Pid { get; init; }
```

The process id.

**Type:** `int`

### <a id="m-role"></a>Role

```csharp
public string Role { get; init; }
```

`server` or `client`.

**Type:** `string`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The node's tick, or -1 when it could not be asked.

**Type:** `long`
