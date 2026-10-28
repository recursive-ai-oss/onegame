[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# DriftException class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Inherits:** [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

```csharp
public sealed class DriftException : InvalidOperationException
```

Thrown by [`Cluster.Step()`](Cluster.md#m-step-int) when [`ClusterOptions.FatalDrift`](ClusterOptions.md#m-fataldrift) is on and a replica drifted.

## Properties

| Member | Description |
| --- | --- |
| [`Report`](#m-report) | The drift report. |

### <a id="m-report"></a>Report

```csharp
public DriftReport Report { get; }
```

The drift report.

**Type:** [`DriftReport`](DriftReport.md)
