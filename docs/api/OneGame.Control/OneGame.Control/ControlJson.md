[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# ControlJson class

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Inherits:** [`JsonSerializerContext`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.jsonserializercontext)  
**Implements:** [`IJsonTypeInfoResolver`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.ijsontypeinforesolver)

```csharp
public sealed class ControlJson : JsonSerializerContext, IJsonTypeInfoResolver
```

Source-generated JSON metadata for control results; nothing here uses reflection.

## Constructors

| Member | Description |
| --- | --- |
| [`ControlJson()`](#m-ctor) |  |
| [`ControlJson(JsonSerializerOptions)`](#m-ctor-jsonserializeroptions) |  |

### <a id="m-ctor"></a>ControlJson()

```csharp
public ControlJson();
```

Creates a new instance.

### <a id="m-ctor-jsonserializeroptions"></a>ControlJson(JsonSerializerOptions)

```csharp
public ControlJson(JsonSerializerOptions options);
```

Creates a new instance.

**Parameters**

- `options` ([`JsonSerializerOptions`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions))

## Properties

| Member | Description |
| --- | --- |
| [`AccessReport`](#m-accessreport) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`AccessReportArray`](#m-accessreportarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`AssetEventInfo`](#m-asseteventinfo) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`AssetEventInfoArray`](#m-asseteventinfoarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`AssetInfo`](#m-assetinfo) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`AssetInfoArray`](#m-assetinfoarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`AssetsResult`](#m-assetsresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`AudioCaptureResult`](#m-audiocaptureresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`AudioDeviceStatus`](#m-audiodevicestatus) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`AudioEventsResult`](#m-audioeventsresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`AudioStatus`](#m-audiostatus) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`Boolean`](#m-boolean) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`BusLoudnessStatus`](#m-busloudnessstatus) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`BusLoudnessStatusArray`](#m-busloudnessstatusarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`BusStatus`](#m-busstatus) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`BusStatusArray`](#m-busstatusarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`CameraResult`](#m-cameraresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`CaptureResult`](#m-captureresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ClipboardResult`](#m-clipboardresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`CommandsResult`](#m-commandsresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`CommandsResultArray`](#m-commandsresultarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ContrastCheck`](#m-contrastcheck) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`Default`](#m-default) | The default [`JsonSerializerContext`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.jsonserializercontext) associated with a default [`JsonSerializerOptions`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions) instance. |
| [`Double`](#m-double) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`DoubleArray`](#m-doublearray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`Draw2DReport`](#m-draw2dreport) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`DriftInfo`](#m-driftinfo) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`DriftInfoArray`](#m-driftinfoarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`DuckingStatus`](#m-duckingstatus) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`FilterStatus`](#m-filterstatus) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`FollowedScope`](#m-followedscope) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`FollowedScopeArray`](#m-followedscopearray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`GeneratedSerializerOptions`](#m-generatedserializeroptions) | The source-generated options associated with this context. |
| [`GraphStats`](#m-graphstats) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`HitResult`](#m-hitresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`IReadOnlyListContrastCheck`](#m-ireadonlylistcontrastcheck) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`IReadOnlyListSteamActionValue`](#m-ireadonlyliststeamactionvalue) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`IReadOnlyListSteamControllerInfo`](#m-ireadonlyliststeamcontrollerinfo) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`IReadOnlyListSteamInputSetInfo`](#m-ireadonlyliststeaminputsetinfo) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`IReadOnlyListString`](#m-ireadonlyliststring) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`InfoResult`](#m-inforesult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`InputResult`](#m-inputresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`Int32`](#m-int32) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`Int64`](#m-int64) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`InvokeResult`](#m-invokeresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`LimiterStatus`](#m-limiterstatus) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`LoudnessStatus`](#m-loudnessstatus) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`MusicStatus`](#m-musicstatus) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`NetDivergeResult`](#m-netdivergeresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`NetStatus`](#m-netstatus) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`NetWaitResult`](#m-netwaitresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`NullableDouble`](#m-nullabledouble) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`NullableReverbOptions`](#m-nullablereverboptions) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`NullableSingle`](#m-nullablesingle) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`NullableUInt64`](#m-nullableuint64) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`OverlayResult`](#m-overlayresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ParticleEmitterInfo`](#m-particleemitterinfo) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ParticleEmitterInfoArray`](#m-particleemitterinfoarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ParticlesReport`](#m-particlesreport) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`PassReport`](#m-passreport) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`PassReportArray`](#m-passreportarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`PatchResult`](#m-patchresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`PickResult`](#m-pickresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`PlayedSound`](#m-playedsound) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`PlayedSoundArray`](#m-playedsoundarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ProjectResult`](#m-projectresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`PurchaseStatus`](#m-purchasestatus) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`QualityResult`](#m-qualityresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`RenderGraphReport`](#m-rendergraphreport) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ResourceReport`](#m-resourcereport) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ResourceReportArray`](#m-resourcereportarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ReverbOptions`](#m-reverboptions) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`SavesResult`](#m-savesresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`SendResult`](#m-sendresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`SequenceFrame`](#m-sequenceframe) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`SequenceFrameArray`](#m-sequenceframearray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`SequenceManifest`](#m-sequencemanifest) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`SequenceResult`](#m-sequenceresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`SettingsEntry`](#m-settingsentry) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`SettingsEntryArray`](#m-settingsentryarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ShaderProblem`](#m-shaderproblem) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ShaderProblemArray`](#m-shaderproblemarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ShaderStatus`](#m-shaderstatus) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ShaderStatusArray`](#m-shaderstatusarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ShadersResult`](#m-shadersresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`Single`](#m-single) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`SlotEntry`](#m-slotentry) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`SlotEntryArray`](#m-slotentryarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`SnapshotResult`](#m-snapshotresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`StateResult`](#m-stateresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`SteamActionValue`](#m-steamactionvalue) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`SteamControllerInfo`](#m-steamcontrollerinfo) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`SteamInputReport`](#m-steaminputreport) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`SteamInputSetInfo`](#m-steaminputsetinfo) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`SteamKeyboardInfo`](#m-steamkeyboardinfo) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`String`](#m-string) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`StringArray`](#m-stringarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`ThemeLintReport`](#m-themelintreport) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`TweenInfo`](#m-tweeninfo) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`TweenInfoArray`](#m-tweeninfoarray) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`TweensResult`](#m-tweensresult) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`UInt32`](#m-uint32) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`UInt64`](#m-uint64) | Defines the source generated JSON serialization contract metadata for a given type. |
| [`UiScaleResult`](#m-uiscaleresult) | Defines the source generated JSON serialization contract metadata for a given type. |

### <a id="m-accessreport"></a>AccessReport

```csharp
public JsonTypeInfo<AccessReport> AccessReport { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`AccessReport`](../../OneGame.Gpu/OneGame.Gpu.Graph/AccessReport.md)&gt;

### <a id="m-accessreportarray"></a>AccessReportArray

```csharp
public JsonTypeInfo<AccessReport[]> AccessReportArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`AccessReport`](../../OneGame.Gpu/OneGame.Gpu.Graph/AccessReport.md)\[\]&gt;

### <a id="m-asseteventinfo"></a>AssetEventInfo

```csharp
public JsonTypeInfo<AssetEventInfo> AssetEventInfo { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`AssetEventInfo`](AssetEventInfo.md)&gt;

### <a id="m-asseteventinfoarray"></a>AssetEventInfoArray

```csharp
public JsonTypeInfo<AssetEventInfo[]> AssetEventInfoArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`AssetEventInfo`](AssetEventInfo.md)\[\]&gt;

### <a id="m-assetinfo"></a>AssetInfo

```csharp
public JsonTypeInfo<AssetInfo> AssetInfo { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`AssetInfo`](AssetInfo.md)&gt;

### <a id="m-assetinfoarray"></a>AssetInfoArray

```csharp
public JsonTypeInfo<AssetInfo[]> AssetInfoArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`AssetInfo`](AssetInfo.md)\[\]&gt;

### <a id="m-assetsresult"></a>AssetsResult

```csharp
public JsonTypeInfo<AssetsResult> AssetsResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`AssetsResult`](AssetsResult.md)&gt;

### <a id="m-audiocaptureresult"></a>AudioCaptureResult

```csharp
public JsonTypeInfo<AudioCaptureResult> AudioCaptureResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`AudioCaptureResult`](AudioCaptureResult.md)&gt;

### <a id="m-audiodevicestatus"></a>AudioDeviceStatus

```csharp
public JsonTypeInfo<AudioDeviceStatus> AudioDeviceStatus { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`AudioDeviceStatus`](../../OneGame.Audio/OneGame.Audio/AudioDeviceStatus.md)&gt;

### <a id="m-audioeventsresult"></a>AudioEventsResult

```csharp
public JsonTypeInfo<AudioEventsResult> AudioEventsResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`AudioEventsResult`](AudioEventsResult.md)&gt;

### <a id="m-audiostatus"></a>AudioStatus

```csharp
public JsonTypeInfo<AudioStatus> AudioStatus { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`AudioStatus`](../../OneGame.Audio/OneGame.Audio/AudioStatus.md)&gt;

### <a id="m-boolean"></a>Boolean

```csharp
public JsonTypeInfo<bool> Boolean { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<bool>`

### <a id="m-busloudnessstatus"></a>BusLoudnessStatus

```csharp
public JsonTypeInfo<BusLoudnessStatus> BusLoudnessStatus { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`BusLoudnessStatus`](BusLoudnessStatus.md)&gt;

### <a id="m-busloudnessstatusarray"></a>BusLoudnessStatusArray

```csharp
public JsonTypeInfo<BusLoudnessStatus[]> BusLoudnessStatusArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`BusLoudnessStatus`](BusLoudnessStatus.md)\[\]&gt;

### <a id="m-busstatus"></a>BusStatus

```csharp
public JsonTypeInfo<BusStatus> BusStatus { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`BusStatus`](../../OneGame.Audio/OneGame.Audio/BusStatus.md)&gt;

### <a id="m-busstatusarray"></a>BusStatusArray

```csharp
public JsonTypeInfo<BusStatus[]> BusStatusArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`BusStatus`](../../OneGame.Audio/OneGame.Audio/BusStatus.md)\[\]&gt;

### <a id="m-cameraresult"></a>CameraResult

```csharp
public JsonTypeInfo<CameraResult> CameraResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`CameraResult`](CameraResult.md)&gt;

### <a id="m-captureresult"></a>CaptureResult

```csharp
public JsonTypeInfo<CaptureResult> CaptureResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`CaptureResult`](CaptureResult.md)&gt;

### <a id="m-clipboardresult"></a>ClipboardResult

```csharp
public JsonTypeInfo<ClipboardResult> ClipboardResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ClipboardResult`](ClipboardResult.md)&gt;

### <a id="m-commandsresult"></a>CommandsResult

```csharp
public JsonTypeInfo<CommandsResult> CommandsResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`CommandsResult`](CommandsResult.md)&gt;

### <a id="m-commandsresultarray"></a>CommandsResultArray

```csharp
public JsonTypeInfo<CommandsResult[]> CommandsResultArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`CommandsResult`](CommandsResult.md)\[\]&gt;

### <a id="m-contrastcheck"></a>ContrastCheck

```csharp
public JsonTypeInfo<ContrastCheck> ContrastCheck { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ContrastCheck`](../../OneGame.UI.Kit/OneGame.UI.Kit/ContrastCheck.md)&gt;

### <a id="m-default"></a>Default

```csharp
public static ControlJson Default { get; }
```

The default [`JsonSerializerContext`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.jsonserializercontext) associated with a default [`JsonSerializerOptions`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions) instance.

**Type:** [`ControlJson`](ControlJson.md)

### <a id="m-double"></a>Double

```csharp
public JsonTypeInfo<double> Double { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<double>`

### <a id="m-doublearray"></a>DoubleArray

```csharp
public JsonTypeInfo<double[]> DoubleArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<double[]>`

### <a id="m-draw2dreport"></a>Draw2DReport

```csharp
public JsonTypeInfo<Draw2DReport> Draw2DReport { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`Draw2DReport`](Draw2DReport.md)&gt;

### <a id="m-driftinfo"></a>DriftInfo

```csharp
public JsonTypeInfo<DriftInfo> DriftInfo { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`DriftInfo`](../../OneGame.Host/OneGame.Host/DriftInfo.md)&gt;

### <a id="m-driftinfoarray"></a>DriftInfoArray

```csharp
public JsonTypeInfo<DriftInfo[]> DriftInfoArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`DriftInfo`](../../OneGame.Host/OneGame.Host/DriftInfo.md)\[\]&gt;

### <a id="m-duckingstatus"></a>DuckingStatus

```csharp
public JsonTypeInfo<DuckingStatus> DuckingStatus { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`DuckingStatus`](../../OneGame.Audio/OneGame.Audio/DuckingStatus.md)&gt;

### <a id="m-filterstatus"></a>FilterStatus

```csharp
public JsonTypeInfo<FilterStatus> FilterStatus { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`FilterStatus`](../../OneGame.Audio/OneGame.Audio/FilterStatus.md)&gt;

### <a id="m-followedscope"></a>FollowedScope

```csharp
public JsonTypeInfo<FollowedScope> FollowedScope { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`FollowedScope`](../../OneGame.Host/OneGame.Host/FollowedScope.md)&gt;

### <a id="m-followedscopearray"></a>FollowedScopeArray

```csharp
public JsonTypeInfo<FollowedScope[]> FollowedScopeArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`FollowedScope`](../../OneGame.Host/OneGame.Host/FollowedScope.md)\[\]&gt;

### <a id="m-generatedserializeroptions"></a>GeneratedSerializerOptions

```csharp
protected override JsonSerializerOptions? GeneratedSerializerOptions { get; }
```

The source-generated options associated with this context.

**Type:** [`JsonSerializerOptions`](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions)?

### <a id="m-graphstats"></a>GraphStats

```csharp
public JsonTypeInfo<GraphStats> GraphStats { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`GraphStats`](../../OneGame.Gpu/OneGame.Gpu.Graph/GraphStats.md)&gt;

### <a id="m-hitresult"></a>HitResult

```csharp
public JsonTypeInfo<HitResult> HitResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`HitResult`](HitResult.md)&gt;

