[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# ClusterSpec record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ClusterSpec`](ClusterSpec.md)&gt;

```csharp
public sealed record ClusterSpec : IEquatable<ClusterSpec>
```

The nodes of a process cluster and how long to wait for them.

## Constructors

| Member | Description |
| --- | --- |
| [`ClusterSpec()`](#m-ctor) |  |

### <a id="m-ctor"></a>ClusterSpec()

```csharp
public ClusterSpec();
```

## Properties

| Member | Description |
| --- | --- |
| [`Dir`](#m-dir) | Where logs, sockets, generated files and `cluster.json` go. |
| [`Name`](#m-name) | The cluster's name. |
| [`Nodes`](#m-nodes) | The nodes. |
| [`StartupTimeoutMs`](#m-startuptimeoutms) | How long each node may take to come up, in milliseconds. |

### <a id="m-dir"></a>Dir

```csharp
public string? Dir { get; init; }
```

Where logs, sockets, generated files and `cluster.json` go. A fresh temporary folder when null.

**Type:** `string?`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The cluster's name.

**Type:** `string`

### <a id="m-nodes"></a>Nodes

```csharp
public IReadOnlyList<NodeSpec> Nodes { get; init; }
```

The nodes. Servers start first, then clients in order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`NodeSpec`](NodeSpec.md)&gt;

### <a id="m-startuptimeoutms"></a>StartupTimeoutMs

```csharp
public int StartupTimeoutMs { get; init; }
```

How long each node may take to come up, in milliseconds.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Load(string)`](#m-load-string) | Reads a spec file. |
| [`Parse(string, string?)`](#m-parse-string-string) | Reads spec text. |
| [`Validate(string?)`](#m-validate-string) | Checks the nodes against each other. |

### <a id="m-load-string"></a>Load(string)

```csharp
public static ClusterSpec Load(string path);
```

Reads a spec file.

**Parameters**

- `path` (`string`)

**Returns:** [`ClusterSpec`](ClusterSpec.md)

**Exceptions**

- [`ScenarioException`](ScenarioException.md): The file is not a valid cluster spec.

### <a id="m-parse-string-string"></a>Parse(string, string?)

```csharp
public static ClusterSpec Parse(string yaml, string? source = null);
```

Reads spec text. Relative `exe` and `dir` paths resolve against the folder of `source`.

**Parameters**

- `yaml` (`string`)
- `source` (`string?`)

**Returns:** [`ClusterSpec`](ClusterSpec.md)

**Exceptions**

- [`ScenarioException`](ScenarioException.md): The text is not a valid cluster spec.

### <a id="m-validate-string"></a>Validate(string?)

```csharp
public void Validate(string? source = null);
```

Checks the nodes against each other.

**Parameters**

- `source` (`string?`)

**Exceptions**

- [`ScenarioException`](ScenarioException.md): A name is repeated, a client has no server, or a transport is not offered.
