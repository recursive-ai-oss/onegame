[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# DriftInfo record

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`DriftInfo`](DriftInfo.md)&gt;

```csharp
public sealed record DriftInfo : IEquatable<DriftInfo>
```

One drift report: a replica whose state differed from the server's at the same tick.

## Constructors

| Member | Description |
| --- | --- |
| [`DriftInfo(string, uint, long, string, string, string[])`](#m-ctor-string-uint-long-string-string-string) | One drift report: a replica whose state differed from the server's at the same tick. |

### <a id="m-ctor-string-uint-long-string-string-string"></a>DriftInfo(string, uint, long, string, string, string\[\])

```csharp
public DriftInfo(string Node, uint Scope, long Tick, string Expected, string Actual, string[] Differences);
```

One drift report: a replica whose state differed from the server's at the same tick.

**Parameters**

- `Node` (`string`): The node holding the replica.
- `Scope` (`uint`): The scope.
- `Tick` (`long`): The tick at which the hashes differed.
- `Expected` (`string`): The server's hash (16 hex digits).
- `Actual` (`string`): The replica's hash.
- `Differences` (`string[]`): The first entities, tables or streams that differ, one line each.

## Properties

| Member | Description |
| --- | --- |
| [`Actual`](#m-actual) | The replica's hash. |
| [`Differences`](#m-differences) | The first entities, tables or streams that differ, one line each. |
| [`Expected`](#m-expected) | The server's hash (16 hex digits). |
| [`Node`](#m-node) | The node holding the replica. |
| [`Scope`](#m-scope) | The scope. |
| [`Tick`](#m-tick) | The tick at which the hashes differed. |

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

The first entities, tables or streams that differ, one line each.

**Type:** `string[]`

### <a id="m-expected"></a>Expected

```csharp
public string Expected { get; init; }
```

The server's hash (16 hex digits).

**Type:** `string`

### <a id="m-node"></a>Node

```csharp
public string Node { get; init; }
```

The node holding the replica.

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

The tick at which the hashes differed.

**Type:** `long`