### <a id="m-ireadonlylistcontrastcheck"></a>IReadOnlyListContrastCheck

```csharp
public JsonTypeInfo<IReadOnlyList<ContrastCheck>> IReadOnlyListContrastCheck { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ContrastCheck`](../../OneGame.UI.Kit/OneGame.UI.Kit/ContrastCheck.md)&gt;&gt;

### <a id="m-ireadonlyliststeamactionvalue"></a>IReadOnlyListSteamActionValue

```csharp
public JsonTypeInfo<IReadOnlyList<SteamActionValue>> IReadOnlyListSteamActionValue { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`SteamActionValue`](../../OneGame.Steam/OneGame.Steam.Input/SteamActionValue.md)&gt;&gt;

### <a id="m-ireadonlyliststeamcontrollerinfo"></a>IReadOnlyListSteamControllerInfo

```csharp
public JsonTypeInfo<IReadOnlyList<SteamControllerInfo>> IReadOnlyListSteamControllerInfo { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`SteamControllerInfo`](../../OneGame.Steam/OneGame.Steam.Input/SteamControllerInfo.md)&gt;&gt;

### <a id="m-ireadonlyliststeaminputsetinfo"></a>IReadOnlyListSteamInputSetInfo

```csharp
public JsonTypeInfo<IReadOnlyList<SteamInputSetInfo>> IReadOnlyListSteamInputSetInfo { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`SteamInputSetInfo`](../../OneGame.Steam/OneGame.Steam.Input/SteamInputSetInfo.md)&gt;&gt;

### <a id="m-ireadonlyliststring"></a>IReadOnlyListString

```csharp
public JsonTypeInfo<IReadOnlyList<string>> IReadOnlyListString { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>>`

### <a id="m-inforesult"></a>InfoResult

```csharp
public JsonTypeInfo<InfoResult> InfoResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`InfoResult`](InfoResult.md)&gt;

