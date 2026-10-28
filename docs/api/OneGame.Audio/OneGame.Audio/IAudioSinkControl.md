[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# IAudioSinkControl interface

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)

```csharp
public interface IAudioSinkControl
```

Implemented by sinks whose device can be stopped and started again, as a phone does when the app leaves the foreground.

## Properties

| Member | Description |
| --- | --- |
| [`SystemSuspendChanged`](#m-systemsuspendchanged) | Set by the output: called with `true` when the system stopped the device on its own (a call, an alarm) and `false` when it handed it back. |

### <a id="m-systemsuspendchanged"></a>SystemSuspendChanged

```csharp
Action<bool>? SystemSuspendChanged { get; set; }
```

Set by the output: called with `true` when the system stopped the device on its own (a call, an alarm) and `false` when it handed it back.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)`<bool>?`

## Methods

| Member | Description |
| --- | --- |
| [`SetActive(bool)`](#m-setactive-bool) | Starts (`true`) or stops (`false`) the device. |

### <a id="m-setactive-bool"></a>SetActive(bool)

```csharp
void SetActive(bool active);
```

Starts (`true`) or stops (`false`) the device. Stopping must make [`IAudioSink.Write()`](IAudioSink.md#m-write-readonlyspan-float) return promptly. Never throws.

**Parameters**

- `active` (`bool`)
