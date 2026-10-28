[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# ScenarioJson class

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Inherits:** [`JsonSerializerContext`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.jsonserializercontext)  
**Implements:** [`IJsonTypeInfoResolver`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.ijsontypeinforesolver)

```csharp
public sealed class ScenarioJson : JsonSerializerContext, IJsonTypeInfoResolver
```

Source-generated JSON for scenario results.

## Constructors

| Member | Description |
| --- | --- |
| [`ScenarioJson()`](#m-ctor) |  |
| [`ScenarioJson(JsonSerializerOptions)`](#m-ctor-jsonserializeroptions) |  |

### <a id="m-ctor"></a>ScenarioJson()

```csharp
public ScenarioJson();
```

### <a id="m-ctor-jsonserializeroptions"></a>ScenarioJson(JsonSerializerOptions)

```csharp
public ScenarioJson(JsonSerializerOptions options);
```

**Parameters**

- `options` ([`JsonSerializerOptions`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions))

## Properties

| Member | Description |
| --- | --- |
| [`Boolean`](#m-boolean) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`Default`](#m-default) | The default [`JsonSerializerContext`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.jsonserializercontext) associated with a default [`JsonSerializerOptions`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions) instance. |
| [`ExpectationResult`](#m-expectationresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ExpectationResultArray`](#m-expectationresultarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`GeneratedSerializerOptions`](#m-generatedserializeroptions) | The source-generated options associated with this context. |
| [`Int32`](#m-int32) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`Int64`](#m-int64) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`PlayedSound`](#m-playedsound) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`PlayedSoundArray`](#m-playedsoundarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ScenarioResult`](#m-scenarioresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ScenarioResultArray`](#m-scenarioresultarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`Single`](#m-single) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`StepResult`](#m-stepresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`StepResultArray`](#m-stepresultarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`String`](#m-string) | Defines the source generated JSON serialization contract metadata for a given type. |

### <a id="m-boolean"></a>Boolean

```csharp
public JsonTypeInfo<bool> Boolean { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<bool>`

### <a id="m-default"></a>Default

```csharp
public static ScenarioJson Default { get; }
```

The default [`JsonSerializerContext`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.jsonserializercontext) associated with a default [`JsonSerializerOptions`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions) instance.

**Type:** [`ScenarioJson`](ScenarioJson.md)

### <a id="m-expectationresult"></a>ExpectationResult

```csharp
public JsonTypeInfo<ExpectationResult> ExpectationResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ExpectationResult`](ExpectationResult.md)&gt;

### <a id="m-expectationresultarray"></a>ExpectationResultArray

```csharp
public JsonTypeInfo<ExpectationResult[]> ExpectationResultArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ExpectationResult`](ExpectationResult.md)\[\]&gt;

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

### <a id="m-playedsound"></a>PlayedSound

```csharp
public JsonTypeInfo<PlayedSound> PlayedSound { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`PlayedSound`](../../OneGame.Audio/OneGame.Audio/PlayedSound.md)&gt;

### <a id="m-playedsoundarray"></a>PlayedSoundArray

```csharp
public JsonTypeInfo<PlayedSound[]> PlayedSoundArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`PlayedSound`](../../OneGame.Audio/OneGame.Audio/PlayedSound.md)\[\]&gt;

### <a id="m-scenarioresult"></a>ScenarioResult

```csharp
public JsonTypeInfo<ScenarioResult> ScenarioResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ScenarioResult`](ScenarioResult.md)&gt;

### <a id="m-scenarioresultarray"></a>ScenarioResultArray

```csharp
public JsonTypeInfo<ScenarioResult[]> ScenarioResultArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ScenarioResult`](ScenarioResult.md)\[\]&gt;

### <a id="m-single"></a>Single

```csharp
public JsonTypeInfo<float> Single { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<float>`

### <a id="m-stepresult"></a>StepResult

```csharp
public JsonTypeInfo<StepResult> StepResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`StepResult`](StepResult.md)&gt;

### <a id="m-stepresultarray"></a>StepResultArray

```csharp
public JsonTypeInfo<StepResult[]> StepResultArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`StepResult`](StepResult.md)\[\]&gt;

### <a id="m-string"></a>String

```csharp
public JsonTypeInfo<string> String { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<string>`

## Methods

| Member | Description |
| --- | --- |
| [`GetTypeInfo(Type)`](#m-gettypeinfo-type) |  |

### <a id="m-gettypeinfo-type"></a>GetTypeInfo(Type)

```csharp
public override JsonTypeInfo? GetTypeInfo(Type type);
```

**Parameters**

- `type` ([`Type`](https://learn.microsoft.com/dotnet/api/system.type))

**Returns:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo)?
