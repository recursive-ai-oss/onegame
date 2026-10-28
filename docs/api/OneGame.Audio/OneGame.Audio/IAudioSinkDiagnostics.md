[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# IAudioSinkDiagnostics interface

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)

```csharp
public interface IAudioSinkDiagnostics
```

Implemented by sinks that can describe their device. The output reports it through the harness.

## Methods

| Member | Description |
| --- | --- |
| [`Report()`](#m-report) | Reads the current report. |

### <a id="m-report"></a>Report()

```csharp
AudioSinkReport Report();
```

Reads the current report. Safe from any thread.

**Returns:** [`AudioSinkReport`](AudioSinkReport.md)
