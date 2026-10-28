[API reference](../../index.md) › [OneGame.Server.Live](../index.md) › OneGame.Server.Live

# ServerReadyLine record

**Namespace:** `OneGame.Server.Live`  
**Package:** [`RecursiveAI.OneGame.Server.Live`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ServerReadyLine`](ServerReadyLine.md)&gt;

```csharp
public sealed record ServerReadyLine : IEquatable<ServerReadyLine>
```

The line printed on standard output once the server is up, so a launcher can find its live socket and listening ports.

## Constructors

| Member | Description |
| --- | --- |
| [`ServerReadyLine(bool, int, string?, ListenEndpoint[])`](#m-ctor-bool-int-string-listenendpoint) | The line printed on standard output once the server is up, so a launcher can find its live socket and listening ports. |

### <a id="m-ctor-bool-int-string-listenendpoint"></a>ServerReadyLine(bool, int, string?, ListenEndpoint\[\])

```csharp
public ServerReadyLine(bool Ready, int Pid, string? Live, ListenEndpoint[] Listen);
```

The line printed on standard output once the server is up, so a launcher can find its live socket and listening ports.

**Parameters**

- `Ready` (`bool`): Always true.
- `Pid` (`int`): The process id.
- `Live` (`string?`): The live-control socket path, or null when none was asked for.
- `Listen` ([`ListenEndpoint`](../../OneGame.Server/OneGame.Server/ListenEndpoint.md)\[\]): The bound listening addresses.

## Properties

| Member | Description |
| --- | --- |
| [`Listen`](#m-listen) | The bound listening addresses. |
| [`Live`](#m-live) | The live-control socket path, or null when none was asked for. |
| [`Pid`](#m-pid) | The process id. |
| [`Ready`](#m-ready) | Always true. |

### <a id="m-listen"></a>Listen

```csharp
public ListenEndpoint[] Listen { get; init; }
```

The bound listening addresses.

**Type:** [`ListenEndpoint`](../../OneGame.Server/OneGame.Server/ListenEndpoint.md)\[\]

### <a id="m-live"></a>Live

```csharp
public string? Live { get; init; }
```

The live-control socket path, or null when none was asked for.

**Type:** `string?`

### <a id="m-pid"></a>Pid

```csharp
public int Pid { get; init; }
```

The process id.

**Type:** `int`

### <a id="m-ready"></a>Ready

```csharp
public bool Ready { get; init; }
```

Always true.

**Type:** `bool`