### <a id="m-inputresult"></a>InputResult

```csharp
public JsonTypeInfo<InputResult> InputResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`InputResult`](InputResult.md)&gt;

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

### <a id="m-invokeresult"></a>InvokeResult

```csharp
public JsonTypeInfo<InvokeResult> InvokeResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`InvokeResult`](InvokeResult.md)&gt;

### <a id="m-limiterstatus"></a>LimiterStatus

```csharp
public JsonTypeInfo<LimiterStatus> LimiterStatus { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`LimiterStatus`](../../OneGame.Audio/OneGame.Audio/LimiterStatus.md)&gt;

### <a id="m-loudnessstatus"></a>LoudnessStatus

```csharp
public JsonTypeInfo<LoudnessStatus> LoudnessStatus { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`LoudnessStatus`](../../OneGame.Audio/OneGame.Audio/LoudnessStatus.md)&gt;

### <a id="m-musicstatus"></a>MusicStatus

```csharp
public JsonTypeInfo<MusicStatus> MusicStatus { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`MusicStatus`](../../OneGame.Audio/OneGame.Audio/MusicStatus.md)&gt;

### <a id="m-netdivergeresult"></a>NetDivergeResult

```csharp
public JsonTypeInfo<NetDivergeResult> NetDivergeResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`NetDivergeResult`](NetDivergeResult.md)&gt;

