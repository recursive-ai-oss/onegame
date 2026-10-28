[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# HashCheck record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`HashCheck`](HashCheck.md)&gt;

```csharp
public sealed record HashCheck : IEquatable<HashCheck>
```

A client replica compared with its server's recorded hash at the replica's tick.

## Constructors

| Member | Description |
| --- | --- |
| [`HashCheck(string, string, uint, long, string, string?, bool, string?)`](#m-ctor-string-string-uint-long-string-string-bool-string) | A client replica compared with its server's recorded hash at the replica's tick. |

### <a id="m-ctor-string-string-uint-long-string-string-bool-string"></a>HashCheck(string, string, uint, long, string, string?, bool, string?)

```csharp
public HashCheck(string Client, string Server, uint Scope, long Tick, string ClientHash, string? ServerHash, bool Match, string? Note);
```

A client replica compared with its server's recorded hash at the replica's tick.

**Parameters**

- `Client` (`string`): The client node.
- `Server` (`string`): The server node.
- `Scope` (`uint`): The scope.
- `Tick` (`long`): The replica's tick, at which both hashes are compared.
- `ClientHash` (`string`): The replica's hash.
- `ServerHash` (`string?`): The server's hash at that tick, or null when it was not recorded.
- `Match` (`bool`): True when the hashes are equal.
- `Note` (`string?`): Why the check could not be made or what differs.

## Properties

| Member | Description |
| --- | --- |
| [`Client`](#m-client) | The client node. |
| [`ClientHash`](#m-clienthash) | The replica's hash. |
| [`Match`](#m-match) | True when the hashes are equal. |
| [`Note`](#m-note) | Why the check could not be made or what differs. |
| [`Scope`](#m-scope) | The scope. |
| [`Server`](#m-server) | The server node. |
| [`ServerHash`](#m-serverhash) | The server's hash at that tick, or null when it was not recorded. |
| [`Tick`](#m-tick) | The replica's tick, at which both hashes are compared. |

### <a id="m-client"></a>Client

```csharp
public string Client { get; init; }
```

The client node.

**Type:** `string`

### <a id="m-clienthash"></a>ClientHash

```csharp
public string ClientHash { get; init; }
```

The replica's hash.

**Type:** `string`

### <a id="m-match"></a>Match

```csharp
public bool Match { get; init; }
```

True when the hashes are equal.

**Type:** `bool`

### <a id="m-note"></a>Note

```csharp
public string? Note { get; init; }
```

Why the check could not be made or what differs.

**Type:** `string?`

### <a id="m-scope"></a>Scope

```csharp
public uint Scope { get; init; }
```

The scope.

**Type:** `uint`

### <a id="m-server"></a>Server

```csharp
public string Server { get; init; }
```

The server node.

**Type:** `string`

### <a id="m-serverhash"></a>ServerHash

```csharp
public string? ServerHash { get; init; }
```

The server's hash at that tick, or null when it was not recorded.

**Type:** `string?`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The replica's tick, at which both hashes are compared.

**Type:** `long`
