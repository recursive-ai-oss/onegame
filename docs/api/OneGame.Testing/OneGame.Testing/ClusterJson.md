[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# ClusterJson class

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Inherits:** [`JsonSerializerContext`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.jsonserializercontext)  
**Implements:** [`IJsonTypeInfoResolver`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.ijsontypeinforesolver)

```csharp
public sealed class ClusterJson : JsonSerializerContext, IJsonTypeInfoResolver
```

Source-generated JSON for the cluster's files and reports.

## Constructors

| Member | Description |
| --- | --- |
| [`ClusterJson()`](#m-ctor) |  |
| [`ClusterJson(JsonSerializerOptions)`](#m-ctor-jsonserializeroptions) |  |

### <a id="m-ctor"></a>ClusterJson()

```csharp
public ClusterJson();
```

Creates a new instance.

### <a id="m-ctor-jsonserializeroptions"></a>ClusterJson(JsonSerializerOptions)

```csharp
public ClusterJson(JsonSerializerOptions options);
```

Creates a new instance.

**Parameters**

- `options` ([`JsonSerializerOptions`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions))

## Properties

| Member | Description |
| --- | --- |
| [`Boolean`](#m-boolean) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ClusterHashReport`](#m-clusterhashreport) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ClusterState`](#m-clusterstate) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ClusterStatus`](#m-clusterstatus) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`Default`](#m-default) | The default [`JsonSerializerContext`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.jsonserializercontext) associated with a default [`JsonSerializerOptions`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions) instance. |
| [`DownReport`](#m-downreport) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`DriftEntry`](#m-driftentry) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`DriftEntryArray`](#m-driftentryarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`EndpointState`](#m-endpointstate) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`EndpointStateArray`](#m-endpointstatearray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`GeneratedSerializerOptions`](#m-generatedserializeroptions) | The source-generated options associated with this context. |
| [`HashCheck`](#m-hashcheck) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`HashCheckArray`](#m-hashcheckarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`Int32`](#m-int32) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`Int64`](#m-int64) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`NodeHash`](#m-nodehash) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`NodeHashArray`](#m-nodehasharray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`NodeState`](#m-nodestate) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`NodeStateArray`](#m-nodestatearray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`NodeStatusLine`](#m-nodestatusline) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`NodeStatusLineArray`](#m-nodestatuslinearray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`String`](#m-string) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`StringArray`](#m-stringarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`UInt32`](#m-uint32) | Defines the source generated JSON serialization contract metadata for a given type. |

### <a id="m-boolean"></a>Boolean

```csharp
public JsonTypeInfo<bool> Boolean { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<bool>`

### <a id="m-clusterhashreport"></a>ClusterHashReport

```csharp
public JsonTypeInfo<ClusterHashReport> ClusterHashReport { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ClusterHashReport`](ClusterHashReport.md)&gt;

### <a id="m-clusterstate"></a>ClusterState

```csharp
public JsonTypeInfo<ClusterState> ClusterState { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ClusterState`](ClusterState.md)&gt;

### <a id="m-clusterstatus"></a>ClusterStatus

```csharp
public JsonTypeInfo<ClusterStatus> ClusterStatus { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ClusterStatus`](ClusterStatus.md)&gt;

### <a id="m-default"></a>Default

```csharp
public static ClusterJson Default { get; }
```

The default [`JsonSerializerContext`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.jsonserializercontext) associated with a default [`JsonSerializerOptions`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions) instance.

**Type:** [`ClusterJson`](ClusterJson.md)

### <a id="m-downreport"></a>DownReport

```csharp
public JsonTypeInfo<DownReport> DownReport { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`DownReport`](DownReport.md)&gt;

### <a id="m-driftentry"></a>DriftEntry

```csharp
public JsonTypeInfo<DriftEntry> DriftEntry { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`DriftEntry`](DriftEntry.md)&gt;

### <a id="m-driftentryarray"></a>DriftEntryArray

```csharp
public JsonTypeInfo<DriftEntry[]> DriftEntryArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`DriftEntry`](DriftEntry.md)\[\]&gt;

### <a id="m-endpointstate"></a>EndpointState

```csharp
public JsonTypeInfo<EndpointState> EndpointState { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`EndpointState`](EndpointState.md)&gt;

### <a id="m-endpointstatearray"></a>EndpointStateArray

```csharp
public JsonTypeInfo<EndpointState[]> EndpointStateArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`EndpointState`](EndpointState.md)\[\]&gt;

### <a id="m-generatedserializeroptions"></a>GeneratedSerializerOptions

```csharp
protected override JsonSerializerOptions? GeneratedSerializerOptions { get; }
```

The source-generated options associated with this context.

**Type:** [`JsonSerializerOptions`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions)?

### <a id="m-hashcheck"></a>HashCheck

```csharp
public JsonTypeInfo<HashCheck> HashCheck { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`HashCheck`](HashCheck.md)&gt;

### <a id="m-hashcheckarray"></a>HashCheckArray

```csharp
public JsonTypeInfo<HashCheck[]> HashCheckArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`HashCheck`](HashCheck.md)\[\]&gt;

### <a id="m-int32"></a>Int32

```csharp
public JsonTypeInfo<int> Int32 { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<int>`

### <a id="m-int64"></a>Int64

```csharp
public JsonTypeInfo<long> Int64 { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<long>`

### <a id="m-nodehash"></a>NodeHash

```csharp
public JsonTypeInfo<NodeHash> NodeHash { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`NodeHash`](NodeHash.md)&gt;

### <a id="m-nodehasharray"></a>NodeHashArray

```csharp
public JsonTypeInfo<NodeHash[]> NodeHashArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`NodeHash`](NodeHash.md)\[\]&gt;

### <a id="m-nodestate"></a>NodeState

```csharp
public JsonTypeInfo<NodeState> NodeState { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`NodeState`](NodeState.md)&gt;

### <a id="m-nodestatearray"></a>NodeStateArray

```csharp
public JsonTypeInfo<NodeState[]> NodeStateArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`NodeState`](NodeState.md)\[\]&gt;

### <a id="m-nodestatusline"></a>NodeStatusLine

```csharp
public JsonTypeInfo<NodeStatusLine> NodeStatusLine { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`NodeStatusLine`](NodeStatusLine.md)&gt;

### <a id="m-nodestatuslinearray"></a>NodeStatusLineArray

```csharp
public JsonTypeInfo<NodeStatusLine[]> NodeStatusLineArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`NodeStatusLine`](NodeStatusLine.md)\[\]&gt;

### <a id="m-string"></a>String

```csharp
public JsonTypeInfo<string> String { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<string>`

### <a id="m-stringarray"></a>StringArray

```csharp
public JsonTypeInfo<string[]> StringArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<string[]>`

### <a id="m-uint32"></a>UInt32

```csharp
public JsonTypeInfo<uint> UInt32 { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<uint>`

## Methods

| Member | Description |
| --- | --- |
| [`GetTypeInfo(Type)`](#m-gettypeinfo-type) |  |

### <a id="m-gettypeinfo-type"></a>GetTypeInfo(Type)

```csharp
public override JsonTypeInfo? GetTypeInfo(Type type);
```

Overrides [`JsonSerializerContext.GetTypeInfo()`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.jsonserializercontext.gettypeinfo).

**Parameters**

- `type` ([`Type`](https://learn.microsoft.com/dotnet/api/system.type))

**Returns:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo)?
