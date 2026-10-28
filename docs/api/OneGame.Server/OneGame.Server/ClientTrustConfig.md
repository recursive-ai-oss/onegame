[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# ClientTrustConfig record

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ClientTrustConfig`](ClientTrustConfig.md)&gt;

```csharp
public sealed record ClientTrustConfig : IEquatable<ClientTrustConfig>
```

What clients may do on this server. A null list means every scope the server hosts.

## Constructors

| Member | Description |
| --- | --- |
| [`ClientTrustConfig(uint[]?, uint[]?)`](#m-ctor-uint-uint) | What clients may do on this server. |

### <a id="m-ctor-uint-uint"></a>ClientTrustConfig(uint\[\]?, uint\[\]?)

```csharp
public ClientTrustConfig(uint[]? Read, uint[]? Command);
```

What clients may do on this server. A null list means every scope the server hosts.

**Parameters**

- `Read` (`uint[]?`): Scopes clients may subscribe to.
- `Command` (`uint[]?`): Scopes clients may send commands to.

## Properties

| Member | Description |
| --- | --- |
| [`Command`](#m-command) | Scopes clients may send commands to. |
| [`Read`](#m-read) | Scopes clients may subscribe to. |

### <a id="m-command"></a>Command

```csharp
public uint[]? Command { get; init; }
```

Scopes clients may send commands to.

**Type:** `uint[]?`

### <a id="m-read"></a>Read

```csharp
public uint[]? Read { get; init; }
```

Scopes clients may subscribe to.

**Type:** `uint[]?`
