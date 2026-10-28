[API reference](../../index.md) › [OneGame.Server.Live](../index.md) › OneGame.Server.Live

# ServerJson class

**Namespace:** `OneGame.Server.Live`  
**Package:** [`RecursiveAI.OneGame.Server.Live`](../index.md)  
**Inherits:** [`JsonSerializerContext`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.jsonserializercontext)  
**Implements:** [`IJsonTypeInfoResolver`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.ijsontypeinforesolver)

```csharp
public sealed class ServerJson : JsonSerializerContext, IJsonTypeInfoResolver
```

Source-generated JSON metadata for server control results.

## Constructors

| Member | Description |
| --- | --- |
| [`ServerJson()`](#m-ctor) |  |
| [`ServerJson(JsonSerializerOptions)`](#m-ctor-jsonserializeroptions) |  |

### <a id="m-ctor"></a>ServerJson()

```csharp
public ServerJson();
```

Creates a new instance.

### <a id="m-ctor-jsonserializeroptions"></a>ServerJson(JsonSerializerOptions)

```csharp
public ServerJson(JsonSerializerOptions options);
```

Creates a new instance.

**Parameters**

- `options` ([`JsonSerializerOptions`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions))

## Properties

| Member | Description |
| --- | --- |
| [`Boolean`](#m-boolean) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ClientInfo`](#m-clientinfo) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ClientInfoArray`](#m-clientinfoarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`Default`](#m-default) | The default [`JsonSerializerContext`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.jsonserializercontext) associated with a default [`JsonSerializerOptions`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions) instance. |
| [`Double`](#m-double) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`GeneratedSerializerOptions`](#m-generatedserializeroptions) | The source-generated options associated with this context. |
| [`Int32`](#m-int32) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`Int64`](#m-int64) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ListenEndpoint`](#m-listenendpoint) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ListenEndpointArray`](#m-listenendpointarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`LogEntry`](#m-logentry) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`LogEntryArray`](#m-logentryarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`PluginFault`](#m-pluginfault) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`PluginInfo`](#m-plugininfo) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`PluginInfoArray`](#m-plugininfoarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`PluginLoadFailure`](#m-pluginloadfailure) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`PluginLoadFailureArray`](#m-pluginloadfailurearray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`PluginState`](#m-pluginstate) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ReloadResult`](#m-reloadresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ScopeHashAt`](#m-scopehashat) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ScopeHashAtArray`](#m-scopehashatarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ScopeInfo`](#m-scopeinfo) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ScopeInfoArray`](#m-scopeinfoarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ServerInfoResult`](#m-serverinforesult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ServerSendResult`](#m-serversendresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ServerStateResult`](#m-serverstateresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ShutdownResult`](#m-shutdownresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`String`](#m-string) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`StringArray`](#m-stringarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`UInt32`](#m-uint32) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`UInt32Array`](#m-uint32array) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`UInt64`](#m-uint64) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`UnloadResult`](#m-unloadresult) | Defines the source generated JSON serialization contract metadata for a given type. |

### <a id="m-boolean"></a>Boolean

```csharp
public JsonTypeInfo<bool> Boolean { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<bool>`

### <a id="m-clientinfo"></a>ClientInfo

```csharp
public JsonTypeInfo<ClientInfo> ClientInfo { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ClientInfo`](../../OneGame.Server/OneGame.Server/ClientInfo.md)&gt;

### <a id="m-clientinfoarray"></a>ClientInfoArray

```csharp
public JsonTypeInfo<ClientInfo[]> ClientInfoArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ClientInfo`](../../OneGame.Server/OneGame.Server/ClientInfo.md)\[\]&gt;

### <a id="m-default"></a>Default

```csharp
public static ServerJson Default { get; }
```

The default [`JsonSerializerContext`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.jsonserializercontext) associated with a default [`JsonSerializerOptions`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions) instance.

**Type:** [`ServerJson`](ServerJson.md)

### <a id="m-double"></a>Double

```csharp
public JsonTypeInfo<double> Double { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<double>`

### <a id="m-generatedserializeroptions"></a>GeneratedSerializerOptions

```csharp
protected override JsonSerializerOptions? GeneratedSerializerOptions { get; }
```

The source-generated options associated with this context.

**Type:** [`JsonSerializerOptions`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions)?

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

### <a id="m-listenendpoint"></a>ListenEndpoint

```csharp
public JsonTypeInfo<ListenEndpoint> ListenEndpoint { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ListenEndpoint`](../../OneGame.Server/OneGame.Server/ListenEndpoint.md)&gt;

### <a id="m-listenendpointarray"></a>ListenEndpointArray

```csharp
public JsonTypeInfo<ListenEndpoint[]> ListenEndpointArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ListenEndpoint`](../../OneGame.Server/OneGame.Server/ListenEndpoint.md)\[\]&gt;

### <a id="m-logentry"></a>LogEntry

```csharp
public JsonTypeInfo<LogEntry> LogEntry { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`LogEntry`](../../OneGame.Server/OneGame.Server/LogEntry.md)&gt;

### <a id="m-logentryarray"></a>LogEntryArray

```csharp
public JsonTypeInfo<LogEntry[]> LogEntryArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`LogEntry`](../../OneGame.Server/OneGame.Server/LogEntry.md)\[\]&gt;