### <a id="m-netstatus"></a>NetStatus

```csharp
public JsonTypeInfo<NetStatus> NetStatus { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`NetStatus`](../../OneGame.Host/OneGame.Host/NetStatus.md)&gt;

### <a id="m-netwaitresult"></a>NetWaitResult

```csharp
public JsonTypeInfo<NetWaitResult> NetWaitResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`NetWaitResult`](NetWaitResult.md)&gt;

### <a id="m-nullabledouble"></a>NullableDouble

```csharp
public JsonTypeInfo<double?> NullableDouble { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<double?>`

### <a id="m-nullablereverboptions"></a>NullableReverbOptions

```csharp
public JsonTypeInfo<ReverbOptions?> NullableReverbOptions { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ReverbOptions`](../../OneGame.Audio/OneGame.Audio/ReverbOptions.md)?&gt;

### <a id="m-nullablesingle"></a>NullableSingle

```csharp
public JsonTypeInfo<float?> NullableSingle { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<float?>`

### <a id="m-nullableuint64"></a>NullableUInt64

```csharp
public JsonTypeInfo<ulong?> NullableUInt64 { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<ulong?>`

### <a id="m-overlayresult"></a>OverlayResult

```csharp
public JsonTypeInfo<OverlayResult> OverlayResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`OverlayResult`](OverlayResult.md)&gt;

### <a id="m-particleemitterinfo"></a>ParticleEmitterInfo

```csharp
public JsonTypeInfo<ParticleEmitterInfo> ParticleEmitterInfo { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ParticleEmitterInfo`](ParticleEmitterInfo.md)&gt;

### <a id="m-particleemitterinfoarray"></a>ParticleEmitterInfoArray

```csharp
public JsonTypeInfo<ParticleEmitterInfo[]> ParticleEmitterInfoArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ParticleEmitterInfo`](ParticleEmitterInfo.md)\[\]&gt;

### <a id="m-particlesreport"></a>ParticlesReport

```csharp
public JsonTypeInfo<ParticlesReport> ParticlesReport { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ParticlesReport`](ParticlesReport.md)&gt;

### <a id="m-passreport"></a>PassReport

```csharp
public JsonTypeInfo<PassReport> PassReport { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`PassReport`](../../OneGame.Gpu/OneGame.Gpu.Graph/PassReport.md)&gt;

### <a id="m-passreportarray"></a>PassReportArray

```csharp
public JsonTypeInfo<PassReport[]> PassReportArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`PassReport`](../../OneGame.Gpu/OneGame.Gpu.Graph/PassReport.md)\[\]&gt;

### <a id="m-patchresult"></a>PatchResult

```csharp
public JsonTypeInfo<PatchResult> PatchResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`PatchResult`](PatchResult.md)&gt;

### <a id="m-pickresult"></a>PickResult

```csharp
public JsonTypeInfo<PickResult> PickResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`PickResult`](PickResult.md)&gt;

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

### <a id="m-projectresult"></a>ProjectResult

```csharp
public JsonTypeInfo<ProjectResult> ProjectResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ProjectResult`](ProjectResult.md)&gt;

### <a id="m-purchasestatus"></a>PurchaseStatus

```csharp
public JsonTypeInfo<PurchaseStatus> PurchaseStatus { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`PurchaseStatus`](../../OneGame.Purchases/OneGame.Purchases/PurchaseStatus.md)&gt;

### <a id="m-qualityresult"></a>QualityResult

```csharp
public JsonTypeInfo<QualityResult> QualityResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`QualityResult`](QualityResult.md)&gt;

### <a id="m-rendergraphreport"></a>RenderGraphReport

```csharp
public JsonTypeInfo<RenderGraphReport> RenderGraphReport { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`RenderGraphReport`](../../OneGame.Gpu/OneGame.Gpu.Graph/RenderGraphReport.md)&gt;

### <a id="m-resourcereport"></a>ResourceReport

```csharp
public JsonTypeInfo<ResourceReport> ResourceReport { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ResourceReport`](../../OneGame.Gpu/OneGame.Gpu.Graph/ResourceReport.md)&gt;

