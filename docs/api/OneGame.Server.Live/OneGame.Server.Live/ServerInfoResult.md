[API reference](../../index.md) › [OneGame.Server.Live](../index.md) › OneGame.Server.Live

# ServerInfoResult record

**Namespace:** `OneGame.Server.Live`  
**Package:** [`RecursiveAI.OneGame.Server.Live`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ServerInfoResult`](ServerInfoResult.md)&gt;

```csharp
public sealed record ServerInfoResult : IEquatable<ServerInfoResult>
```

Result of `server_info`.

`Listen` is where the server accepts clients (ports resolved) and `Clients` how many are connected.

## Constructors

| Member | Description |
| --- | --- |
| [`ServerInfoResult(string, string, long, int, ScopeInfo[], PluginInfo[], PluginLoadFailure[], ListenEndpoint[], int)`](#m-ctor-string-string-long-int-scopeinfo-plugininfo-pluginloadfailure-listenendpoint-int) | Result of `server_info`. |

### <a id="m-ctor-string-string-long-int-scopeinfo-plugininfo-pluginloadfailure-listenendpoint-int"></a>ServerInfoResult(string, string, long, int, ScopeInfo\[\], PluginInfo\[\], PluginLoadFailure\[\], ListenEndpoint\[\], int)

```csharp
public ServerInfoResult(string Name, string State, long Tick, int TickRate, ScopeInfo[] Scopes, PluginInfo[] Plugins, PluginLoadFailure[] LoadFailures, ListenEndpoint[] Listen, int Clients);
```

Result of `server_info`.

`Listen` is where the server accepts clients (ports resolved) and `Clients` how many are connected.

**Parameters**

- `Name` (`string`)
- `State` (`string`)
- `Tick` (`long`)
- `TickRate` (`int`)
- `Scopes` ([`ScopeInfo`](../../OneGame.Server/OneGame.Server/ScopeInfo.md)\[\])
- `Plugins` ([`PluginInfo`](../../OneGame.Server/OneGame.Server/PluginInfo.md)\[\])
- `LoadFailures` ([`PluginLoadFailure`](../../OneGame.Server/OneGame.Server/PluginLoadFailure.md)\[\])
- `Listen` ([`ListenEndpoint`](../../OneGame.Server/OneGame.Server/ListenEndpoint.md)\[\])
- `Clients` (`int`)

## Properties

| Member | Description |
| --- | --- |
| [`Clients`](#m-clients) |  |
| [`Listen`](#m-listen) |  |
| [`LoadFailures`](#m-loadfailures) |  |
| [`Name`](#m-name) |  |
| [`Plugins`](#m-plugins) |  |
| [`Scopes`](#m-scopes) |  |
| [`State`](#m-state) |  |
| [`Tick`](#m-tick) |  |
| [`TickRate`](#m-tickrate) |  |

### <a id="m-clients"></a>Clients

```csharp
public int Clients { get; init; }
```

**Type:** `int`

### <a id="m-listen"></a>Listen

```csharp
public ListenEndpoint[] Listen { get; init; }
```

**Type:** [`ListenEndpoint`](../../OneGame.Server/OneGame.Server/ListenEndpoint.md)\[\]

### <a id="m-loadfailures"></a>LoadFailures

```csharp
public PluginLoadFailure[] LoadFailures { get; init; }
```

**Type:** [`PluginLoadFailure`](../../OneGame.Server/OneGame.Server/PluginLoadFailure.md)\[\]

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

**Type:** `string`

### <a id="m-plugins"></a>Plugins

```csharp
public PluginInfo[] Plugins { get; init; }
```

**Type:** [`PluginInfo`](../../OneGame.Server/OneGame.Server/PluginInfo.md)\[\]

### <a id="m-scopes"></a>Scopes

```csharp
public ScopeInfo[] Scopes { get; init; }
```

**Type:** [`ScopeInfo`](../../OneGame.Server/OneGame.Server/ScopeInfo.md)\[\]

### <a id="m-state"></a>State

```csharp
public string State { get; init; }
```

**Type:** `string`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

**Type:** `long`

### <a id="m-tickrate"></a>TickRate

```csharp
public int TickRate { get; init; }
```

**Type:** `int`
