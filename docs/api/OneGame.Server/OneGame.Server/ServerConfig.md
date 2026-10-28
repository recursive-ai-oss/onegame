[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# ServerConfig class

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)

```csharp
public sealed class ServerConfig
```

The server configuration. Unknown keys are errors, so a misspelled setting never silently does nothing.

```csharp
name: arena
tickRate: 60
scopes:
  - { id: 1, seed: 42 }
plugins:
  - name: counter
    path: plugins/counter
    scopes: [1]
    settings: { step: 2 }
```

## Constructors

| Member | Description |
| --- | --- |
| [`ServerConfig()`](#m-ctor) |  |

### <a id="m-ctor"></a>ServerConfig()

```csharp
public ServerConfig();
```

## Fields

| Member | Description |
| --- | --- |
| [`MaxEntries`](#m-maxentries) | The most scopes or plugins a file may declare; a bound so a hostile file cannot ask for millions. |

### <a id="m-maxentries"></a>MaxEntries

```csharp
public const int MaxEntries = 1024;
```

The most scopes or plugins a file may declare; a bound so a hostile file cannot ask for millions.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`ClientTrust`](#m-clienttrust) | What every accepted client may do, unless [`ServerHost.Authenticate`](ServerHost.md#m-authenticate) decides otherwise. |
| [`HashHistory`](#m-hashhistory) | How many recent ticks of per-scope state hashes to keep so another node's hash can be compared at the same tick; 0 turns it off. |
| [`Listen`](#m-listen) | The addresses to listen on for clients; none means a server that only ticks. |
| [`Live`](#m-live) | A socket path for live control, or null for none (the runner may supply its own). |
| [`MaxCatchUp`](#m-maxcatchup) | The most ticks run for one pass of the loop when the server falls behind; the rest are dropped. |
| [`Name`](#m-name) | The server's name, used for the node and in logs. |
| [`Options`](#m-options) | Free-form `key=value` settings for the game's own server code (the command line's `--option key=value`), for example the addresses of other servers to connect to. |
| [`Plugins`](#m-plugins) | The plugins to load, in order. |
| [`Scopes`](#m-scopes) | The scopes this server is authority for (default: one scope, id 1, seed 1). |
| [`Security`](#m-security) | How client links are encrypted: the key files, the game id, or an explicit insecure switch. |
| [`TickRate`](#m-tickrate) | Simulation ticks per second (default 60). |

### <a id="m-clienttrust"></a>ClientTrust

```csharp
public ClientTrustConfig ClientTrust { get; init; }
```

What every accepted client may do, unless [`ServerHost.Authenticate`](ServerHost.md#m-authenticate) decides otherwise.

**Type:** [`ClientTrustConfig`](ClientTrustConfig.md)

### <a id="m-hashhistory"></a>HashHistory

```csharp
public int HashHistory { get; init; }
```

How many recent ticks of per-scope state hashes to keep so another node's hash can be compared at the same tick; 0 turns it off.

**Type:** `int`

### <a id="m-listen"></a>Listen

```csharp
public IReadOnlyList<ListenConfig> Listen { get; init; }
```

The addresses to listen on for clients; none means a server that only ticks.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ListenConfig`](ListenConfig.md)&gt;

### <a id="m-live"></a>Live

```csharp
public string? Live { get; init; }
```

A socket path for live control, or null for none (the runner may supply its own).

**Type:** `string?`

### <a id="m-maxcatchup"></a>MaxCatchUp

```csharp
public int MaxCatchUp { get; init; }
```

The most ticks run for one pass of the loop when the server falls behind; the rest are dropped.

**Type:** `int`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The server's name, used for the node and in logs.

**Type:** `string`

### <a id="m-options"></a>Options

```csharp
public IReadOnlyList<KeyValuePair<string, string>> Options { get; init; }
```

Free-form `key=value` settings for the game's own server code (the command line's `--option key=value`), for example the addresses of other servers to connect to.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`KeyValuePair`](https://learn.microsoft.com/dotnet/api/system.collections.generic.keyvaluepair-2)`<string, string>>`

### <a id="m-plugins"></a>Plugins

```csharp
public IReadOnlyList<PluginConfig> Plugins { get; init; }
```

The plugins to load, in order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`PluginConfig`](PluginConfig.md)&gt;

### <a id="m-scopes"></a>Scopes

```csharp
public IReadOnlyList<ScopeConfig> Scopes { get; init; }
```

The scopes this server is authority for (default: one scope, id 1, seed 1). Each has its own world and random streams, seeded from [`ScopeConfig.Seed`](ScopeConfig.md#m-seed).

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ScopeConfig`](ScopeConfig.md)&gt;

### <a id="m-security"></a>Security

```csharp
public SecurityConfig Security { get; init; }
```

How client links are encrypted: the key files, the game id, or an explicit insecure switch. A server that listens with neither fails to start with [`SecurityNotConfiguredException`](../../OneGame.Net/OneGame.Net/SecurityNotConfiguredException.md).

**Type:** [`SecurityConfig`](SecurityConfig.md)

### <a id="m-tickrate"></a>TickRate

```csharp
public int TickRate { get; init; }
```

Simulation ticks per second (default 60). Every hosted scope steps at this rate; clients see the same rate in their replicas.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Load(string)`](#m-load-string) | Reads a configuration file. |
| [`Parse(string, string?)`](#m-parse-string-string) | Reads configuration text. |

### <a id="m-load-string"></a>Load(string)

```csharp
public static ServerConfig Load(string path);
```

Reads a configuration file.

**Parameters**

- `path` (`string`): The YAML file.

**Returns:** [`ServerConfig`](ServerConfig.md)

**Exceptions**

- [`ServerConfigException`](ServerConfigException.md): The file is missing or invalid.

### <a id="m-parse-string-string"></a>Parse(string, string?)

```csharp
public static ServerConfig Parse(string yaml, string? source = null);
```

Reads configuration text.

**Parameters**

- `yaml` (`string`): The YAML text.
- `source` (`string?`): The file name for messages, and the folder relative plugin paths resolve against.

**Returns:** [`ServerConfig`](ServerConfig.md)

**Exceptions**

- [`ServerConfigException`](ServerConfigException.md): The text is not a valid configuration.