### <a id="m-resourcereportarray"></a>ResourceReportArray

```csharp
public JsonTypeInfo<ResourceReport[]> ResourceReportArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ResourceReport`](../../OneGame.Gpu/OneGame.Gpu.Graph/ResourceReport.md)\[\]&gt;

### <a id="m-reverboptions"></a>ReverbOptions

```csharp
public JsonTypeInfo<ReverbOptions> ReverbOptions { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ReverbOptions`](../../OneGame.Audio/OneGame.Audio/ReverbOptions.md)&gt;

### <a id="m-savesresult"></a>SavesResult

```csharp
public JsonTypeInfo<SavesResult> SavesResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`SavesResult`](SavesResult.md)&gt;

### <a id="m-sendresult"></a>SendResult

```csharp
public JsonTypeInfo<SendResult> SendResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`SendResult`](SendResult.md)&gt;

### <a id="m-sequenceframe"></a>SequenceFrame

```csharp
public JsonTypeInfo<SequenceFrame> SequenceFrame { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`SequenceFrame`](SequenceFrame.md)&gt;

### <a id="m-sequenceframearray"></a>SequenceFrameArray

```csharp
public JsonTypeInfo<SequenceFrame[]> SequenceFrameArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`SequenceFrame`](SequenceFrame.md)\[\]&gt;

