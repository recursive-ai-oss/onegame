[API reference](../../index.md) › [OneGame.Server.Live](../index.md) › OneGame.Server.Live

# ServerAppJson class

**Namespace:** `OneGame.Server.Live`  
**Package:** [`RecursiveAI.OneGame.Server.Live`](../index.md)  
**Inherits:** [`JsonSerializerContext`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.jsonserializercontext)  
**Implements:** [`IJsonTypeInfoResolver`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.ijsontypeinforesolver)

```csharp
public sealed class ServerAppJson : JsonSerializerContext, IJsonTypeInfoResolver
```

Source-generated JSON for the lines a server process prints.

## Constructors

| Member | Description |
| --- | --- |
| [`ServerAppJson()`](#m-ctor) |  |
| [`ServerAppJson(JsonSerializerOptions)`](#m-ctor-jsonserializeroptions) |  |

### <a id="m-ctor"></a>ServerAppJson()

```csharp
public ServerAppJson();
```

Creates a new instance.

### <a id="m-ctor-jsonserializeroptions"></a>ServerAppJson(JsonSerializerOptions)

```csharp
public ServerAppJson(JsonSerializerOptions options);
```

Creates a new instance.

**Parameters**

- `options` ([`JsonSerializerOptions`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions))

## Properties

| Member | Description |
| --- | --- |
| [`Boolean`](#m-boolean) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`Default`](#m-default) | The default [`JsonSerializerContext`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.jsonserializercontext) associated with a default [`JsonSerializerOptions`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions) instance. |
| [`GeneratedSerializerOptions`](#m-generatedserializeroptions) | The source-generated options associated with this context. |
| [`Int32`](#m-int32) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`Int64`](#m-int64) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ListenEndpoint`](#m-listenendpoint) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ListenEndpointArray`](#m-listenendpointarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ScopeInfo`](#m-scopeinfo) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ScopeInfoArray`](#m-scopeinfoarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ServerReadyLine`](#m-serverreadyline) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ServerStoppedLine`](#m-serverstoppedline) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`String`](#m-string) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`UInt32`](#m-uint32) | Defines the source generated JSON serialization contract metadata for a given type. |

### <a id="m-boolean"></a>Boolean

```csharp
public JsonTypeInfo<bool> Boolean { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<bool>`

### <a id="m-default"></a>Default

```csharp
public static ServerAppJson Default { get; }
```

The default [`JsonSerializerContext`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.jsonserializercontext) associated with a default [`JsonSerializerOptions`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions) instance.

**Type:** [`ServerAppJson`](ServerAppJson.md)

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

### <a id="m-serverreadyline"></a>ServerReadyLine

```csharp
public JsonTypeInfo<ServerReadyLine> ServerReadyLine { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ServerReadyLine`](ServerReadyLine.md)&gt;

### <a id="m-serverstoppedline"></a>ServerStoppedLine

```csharp
public JsonTypeInfo<ServerStoppedLine> ServerStoppedLine { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ServerStoppedLine`](ServerStoppedLine.md)&gt;

### <a id="m-string"></a>String

```csharp
public JsonTypeInfo<string> String { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<string>`

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