### <a id="m-pluginfault"></a>PluginFault

```csharp
public JsonTypeInfo<PluginFault> PluginFault { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`PluginFault`](../../OneGame.Server/OneGame.Server/PluginFault.md)&gt;

### <a id="m-plugininfo"></a>PluginInfo

```csharp
public JsonTypeInfo<PluginInfo> PluginInfo { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`PluginInfo`](../../OneGame.Server/OneGame.Server/PluginInfo.md)&gt;

### <a id="m-plugininfoarray"></a>PluginInfoArray

```csharp
public JsonTypeInfo<PluginInfo[]> PluginInfoArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`PluginInfo`](../../OneGame.Server/OneGame.Server/PluginInfo.md)\[\]&gt;

### <a id="m-pluginloadfailure"></a>PluginLoadFailure

```csharp
public JsonTypeInfo<PluginLoadFailure> PluginLoadFailure { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`PluginLoadFailure`](../../OneGame.Server/OneGame.Server/PluginLoadFailure.md)&gt;

### <a id="m-pluginloadfailurearray"></a>PluginLoadFailureArray

```csharp
public JsonTypeInfo<PluginLoadFailure[]> PluginLoadFailureArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`PluginLoadFailure`](../../OneGame.Server/OneGame.Server/PluginLoadFailure.md)\[\]&gt;

### <a id="m-pluginstate"></a>PluginState

```csharp
public JsonTypeInfo<PluginState> PluginState { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`PluginState`](../../OneGame.Server/OneGame.Server/PluginState.md)&gt;

### <a id="m-reloadresult"></a>ReloadResult

```csharp
public JsonTypeInfo<ReloadResult> ReloadResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ReloadResult`](../../OneGame.Server/OneGame.Server/ReloadResult.md)&gt;

### <a id="m-scopehashat"></a>ScopeHashAt

```csharp
public JsonTypeInfo<ScopeHashAt> ScopeHashAt { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ScopeHashAt`](../../OneGame.Server/OneGame.Server/ScopeHashAt.md)&gt;

### <a id="m-scopehashatarray"></a>ScopeHashAtArray

```csharp
public JsonTypeInfo<ScopeHashAt[]> ScopeHashAtArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ScopeHashAt`](../../OneGame.Server/OneGame.Server/ScopeHashAt.md)\[\]&gt;

### <a id="m-scopeinfo"></a>ScopeInfo

```csharp
public JsonTypeInfo<ScopeInfo> ScopeInfo { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ScopeInfo`](../../OneGame.Server/OneGame.Server/ScopeInfo.md)&gt;

### <a id="m-scopeinfoarray"></a>ScopeInfoArray

```csharp
public JsonTypeInfo<ScopeInfo[]> ScopeInfoArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ScopeInfo`](../../OneGame.Server/OneGame.Server/ScopeInfo.md)\[\]&gt;

### <a id="m-serverinforesult"></a>ServerInfoResult

```csharp
public JsonTypeInfo<ServerInfoResult> ServerInfoResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ServerInfoResult`](ServerInfoResult.md)&gt;

### <a id="m-serversendresult"></a>ServerSendResult

```csharp
public JsonTypeInfo<ServerSendResult> ServerSendResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ServerSendResult`](ServerSendResult.md)&gt;

### <a id="m-serverstateresult"></a>ServerStateResult

```csharp
public JsonTypeInfo<ServerStateResult> ServerStateResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ServerStateResult`](ServerStateResult.md)&gt;

### <a id="m-shutdownresult"></a>ShutdownResult

```csharp
public JsonTypeInfo<ShutdownResult> ShutdownResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ShutdownResult`](ShutdownResult.md)&gt;

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

### <a id="m-uint32array"></a>UInt32Array

```csharp
public JsonTypeInfo<uint[]> UInt32Array { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<uint[]>`

### <a id="m-uint64"></a>UInt64

```csharp
public JsonTypeInfo<ulong> UInt64 { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<ulong>`

### <a id="m-unloadresult"></a>UnloadResult

```csharp
public JsonTypeInfo<UnloadResult> UnloadResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`UnloadResult`](../../OneGame.Server/OneGame.Server/UnloadResult.md)&gt;

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