### <a id="m-sequencemanifest"></a>SequenceManifest

```csharp
public JsonTypeInfo<SequenceManifest> SequenceManifest { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`SequenceManifest`](SequenceManifest.md)&gt;

### <a id="m-sequenceresult"></a>SequenceResult

```csharp
public JsonTypeInfo<SequenceResult> SequenceResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`SequenceResult`](SequenceResult.md)&gt;

### <a id="m-settingsentry"></a>SettingsEntry

```csharp
public JsonTypeInfo<SettingsEntry> SettingsEntry { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`SettingsEntry`](SettingsEntry.md)&gt;

### <a id="m-settingsentryarray"></a>SettingsEntryArray

```csharp
public JsonTypeInfo<SettingsEntry[]> SettingsEntryArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`SettingsEntry`](SettingsEntry.md)\[\]&gt;

### <a id="m-shaderproblem"></a>ShaderProblem

```csharp
public JsonTypeInfo<ShaderProblem> ShaderProblem { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ShaderProblem`](ShaderProblem.md)&gt;

### <a id="m-shaderproblemarray"></a>ShaderProblemArray

```csharp
public JsonTypeInfo<ShaderProblem[]> ShaderProblemArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ShaderProblem`](ShaderProblem.md)\[\]&gt;

### <a id="m-shaderstatus"></a>ShaderStatus

```csharp
public JsonTypeInfo<ShaderStatus> ShaderStatus { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ShaderStatus`](ShaderStatus.md)&gt;

### <a id="m-shaderstatusarray"></a>ShaderStatusArray

```csharp
public JsonTypeInfo<ShaderStatus[]> ShaderStatusArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ShaderStatus`](ShaderStatus.md)\[\]&gt;

### <a id="m-shadersresult"></a>ShadersResult

