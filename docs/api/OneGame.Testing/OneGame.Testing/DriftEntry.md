[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# DriftEntry record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`DriftEntry`](DriftEntry.md)&gt;

```csharp
public sealed record DriftEntry : IEquatable<DriftEntry>
```

A drift report filed by a client's replica.

## Constructors

| Member | Description |
| --- | --- |
| [`DriftEntry(string, uint, long, string, string, string[])`](#m-ctor-string-uint-long-string-string-string) | A drift report filed by a client's replica. |

### <a id="m-ctor-string-uint-long-string-string-string"></a>DriftEntry(string, uint, long, string, string, string\[\])

```csharp
public DriftEntry(string Node, uint Scope, long Tick, string Expected, string Actual, string[] Differences);
```

A drift report filed by a client's replica.

**Parameters**

- `Node` (`string`): The client node.
- `Scope` (`uint`): The scope.
- `Tick` (`long`): The tick at which the replica differed from the server.
- `Expected` (`string`): The server's hash.
- `Actual` (`string`): The replica's hash.
- `Differences` (`string[]`): The first entities that differ.

## Properties

| Member | Description |
| --- | --- |
| [`Actual`](#m-actual) | The replica's hash. |
| [`Differences`](#m-differences) | The first entities that differ. |
| [`Expected`](#m-expected) | The server's hash. |
| [`Node`](#m-node) | The client node. |
| [`Scope`](#m-scope) | The scope. |
| [`Tick`](#m-tick) | The tick at which the replica differed from the server. |

### <a id="m-actual"></a>Actual

```csharp
public string Actual { get; init; }
```

The replica's hash.

**Type:** `string`

### <a id="m-differences"></a>Differences

```csharp
public string[] Differences { get; init; }
```

The first entities that differ.

**Type:** `string[]`

### <a id="m-expected"></a>Expected

```csharp
public string Expected { get; init; }
```

The server's hash.

**Type:** `string`

### <a id="m-node"></a>Node

```csharp
public string Node { get; init; }
```

The client node.

**Type:** `string`

### <a id="m-scope"></a>Scope

```csharp
public uint Scope { get; init; }
```

The scope.

**Type:** `uint`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The tick at which the replica differed from the server.

**Type:** `long`