```csharp
public JsonTypeInfo<ShadersResult> ShadersResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ShadersResult`](ShadersResult.md)&gt;

### <a id="m-single"></a>Single

```csharp
public JsonTypeInfo<float> Single { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<float>`

### <a id="m-slotentry"></a>SlotEntry

```csharp
public JsonTypeInfo<SlotEntry> SlotEntry { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`SlotEntry`](SlotEntry.md)&gt;

### <a id="m-slotentryarray"></a>SlotEntryArray

```csharp
public JsonTypeInfo<SlotEntry[]> SlotEntryArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`SlotEntry`](SlotEntry.md)\[\]&gt;

### <a id="m-snapshotresult"></a>SnapshotResult

```csharp
public JsonTypeInfo<SnapshotResult> SnapshotResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`SnapshotResult`](SnapshotResult.md)&gt;

### <a id="m-stateresult"></a>StateResult

```csharp
public JsonTypeInfo<StateResult> StateResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`StateResult`](StateResult.md)&gt;

### <a id="m-steamactionvalue"></a>SteamActionValue

```csharp
public JsonTypeInfo<SteamActionValue> SteamActionValue { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`SteamActionValue`](../../OneGame.Steam/OneGame.Steam.Input/SteamActionValue.md)&gt;

### <a id="m-steamcontrollerinfo"></a>SteamControllerInfo

```csharp
public JsonTypeInfo<SteamControllerInfo> SteamControllerInfo { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`SteamControllerInfo`](../../OneGame.Steam/OneGame.Steam.Input/SteamControllerInfo.md)&gt;

### <a id="m-steaminputreport"></a>SteamInputReport

```csharp
public JsonTypeInfo<SteamInputReport> SteamInputReport { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`SteamInputReport`](../../OneGame.Steam/OneGame.Steam.Input/SteamInputReport.md)&gt;

### <a id="m-steaminputsetinfo"></a>SteamInputSetInfo

```csharp
public JsonTypeInfo<SteamInputSetInfo> SteamInputSetInfo { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`SteamInputSetInfo`](../../OneGame.Steam/OneGame.Steam.Input/SteamInputSetInfo.md)&gt;

### <a id="m-steamkeyboardinfo"></a>SteamKeyboardInfo

```csharp
public JsonTypeInfo<SteamKeyboardInfo> SteamKeyboardInfo { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`SteamKeyboardInfo`](../../OneGame.Steam/OneGame.Steam.Input/SteamKeyboardInfo.md)&gt;

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

### <a id="m-themelintreport"></a>ThemeLintReport

```csharp
public JsonTypeInfo<ThemeLintReport> ThemeLintReport { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`ThemeLintReport`](../../OneGame.UI.Kit/OneGame.UI.Kit/ThemeLintReport.md)&gt;

### <a id="m-tweeninfo"></a>TweenInfo

```csharp
public JsonTypeInfo<TweenInfo> TweenInfo { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`TweenInfo`](../../OneGame.UI/OneGame.Tweens/TweenInfo.md)&gt;

### <a id="m-tweeninfoarray"></a>TweenInfoArray

```csharp
public JsonTypeInfo<TweenInfo[]> TweenInfoArray { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`TweenInfo`](../../OneGame.UI/OneGame.Tweens/TweenInfo.md)\[\]&gt;

### <a id="m-tweensresult"></a>TweensResult

```csharp
public JsonTypeInfo<TweensResult> TweensResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`TweensResult`](TweensResult.md)&gt;

### <a id="m-uint32"></a>UInt32

```csharp
public JsonTypeInfo<uint> UInt32 { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<uint>`

### <a id="m-uint64"></a>UInt64

```csharp
public JsonTypeInfo<ulong> UInt64 { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)`<ulong>`

### <a id="m-uiscaleresult"></a>UiScaleResult

```csharp
public JsonTypeInfo<UiScaleResult> UiScaleResult { get; }
```

Defines the source generated JSON serialization contract metadata for a given type.

**Type:** [`JsonTypeInfo`](https://learn.microsoft.com/dotnet/api/system.text.json.serialization.metadata.jsontypeinfo-1)&lt;[`UiScaleResult`](UiScaleResult.md)&gt;

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
